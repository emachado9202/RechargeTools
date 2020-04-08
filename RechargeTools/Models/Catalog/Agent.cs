using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RechargeTools.Models.Catalog
{
    public class Agent : GenericEntity
    {
        public string Name { get; set; }

        public bool Activated { get; set; }
        public bool Selected { get; set; }

        public int OrderDisplay { get; set; }

        public DateTime LastUpdated { get; set; }

        [ForeignKey("Business")]
        public Guid? Business_Id { get; set; }

        public virtual Business Business { get; set; }
    }
}