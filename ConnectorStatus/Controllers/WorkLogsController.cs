using ConnectorStatus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace ConnectorStatus.Controllers
{
    public class WorkLogsController : Controller
    {
        // GET: WorkLogs
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GetClientStageData(string startDate = null, string endDate = null)
        {
            DateTime? start = new DateTime(1900, 1, 1); ;
            if(startDate != null)
            {
                DateTime st;
                DateTime.TryParse(startDate, out st);
                if (st.Year > 1)
                    start = st;
            }


            DateTime? end = DateTime.Now;
            if (endDate != null)
            {
                DateTime st;
                DateTime.TryParse(endDate, out st);
                if (st.Year > 1)
                    end = st;
            }

            List<LogGroup> workLogGroups = new List<LogGroup>();
            List<ChildTicket> allChildren = new List<ChildTicket>();
            List<ParentTicket> allParents = new List<ParentTicket>();

            if ((bool)Session["SuccessfulLogin"] && FileWriter.ReadJsonFile() != null)
            {
                var builds = FileWriter.ReadJsonFile();

                foreach(var build in builds)
                {
                    allChildren.AddRange(build.StageColors.Select(x=> x.Value));
                    allParents.Add(build.ParentTicket);
                }
                var groupedChildren = allChildren.Select(x => new { x.Client, x.TicketStage, HoursLogged = x.GetHoursLogged(start, end) })
                                              .GroupBy(s => new { s.Client, s.TicketStage }) //Group by client + Stage to get sum by client/stage. 
                                              .Select(g => new
                                              {
                                                  key = g.Key.Client,
                                                  stage = g.Key.TicketStage,
                                                  hours = g.Sum(x => Math.Round(Convert.ToDecimal(x.HoursLogged), 2))
                                              })
                                              .OrderBy(x => BuildProcessConfig.Stages.Where(s => s.Value == x.stage).Select(o => o.Key).FirstOrDefault())
                                              .GroupBy(g2 => new { g2.key }); //Roll up one last time to make it easier to conver to JSON (one series per client).

                var groupedParents = allParents.Select(x => new { x.Client, HoursLogged = x.GetHoursLogged(start, end) })
                                              .GroupBy(s => new { s.Client })
                                              .Select(g => new
                                              {
                                                  key = g.Key.Client,
                                                  hours = g.Sum(x => Math.Round(Convert.ToDecimal(x.HoursLogged), 2))
                                              });


                foreach (var group in groupedChildren)
                {
                    LogGroup logGroup = new LogGroup() { key = group.Key.key, values = new List<StageLog>() };
                    foreach(var stage in group)
                    {
                        logGroup.values.Add(new StageLog() { stage = stage.stage, hours = stage.hours });
                    }

                    var epicTicket = groupedParents.Where(x => x.key == group.Key.key).FirstOrDefault();
                    if (epicTicket != null)
                        logGroup.values.Add(new StageLog() { stage = "Master Ticket", hours = epicTicket.hours });

                    workLogGroups.Add(logGroup);
                }

                var workLogJson = Json(workLogGroups);
                return workLogJson;
            }

                return Json("");
        }

        [HttpPost]
        public ActionResult GetTicketEffortAndDuration()
        {
            List<ChildTicket> allChildren = new List<ChildTicket>();
            List<ParentTicket> allParents = new List<ParentTicket>();

            if ((bool)Session["SuccessfulLogin"] && FileWriter.ReadJsonFile() != null)
            {
                var builds = FileWriter.ReadJsonFile();

                foreach (var build in builds)
                {
                    allChildren.AddRange(build.StageColors.Select(x => x.Value));
                    allParents.Add(build.ParentTicket);
                }


                var tickets = allChildren.Select(x => new { Key = x.Client + " - " + x.Source, Duration = x.GetPseudoDuration(), Effort = x.GetHoursLogged(), Stage = BuildProcessConfig.Stages.Where(y => y.Value == x.TicketStage).Select(z => z.Key).FirstOrDefault(), StageLabel = x.TicketStage})
                                         .Where(x => x.Duration > 0)
                                         .ToList();

                var parentTickets = allParents.Select(x => new { Key = x.Client + " - " + x.Source, Duration = x.GetPseudoDuration(), Effort = x.GetHoursLogged(), Stage = 0, StageLabel = "Parent" })
                                                .Where(x => x.Duration > 0)
                                                .ToList();
                tickets.AddRange(parentTickets);

                List<BubbleGroup> groups = new List<BubbleGroup>();
                foreach(var ticket in tickets)
                {
                    var exists = groups.Where(x => x.key == ticket.Key).FirstOrDefault();
                    if (exists == null)
                        groups.Add(new BubbleGroup { key = ticket.Key, values = new List<BubbleData>() });
                    else
                        exists.values.Add(new BubbleData { StageNumber = ticket.Stage, Duration = ticket.Duration, Effort = ticket.Effort, StageLabel = ticket.StageLabel });
                }

                var workLogJson = Json(groups);
                return workLogJson;
            }

            return Json("");
        }

        class LogGroup
        {
            public string key { get; set; }
            public List<StageLog> values { get; set; }
        }
        class StageLog
        {
            public string stage { get; set; }
            public decimal hours { get; set; }
        }

        class BubbleGroup
        {
            public string key { get; set; }
            public List<BubbleData> values { get; set; }

        }

        class BubbleData
        {
            public double StageNumber { get; set; }
            public string StageLabel { get; set; }
            public double Duration { get; set; }
            public double Effort { get; set; }
        }

      

    }
}