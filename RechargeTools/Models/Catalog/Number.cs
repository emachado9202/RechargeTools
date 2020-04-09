using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechargeTools.Models.Catalog
{
    public class Number : GenericEntity
    {
        public string Value { get; set; }

        [Index("IX_Number_Consecutive", 1)]
        public int Consecutive { get; set; }

        [Index("IX_Number_Confirmation", 0)]
        public bool Confirmation { get; set; }

        [StringLength(100)]
        public string Description { get; set; }

        [ForeignKey("RechargeAgent")]
        public Guid RechargeAgent_Id { get; set; }

        [ForeignKey("User")]
        public string User_Id { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public virtual User User { get; set; }
        public virtual RechargeAgent RechargeAgent { get; set; }
    }
}