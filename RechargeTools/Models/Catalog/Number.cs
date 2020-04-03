using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Catalog
{
    public class Number
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
        public string Confirmation { get; set; }

        [ForeignKey("Agent")]
        public Guid Agent_Id { get; set; }

        [ForeignKey("User")]
        public string User_Id { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public virtual User User { get; set; }
        public virtual Agent Agent { get; set; }
    }
}