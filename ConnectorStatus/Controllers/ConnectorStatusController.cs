using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConnectorStatus.Models;
using Atlassian.Jira;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConnectorStatus.Controllers
{
    public class ConnectorStatusController : Controller
    {
        private List<ParentTicket> AllParents;
        private List<ChildTicket> AllChildren;
        private List<ConnectorBuildItem> FinalBuilds;
        private Jira Jira;
        private static int MaxIssueCount = 1000;
        private bool AreClosedLoaded = false;


        // GET: ConnectorStatus
        public ActionResult Index(FormCollection collection, bool showClosed = false, ConnectorBuildItem.SortOrder sortOrder = ConnectorBuildItem.SortOrder.Default, bool ascending = false)
        {

            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            FinalBuilds = new List<ConnectorBuildItem>();
            List<ConnectorBuildItem> DisplayBuilds = new List<ConnectorBuildItem>();


            ViewBag.StageLabels = (new ChildTicket()).StageNames;
            ViewBag.Toggle = !showClosed;

            string username;
            string password;

            if (collection != null && collection.Count > 0)
            {
                username = collection.Get("username");
                password = collection.Get("password");
                if (!String.IsNullOrEmpty(password))
                {
                    InitiateConnection(username, password);
                    System.Web.HttpContext.Current.Cache["jira"] = Jira;
                    GetParents();

                    GetSubTickets(showClosed);
                }
            }
            else if (System.Web.HttpContext.Current.Cache["builds"] != null)
                FinalBuilds = System.Web.HttpContext.Current.Cache["builds"] as List<ConnectorBuildItem>;

            
            System.Web.HttpContext.Current.Cache["builds"] = FinalBuilds;


            if (showClosed && !AreClosedLoaded)
            {
                Jira = System.Web.HttpContext.Current.Cache["jira"] as Jira;
                AllParents = new List<ParentTicket>();
                foreach (var build in FinalBuilds)
                    AllParents.Add(build.ParentTicket);

                GetSubTickets(showClosed);
            }

            DisplayBuilds = FinalBuilds;

            if (sortOrder == ConnectorBuildItem.SortOrder.Default)
                DisplayBuilds = DisplayBuilds.OrderByDescending(x => x.ParentTicket.TotalScore).ToList();
            else if (sortOrder == ConnectorBuildItem.SortOrder.Client && ascending)
                DisplayBuilds = DisplayBuilds.OrderBy(x => x.ParentTicket.TotalScore).ToList();
            else if (sortOrder == ConnectorBuildItem.SortOrder.Client && !ascending)
                DisplayBuilds = DisplayBuilds.OrderBy(x => x.ParentTicket.TotalScore).ToList();

            if (!showClosed)
                DisplayBuilds = DisplayBuilds.Where(p => p.ParentTicket.Status.ToLower() != "closed").ToList();

            return View(DisplayBuilds);
        }

        [HttpPost]
        public ActionResult Comment(FormCollection collection)
        {
            string comment = collection.Get("commentString");
            string key = collection.Get("key");
            string showClosed = collection.Get("showClosed").ToLower();
            bool showClosedbool = showClosed == "true" ? true : false;
            return RedirectToAction("Index", "ConnectorStatus",  new { showClosed = showClosedbool } );
        }

        private void InitiateConnection(string userName, string password)
        {
            try
            {
                Jira = new Jira("https://jira.arcadiasolutions.com/", userName, password);
                Jira.MaxIssuesPerRequest = MaxIssueCount;

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }


        private void GetParents()
        {
            if (Jira != null)
            {
                try
                {
                    string ticketType = "Build Tracking";

                    var issues = from issue in Jira.GetIssuesFromJql("project = \"CD\" AND \"Ticket Type\" = \"Build Tracking\"")
                                 where issue.Project == "CD" && issue["Ticket Type"] == new LiteralMatch(ticketType)
                                 orderby issue.Created
                                 select issue;

                    foreach (var issue in issues)
                    {
                        ParentTicket parent = JiraIssueToParentIssue(issue);
                        if(!AllParents.Contains(parent))
                        {
                            AllParents.Add(parent);
                            FinalBuilds.Add(new ConnectorBuildItem(parent));
                        }      
                    }
                }
                catch{ }

            }
        }

        private void GetSubTickets(bool showClosed)
        {
            if(Jira != null)
            {

                Parallel.ForEach(AllParents.Where(p => p.SubTasks.Count() == 0 && ((p.Status.ToLower() != "closed") || showClosed)), parent =>
               {
                   var currentBuildItem = (from b in FinalBuilds
                                           where b.ParentTicket.Key == parent.Key
                                           select b).First();
                   var subTickets = Jira.GetIssuesFromJql(BuildSubTicketJql(parent.Key), 999);

                   foreach (var subTicket in subTickets)
                   {
                       ChildTicket thisChild = JiraIssueToChildIssue(subTicket);
                       parent.SubTasks.Add(thisChild);
                       if (!currentBuildItem.StageColors.ContainsKey(thisChild.TicketStage))
                           currentBuildItem.StageColors.Add(thisChild.TicketStage, thisChild);
                   }

                   if (parent.SubTasks.Count == 0)
                   {
                       FinalBuilds.Remove(currentBuildItem);
                   }

               });
                if (showClosed)
                    AreClosedLoaded = true;
            }
            

        }

        private ChildTicket JiraIssueToChildIssue(Issue issue)
        {
            return new ChildTicket
            {
                Key = issue.Key.ToString(),
                Assignee = issue.Assignee,
                //TicketType = issue["Ticket Type"] != null ? issue["Ticket Type"].ToString() : "",
                Status = issue.Status.Name,
                Summary = issue.Summary,
                TicketStage = GetIssueStage(issue),
                Client = issue.Components.Count > 0 ? issue.Components[0].ToString() : ""
            };
        }

        private ParentTicket JiraIssueToParentIssue(Issue issue)
        {
            return new ParentTicket
            {
                Key = issue.Key.ToString(),
                Assignee = issue.Assignee,
                TicketType = issue["Ticket Type"].ToString(),
                Status = issue.Status.Name.ToString(),
                Summary = issue.Summary,
                Client = issue.Components.Count > 0 ? issue.Components[0].ToString() : "",
                Source = GetIssueSource(issue),
                Description = issue.Description,
                DueDate = issue.DueDate
            };
        }

        private string BuildSubTicketJql(string parentTicket)
        {
            return "parent = \"" + parentTicket + "\"";
        }

        private ChildTicket.Stage GetIssueStage(Issue issue)
        {
            Regex rgx = new Regex(@"(?<=\b[Ss]tage\s)(\w+)");
            ChildTicket.Stage stage;
            Enum.TryParse(rgx.Match(issue.Summary).Value, out stage);
            return stage;
        }

        private string GetIssueSource(Issue issue)
        {

            string raw = issue.Summary.Substring(0, issue.Summary.IndexOf(' '));
            string returnVal = "Unknown Source";

            if (raw.Contains('-'))
            {
                string[] labelComponents = raw.Split('-');

                if (labelComponents.Length == 3)
                    return returnVal = labelComponents[1];
                        
                else if (labelComponents.Length == 4)
                    return returnVal = labelComponents[1] + "-" + labelComponents[2];
                        
                else
                    returnVal = raw;
            }
            else if (returnVal == "Unknown Source")
                returnVal = raw;
                

            return returnVal;
        }

    }
}
