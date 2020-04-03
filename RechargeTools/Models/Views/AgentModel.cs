using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Views
{
    public class AgentModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastUpdated { get; set; }
        public string ShowDashboard { get; set; }
        public string ActionIn { get; set; }
        public string ActionOut { get; set; }
        public string SystemAction { get; set; }
    }

    public class CategoryListModel
    {
        public int GridPageSize { get; set; }
    }
}