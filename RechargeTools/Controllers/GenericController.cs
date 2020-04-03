using Microsoft.AspNet.Identity;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RechargeTools.Controllers
{
    public class GenericController : Controller
    {
        public static ApplicationDbContext applicationDbContext = new ApplicationDbContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (User.Identity.IsAuthenticated)
            {
                string userId = User.Identity.GetUserId();
                List<Business> businesses = applicationDbContext.Businesses.Where(x => x.BusinessUsers.FirstOrDefault(y => y.User_Id == userId) != null).ToList();

                Guid business_working = Guid.Empty;

                User user = applicationDbContext.Users.FirstOrDefault(x => x.Id == userId);
                if (user.CurrentBusiness_Id == null)
                {
                    try
                    {
                        Business business_user = businesses.FirstOrDefault(x => x.IsPrimary);

                        business_working = business_user.Id;
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    business_working = user.CurrentBusiness_Id.Value;
                }

                Session["BusinessWorking"] = business_working;

                ViewBag.Business = businesses;
                ViewBag.BusinessWorking = businesses.FirstOrDefault(x => x.Id == business_working);
            }
            Session["app_protocol"] = ConfigurationManager.AppSettings.Get("httpProtocol");
        }
    }
}