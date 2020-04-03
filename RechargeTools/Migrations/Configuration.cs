namespace RechargeTools.Migrations
{
    using RechargeTools.Models;
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "MovilShopStock.Models.ApplicationDbContext";
        }

        protected override void Seed(ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
            /*
            context.ActivityLogTypes.AddOrUpdate(
                  a => a.SystemKeybord,
                  new ActivityLogType { SystemKeybord = "Stock_Create", Name = "Nuevo producto en Inventario", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Edit", Name = "Editar producto en Inventario", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Remove", Name = "Eliminar producto en Inventario", Enabled = false },
                  new ActivityLogType { SystemKeybord = "Stock_In_Create", Name = "Nueva Entrada", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_In_Edit", Name = "Editar Entrada", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_In_Remove", Name = "Eliminar Entrada", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Out_Create", Name = "Nueva Salida", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Out_Edit", Name = "Nueva Salida", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Out_Remove", Name = "Eliminar Salida", Enabled = true },
                  new ActivityLogType { SystemKeybord = "Stock_Out_Receive", Name = "Recibir dinero a trabajador", Enabled = true }
               );
               */

            context.SaveChanges();
        }
    }
}