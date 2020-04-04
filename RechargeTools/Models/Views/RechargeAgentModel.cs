using System.ComponentModel;

namespace RechargeTools.Models.Views
{
    public class RechargeAgentModel
    {
        public string DT_RowId { get; set; }

        [DisplayName("Nombre")]
        public string Name { get; set; }

        [DisplayName("Costo")]
        public string Cost { get; set; }
    }
}