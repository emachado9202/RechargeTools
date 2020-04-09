using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Views
{
    public class DashboardViewModel
    {
        public List<Tuple<string, long, long, string, string>> Agents { get; set; }
        public List<Tuple<string, string>> PendentNumbers { get; set; }
    }
}