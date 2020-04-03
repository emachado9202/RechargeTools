﻿using Microsoft.AspNet.Identity;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;
using RechargeTools.Models.Views;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RechargeTools.Controllers
{
    public class HomeController : GenericController
    {
        [Models.Handlers.Authorize]
        public async Task<ActionResult> Dashboard()
        {
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
            DashboardViewModel model = new DashboardViewModel();
            model.Agents = new List<Tuple<string, long, long>>();
            model.PendentNumbers = new List<Tuple<string, string>>();

            List<Number> numbers = await applicationDbContext.Numbers.Include("Agent").Where(x => x.Agent.Business_Id == business_working && !string.IsNullOrEmpty(x.Value)).ToListAsync();

            foreach (var number in numbers.GroupBy(x => x.Agent))
            {
                model.Agents.Add(new Tuple<string, long, long>(number.Key.Name, number.LongCount(x => string.IsNullOrEmpty(x.Confirmation)), number.LongCount()));
            }

            foreach (var number in numbers.Where(x => string.IsNullOrEmpty(x.Confirmation)))
            {
                model.PendentNumbers.Add(new Tuple<string, string>(number.Value, number.Agent_Id.ToString()));
            }

            return View(model);
        }

        [Models.Handlers.Authorize]
        public async Task<ActionResult> Index(string agent_select, string number_search)
        {
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());

            if (!string.IsNullOrEmpty(agent_select))
            {
                await AgentSelectCore(agent_select);
            }

            ViewBag.number_search = number_search;

            List<Agent> model = await applicationDbContext.Agents.Where(x => x.Activated && x.Business_Id == business_working).OrderBy(x => x.OrderDisplay).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Models.Handlers.Authorize]
        public async Task<ActionResult> Search(TableFilterViewModel filter)
        {
            Guid agent_id = Guid.Parse(filter.type);
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.Numbers.CountAsync(x => x.Agent_Id == agent_id);
            List<Number> model;

            var entity = applicationDbContext.Numbers.Include("Agent").Include("User").Where(x => x.Agent_Id == agent_id);

            IOrderedQueryable<Number> sort = null;
            if (filter.order[0].column == 0)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Value);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Value);
                }
            }
            else if (filter.order[0].column == 1)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Confirmation);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Confirmation);
                }
            }
            else if (filter.order[0].column == 3)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.CreatedDate);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.CreatedDate);
                }
            }
            else if (filter.order[0].column == 4)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.UpdatedDate);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.UpdatedDate);
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
                   applicationDbContext.Numbers.Include("Agent").Include("User").CountAsync(x => x.Agent_Id == agent_id &&
                   (x.Value.ToString().Contains(filter.search.value) ||
                   x.Confirmation.ToString().Contains(filter.search.value) ||
                   x.User.UserName.ToString().Contains(filter.search.value) ||
                   x.CreatedDate.ToString().Contains(filter.search.value) ||
                   x.UpdatedDate.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.Agent_Id == agent_id &&
                   (x.Value.ToString().Contains(filter.search.value) ||
                   x.Confirmation.ToString().Contains(filter.search.value) ||
                   x.User.UserName.ToString().Contains(filter.search.value) ||
                   x.CreatedDate.ToString().Contains(filter.search.value) ||
                   x.UpdatedDate.ToString().Contains(filter.search.value)))
                        .Skip(filter.start)
                        .Take(filter.length)
                        .ToListAsync();
            }

            List<NumberViewModel> result = new List<NumberViewModel>();

            foreach (var number in model)
            {
                result.Add(new NumberViewModel()
                {
                    DT_RowId = number.Id.ToString(),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation,
                    user = number.User?.UserName
                });
            }

            if (model.Count == 0)
            {
                Number number = new Number()
                {
                    Id = Guid.NewGuid(),
                    Agent_Id = agent_id,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                applicationDbContext.Numbers.Add(number);
                await applicationDbContext.SaveChangesAsync();

                result.Add(new NumberViewModel()
                {
                    DT_RowId = number.Id.ToString(),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation,
                    user = number.User?.UserName
                });
            }

            return Json(new
            {
                draw = filter.draw,
                recordsTotal = totalRows,
                recordsFiltered = totalRowsFiltered,
                data = result
            });
        }

        [HttpPost]
        [Models.Handlers.Authorize]
        public async Task<ActionResult> Edit(FormCollection form)
        {
            string data = form[0];
            string key = form.GetKey(0); //data[fb0a6764-7d29-4621-80eb-2cb304de3299][number]
            string[] temp = key.Split('[', ']'); //index 1 and 3

            Guid number_id = Guid.Parse(temp[1]);
            string attr_class = temp[3];

            Number number = await applicationDbContext.Numbers.Include("User").FirstOrDefaultAsync(x => x.Id == number_id);

            number.User_Id = User.Identity.GetUserId();
            number.UpdatedDate = DateTime.Now;
            switch (attr_class)
            {
                case "number":
                    {
                        number.Value = data;
                    }
                    break;

                case "confirmation":
                    {
                        number.Confirmation = data;
                    }
                    break;
            }

            applicationDbContext.Entry(number).State = System.Data.Entity.EntityState.Modified;
            await applicationDbContext.SaveChangesAsync();

            Number number_temp = await applicationDbContext.Numbers.FirstOrDefaultAsync(x => x.CreatedDate > number.CreatedDate && string.IsNullOrEmpty(x.Value));

            if (number_temp == null)
            {
                number_temp = new Number()
                {
                    Id = Guid.NewGuid(),
                    Agent_Id = number.Agent_Id,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                applicationDbContext.Numbers.Add(number_temp);
                await applicationDbContext.SaveChangesAsync();
            }

            return Json(new
            {
                data = new NumberViewModel()
                {
                    DT_RowId = number.Id.ToString(),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation,
                    user = number.User?.UserName
                }
            });
        }

        [HttpPost]
        [Models.Handlers.Authorize]
        public async Task<ActionResult> AgentSelect(string agent_id)
        {
            await AgentSelectCore(agent_id);

            return Json(true);
        }

        [Models.Handlers.Authorize]
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            Guid number_id = Guid.Parse(id);

            applicationDbContext.Numbers.Remove(await applicationDbContext.Numbers.FirstOrDefaultAsync(x => x.Id == number_id));
            await applicationDbContext.SaveChangesAsync();

            return Json(true);
        }

        private async Task AgentSelectCore(string agent_id)
        {
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
            Guid agentId = Guid.Parse(agent_id);
            Agent selected = await applicationDbContext.Agents.FirstOrDefaultAsync(x => x.Business_Id == business_working && x.Selected);
            selected.Selected = false;
            Agent agent = await applicationDbContext.Agents.FirstOrDefaultAsync(x => x.Id == agentId);
            agent.Selected = true;
            applicationDbContext.Entry(agent).State = System.Data.Entity.EntityState.Modified;
            applicationDbContext.Entry(selected).State = System.Data.Entity.EntityState.Modified;
            await applicationDbContext.SaveChangesAsync();
        }
    }
}