using System.Collections.Generic;
using RechargeTools.Models.Catalog;

namespace RechargeTools.Tasks
{
    public interface ITaskExecutor
    {
        void Execute(
            ScheduleTask task,
            IDictionary<string, string> taskParameters = null,
            bool throwOnError = false);
    }
}