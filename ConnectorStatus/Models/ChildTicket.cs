using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;

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
        }

        public string TicketStage { get; set; }

        public BuildProcessConfig.StatusCode StageScore
        {
            get
            {
                foreach(BuildProcessConfig.StatusCode status in Enum.GetValues(typeof(BuildProcessConfig.StatusCode)))
                {
                    if (this.Status.ToLower().Replace(" ", "").Replace("-","") == status.ToString().ToLower())
                        return status;
                }
                return BuildProcessConfig.StatusCode.BackLog;
            }
        } 
                
        public string DisplayColor
        {
            get
            {
                string color = BuildProcessConfig.StageColors[StageScore];

                if (string.IsNullOrEmpty(color))
                    color = BuildProcessConfig.StageColors[BuildProcessConfig.StatusCode.BackLog];

                return color;
            }
        }
    }
}