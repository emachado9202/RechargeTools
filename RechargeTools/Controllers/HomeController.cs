using Microsoft.AspNet.Identity;
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
        public async Task<string> Run()
        {
            return "procesando";
        }

        [Models.Handlers.Authorize]
        public async Task<ActionResult> Dashboard()
        {
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
            DashboardViewModel model = new DashboardViewModel();
            model.Agents = new List<Tuple<string, long, long, string, string>>();
            model.PendentNumbers = new List<Tuple<string, string>>();

            List<Number> numbers = await applicationDbContext.Numbers.Include("RechargeAgent").Include("RechargeAgent.Agent").Where(x => x.RechargeAgent.Agent.Business_Id == business_working && x.RechargeAgent.Recharge.Activated && !string.IsNullOrEmpty(x.Value)).ToListAsync();

            foreach (var number in numbers.GroupBy(x => x.RechargeAgent))
            {
                model.Agents.Add(new Tuple<string, long, long, string, string>(number.Key.Agent.Name, number.LongCount(x => !x.Confirmation), number.LongCount(), "", (number.Key.Cost * number.LongCount()).ToString("#,##0.00")));
            }

            foreach (var number in numbers.Where(x => !x.Confirmation))
            {
                model.PendentNumbers.Add(new Tuple<string, string>(number.Value, number.RechargeAgent.Agent_Id.ToString()));
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

            List<Agent> model = await applicationDbContext.RechargeAgents.Where(x => x.Recharge.Activated && x.Agent.Activated && x.Agent.Business_Id == business_working).Select(x => x.Agent).OrderBy(x => x.OrderDisplay).ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> ReorganizeConsecutives(string recharge_id, string agent_id)
        {
            Guid agentId = Guid.Parse(agent_id);
            Guid rechargeId = Guid.Parse(recharge_id);

            List<Number> numbers = await applicationDbContext.Numbers.Include("RechargeAgent").Where(x => x.RechargeAgent.Agent_Id == agentId && x.RechargeAgent.Recharge_Id == rechargeId).OrderBy(x => x.CreatedDate).ToListAsync();

            int cont = 1;
            foreach (var number in numbers)
            {
                number.Consecutive = cont++;

                applicationDbContext.Entry(number).State = System.Data.Entity.EntityState.Modified;
            }
            await applicationDbContext.SaveChangesAsync();

            return Json(true);
        }

        [HttpPost]
        [Models.Handlers.Authorize]
        public async Task<ActionResult> Search(TableFilterViewModel filter)
        {
            Guid agent_id = Guid.Parse(filter.type);
            Guid business_working = Guid.Parse(Session["BusinessWorking"].ToString());
            long totalRowsFiltered = 0;
            long totalRows = await applicationDbContext.Numbers.Include("RechargeAgent").Include("RechargeAgent.Recharge").CountAsync(x => x.RechargeAgent.Agent_Id == agent_id && x.RechargeAgent.Recharge.Activated);
            List<Number> model;

            var entity = applicationDbContext.Numbers.Include("RechargeAgent").Include("RechargeAgent.Agent").Include("RechargeAgent.Recharge").Include("User").Where(x => x.RechargeAgent.Agent_Id == agent_id && x.RechargeAgent.Recharge.Activated);

            IOrderedQueryable<Number> sort = null;
            if (filter.order[0].column == 0)
            {
                if (filter.order[0].dir.Equals("asc"))
                {
                    sort = entity.OrderBy(x => x.Consecutive);
                }
                else
                {
                    sort = entity.OrderByDescending(x => x.Consecutive);
                }
            }
            else if (filter.order[0].column == 1)
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
            else if (filter.order[0].column == 2)
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
            else if (filter.order[0].column == 4)
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
            else if (filter.order[0].column == 5)
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
                   applicationDbContext.Numbers.Include("RechargeAgent").Include("RechargeAgent.Agent").Include("RechargeAgent.Recharge").Include("User").CountAsync(x => x.RechargeAgent.Agent_Id == agent_id && x.RechargeAgent.Recharge.Activated &&
                   (x.Value.ToString().Contains(filter.search.value) ||
                   x.Consecutive.ToString().Contains(filter.search.value) ||
                   x.Confirmation.ToString().Contains(filter.search.value) ||
                   x.User.UserName.ToString().Contains(filter.search.value) ||
                   x.CreatedDate.ToString().Contains(filter.search.value) ||
                   x.UpdatedDate.ToString().Contains(filter.search.value)));

                model = await
                    sort.Where(x => x.RechargeAgent.Agent_Id == agent_id &&
                   (x.Value.ToString().Contains(filter.search.value) ||
                   x.Consecutive.ToString().Contains(filter.search.value) ||
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
                    consecutive = number.Consecutive.ToString("###000"),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation.ToString(),
                    user = number.User?.UserName
                });
            }

            if (model.Count == 0)
            {
                RechargeAgent rechargeAgent = await applicationDbContext.RechargeAgents.FirstOrDefaultAsync(x => x.Agent_Id == agent_id && x.Recharge.Activated);

                Number number = new Number()
                {
                    Id = Guid.NewGuid(),
                    RechargeAgent = rechargeAgent,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    Consecutive = 1
                };
                applicationDbContext.Numbers.Add(number);
                await applicationDbContext.SaveChangesAsync();

                result.Add(new NumberViewModel()
                {
                    DT_RowId = number.Id.ToString(),
                    consecutive = number.Consecutive.ToString("###000"),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation.ToString(),
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
            string data = form[0].Trim();
            string key = form.GetKey(0); //data[fb0a6764-7d29-4621-80eb-2cb304de3299][number]
            string[] temp = key.Split('[', ']'); //index 1 and 3

            Guid number_id = Guid.Parse(temp[1]);
            string attr_class = temp[3];

            Number number = await applicationDbContext.Numbers.Include("User").FirstOrDefaultAsync(x => x.Id == number_id);

            number.User_Id = User.Identity.GetUserId();
            number.UpdatedDate = DateTime.Now;

            bool next = true;
            switch (attr_class)
            {
                case "number":
                    {
                        if (!string.IsNullOrEmpty(data) && !IsDigitsOnly(data))
                        {
                            next = false;
                        }
                        if (next)
                            number.Value = data;
                    }
                    break;

                case "confirmation":
                    {
                        number.Confirmation = bool.Parse(data);
                    }
                    break;
            }
            if (next)
            {
                applicationDbContext.Entry(number).State = System.Data.Entity.EntityState.Modified;
                await applicationDbContext.SaveChangesAsync();

                Number number_temp = await applicationDbContext.Numbers.FirstOrDefaultAsync(x => x.CreatedDate > number.CreatedDate && string.IsNullOrEmpty(x.Value) && x.RechargeAgent_Id == number.RechargeAgent_Id);

                if (number_temp == null)
                {
                    number_temp = new Number()
                    {
                        Id = Guid.NewGuid(),
                        RechargeAgent_Id = number.RechargeAgent_Id,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        Consecutive = number.Consecutive + 1
                    };
                    applicationDbContext.Numbers.Add(number_temp);
                    await applicationDbContext.SaveChangesAsync();
                }
            }

            return Json(new
            {
                data = new NumberViewModel()
                {
                    DT_RowId = number.Id.ToString(),
                    consecutive = number.Consecutive.ToString("###000"),
                    number = number.Value,
                    created_date = number.CreatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    updated_date = number.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt"),
                    confirmation = number.Confirmation.ToString(),
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

        [Models.Handlers.Authorize]
        [HttpPost]
        public async Task<ActionResult> Change(string id)
        {
            Guid number_id = Guid.Parse(id);

            Number number = await applicationDbContext.Numbers.FirstOrDefaultAsync(x => x.Id == number_id);
            number.Confirmation = !number.Confirmation;

            applicationDbContext.Entry(number).State = System.Data.Entity.EntityState.Modified;
            await applicationDbContext.SaveChangesAsync();

            return Json(true);
        }

        #region Helpers

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

        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        #endregion Helpers

        [Models.Handlers.Authorize]
        [HttpPost]
        public async Task<ActionResult> UpdateNumbers(string recharge_id, string agent_id, string data)
        {
            Guid agentId = Guid.Parse(agent_id);
            Guid rechargeId = Guid.Parse(recharge_id);

            string[] numbers = data.Trim().Split('\n');

            foreach (var temp in numbers)
            {
                Number number = await applicationDbContext.Numbers.Include("RechargeAgent").FirstOrDefaultAsync(x => x.RechargeAgent.Agent_Id == agentId && x.RechargeAgent.Recharge_Id == rechargeId && !x.Confirmation && x.Value == temp);

                number.Confirmation = true;

                applicationDbContext.Entry(number).State = System.Data.Entity.EntityState.Modified;
            }
            await applicationDbContext.SaveChangesAsync();

            return Json(true);
        }

        [Models.Handlers.Authorize]
        [HttpPost]
        public async Task<ActionResult> AddNumbers(string recharge_id, string agent_id, string data)
        {
            Guid agentId = Guid.Parse(agent_id);
            Guid rechargeId = Guid.Parse(recharge_id);

            RechargeAgent rechargeAgent = await applicationDbContext.RechargeAgents.FirstOrDefaultAsync(x => x.Agent_Id == agentId && x.Recharge_Id == rechargeId);

            string[] numbers = data.Trim().Split('\n');

            string sreturn = "";

            Number number_empty = await applicationDbContext.Numbers.FirstOrDefaultAsync(x => x.RechargeAgent_Id == rechargeAgent.Id && !x.Confirmation && string.IsNullOrEmpty(x.Value));

            foreach (var temp in numbers)
            {
                string number = temp.Replace("+53", "").Trim();
                if (number.Length == 8 && IsDigitsOnly(number))
                {
                    Number add = new Number()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        User_Id = User.Identity.GetUserId(),
                        RechargeAgent_Id = rechargeAgent.Id,
                        Value = number,
                        Consecutive = number_empty.Consecutive++
                    };
                    applicationDbContext.Numbers.Add(add);
                }
                else
                {
                    sreturn += $"{number}\n";
                }
            }

            number_empty.CreatedDate = DateTime.Now;
            number_empty.UpdatedDate = DateTime.Now;

            applicationDbContext.Entry(number_empty).State = System.Data.Entity.EntityState.Modified;
            await applicationDbContext.SaveChangesAsync();

            return Json(sreturn.Length > 2 ? sreturn.Substring(0, sreturn.Length - 1) : "");
        }
    }
}