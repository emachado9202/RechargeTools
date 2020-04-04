using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Views
{
    public class RechargeViewModel
    {
        public string DT_RowId { get; set; }

        [DisplayName("Nombre")]
        public string Name { get; set; }

        [DisplayName("Costo")]
        public string Cost { get; set; }

        [DisplayName("Fecha de Inicio")]
        public string DateStart { get; set; }

        [DisplayName("Fecha Final")]
        public string DateEnd { get; set; }

        public bool Activated { get; set; }
        public long AgentsCount { get; set; }

        //Formulario de agregar un agente
        [DisplayName("Agente")]
        public string Agent_Id { get; set; }

        [DisplayName("Costo")]
        public string Agent_Cost { get; set; }
    }
}