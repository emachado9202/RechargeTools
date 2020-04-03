using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RechargeTools.Models.Views
{
    public class BusinessModel
    {
        public string DT_RowId { get; set; }

        [DisplayName("Foto")]
        public string Photo { get; set; }

        [DisplayName("Nombre")]
        public string Name { get; set; }

        [DisplayName("Cant. Trabajadores")]
        public int CountWorkers { get; set; }

        [DisplayName("Creado el")]
        public string CreatedOn { get; set; }

        [DisplayName("Correo")]
        public string EmailInvitation { get; set; }

        [DisplayName("Role Trabajador")]
        public string RoleInvitation { get; set; }
    }
}