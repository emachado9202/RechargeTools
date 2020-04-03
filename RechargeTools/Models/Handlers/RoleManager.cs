using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using RechargeTools.Models.Catalog;
using System.Linq;
using System.Web;

namespace RechargeTools.Models.Handlers
{
    public class RoleManager
    {
        public const string Administrator = "Administrador";
        public const string Editor = "Editor";
        public const string Dealer = "Vendedor";
        public const string Reading = "Lectura";

        public static bool IsInRole(string role)
        {
            ApplicationDbContext applicationDbContext = new ApplicationDbContext();
            bool flag = false;
            string UserId;

            //Check if Http Context
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                //Get User Id from HttpContext
                UserId = HttpContext.Current.User.Identity.GetUserId();
                User user = applicationDbContext.Users.FirstOrDefault(x => x.Id == UserId);

                if (user != null)
                {
                    BusinessUser businessUser = applicationDbContext.BusinessUsers.FirstOrDefault(x => x.Business_Id == user.CurrentBusiness_Id && x.User_Id == user.Id);

                    if (businessUser == null)
                    {
                        businessUser = applicationDbContext.BusinessUsers.FirstOrDefault(x => x.Business.IsPrimary && x.User_Id == user.Id);
                    }

                    IdentityRole role1 = applicationDbContext.Roles.FirstOrDefault(x => x.Id == businessUser.Role_Id);

                    if (role.Equals(role1.Name))
                    {
                        flag = true;
                    }
                }
            }

            return flag;
        }
    }
}