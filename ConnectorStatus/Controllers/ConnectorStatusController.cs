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
        private static string BaseURL = "https://jira.arcadiasolutions.com/";
        private static int MaxIssueCount = 1000;
        private bool AreClosedLoaded = false;


        // GET: ConnectorStatus
        public ActionResult Index(FormCollection collection, bool showClosed = false, ConnectorBuildItem.SortOrder sortOrder = ConnectorBuildItem.SortOrder.Default, bool ascending = false)
        {

            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            FinalBuilds = new List<ConnectorBuildItem>();
            List<ConnectorBuildItem> DisplayBuilds = new List<ConnectorBuildItem>();


            ViewBag.StageLabels = (new ChildTicket()).Stages;
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
                    Session["jira"] = Jira;
                    GetParents();

                    GetSubTickets(showClosed);
                }
            }
            else if (Session["builds"] != null)
                FinalBuilds = Session["builds"] as List<ConnectorBuildItem>;

            
            Session["builds"] = FinalBuilds;


            if (showClosed && !AreClosedLoaded)
            {
                Jira = Session["jira"] as Jira;
                AllParents = new List<ParentTicket>();
                foreach (var build in FinalBuilds)
                    AllParents.Add(build.ParentTicket);

                GetSubTickets(showClosed);
            }

            DisplayBuilds = FinalBuilds.Where(p => p.ParentTicket.SubTasks != null && p.ParentTicket.SubTasks.Count > 0).OrderBy(x => x.ParentTicket.Client).ToList();

            if (!showClosed)
                DisplayBuilds = DisplayBuilds.Where(p => p.ParentTicket.Status.ToLower() != "closed").ToList();

            return View(DisplayBuilds);
        }

        [HttpPost]
        public ActionResult Comment(FormCollection collection)
        {
            string comment = "";
            string key = "";
            int i = 0; 
            while (comment != null && key != null)
            {
                comment = collection.Get("[" + i + "].ParentTicket.Description");
                key = collection.Get("[" + i + "].ParentTicket.Key");
                i++;
                if(key!=null && comment != null)
                    SubmitIfDifferentComment(key, comment);
            }
            //string showClosed = collection.Get("showClosed").ToLower();
            //bool showClosedbool = showClosed == "true" ? true : false;

            return RedirectToAction("Index", "ConnectorStatus",  false  );
        }

        private void InitiateConnection(string userName, string password)
        {
            try
            {
                Jira = new Jira(BaseURL, userName, password);
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
                    var issues = Jira.GetIssuesFromJql("project = AAI and \"Data Source Name\" IS NOT EMPTY AND \"Implementation Round\" IS NOT EMPTY AND type = Epic and \"Customer Name\" is not EMPTY");

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
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }

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
                   var subTickets = Jira.GetIssuesFromJql(BuildSubTicketJql(parent), 999);

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

        private void SubmitIfDifferentComment(string key, string comment)
        {
            Jira = Session["jira"] as Jira;
            if(Jira != null)
            {
                var issue = Jira.GetIssue(key);
                FinalBuilds = Session["builds"] as List<ConnectorBuildItem>;
                var issueInCache = FinalBuilds.Where(x => x.ParentTicket.Key == key).Select(x => x).FirstOrDefault();
                if(issueInCache!=null && (issueInCache.ParentTicket.Description == null ? "" : Regex.Replace(issueInCache.ParentTicket.Description, @"\s+", "")) != Regex.Replace(comment, @"\s+", ""))
                {
                    issue.AddComment(comment);
                    if (issueInCache != null && FinalBuilds.Contains(issueInCache))
                    {
                        FinalBuilds.Remove(issueInCache);
                        issueInCache.ParentTicket.Description = comment;
                        FinalBuilds.Add(issueInCache);
                        Session["builds"] = FinalBuilds;
                    }
                }
            }
        }

        private ChildTicket JiraIssueToChildIssue(Issue issue)
        {
            var test = issue.CustomFields.Where(c => c.Id == "customfield_11511");
            //issue["Implementation Phase"].ToString(),//GetIssueStage(issue),
            return new ChildTicket
            {
                Key = issue.Key.ToString(),
                Assignee = issue.Assignee,
                //TicketType = issue["Ticket Type"] != null ? issue["Ticket Type"].ToString() : "",
                Status = issue.Status.Name,
                Summary = issue.Summary,
                TicketStage = test.FirstOrDefault().Values.FirstOrDefault(),
                Client = issue["Customer Name"].ToString(),
                Source = issue["Data Source Name"].ToString(),
                ImplementationRound = issue["Implementation Round"].ToString()
            };
        }

        private ParentTicket JiraIssueToParentIssue(Issue issue)
        {
            var comments = issue.GetComments();
            string desc;
            if (comments != null && comments.Count > 0)
                desc = comments.OrderByDescending(c => c.CreatedDate).FirstOrDefault().Body;
            else
                desc = issue.Description;
            return new ParentTicket
            {
                Key = issue.Key.ToString(),
                Assignee = issue.Assignee,
                //TicketType = issue["Ticket Type"].ToString(),
                Status = issue.Status.Name.ToString(),
                Summary = issue.Summary,
                Client = issue["Customer Name"].ToString(),
                Source = issue["Data Source Name"].ToString(),
                Description = desc,
                DueDate = issue.DueDate,
                ImplementationRound = issue["Implementation Round"].ToString(),

            };
        }

        private string BuildSubTicketJql(ParentTicket parentTicket)
        {
            return "project = AAI and \"Data Source Name\" = \"" + parentTicket.Source+ "\" AND \"Customer Name\" = \"" + parentTicket.Client + "\" AND \"Implementation Round\" = \"" + parentTicket.ImplementationRound + "\" AND type = Story and \"Implementation Phase\" is not EMPTY";
            //
        }

        //private ChildTicket.Stage GetIssueStage(Issue issue)
        //{
        //    //Regex rgx = new Regex(@"(?<=\b[Ss]tage\s)(\w+)");
        //    string implementationPhase = issue["Implementation Phase"].ToString().Replace(' ', '_').Replace('-','');
        //    ChildTicket.Stage stage;
        //    Enum.TryParse(implementationPhase, out stage);
            
        //    return stage;
        //}

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
