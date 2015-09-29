using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConnectorStatus.Models;
using Atlassian.Jira;
using System.Text.RegularExpressions;

namespace ConnectorStatus.Controllers
{
    public class ConnectorStatusController : Controller
    {
        private List<ParentTicket> AllParents;
        private List<ChildTicket> AllChildren;
        private List<ConnectorBuildItem> FinalBuilds;
        private Jira Jira;
        private static int MaxIssueCount = 25;

        // GET: ConnectorStatus
        public ActionResult Index(FormCollection collection, bool filterCompleted = false)
        {
            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            FinalBuilds = new List<ConnectorBuildItem>();

            if (System.Web.HttpContext.Current.Cache["builds"] == null)
            {
                string username = collection.Get("username");
                string password = collection.Get("password");

                InitiateConnection(username, password);

                GetParents();

                GetSubTickets();
            }
            else
                FinalBuilds = System.Web.HttpContext.Current.Cache["builds"] as List<ConnectorBuildItem>;


            ViewBag.StageLabels = FinalBuilds[0].ParentTicket.SubTasks[0].StageNames;
            ViewBag.Toggle = !filterCompleted;

            System.Web.HttpContext.Current.Cache["builds"] = FinalBuilds;

            if(filterCompleted)
            {
                List<ConnectorBuildItem> filteredBuilds = new List<ConnectorBuildItem>();
                foreach (var item in FinalBuilds)
                    if (item.ParentTicket.TotalScore < 30)
                        filteredBuilds.Add(item);

                return View(filteredBuilds);
            }
            else
               return View(FinalBuilds);
        }

        private void InitiateConnection(string userName, string password)
        {
            try
            {
                Jira = new Jira("https://jira.wedostuffwell.com/", userName, password);
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
                string ticketType = "Build Tracking";

                var issues = from issue in Jira.Issues
                             where issue.Project == "CD" && issue["Ticket Type"] == new LiteralMatch(ticketType)
                             orderby issue.Created
                             select issue;

                foreach (var issue in issues)
                {
                    ParentTicket parent = JiraIssueToParentIssue(issue);
                    AllParents.Add(parent);
                    FinalBuilds.Add(new ConnectorBuildItem(parent));
                }

            }
        }

        private void GetSubTickets()
        {
            foreach(var parent in AllParents)
            {
               var currentBuildItem = (from b in FinalBuilds
                                        where b.ParentTicket.Key == parent.Key
                                        select b).First();

                foreach (var subTicket in Jira.GetIssuesFromJql(BuildSubTicketJql(parent.Key), 999))
                {
                    ChildTicket thisChild = JiraIssueToChildIssue(subTicket);
                    parent.SubTasks.Add(thisChild);
                    if(!currentBuildItem.StageColors.ContainsKey(thisChild.TicketStage))
                        currentBuildItem.StageColors.Add(thisChild.TicketStage, thisChild);
                }

                if(parent.SubTasks.Count == 0)
                {
                    FinalBuilds.Remove(currentBuildItem); 
                }

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
                Status = issue.Status.ToString(),
                Summary = issue.Summary,
                Client = issue.Components.Count > 0 ? issue.Components[0].ToString() : "",
                Source = GetIssueSource(issue),
                Description = issue.Description
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
