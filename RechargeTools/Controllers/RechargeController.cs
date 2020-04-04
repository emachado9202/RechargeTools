using Microsoft.AspNet.Identity;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;
using RechargeTools.Models.Handlers;
using RechargeTools.Models.Views;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RechargeTools.Controllers
{
    [Models.Handlers.Authorize(Roles = RoleManager.Editor + "," + RoleManager.Administrator)]
    public class RechargeController : GenericController
    {
        // GET: Recharge
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Search(TableFilterViewModel filter)
        {
            Guid business_id = Guid.Parse(Session["BusinessWorking"].ToString());

            List<RechargeViewModel> result = new List<RechargeViewModel>();
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.Recharges.CountAsync(x => x.Business_Id == business_id);
            List<Recharge> model;

            var entity = applicationDbContext.Recharges.Include("RechargeAgents").Where(x => x.Business_Id == business_id);

            IOrderedQueryable<Recharge> sort = null;
            if (filter.order[0].column == 0)
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
            else if (filter.order[0].column == 1)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Cost);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Cost);
                }
            }
            else if (filter.order[0].column == 2)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Activated);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Activated);
                }
            }
            else if (filter.order[0].column == 3)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.DateStart);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.DateStart);
                }
            }
            else if (filter.order[0].column == 4)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.DateEnd);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.DateEnd);
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
               applicationDbContext.Recharges.CountAsync(x => x.Business_Id == business_id && (x.Name.ToString().Contains(filter.search.value) ||
               x.Cost.ToString().Contains(filter.search.value) || x.DateStart.ToString().Contains(filter.search.value) ||
               x.DateEnd.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.Business_Id == business_id && (x.Name.ToString().Contains(filter.search.value) ||
               x.Cost.ToString().Contains(filter.search.value) || x.DateStart.ToString().Contains(filter.search.value) ||
               x.DateEnd.ToString().Contains(filter.search.value)))
                        .Skip(filter.start)
                        .Take(filter.length)
                        .ToListAsync();
            }

            foreach (var recharge in model)
            {
                result.Add(new RechargeViewModel()
                {
                    DT_RowId = recharge.Id.ToString(),
                    Name = recharge.Name,
                    Cost = recharge.Cost.ToString("#,##0.00"),
                    DateStart = recharge.DateStart.ToString("yyyy-MM-dd hh:mm tt"),
                    DateEnd = recharge.DateEnd.ToString("yyyy-MM-dd hh:mm tt"),
                    AgentsCount = recharge.RechargeAgents.Count,
                    Activated = recharge.Activated
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

        [HttpGet]
        public ActionResult Create()
        {
            return View(new RechargeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RechargeViewModel model)
        {
            if (ModelState.IsValid)
            {
                Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());

                if (model.Activated)
                {
                    Recharge old_recharge = await applicationDbContext.Recharges.FirstOrDefaultAsync(x => x.Activated);
                    if (old_recharge != null)
                    {
                        old_recharge.Activated = false;

                        applicationDbContext.Entry(old_recharge).State = EntityState.Modified;
                    }
                }

                Recharge recharge = new Recharge()
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    Activated = model.Activated,
                    Cost = decimal.Parse(model.Cost.Replace(".", ",")),
                    Business_Id = business_working,
                    DateStart = DateTime.Parse(model.DateStart),
                    DateEnd = DateTime.Parse(model.DateEnd)
                };
                applicationDbContext.Recharges.Add(recharge);

                await applicationDbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            Guid recharge_id = Guid.Parse(id);

            Recharge recharge = await applicationDbContext.Recharges.FirstOrDefaultAsync(x => x.Id == recharge_id);

            if (recharge != null)
            {
                RechargeViewModel model = new RechargeViewModel()
                {
                    DT_RowId = recharge.Id.ToString(),
                    Name = recharge.Name,
                    Cost = recharge.Cost.ToString("#,##0.00"),
                    DateStart = recharge.DateStart.ToString("yyyy-MM-dd hh:mm tt"),
                    DateEnd = recharge.DateEnd.ToString("yyyy-MM-dd hh:mm tt"),
                    Activated = recharge.Activated
                };

                return View(model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(RechargeViewModel model)
        {
            if (ModelState.IsValid)
            {
                Guid recharge_id = Guid.Parse(model.DT_RowId);

                Recharge recharge = await applicationDbContext.Recharges.FirstOrDefaultAsync(x => x.Id == recharge_id);

                if (recharge != null)
                {
                    if (model.Activated)
                    {
                        Recharge old_recharge = await applicationDbContext.Recharges.FirstOrDefaultAsync(x => x.Activated && x.Id != recharge_id);
                        if (old_recharge != null)
                        {
                            old_recharge.Activated = false;

                            applicationDbContext.Entry(old_recharge).State = EntityState.Modified;
                        }
                    }

                    recharge.Name = model.Name;
                    recharge.Activated = model.Activated;
                    recharge.Cost = decimal.Parse(model.Cost.Replace(".", ","));
                    recharge.DateStart = DateTime.Parse(model.DateStart);
                    recharge.DateEnd = DateTime.Parse(model.DateEnd);

                    applicationDbContext.Entry(recharge).State = EntityState.Modified;

                    await applicationDbContext.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<ActionResult> View(string id)
        {
            Guid recharge_id = Guid.Parse(id);

            Recharge recharge = await applicationDbContext.Recharges.FirstOrDefaultAsync(x => x.Id == recharge_id);

            if (recharge == null)
            {
                return RedirectToAction("Index");
            }

            RechargeViewModel result = new RechargeViewModel()
            {
                DT_RowId = recharge.Id.ToString(),
                Name = recharge.Name,
                Cost = recharge.Cost.ToString("#,##0.00"),
                DateStart = recharge.DateStart.ToString("yyyy-MM-dd hh:mm tt"),
                DateEnd = recharge.DateEnd.ToString("yyyy-MM-dd hh:mm tt"),
                Activated = recharge.Activated
            };

            List<Agent> agents = await applicationDbContext.Agents.Where(x => x.Business_Id == recharge.Business_Id && x.Activated).OrderBy(x => x.OrderDisplay).ToListAsync();
            ViewBag.Agents = agents;

            return View(result);
        }

        [HttpPost]
        public async Task<ActionResult> AgentSearch(TableFilterViewModel filter)
        {
            Guid recharge_id = Guid.Parse(filter.type);

            List<RechargeAgentModel> result = new List<RechargeAgentModel>();
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.RechargeAgents.CountAsync(x => x.Recharge_Id == recharge_id);
            List<RechargeAgent> model;

            var entity = applicationDbContext.RechargeAgents.Include("Agent").Where(x => x.Recharge_Id == recharge_id);

            IOrderedQueryable<RechargeAgent> sort = null;
            if (filter.order[0].column == 0)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Agent.Name);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Agent.Name);
                }
            }
            if (filter.order[0].column == 1)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Cost);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Cost);
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
               applicationDbContext.RechargeAgents.Include("Agent").CountAsync(x => x.Recharge_Id == recharge_id && (x.Agent.Name.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.Recharge_Id == recharge_id && (x.Agent.Name.ToString().Contains(filter.search.value)))
                        .Skip(filter.start)
                        .Take(filter.length)
                        .ToListAsync();
            }

            foreach (var agent in model)
            {
                result.Add(new RechargeAgentModel()
                {
                    DT_RowId = agent.Id.ToString(),
                    Name = agent.Agent.Name,
                    Cost = agent.Cost.ToString("#,##0.00")
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddAgent(RechargeViewModel model)
        {
            Guid agent_id = Guid.Parse(model.Agent_Id);
            Guid recharge_id = Guid.Parse(model.DT_RowId);

            RechargeAgent rechargeAgent = await applicationDbContext.RechargeAgents.FirstOrDefaultAsync(x => x.Agent_Id == agent_id && x.Recharge_Id == recharge_id);

            if (rechargeAgent == null)
            {
                rechargeAgent = new RechargeAgent()
                {
                    Id = Guid.NewGuid(),
                    Recharge_Id = recharge_id,
                    Agent_Id = agent_id,
                    Cost = decimal.Parse(model.Agent_Cost.Replace(".", ","))
                };
                applicationDbContext.RechargeAgents.Add(rechargeAgent);
                await applicationDbContext.SaveChangesAsync();
            }

            return RedirectToAction("View", new { id = model.DT_RowId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImportAgents(string DT_RowId)
        {
            Guid recharge_id = Guid.Parse(DT_RowId);

            Recharge old_recharge = await applicationDbContext.Recharges.Include("RechargeAgents").OrderByDescending(x => x.DateEnd).FirstOrDefaultAsync(x => x.Id != recharge_id);

            foreach (var agent in old_recharge.RechargeAgents)
            {
                RechargeAgent rechargeAgent = new RechargeAgent()
                {
                    Id = Guid.NewGuid(),
                    Agent_Id = agent.Agent_Id,
                    Cost = agent.Cost,
                    Recharge_Id = recharge_id
                };
                applicationDbContext.RechargeAgents.Add(rechargeAgent);
            }

            await applicationDbContext.SaveChangesAsync();

            return RedirectToAction("View", new { id = DT_RowId });
        }
    }
}