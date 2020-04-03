using RechargeTools.Models.Catalog;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace RechargeTools.Models.Handlers
{
    public class ActivityPublisher
    {
        private static ApplicationDbContext applicationDbContext = new ApplicationDbContext();

        public static async Task Publish(string userId, ActivityTypeConstants activityType, Guid guid, string name, Guid currentBusinessId)
        {
            ActivityLogType activityLogType = await applicationDbContext.ActivityLogTypes.FirstOrDefaultAsync(x => x.Id == (int)activityType);

            if (activityLogType.Enabled)
            {
                string comment = "";

                switch (activityType)
                {
                    case ActivityTypeConstants.Stock_Create:
                        {
                            comment = $"Ha agregado un nuevo producto al Inventario (<a href='/Stock?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Edit:
                        {
                            comment = $"Ha editado el producto al Inventario (<a href='/Stock?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Remove:
                        {
                            comment = $"Ha eliminado el producto al Inventario ({name})";
                        }
                        break;

                    case ActivityTypeConstants.Stock_In_Create:
                        {
                            comment = $"Ha agregado una entrada al producto (<a href='/StockIn?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_In_Edit:
                        {
                            comment = $"Ha editado una entrada del producto (<a href='/StockIn?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_In_Remove:
                        {
                            comment = $"Ha eliminado una entrada al producto ({name})";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Out_Create:
                        {
                            comment = $"Ha agregado una salida al producto (<a href='/StockOut?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Out_Edit:
                        {
                            comment = $"Ha editado una salida del producto (<a href='/StockOut?id={guid}'>{name}</a>)";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Out_Remove:
                        {
                            comment = $"Ha eliminado una salida al producto ({name})";
                        }
                        break;

                    case ActivityTypeConstants.Stock_Out_Receive:
                        {
                            comment = $"Ha recibido el dinero de la salida al producto (<a href='/StockOut?id={guid}'>{name}</a>)";
                        }
                        break;
                }

                ActivityLog log = new ActivityLog()
                {
                    Id = Guid.NewGuid(),
                    ActivityLogType_Id = (int)activityType,
                    Business_Id = currentBusinessId,
                    Date = DateTime.Now,
                    User_Id = userId,
                    Comment = comment
                };

                applicationDbContext.ActivityLogs.Add(log);
                await applicationDbContext.SaveChangesAsync();
            }
        }
    }

    public enum ActivityTypeConstants
    {
        Stock_Create = 1,
        Stock_Edit = 2,
        Stock_Remove = 3,
        Stock_In_Create = 4,
        Stock_In_Edit = 5,
        Stock_In_Remove = 6,
        Stock_Out_Create = 7,
        Stock_Out_Edit = 8,
        Stock_Out_Remove = 9,
        Stock_Out_Receive = 10,
    }
}