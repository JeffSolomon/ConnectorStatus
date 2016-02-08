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
        private static int MaxIssueCount = 10;


        // GET: ConnectorStatus
        public ActionResult Index(FormCollection collection)
        {

            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            FinalBuilds = new List<ConnectorBuildItem>();
            List<ConnectorBuildItem> DisplayBuilds = new List<ConnectorBuildItem>();

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
                    GetSubTickets();
                }
            }
            else if (Session["builds"] != null)
                FinalBuilds = Session["builds"] as List<ConnectorBuildItem>;
            
            Session["builds"] = FinalBuilds;

            DisplayBuilds = DisplayBuilds.Where(p => p.ParentTicket.Status.ToLower() != "closed" && p.ParentTicket.Stories != null && p.ParentTicket.Stories.Count > 0).OrderBy(x => x.ParentTicket.Client).OrderBy(x => x.ParentTicket.Source).ToList();

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
                    SubmitCommentIfDifferent(key, comment);
            }
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
                    var issues = Jira.GetIssuesFromJql("project = AAI and type = Epic and \"Data Source Name\" is not EMPTY and \"Customer Name\" is not empty and createdDate >= \"2016-02-03\"");

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

        private void GetSubTickets()
        {
            if(Jira != null)
            {
                Parallel.ForEach(AllParents.Where(p => p.Stories.Count() == 0 && p.Status.ToLower() != "closed"), parent =>
               {
                   var currentBuildItem = (from b in FinalBuilds
                                           where b.ParentTicket.Key == parent.Key
                                           select b).First();
                   var subTickets = Jira.GetIssuesFromJql(BuildStoryTicketJql(parent), 999);

                   foreach (var subTicket in subTickets)
                   {
                       ChildTicket thisChild = JiraIssueToChildIssue(subTicket);
                       parent.Stories.Add(thisChild);
                       if (!currentBuildItem.StageColors.ContainsKey(thisChild.TicketStage))
                           currentBuildItem.StageColors.Add(thisChild.TicketStage, thisChild);
                   }

                   if (parent.Stories.Count == 0)
                   {
                       FinalBuilds.Remove(currentBuildItem);
                   }

               });

            }
            

        }

        private void SubmitCommentIfDifferent(string key, string comment)
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
            return new ChildTicket
            {
                Key = issue.Key.ToString(),
                Assignee = issue.Assignee,
                Status = issue.Status.Name,
                Summary = issue.Summary,
                TicketStage = GetCustomFieldByID(issue, "11603"),
                Client = GetCustomField(issue, "Customer Name"),
                Source = GetCustomField(issue, "Data Source Name"),
                ImplementationRound = GetCustomField(issue, "Implementation Round")
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
                Status = issue.Status.Name.ToString(),
                Summary = issue.Summary,
                Client = GetCustomField(issue, "Customer Name"), 
                Source = GetCustomField(issue, "Data Source Name"),
                Description = desc,
                DueDate = issue.DueDate,
                ImplementationRound = GetCustomField(issue, "Implementation Round"),
                ContractID = GetCustomField(issue, "Customer Contract ID")
            };
        }

        private string BuildStoryTicketJql(ParentTicket parentTicket)
        {
            return "project = AAI and  \"Epic Link\" = " + parentTicket.Key;
        }

        private string GetCustomField(Issue issue, string fieldName)
        {
            var maybeNull = issue[fieldName];
            return maybeNull != null ? maybeNull.ToString() : "";
        }

        private string GetCustomFieldByID(Issue issue, string ID)
        {
            var customFieldID = "customfield_" + ID;
            var maybeNull = issue.CustomFields.Where(c => c.Id == customFieldID);
            return maybeNull != null ? maybeNull.FirstOrDefault().Values.FirstOrDefault().ToString() : "";
        }
    }
}
