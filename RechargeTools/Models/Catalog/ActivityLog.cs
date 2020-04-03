using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Catalog
{
    public class ActivityLog
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Business")]
        public Guid Business_Id { get; set; }

        [ForeignKey("User")]
        public string User_Id { get; set; }

        [ForeignKey("ActivityLogType")]
        public int ActivityLogType_Id { get; set; }

        public string Comment { get; set; }
        public DateTime Date { get; set; }

        public virtual Business Business { get; set; }

        public virtual User User { get; set; }
        public virtual ActivityLogType ActivityLogType { get; set; }
    }
}