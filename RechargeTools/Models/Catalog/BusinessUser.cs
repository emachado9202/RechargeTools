using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity.EntityFramework;

namespace RechargeTools.Models.Catalog
{
    public class BusinessUser
    {
        [Key]
        [ForeignKey("Business")]
        [Column(Order = 0)]
        public Guid Business_Id { get; set; }

        [Key]
        [ForeignKey("User")]
        [Column(Order = 1)]
        public string User_Id { get; set; }

        [ForeignKey("Role")]
        public string Role_Id { get; set; }

        [DataType(DataType.Currency)]
        public decimal Cash { get; set; }

        public bool IsRoot { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual Business Business { get; set; }

        public virtual User User { get; set; }
        public virtual IdentityRole Role { get; set; }
    }
}