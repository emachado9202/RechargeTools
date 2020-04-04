using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;
using RechargeTools.Models.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RechargeTools.Controllers
{
    [Models.Handlers.Authorize]
    public class BusinessController : GenericController
    {
        private ApplicationUserManager _userManager;
        private ApplicationSignInManager _signInManager;

        public BusinessController()
        {
        }

        public BusinessController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Search(TableFilterViewModel filter)
        {
            string current_user_id = User.Identity.GetUserId();

            List<BusinessModel> result = new List<BusinessModel>();
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.Businesses.Include("BusinessUsers").CountAsync(x => x.BusinessUsers.FirstOrDefault(y => y.User_Id == current_user_id && y.IsRoot) != null);
            List<Business> model;

            var entity = applicationDbContext.Businesses.Include("BusinessUsers").Where(x => x.BusinessUsers.FirstOrDefault(y => y.User_Id == current_user_id && y.IsRoot) != null);

            IOrderedQueryable<Business> sort = null;
            if (filter.order[0].column == 1)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Name);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Name);
                }
            }
            else if (filter.order[0].column == 2)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.CreatedOn);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.CreatedOn);
                }
            }
            else if (filter.order[0].column == 3)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.BusinessUsers.Count);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.BusinessUsers.Count);
                }
            }

            if (string.IsNullOrEmpty(filter.search.value))
            {
                totalRowsFiltered = totalRows;
                model = await sort.Skip(filter.start)
                    .Take(filter.length)
                    .ToListAsync();
            }
            else
            {
                totalRowsFiltered = await
               applicationDbContext.Businesses.CountAsync(x => x.BusinessUsers.FirstOrDefault(y => y.User_Id == current_user_id && y.IsRoot) != null && (x.Name.ToString().Contains(filter.search.value) ||
               x.CreatedOn.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.BusinessUsers.FirstOrDefault(y => y.User_Id == current_user_id && y.IsRoot) != null && (x.Name.ToString().Contains(filter.search.value) ||
               x.CreatedOn.ToString().Contains(filter.search.value)))
                        .Skip(filter.start)
                        .Take(filter.length)
                        .ToListAsync();
            }

            foreach (var business in model)
            {
                result.Add(new BusinessModel()
                {
                    DT_RowId = business.Id.ToString(),
                    Name = business.Name,
                    CreatedOn = business.CreatedOn.ToString("yyyy-MM-dd hh:mm tt"),
                    CountWorkers = business.BusinessUsers.Count,
                    Photo = business.Photo
                });
            }

            return Json(new
            {
                filter.draw,
                recordsTotal = totalRows,
                recordsFiltered = totalRowsFiltered,
                data = result
            });
        }

        public async Task<ActionResult> View(string id, string invitation)
        {
            Guid business_id = Guid.Parse(id);
            string current_user_id = User.Identity.GetUserId();

            Business business = await applicationDbContext.Businesses.Include("BusinessUsers").FirstOrDefaultAsync(x => x.Id == business_id);

            if (business.BusinessUsers.FirstOrDefault(x => x.User_Id == current_user_id && x.IsRoot) == null)
            {
                return RedirectToAction("Index");
            }

            BusinessModel result = new BusinessModel()
            {
                DT_RowId = business.Id.ToString(),
                Name = business.Name,
                CreatedOn = business.CreatedOn.ToString("yyyy-MM-dd hh:mm tt")
            };

            if (!string.IsNullOrEmpty(invitation))
                ViewBag.Invitation = invitation;

            List<IdentityRole> roles = await applicationDbContext.Roles.ToListAsync();
            ViewBag.Roles = roles;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> WorkerInvitation(BusinessModel model)
        {
            string current_user_id = User.Identity.GetUserId();

            EmailService emailService = new EmailService();

            IdentityMessage identityMessage = new IdentityMessage();
            identityMessage.Subject = $"Invitación de {model.Name}";
            identityMessage.Body = $"Ha recibido una invitación para unirse al grupo de trabajo de {model.Name} en <a href=\"{Url.Action("Index", "Home", null, protocol: Request.Url.Scheme)}\">{ConfigurationManager.AppSettings.Get("mailFromName")}</a>." +
                $"<br/>Por favor, presione <a href=\"{Url.Action("WorkerInvitationConfig", "Business", new { businessId = model.DT_RowId, ownerId = current_user_id, roleId = model.RoleInvitation, email = model.EmailInvitation }, protocol: Request.Url.Scheme)}\">AQUÍ</a> para aceptar la invitación, en caso contrario obvie este Email." +
                $"<br/><br/><br/>Atentamente,<br/>Equipo Minventario.";
            identityMessage.Destination = model.EmailInvitation;

            await emailService.SendAsync(identityMessage);

            return RedirectToAction("View", new { id = model.DT_RowId, invitation = model.EmailInvitation });
        }

        [HttpPost]
        public async Task<ActionResult> WorkerSearch(TableFilterViewModel filter)
        {
            Guid business_id = Guid.Parse(filter.type);

            List<BusinessUserModel> result = new List<BusinessUserModel>();
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.BusinessUsers.CountAsync(x => x.Business_Id == business_id);
            List<BusinessUser> model;

            var entity = applicationDbContext.BusinessUsers.Include("User").Where(x => x.Business_Id == business_id);

            IOrderedQueryable<BusinessUser> sort = null;
            if (filter.order[0].column == 0)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.User.UserName);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.User.UserName);
                }
            }
            else if (filter.order[0].column == 1)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.LastUpdated);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.LastUpdated);
                }
            }

            if (string.IsNullOrEmpty(filter.search.value))
            {
                totalRowsFiltered = totalRows;
                model = await sort.Skip(filter.start)
                    .Take(filter.length)
                    .ToListAsync();
            }
            else
            {
                totalRowsFiltered = await
               applicationDbContext.BusinessUsers.Include("User").CountAsync(x => x.Business_Id == business_id && (x.User.UserName.ToString().Contains(filter.search.value) ||
               x.LastUpdated.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.Business_Id == business_id && (x.User.UserName.ToString().Contains(filter.search.value) ||
               x.LastUpdated.ToString().Contains(filter.search.value)))
                        .Skip(filter.start)
                        .Take(filter.length)
                        .ToListAsync();
            }

            foreach (var user in model)
            {
                result.Add(new BusinessUserModel()
                {
                    DT_RowId = $"{user.Business_Id.ToString()}+{user.User_Id}",
                    Name = user.User.UserName,
                    LastUpdated = user.LastUpdated.ToString("yyyy-MM-dd hh:mm tt")
                });
            }

            return Json(new
            {
                filter.draw,
                recordsTotal = totalRows,
                recordsFiltered = totalRowsFiltered,
                data = result
            });
        }

        [AllowAnonymous]
        public async Task<ActionResult> WorkerInvitationConfig(string businessId, string ownerId, string email, string roleId)
        {
            Guid business_id = Guid.Parse(businessId);

            Business business = await applicationDbContext.Businesses.Include("BusinessUsers").FirstOrDefaultAsync(x => x.Id == business_id);

            User user = await applicationDbContext.Users.FirstOrDefaultAsync(x => x.Id == ownerId);

            User worker = await applicationDbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

            IdentityRole role = await applicationDbContext.Roles.FirstOrDefaultAsync(x => x.Id == roleId);

            bool exist = false;

            if (User.Identity.IsAuthenticated)
            {
                string currentId = User.Identity.GetUserId();

                exist = business.BusinessUsers.FirstOrDefault(x => x.User_Id == currentId) != null;
            }

            BusinessInvitationModel result = new BusinessInvitationModel()
            {
                Email = email,
                Owner = user,
                Business = new BusinessModel()
                {
                    DT_RowId = business.Id.ToString(),
                    Name = business.Name
                },
                AlreadySubscribed = exist,
                AlreadySystem = worker != null,
                Role = role
            };

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AcceptInvitation(string BusinessId, string RoleId)
        {
            Guid business_id = Guid.Parse(BusinessId);
            string userId = User.Identity.GetUserId();

            applicationDbContext.BusinessUsers.Add(new BusinessUser()
            {
                User_Id = userId,
                Business_Id = business_id,
                IsRoot = false,
                LastUpdated = DateTime.Now,
                Role_Id = RoleId
            });
            await applicationDbContext.SaveChangesAsync();

            return RedirectToAction("SetBusiness", "Home", new { id = business_id, returnUrl = "/" });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AcceptRegisterInvitation(BusinessInvitationModel model)
        {
            if (ModelState.IsValid)
            {
                Guid business_id = Guid.Parse(model.BusinessId);

                var user = new User { UserName = model.UserName, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    applicationDbContext.BusinessUsers.Add(new BusinessUser()
                    {
                        User_Id = user.Id,
                        Business_Id = business_id,
                        IsRoot = false,
                        LastUpdated = DateTime.Now,
                        Role_Id = model.RoleId
                    });
                    await applicationDbContext.SaveChangesAsync();

                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            return RedirectToAction("WorkerInvitationConfig", new { businessId = model.BusinessId, ownerId = model.OwnerId, email = model.Email });
        }
    }
}