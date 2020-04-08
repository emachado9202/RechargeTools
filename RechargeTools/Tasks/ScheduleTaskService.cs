using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using RechargeTools.Infrastructure;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;
using SmartStore.Services.Tasks;

namespace RechargeTools.Tasks
{
    public partial class ScheduleTaskService : IScheduleTaskService
    {
        public ScheduleTaskService()
        {
            Logger = LogManager.GetLogger(typeof(ScheduleTaskService));
            applicationDbContext = new ApplicationDbContext();
        }

        public ILog Logger { get; set; }
        public ApplicationDbContext applicationDbContext { get; set; }

        public virtual void DeleteTask(ScheduleTask task)
        {
            Guard.NotNull(task, nameof(task));

            applicationDbContext.ScheduleTasks.Remove(task);
            applicationDbContext.SaveChanges();
        }

        public virtual ScheduleTask GetTaskById(Guid taskId)
        {
            if (taskId == Guid.Empty)
                return null;

            return Retry.Run(
                () => applicationDbContext.ScheduleTasks.FirstOrDefault(x => x.Id == taskId),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnDeadlockException);
        }

        public virtual ScheduleTask GetTaskByType(string type)
        {
            try
            {
                if (type.HasValue())
                {
                    var query = applicationDbContext.ScheduleTasks
                        .Where(t => t.Type == type)
                        .OrderByDescending(t => t.Id);

                    var task = query.FirstOrDefault();
                    return task;
                }
            }
            catch (Exception exc)
            {
                // Do not throw an exception if the underlying provider failed on Open.
                exc.Dump();
            }

            return null;
        }

        public virtual IList<ScheduleTask> GetAllTasks(bool includeDisabled = false)
        {
            var query = applicationDbContext.ScheduleTasks.Where(x => x.Id != Guid.Empty);
            if (!includeDisabled)
            {
                query = query.Where(t => t.Enabled);
            }
            query = query.OrderByDescending(t => t.Enabled);

            return Retry.Run(
                () => query.ToList(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnDeadlockException);
        }

        public virtual IList<ScheduleTask> GetPendingTasks()
        {
            var now = DateTime.UtcNow;
            var machineName = Environment.MachineName;

            var query =
                from t in applicationDbContext.ScheduleTasks
                where t.NextRunUtc.HasValue && t.NextRunUtc <= now && t.Enabled
                select new
                {
                    Task = t,
                    LastEntry = t.ScheduleTaskHistory
                        .Where(th => !t.RunPerMachine || t.RunPerMachine && th.MachineName == machineName)
                        .OrderByDescending(th => th.StartedOnUtc)
                        .ThenByDescending(th => th.Id)
                        .FirstOrDefault()
                };

            var tasks = Retry.Run(
                () => query.ToList(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnDeadlockException);

            var pendingTasks = tasks
                .Select(x =>
                {
                    x.Task.LastHistoryEntry = x.LastEntry;
                    return x.Task;
                })
                .Where(x => x.IsPending)
                .ToList();

            return pendingTasks;
        }

        public virtual void InsertTask(ScheduleTask task)
        {
            Guard.NotNull(task, nameof(task));

            applicationDbContext.ScheduleTasks.Add(task);
            applicationDbContext.SaveChanges();
        }

        public virtual void UpdateTask(ScheduleTask task)
        {
            Guard.NotNull(task, nameof(task));

            try
            {
                applicationDbContext.Entry(task).State = EntityState.Modified;
                applicationDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public ScheduleTask GetOrAddTask<T>(Action<ScheduleTask> newAction) where T : ITask
        {
            Guard.NotNull(newAction, nameof(newAction));

            var type = typeof(T);

            if (type.IsAbstract || type.IsInterface || type.IsNotPublic)
            {
                throw new InvalidOperationException("Only concrete public task types can be registered.");
            }

            var scheduleTask = this.GetTaskByType<T>();

            if (scheduleTask == null)
            {
                scheduleTask = new ScheduleTask { Type = type.AssemblyQualifiedNameWithoutVersion() };
                newAction(scheduleTask);
                InsertTask(scheduleTask);
            }

            return scheduleTask;
        }

        public void CalculateFutureSchedules(IEnumerable<ScheduleTask> tasks, bool isAppStart = false)
        {
            Guard.NotNull(tasks, nameof(tasks));

            foreach (var task in tasks)
            {
                task.NextRunUtc = GetNextSchedule(task);
                if (isAppStart)
                {
                    FixTypeName(task);
                }
                else
                {
                    UpdateTask(task);
                }
            }

            if (isAppStart)
            {
                // On app start this method's execution is thread-safe, making it sufficient
                // to commit all changes in one go.
                applicationDbContext.SaveChanges();
            }

            if (isAppStart)
            {
                // Normalize task history entries.
                // That is, no task can run when the application starts and therefore no entry may be marked as running.
                var entries = applicationDbContext.ScheduleTaskHistories
                    .Where(x =>
                        x.IsRunning ||
                        x.ProgressPercent != null ||
                        !string.IsNullOrEmpty(x.ProgressMessage) ||
                        x.FinishedOnUtc != null && x.FinishedOnUtc < x.StartedOnUtc
                    )
                    .ToList();

                if (entries.Any())
                {
                    string abnormalAbort = "Anormalmente interrumpida debido a un cierre de la aplicación";
                    foreach (var entry in entries)
                    {
                        var invalidTimeRange = entry.FinishedOnUtc.HasValue && entry.FinishedOnUtc < entry.StartedOnUtc;
                        if (invalidTimeRange || entry.IsRunning)
                        {
                            entry.Error = abnormalAbort;
                        }

                        entry.IsRunning = false;
                        entry.ProgressPercent = null;
                        entry.ProgressMessage = null;
                        if (invalidTimeRange)
                        {
                            entry.FinishedOnUtc = entry.StartedOnUtc;
                        }
                    }
                    applicationDbContext.Entry(entries).State = EntityState.Modified;
                    applicationDbContext.SaveChanges();
                }
            }
        }

        private void FixTypeName(ScheduleTask task)
        {
            // In versions prior V3 a double space could exist in ScheduleTask type name.
            if (task.Type.IndexOf(",  ") > 0)
            {
                task.Type = task.Type.Replace(",  ", ", ");
            }
        }

        public virtual DateTime? GetNextSchedule(ScheduleTask task)
        {
            if (task.Enabled)
            {
                try
                {
                    var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("US Mountain Standard Time");
                    var baseTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, localTimeZone);
                    var next = CronExpression.GetNextSchedule(task.CronExpression, baseTime);
                    var utcTime = TimeZoneInfo.ConvertTimeToUtc(next, localTimeZone);

                    return utcTime;
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Could not calculate next schedule time for task '{0}'", task.Name, ex);
                }
            }

            return null;
        }

        private static void RetryOnDeadlockException(int attemp, Exception ex)
        {
            var isDeadLockException =
                (ex as EntityCommandExecutionException).IsDeadlockException() ||
                (ex as SqlException).IsDeadlockException();

            if (!isDeadLockException)
            {
                // We only want to retry on deadlock stuff.
                throw ex;
            }
        }

        #region Schedule task history

        protected virtual IQueryable<ScheduleTaskHistory> GetHistoryEntriesQuery(
            Guid taskId,
            bool forCurrentMachine = false,
            bool lastEntryOnly = false,
            bool? isRunning = null)
        {
            var query = applicationDbContext.ScheduleTaskHistories.Where(x => x.Id != Guid.Empty);

            if (taskId != Guid.Empty)
            {
                query = query.Where(x => x.ScheduleTask_Id == taskId);
            }
            if (forCurrentMachine)
            {
                var machineName = Environment.MachineName;
                query = query.Where(x => x.MachineName == machineName);
            }
            if (isRunning.HasValue)
            {
                query = query.Where(x => x.IsRunning == isRunning.Value);
            }

            if (lastEntryOnly)
            {
                query =
                    from th in query
                    group th by th.ScheduleTask_Id into grp
                    select grp
                        .OrderByDescending(x => x.StartedOnUtc)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();
            }

            query = query
                .OrderByDescending(x => x.StartedOnUtc)
                .ThenByDescending(x => x.Id);

            return query;
        }

        protected virtual IQueryable<ScheduleTaskHistory> GetHistoryEntriesQuery(
            ScheduleTask task,
            bool forCurrentMachine = false,
            bool? isRunning = null)
        {
            applicationDbContext.LoadCollection(
                task,
                (ScheduleTask x) => x.ScheduleTaskHistory,
                false,
                (IQueryable<ScheduleTaskHistory> query) =>
                {
                    if (forCurrentMachine)
                    {
                        var machineName = Environment.MachineName;
                        query = query.Where(x => x.MachineName == machineName);
                    }
                    if (isRunning.HasValue)
                    {
                        query = query.Where(x => x.IsRunning == isRunning.Value);
                    }

                    query = query
                        .OrderByDescending(x => x.StartedOnUtc)
                        .ThenByDescending(x => x.Id);

                    return query;
                });

            return task.ScheduleTaskHistory.AsQueryable();
        }

        public virtual List<ScheduleTaskHistory> GetHistoryEntries(
            int pageIndex,
            int pageSize,
            Guid taskId,
            bool forCurrentMachine = false,
            bool lastEntryOnly = false,
            bool? isRunning = null)
        {
            var query = GetHistoryEntriesQuery(taskId, forCurrentMachine, lastEntryOnly, isRunning);
            var entries = query.Skip(pageIndex)
                        .Take(pageSize).ToList();
            return entries;
        }

        public virtual List<ScheduleTaskHistory> GetHistoryEntries(
            int pageIndex,
            int pageSize,
            ScheduleTask task,
            bool forCurrentMachine = false,
            bool? isRunning = null)
        {
            if (task == null)
            {
                return new List<ScheduleTaskHistory>();
            }

            var query = GetHistoryEntriesQuery(task, forCurrentMachine, isRunning);
            var entries = query.Skip(pageIndex)
                        .Take(pageSize).ToList();
            return entries;
        }

        public virtual ScheduleTaskHistory GetLastHistoryEntryByTaskId(Guid taskId, bool? isRunning = null)
        {
            if (taskId == Guid.Empty)
            {
                return null;
            }

            var query = GetHistoryEntriesQuery(taskId, true, false, isRunning);
            query = query.Include(x => x.ScheduleTask);

            var entry = Retry.Run(
                () => query.FirstOrDefault(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnDeadlockException);

            return entry;
        }

        public virtual ScheduleTaskHistory GetLastHistoryEntryByTask(ScheduleTask task, bool? isRunning = null)
        {
            if (task == null)
            {
                return null;
            }

            var query = GetHistoryEntriesQuery(task, true, isRunning);

            var entry = Retry.Run(
                () => query.FirstOrDefault(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnDeadlockException);

            return entry;
        }

        public virtual ScheduleTaskHistory GetHistoryEntryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            return applicationDbContext.ScheduleTaskHistories.FirstOrDefault(x => x.Id == id);
        }

        public virtual void InsertHistoryEntry(ScheduleTaskHistory historyEntry)
        {
            Guard.NotNull(historyEntry, nameof(historyEntry));

            applicationDbContext.ScheduleTaskHistories.Add(historyEntry);
            applicationDbContext.SaveChanges();
        }

        public virtual void UpdateHistoryEntry(ScheduleTaskHistory historyEntry)
        {
            Guard.NotNull(historyEntry, nameof(historyEntry));

            try
            {
                applicationDbContext.Entry(historyEntry).State = System.Data.Entity.EntityState.Modified;
                applicationDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                // Do not throw.
            }
        }

        public virtual void DeleteHistoryEntry(ScheduleTaskHistory historyEntry)
        {
            Guard.NotNull(historyEntry, nameof(historyEntry));
            Guard.IsTrue(!historyEntry.IsRunning, nameof(historyEntry.IsRunning), "Cannot delete a running schedule task history entry.");

            applicationDbContext.ScheduleTaskHistories.Remove(historyEntry);
            applicationDbContext.SaveChanges();
        }

        public virtual int DeleteHistoryEntries()
        {
            var count = 0;
            var idsToDelete = new HashSet<Guid>();

            var earliestDate = DateTime.UtcNow.AddDays(-1 * 30);
            var ids = applicationDbContext.ScheduleTaskHistories
                .Where(x => x.StartedOnUtc <= earliestDate && !x.IsRunning)
                .Select(x => x.Id)
                .ToList();

            idsToDelete.AddRange(ids);

            // We have to group by task otherwise we would only keep entries from very frequently executed tasks.

            var query =
                from th in applicationDbContext.ScheduleTaskHistories
                where !th.IsRunning
                group th by th.ScheduleTask_Id into grp
                select grp
                    .OrderByDescending(x => x.StartedOnUtc)
                    .ThenByDescending(x => x.Id)
                    .Skip(100)
                    .Select(x => x.Id);

            ids = query.SelectMany(x => x).ToList();

            idsToDelete.AddRange(ids);

            try
            {
                if (idsToDelete.Any())
                {
                    var entries = applicationDbContext.ScheduleTaskHistories
                                .Where(x => idsToDelete.Contains(x.Id))
                                .ToList();

                    entries.Each(x => DeleteHistoryEntry(x));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return count;
        }

        #endregion Schedule task history
    }
}