using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;
using System.Text.RegularExpressions;

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
        public DateTime? Updated { get; set; }

        public string ImplementationPhase { get; set; }
        public string ImplementationRound { get; set; }
        public string Description { get; set; }
        public string ContractID { get; set; }

        public List<EffortLog> WorkLogs { get; set; }

        public DateTime? TrueLastUpdate
        {
            get
            {
                List<DateTime?> bothDates = new List<DateTime?>();
                bothDates.Add(Updated);
                bothDates.Add(MostRecentLogDate);
                return bothDates.Max(); 
            }
        }

        public double TotalHours
        {
            get
            {
                if (WorkLogs != null && WorkLogs.Count > 0)
                {
                    var wl = WorkLogs.Select(l => l.Hours).Sum();
                    return wl;
                }
                return 0;
            }
        }

        public double LogDuration
        {
            get
            {
                if (WorkLogs != null && WorkLogs.Count > 0)
                {
                    var maxDate = (DateTime)WorkLogs.Select(x => x.StartDate).Max();
                    var minDate = (DateTime)WorkLogs.Select(x => x.StartDate).Min();
                    return (maxDate - minDate).TotalDays;
                }
                return 0;
            }
        }

        public DateTime? FirstLogDate
        {
            get
            {
                if (WorkLogs != null && WorkLogs.Count > 0)
                    return WorkLogs.Select(w => w.StartDate).Min();

                return null;
            }
        }

        public DateTime? MostRecentLogDate
        {
            get
            {
                if (WorkLogs != null && WorkLogs.Count > 0)
                    return WorkLogs.Select(w => w.StartDate).Max();

                return null;
            }
        }



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
            var returnString = "";
            var maybeNull = issue.CustomFields.GetCascadingSelectField("Customer Contract ID");
            if (maybeNull != null && maybeNull.ChildOption != null)
                returnString = maybeNull.ChildOption.ToString();
            else if (maybeNull != null && maybeNull.ParentOption != null)
            {
                var fullString = maybeNull.ParentOption.ToString();
                if (fullString.Contains(':'))
                    returnString = fullString.Substring(fullString.IndexOf(':') + 1);
                else
                    returnString = fullString;
            }
            return Regex.Match(returnString, @"\d+").Value; ;
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

        public double GetHoursLogged(DateTime? start = null, DateTime? end = null)
        {

            if (start == null)
                start = new DateTime(2015, 1, 1);
            if (end == null)
                end = DateTime.Now;

            if(WorkLogs != null && WorkLogs.Count > 0)
            {
                var wl = WorkLogs.Where(l => (l.StartDate >= start && l.StartDate <= end) || l.StartDate == null).Select(l => l.Hours).Sum();
                return wl;
            }
            return 0;
            
        }

        

        public List<EffortLog> GetWorklogs(Issue issue)
        {
            var workLogs = new List<EffortLog>();

            var rawWorklogs = issue.GetWorklogs().ToList();

            foreach(var wl in rawWorklogs)
            {
                workLogs.Add(
                new EffortLog
                {
                    StartDate = wl.StartDate,
                    TimeSpent = wl.TimeSpent,
                    Hours = wl.TimeSpentInSeconds / 3600.0,
                    User = wl.Author

                });
            }
            return workLogs;
        }

        

        public class EffortLog
        {
            public DateTime? StartDate { get; set; }
            public string TimeSpent { get; set;  }
            public double Hours { get; set;  }
            public string User { get; set; }
        }



    }
}