using RechargeTools.Models.Catalog;
using RechargeTools.Models.Handlers;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RechargeTools.Controllers
{
    [Models.Handlers.Authorize(Roles = RoleManager.Editor + "," + RoleManager.Administrator)]
    public class AgentController : GenericController
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());

            return View(await applicationDbContext.Agents.Where(x => x.Business_Id == business_working).OrderBy(x => x.OrderDisplay).ToListAsync());
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View(new Agent());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Agent model)
        {
            if (ModelState.IsValid)
            {
                Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
                model.Id = Guid.NewGuid();
                model.LastUpdated = DateTime.Now;
                model.Business_Id = business_working;
                applicationDbContext.Agents.Add(model);

                await applicationDbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            Guid cat_id = Guid.Parse(id);

            return View(await applicationDbContext.Agents.FirstOrDefaultAsync(x => x.Id == cat_id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, Agent model)
        {
            if (ModelState.IsValid)
            {
                Guid cat_id = Guid.Parse(id);

                Agent category = await applicationDbContext.Agents.FirstOrDefaultAsync(x => x.Id == cat_id);
                category.Name = model.Name;
                category.LastUpdated = DateTime.Now;
                category.OrderDisplay = model.OrderDisplay;

                applicationDbContext.Entry(category).State = EntityState.Modified;

                await applicationDbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }
    }
}