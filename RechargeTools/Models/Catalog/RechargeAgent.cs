using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Catalog
{
    public class RechargeAgent : GenericEntity
    {
        [ForeignKey("Agent")]
        [Required]
        public Guid Agent_Id { get; set; }

        [ForeignKey("Recharge")]
        [Required]
        public Guid Recharge_Id { get; set; }

        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        public virtual Agent Agent { get; set; }
        public virtual Recharge Recharge { get; set; }
    }
}