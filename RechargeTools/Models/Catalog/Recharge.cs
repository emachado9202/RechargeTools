using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Catalog
{
    public class Recharge
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Required]
        public DateTime DateStart { get; set; }

        [Required]
        public DateTime DateEnd { get; set; }

        [Required]
        public bool Activated { get; set; }

        [ForeignKey("Business")]
        [Required]
        public Guid Business_Id { get; set; }

        public virtual Business Business { get; set; }
        public virtual List<RechargeAgent> RechargeAgents { get; set; }
    }
}