using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConnectorStatus.Models;
using Atlassian.Jira;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConnectorStatus.Controllers
{
    public class ConnectorStatusController : Controller
    {
        private List<ParentTicket> AllParents;
        private List<ChildTicket> AllChildren;
        private List<ConnectorBuildItem> FinalBuilds;
        private Jira Jira;
        private static string BaseURL = "https://jira.arcadiasolutions.com/";
        private static string EpicJQLQuery = "project = AAI and type = Epic and status != 190-Completed and \"Data Source Name\" is not EMPTY and \"Customer Name\" is not empty and createdDate >= \"2016-02-03\"";
        private static string StoryJQLQueryBase = "project = AAI and type = Story and \"Customer Name\" is not EMPTY and \"Data Source Name\" is not EMPTY and \"Implementation Round\" is not EMPTY and \"Implementation Phase\" is not EMPTY and  \"Epic Link\" = ";
        private static string UpdatedTicketsQuery = "project = AAI and type in(Epic, Story, Sub-task) and status != 190-Completed and \"Data Source Name\" is not EMPTY and \"Customer Name\" is not empty and (updatedDate >= \"{0}\" or worklogDate >= \"{0}\")";

        private static int MaxIssueCount = 1000;
        
        // GET: ConnectorStatus
        public async Task<ActionResult> Index(FormCollection collection)
        {
            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            FinalBuilds = new List<ConnectorBuildItem>();
            var DisplayBuilds = new List<ConnectorBuildItem>();

            string username;
            string password;
            var currentDateTime = DateTime.Now;

            if (collection != null && collection.Count > 0)
            {
                username = collection.Get("username");
                Session["username"] = username;
                password = collection.Get("password");
                FileWriter.Log("Login by " + username);
                if (!String.IsNullOrEmpty(password))
                {
                    if(InitiateConnection(username, password))
                    {
                        Session["jira"] = Jira;
                        var fromFile = FileWriter.ReadJsonFile();
                        if(fromFile == null)
                        {
                            GetParents();
                            await GetSubTickets();
                        }
                        else
                        {
                            FinalBuilds = fromFile;
                            AllParents = FinalBuilds.Select(x => x.ParentTicket).ToList();
                            GetAndApplyUpdates();
                            await GetSubTickets();
                        }
                        FileWriter.WriteLastUpdateTime(currentDateTime);
                    }
                    
                }
            }
            //else if (Session["builds"] != null)
            //    FinalBuilds = Session["builds"] as List<ConnectorBuildItem>;            
            else if ((bool)Session["SuccessfulLogin"] && FileWriter.ReadJsonFile() != null)
                FinalBuilds = FileWriter.ReadJsonFile();


            //Session["builds"] = FinalBuilds;
            FileWriter.WriteJsonFile(FinalBuilds);

            DisplayBuilds = FinalBuilds
                            .Where(p => p.ParentTicket.Stories != null && 
                                        p.ParentTicket.Stories.Count > 0)
                            .OrderBy(x => x.ParentTicket.Source)
                            .OrderByDescending(x => x.ParentTicket.TotalScore)
                            .OrderBy(x => x.ParentTicket.Client).ToList();

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

        private bool InitiateConnection(string userName, string password)
        {
            bool success = true;
            try
            {
                Jira = Jira.CreateRestClient(BaseURL, userName, password);
                Jira.MaxIssuesPerRequest = MaxIssueCount;
                var testQueryResult = Jira.GetIssue("AAI-1");
                success = testQueryResult != null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                success = false;
            }
            Session["SuccessfulLogin"] = success;
            return success;
        }

        private void GetParents()
        {
            if (Jira != null)
            {
                try
                {
                    var issues = Jira.GetIssuesFromJql(EpicJQLQuery).ToList();

                    foreach (var issue in issues)
                    {
                        var parent = new ParentTicket(issue);
                        if(!AllParents.Contains(parent))
                        {
                            AllParents.Add(parent);
                            FinalBuilds.Add(new ConnectorBuildItem(parent));
                        }      
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

            }
        }

        private void GetAndApplyUpdates()
        {
            var lastUpdateTime = FileWriter.GetLastUpdateTime();
            if (lastUpdateTime == null)
                lastUpdateTime = "2016-01-01";


            if(Jira != null)
            {
                try
                {
                    var jqlQuery = string.Format(UpdatedTicketsQuery, lastUpdateTime);
                    var updatedIssues = Jira.GetIssuesFromJql(jqlQuery).ToList();
                    foreach(var issue in updatedIssues.Where(x => x.Type.Name == "Epic"))
                    {
                        var ticketToUpdate = FinalBuilds.Where(x => x.ParentTicket.Key == issue.Key.ToString()).Select(x => x.ParentTicket).FirstOrDefault();
                        if(ticketToUpdate != null)
                        {
                            ticketToUpdate = new ParentTicket(issue);
                        }
                        else
                        {
                            AllParents.Add(new ParentTicket(issue));
                        }
                    }

                    foreach (var issue in updatedIssues.Where(x => x.Type.Name == "Story"))
                    {
                        ChildTicket ticketToUpdate = null;
                        var epicLink = issue["Epic Link"].ToString();
                        var stagesForThisEpic = FinalBuilds.Where(x => x.ParentTicket.Key == epicLink).Select(x => x.StageColors.Values).FirstOrDefault();
                        if (stagesForThisEpic != null)
                            ticketToUpdate = stagesForThisEpic.Where(x => x.Key == issue.Key.ToString()).FirstOrDefault();

                        if (ticketToUpdate != null)
                        {
                            ticketToUpdate = new ChildTicket(issue);
                        }
                    }

                    foreach (var issue in updatedIssues.Where(x => x.Type.IsSubTask))
                    {
                        ChildTicket ticketToUpdate = null;
                        var epicLink = issue["Epic Link"].ToString();
                        var stagesForThisEpic = FinalBuilds.Where(x => x.ParentTicket.Key == epicLink).Select(x => x.StageColors.Values).FirstOrDefault();
                        if (stagesForThisEpic != null)
                            ticketToUpdate = stagesForThisEpic.Where(x => x.Key == issue.ParentIssueKey.ToString()).FirstOrDefault();

                        if (ticketToUpdate != null)
                        {
                            var story = Jira.GetIssue(issue.ParentIssueKey);
                            if(story != null)
                                ticketToUpdate = new ChildTicket(story);
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }


        async Task GetSubTickets()
        {
            if(Jira != null)
            {
                Stopwatch sw = new Stopwatch();

                sw.Start();

                List<ParentTicket> epicKeys = AllParents.Where(p => p.Stories.Count() == 0).Select(p => p).ToList();

                //Set up enumerable of tasks. Each returns a list of issues. 
                IEnumerable<Task<IEnumerable<Issue>>> getStoriesQuery = from parent in epicKeys
                                                                        select Jira.GetIssuesFromJqlAsync(BuildStoryTicketJql(parent), 100, 0, System.Threading.CancellationToken.None);
                
                //ToList starts asynchronous calls for all tasks in enumerable. 
                var getStoriesTasks = getStoriesQuery.ToList();

                //Wait for all to finish. Process each as they finish. 
                while( getStoriesTasks.Count > 0 )
                {
                    //Get Completed task and remove from list. 
                    Task<IEnumerable<Issue>> finishedTask = await Task.WhenAny(getStoriesTasks);
                    getStoriesTasks.Remove(finishedTask);

                    try
                    {
                        var subtickets = await finishedTask;
                        LinkStoriesToBuild(subtickets);
                    }
                    catch(Exception e)
                    {
                        FileWriter.Log("[ERROR] ---- " + e.Message);
                    }

                }


                sw.Stop();
                Debug.WriteLine("Elapsed={0}",sw.Elapsed);


            }
            

        }

        private void LinkStoriesToBuild(IEnumerable<Issue> issues)
        {
            //Get key (epic link) from first entry in enumerable. All are identical based on JQL. 
            var parentKey = issues.Select(i => i["Epic Link"]).FirstOrDefault().ToString();

            //Retrieve objects to update. 
            var parent = AllParents.Where(p => p.Key == parentKey).Select(p => p).First();
            var currentBuildItem = FinalBuilds.Where(p => p.ParentTicket.Key == parentKey).Select(p => p).First();

            foreach (var issue in issues)
            {
                ChildTicket thisChild = new ChildTicket(issue);
                parent.Stories.Add(thisChild);
                if (!currentBuildItem.StageColors.ContainsKey(thisChild.TicketStage))
                    currentBuildItem.StageColors.Add(thisChild.TicketStage, thisChild);
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
                    FileWriter.Log(string.Format("Comment on {0} by {1}", key, Session["username"] != null ? Session["username"].ToString(): "Unknown user"));
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

        private string BuildStoryTicketJql(ParentTicket parentTicket)
        {
            return StoryJQLQueryBase + parentTicket.Key;
        }

        
    }
}
