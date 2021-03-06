﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace RechargeTools.Models.Catalog
{
    public class ScheduleTaskHistory : GenericEntity
    {
        /// <summary>
        /// Gets or sets the schedule task identifier.
        /// </summary>
        [ForeignKey("ScheduleTask")]
        public Guid ScheduleTask_Id { get; set; }

        /// <summary>
        /// Gets or sets whether the task is running.
        /// </summary>
        [Index("IX_MachineName_IsRunning", 1)]
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the server machine name.
        /// </summary>
        [Index("IX_MachineName_IsRunning", 0)]
        [StringLength(50)]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the date when the task was started. It is also the date when this entry was created.
        /// </summary>
        [Index("IX_Started_Finished", 0)]
        public DateTime StartedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date when the task has been finished.
        /// </summary>
        [Index("IX_Started_Finished", 1)]
        public DateTime? FinishedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date when the task succeeded.
        /// </summary>
        public DateTime? SucceededOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the current percentual progress for a running task.
        /// </summary>
        public int? ProgressPercent { get; set; }

        /// <summary>
        /// Gets or sets the current progress message for a running task.
        /// </summary>
        public string ProgressMessage { get; set; }

        /// <summary>
        /// Gets or sets the schedule task.
        /// </summary>
        public virtual ScheduleTask ScheduleTask { get; set; }

        public ScheduleTaskHistory Clone()
        {
            var clone = (ScheduleTaskHistory)MemberwiseClone();
            clone.ScheduleTask = ScheduleTask.Clone();
            return clone;
        }
    }
}