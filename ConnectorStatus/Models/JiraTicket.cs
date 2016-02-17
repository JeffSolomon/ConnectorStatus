using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;

namespace ConnectorStatus.Models
{
    public class JiraTicket
    {
        //Properties
        public string Key { get; set; }
        public string Summary { get; set; }
        public string TicketType { get; set; }
        public string Assignee { get; set; }
        public string Status { get; set; }

        public string Client { get; set; }
        public string Source { get; set; }
        public DateTime? DueDate { get; set; }

        public string ImplementationPhase { get; set; }
        public string ImplementationRound { get; set; }
        public string Description { get; set; }
        public string ContractID { get; set; }


        //Methods
        public string GetCustomField(Issue issue, string fieldName)
        {
            var maybeNull = issue[fieldName];
            return maybeNull != null ? maybeNull.ToString() : "";
        }

        public string GetCustomFieldByID(Issue issue, string ID)
        {
            try
            {
                var customFieldID = "customfield_" + ID;
                var maybeNull = issue.CustomFields.Where(c => c.Id == customFieldID);
                return maybeNull != null ? maybeNull.FirstOrDefault().Values.FirstOrDefault().ToString() : "";
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return "";
            }

        }

        public string GetContractIDFromCascading(Issue issue)
        {
            var maybeNull = issue.CustomFields.GetCascadingSelectField("Customer Contract ID");
            if (maybeNull != null && maybeNull.ChildOption != null)
                return maybeNull.ChildOption.ToString();
            else if (maybeNull != null && maybeNull.ParentOption != null)
            {
                var fullString = maybeNull.ParentOption.ToString();
                if (fullString.Contains(':'))
                    return fullString.Substring(fullString.IndexOf(':') + 1);
                else return fullString;
            }
            return "";
        }

        public string GetLatestCommentOrDescription(Issue issue, bool fallBackToDescription = true)
        {
            var comments = issue.GetComments();
            string comment = "";
            if (comments != null && comments.Count > 0)
                comment = comments.OrderByDescending(c => c.CreatedDate).FirstOrDefault().Body;
            else if(fallBackToDescription)
                comment = issue.Description;

            return comment;
        }
    }
}