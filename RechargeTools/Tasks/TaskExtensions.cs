using System;
using RechargeTools.Infrastructure;
using RechargeTools.Models.Catalog;

namespace RechargeTools.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Gets whether the schedule task is visible or not.
        /// </summary>
        /// <param name="task">Scheduled task.</param>
        /// <returns><c>true</c> task is visible, <c>false</c> task is not visible.</returns>
        public static bool IsVisible(this ScheduleTask task)
        {
            Guard.NotNull(task, nameof(task));
            if (task.IsHidden)
            {
                return false;
            }

            return true;
        }
    }
}