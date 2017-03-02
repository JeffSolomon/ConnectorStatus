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
using Newtonsoft.Json;

namespace ConnectorStatus.Controllers
{
    public class ConnectorStatusController : Controller
    {
        private List<ParentTicket> AllParents;
        private List<ChildTicket> AllChildren;
        private List<string> ChildrenWithWorkLogged;
        private List<ConnectorBuildItem> FinalBuilds;
        private Jira Jira;

        private static string BaseURL = "https://jira.arcadiasolutions.com/";

        private static string EpicJQLQuery = "project = AAI and type = Epic " + // and status != 190-Completed
                                             "and \"Data Source Name\" is not EMPTY and \"Customer Name\"" +
                                             "is not empty and createdDate >= \"2016-02-03\"";

        private static string WorkLoggedJQLQuery = "project = AAI and type in (Story) and worklogDate >= \"2016-01-01\"";

        private static string StoryJQLQueryBase = "project = AAI and type = Story and \"Customer Name\" " + 
                                                   "is not EMPTY and \"Data Source Name\" is not EMPTY and " + 
                                                   "\"Implementation Round\" is not EMPTY and " +
                                                   "\"Implementation Phase\" is not EMPTY " + 
                                                   "and  \"Epic Link\" = ";

        private static string SubtaskJQLQueryBase = "project = AAI and type = Sub-task and worklogDate >= \"2016-01-01\" and parent = ";

        private static string UpdatedTicketsQuery = "project = AAI and type in(Epic, Story, Sub-task) and " + 
                                                    "\"Data Source Name\" is not EMPTY " +
                                                    "and \"Customer Name\" is not empty and ((updatedDate >= \"{0}\" and type in(Epic, Story)) " + 
                                                    "or worklogDate >= \"{1}\")";

        private static int MaxIssueCount = 100000;
        
        // GET: ConnectorStatus
        public async Task<ActionResult> Index(FormCollection collection)
        {
            string username = null;
            string password = null;
            if (collection != null && collection.Count > 0)
            {
                username = collection.Get("username");
                Session["username"] = username;
                password = collection.Get("password");
            }

            var builds = await PopulateFinalBuilds(username, password);

            if(FinalBuilds != null && FinalBuilds.Count > 0)
            {
                var DisplayBuilds = new List<ConnectorBuildItem>();
                DisplayBuilds = FinalBuilds
                            .Where(p => p.ParentTicket.Stories != null &&
                                        p.ParentTicket.Stories.Count > 0 &&
                                        p.ParentTicket.Status != "190-Completed")
                            .OrderBy(x => x.ParentTicket.Source)
                            .OrderByDescending(x => x.ParentTicket.TotalScore)
                            .OrderBy(x => x.ParentTicket.Client).ToList();
                return View(DisplayBuilds);
            }
            else
                return RedirectToAction("Index", "Home", false);
        }

        [HttpPost]
        public async Task<string> GetBuilds(string username, string password)
        {
            var builds = await PopulateFinalBuilds(username, password);

            if (builds != null && builds.Count > 0)
            {
                var DisplayBuilds = new List<ConnectorBuildItem>();
                DisplayBuilds = builds
                            .Where(p => p.ParentTicket.Stories != null &&
                                        p.ParentTicket.Stories.Count > 0) //&&
                                       // p.ParentTicket.Status != "190-Completed")
                            .OrderBy(x => x.ParentTicket.Source)
                            .OrderByDescending(x => x.ParentTicket.TotalScore)
                            .OrderBy(x => x.ParentTicket.Client).ToList();
                var json = JsonConvert.SerializeObject(DisplayBuilds);
                return json;
            }

            else
                return "";//RedirectToAction("Index", "Home", false);
        }

        private async Task<List<ConnectorBuildItem>> PopulateFinalBuilds(string username, string password)
        {
            
            AllParents = new List<ParentTicket>();
            AllChildren = new List<ChildTicket>();
            ChildrenWithWorkLogged = new List<string>();
            FinalBuilds = new List<ConnectorBuildItem>();

            var currentDateTime = DateTime.Now;
            
            FileWriter.Log("Attempted login by " + (username == "reload" ? Session["user"] : username));
            if (!String.IsNullOrEmpty(password))
            {
                if (InitiateConnection(username, password))
                {
                    FileWriter.Log("Successful login by " + Session["user"]);
                    var fromFile = FileWriter.ReadJsonFile();
                    if (fromFile == null)
                    {
                        Debug.WriteLine(DateTime.Now + " : --- Start getting parents.");
                        GetParents();
                        Debug.WriteLine(DateTime.Now + " : --- Start getting tickets with worklogs.");
                        GetTicketsWithWorkLogs();
                        Debug.WriteLine(DateTime.Now + " : --- Start getting stories.");
                        await GetStories();
                    }
                    else
                    {
                        FinalBuilds = fromFile;
                        foreach (var build in FinalBuilds) //Reload lists.
                        {
                            AllChildren.AddRange(build.StageColors.Select(x => x.Value));
                            AllParents.Add(build.ParentTicket);
                        }
                        GetAndApplyUpdates();
                    }
                    FileWriter.WriteLastUpdateTime(currentDateTime);
                    FileWriter.WriteJsonFile(FinalBuilds);
                }

            }
            else if (Session["SuccessfulLogin"] != null && (bool)Session["SuccessfulLogin"] && FileWriter.ReadJsonFile() != null)
                FinalBuilds = FileWriter.ReadJsonFile();

            return FinalBuilds;
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

        [HttpPost]
        public string SubmitComment(string key, string comment)
        {
            if (key != null && comment != null)
                if (SubmitCommentIfDifferent(key, comment))
                    return "{\"status\": \"success\"}";

            return "{\"status\": \"error\"}";
        }

        private bool InitiateConnection(string userName, string password)
        {
            bool success = true;
            try
            {
                if(userName == "reload" && Session["SuccessfulLogin"] != null && (bool)Session["SuccessfulLogin"])
                {
                    Jira = (Jira)Session["jira"];
                }
                else
                {
                    Jira = Jira.CreateRestClient(BaseURL, userName, password);
                    Jira.MaxIssuesPerRequest = MaxIssueCount;
                    var testQueryResult = Jira.GetIssue("AAI-1");
                    success = testQueryResult != null;
                    Session["jira"] = Jira;
                    Session["user"] = userName;
                }
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

        private void GetTicketsWithWorkLogs()
        {
            if (Jira != null)
            {
                try
                {
                    var issues = Jira.GetIssuesFromJql(WorkLoggedJQLQuery).ToList();

                    foreach (var issue in issues)
                        ChildrenWithWorkLogged.Add(issue.Key.ToString());
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
                lastUpdateTime = "2016-01-01 00:00:00";

            if(Jira != null)
            {
                try
                {
                    
                    var jqlQuery = string.Format(UpdatedTicketsQuery, lastUpdateTime, lastUpdateTime.Substring(0, 10));
                    var updatedIssues = Jira.GetIssuesFromJql(jqlQuery).ToList();
                    FileWriter.Log("Got " + updatedIssues.Count + " tickets updated since " + lastUpdateTime);
                    //Update Epics within FinalBuilds.
                    foreach (var issue in updatedIssues.Where(x => x.Type.Name == "Epic"))
                    {
                        var newEpic = new ParentTicket(issue);
                        bool update = false;
                        for (int i = 0; i < FinalBuilds.Count; i++)
                        {
                            if(FinalBuilds[i].ParentTicket.Key == newEpic.Key)
                            {
                                newEpic.Stories = FinalBuilds[i].ParentTicket.Stories;
                                FinalBuilds[i].ParentTicket = newEpic;
                                update = true;
                            }
                        }

                        if (!update) //Insert new CBI
                        {
                            FinalBuilds.Add(new ConnectorBuildItem(newEpic));
                        }
                    }

                    //Update Stories
                    foreach (var issue in updatedIssues.Where(x => x.Type.Name == "Story"))
                    {
                        var newStory = new ChildTicket(issue, true);
                        bool update = false;

                        for (int i = 0; i < FinalBuilds.Count; i ++)
                            if(FinalBuilds[i].ParentTicket.Key == newStory.EpicLink)
                            {
                                for( int j = 0; j < FinalBuilds[i].ParentTicket.Stories.Count; j++)
                                    if(FinalBuilds[i].ParentTicket.Stories[j].Key == newStory.Key)
                                    {
                                        FinalBuilds[i].ParentTicket.Stories[j] = newStory;
                                        update = true;
                                    }

                                if(!update)
                                    FinalBuilds[i].ParentTicket.Stories.Add(newStory);
                            }
                    }

                    //Update story sub-tickets
                    foreach (var issue in updatedIssues.Where(x => x.Type.IsSubTask))
                    {
                        var child = new ChildTicket(issue, true);
                        var parent = AllChildren.Where(x => x.Key == issue.ParentIssueKey).FirstOrDefault();
                        bool update = false;
                        for(int i = 0; i < FinalBuilds.Count; i++)
                            if(FinalBuilds[i].ParentTicket.Key == parent.EpicLink)
                                for(int j = 0; j < FinalBuilds[i].ParentTicket.Stories.Count; j++)
                                    if(FinalBuilds[i].ParentTicket.Stories[j].Key == parent.Key)
                                    {
                                        for (int k = 0; k < FinalBuilds[i].ParentTicket.Stories[j].SubTickets.Count; k++)
                                            if(FinalBuilds[i].ParentTicket.Stories[j].SubTickets[k].Key == child.Key)
                                            {
                                                FinalBuilds[i].ParentTicket.Stories[j].SubTickets[k] = child;
                                                update = true;
                                            }

                                        if(!update)
                                            FinalBuilds[i].ParentTicket.Stories[j].SubTickets.Add(child);
                                    }
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        private async Task GetStories()
        {
            if(Jira != null)
            {
                List<ParentTicket> epicKeys = AllParents.Where(p => p.Stories.Count() == 0).Select(p => p).ToList();

                //Set up enumerable of tasks. Each returns a list of issues. 
                IEnumerable<Task<List<ChildTicket>>> getStoriesQuery = from parent in epicKeys
                                                                        select GetChildrenAndSubTicketsAsync(parent);//Jira.GetIssuesFromJqlAsync(BuildStoryTicketJql(parent), 100, 0, System.Threading.CancellationToken.None);
                
                //ToList starts asynchronous calls for all tasks in enumerable. 
                var getStoriesTasks = getStoriesQuery.ToList();

                //Wait for all to finish. Process each as they finish. 
                while( getStoriesTasks.Count > 0 )
                {
                    //Get Completed task and remove from list. 
                    var finishedTask = await Task.WhenAny(getStoriesTasks);
                    getStoriesTasks.Remove(finishedTask);

                    try
                    {
                        var subtickets = await finishedTask;
                        LinkStoriesToBuild(subtickets);
                        Debug.WriteLine(DateTime.Now + " : --- Start getting subtickets for: " + subtickets.FirstOrDefault().Summary);
                        subtickets = await GetSubtickets(subtickets);
                        if (subtickets.Count > 0)
                            Debug.WriteLine("\t\t Got subtickets for: " + subtickets.FirstOrDefault().Summary);
                        else
                            Debug.WriteLine("No subtickets found");


                    }
                    catch(Exception e)
                    {
                        FileWriter.Log("[ERROR] ---- " + e.Message);
                        FileWriter.Log("[ERROR] ---- " + e.StackTrace);
                    }
                }
            }
        }


        private async Task<List<ChildTicket>> GetChildrenAndSubTicketsAsync(ParentTicket parent)
        {
            var ticketList = new List<ChildTicket>();
            var test = BuildStoryTicketJql(parent);
            var maxRetries = 10;
            int retryCount = 0;
            for (;;)
            {            
                try
                {
                    var stories = await Jira.GetIssuesFromJqlAsync(BuildStoryTicketJql(parent), 100, 0, System.Threading.CancellationToken.None);

                    foreach (var story in stories)
                        ticketList.Add(new ChildTicket(story, ChildrenWithWorkLogged.Contains(story.Key.ToString())));

                    Debug.WriteLine(DateTime.Now + " : --- Got " + stories.Count() + " stories for: " + parent.Summary);

                    if (retryCount > 0)
                        Debug.WriteLine("*******Retry number {0} succeeded for {1}", retryCount, parent.Summary);

                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);

                    Debug.WriteLine("*******Initiate retry number {0} for {1}", retryCount, parent.Summary);

                    retryCount++;
                    if(retryCount >= maxRetries)
                    {
                        Debug.WriteLine("*******Above retry limit to get stories for {1}", retryCount, parent.Summary);
                    }
                }
            }
            return ticketList;
        }


        private async Task<List<ChildTicket>> GetSubtickets(List<ChildTicket> stories)
        {
            var ticketList = new List<ChildTicket>();
            if (Jira != null)
            {

                //Set up enumerable of tasks. Each returns a list of issues. 
                IEnumerable<Task<IEnumerable<Issue>>> getStoriesQuery = from story in stories
                                                                        select Jira.GetIssuesFromJqlAsync(BuildSubTicketJql(story), 100, 0, System.Threading.CancellationToken.None);

                //ToList starts asynchronous calls for all tasks in enumerable. 
                var getSubTasksTasks = getStoriesQuery.ToList();

                //Wait for all to finish. Process each as they finish. 
                while (getSubTasksTasks.Count > 0)
                {
                    //Get Completed task and remove from list. 
                    Task<IEnumerable<Issue>> finishedTask = await Task.WhenAny(getSubTasksTasks);
                    getSubTasksTasks.Remove(finishedTask);

                    try
                    {
                        var subtickets = await finishedTask;
                        
                        var parentKey = subtickets.Select(x => x.ParentIssueKey).FirstOrDefault();
                        var storyToAddTo = stories.Where(x => x.Key == parentKey).FirstOrDefault();
                        if (storyToAddTo != null)
                            foreach (var subticket in subtickets)
                            {
                                var subticketChild = new ChildTicket(subticket, true);//JQL Limits tickets to only those with work logs. 
                                storyToAddTo.SubTickets.Add(subticketChild);
                                storyToAddTo.WorkLogs.AddRange(subticketChild.WorkLogs);
                            }

                    }
                    catch (Exception e)
                    {
                        FileWriter.Log("[ERROR] ---- " + e.Message);
                    }
                }
            }
            return ticketList;
        }

        private void LinkStoriesToBuild(List<ChildTicket> stories)
        {
            //Get key (epic link) from first entry in enumerable. All are identical based on JQL. 
            var epicLink = stories.Select(i => i.EpicLink).FirstOrDefault().ToString();

            //Retrieve objects to update. 
            var parent = AllParents.Where(p => p.Key == epicLink).Select(p => p).First();
            var currentBuildItem = FinalBuilds.Where(p => p.ParentTicket.Key == epicLink).Select(p => p).First();

            foreach (var story in stories)
                parent.Stories.Add(story);
            
            Debug.WriteLine(DateTime.Now + "--------------- " + FinalBuilds.Where(x => x.StageColors  != null && x.StageColors.Count > 0).Count() + " EPICS PROCESSED -----------");
        }

        private bool SubmitCommentIfDifferent(string key, string comment)
        {
            Jira = Session["jira"] as Jira;
            List<ConnectorBuildItem> builds;
            if (Jira != null && Session["SuccessfulLogin"] != null && (bool)Session["SuccessfulLogin"] && (builds = FileWriter.ReadJsonFile()) != null)
            {
                var issue = Jira.GetIssue(key);
                FinalBuilds = builds;
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
                        FileWriter.WriteJsonFile(FinalBuilds);
                        return true;
                    }
                }
            }
            return false;
        }        

        private string BuildStoryTicketJql(ParentTicket parentTicket)
        {
            return StoryJQLQueryBase + parentTicket.Key;
        }

        private string BuildSubTicketJql(ChildTicket childTicket)
        {
            return SubtaskJQLQueryBase + childTicket.Key;
        }


    }
}
