using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;

namespace ConnectorStatus.Models
{
    public class ParentTicket : JiraTicket
    {
        public ParentTicket()
        {
            Stories = new List<ChildTicket>();
        }

        public ParentTicket(Issue issue)
        {
            Stories = new List<ChildTicket>();

            Key = issue.Key.ToString();
            Assignee = issue.Assignee;
            Status = issue.Status.Name.ToString();
            Summary = issue.Summary;
            Client = GetCustomField(issue, "Customer Name");
            Source = GetCustomField(issue, "Data Source Name");
            Description = GetLatestCommentOrDescription(issue, true);
            DueDate = issue.DueDate;
            ImplementationRound = GetCustomField(issue, "Implementation Round");
            ContractID = GetContractIDFromCascading(issue);
        } 


        public List<ChildTicket> Stories { get; set; }

        public int TotalScore
        {
            get
            {
                var score = (from s in Stories
                             select s.StageScore).Sum(x => (int)x);
                System.Diagnostics.Debug.WriteLine(this.Client + "-" + this.Source + ", Score: " + score);
                return score;        
            }
        }

    }
}