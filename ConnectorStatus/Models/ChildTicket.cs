using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;
using System.Net;

namespace ConnectorStatus.Models
{
    public class ChildTicket : JiraTicket
    {
        
        public ChildTicket(Issue issue)
        {
            Key = issue.Key.ToString();
            Assignee = issue.Assignee;
            Status = issue.Status.Name;
            Summary = issue.Summary;
            TicketStage = GetCustomField(issue, "Implementation Phase");
            Client = GetCustomField(issue, "Customer Name");
            Source = GetCustomField(issue, "Data Source Name");
            ImplementationRound = GetCustomField(issue, "Implementation Round");
            Description = Status == "Open" || Status == "In Progress" || Status.StartsWith("On Hold") ? GetLatestCommentOrDescription(issue, false) : "";
            WorkLogs = GetWorklogs(issue);
        }

        public ChildTicket() { }

        public string TicketStage { get; set; }

        public int StageScore
        {
            get
            {
                foreach(BuildProcessConfig.StatusCode status in Enum.GetValues(typeof(BuildProcessConfig.StatusCode)))
                {
                    if (this.Status.ToLower().Replace(" ", "").Replace("-","") == status.ToString().ToLower())
                        return (int)status;
                }
                return (int)BuildProcessConfig.StatusCode.BackLog;
            }
        } 
                
        public string DisplayColor
        {
            get
            {
                string color = BuildProcessConfig.StageColors[(BuildProcessConfig.StatusCode)StageScore];

                if (string.IsNullOrEmpty(color))
                    color = BuildProcessConfig.StageColors[BuildProcessConfig.StatusCode.BackLog];

                return color;
            }
        }

        public string ToolTipLabel
        {
            get
            {
                return  TicketStage +
                    "<br><strong>" +
                    Status + "</strong>" +
                    (!string.IsNullOrEmpty(Description) ? "<br><br><p style='text-align:left'><strong>Last Comment:</strong><br><i>" + WebUtility.HtmlEncode(Description) + "</i></p>": "");
            }
        }

        new public string GetLatestCommentOrDescription(Issue issue, bool fallBackToDescription = true)
        {
            var comments = issue.GetComments();
            
            string comment = "";
            if (comments != null && comments.Count > 0)
            {
                var fullComment = comments.OrderByDescending(c => c.CreatedDate).FirstOrDefault();
                comment = fullComment.Body + "<br><br>" + fullComment.Author + " - " + (fullComment.CreatedDate != null ? ((DateTime)fullComment.CreatedDate).ToString("d") : "");
            }
            
            else if (fallBackToDescription)
                comment = issue.Description;

            return comment;
        }
    }
}