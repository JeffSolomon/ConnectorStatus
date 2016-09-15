using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Atlassian.Jira;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ConnectorStatus.Models
{
    public class ChildTicket : JiraTicket
    {
        
        public ChildTicket(Issue issue, bool getWorkLogs)
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
            if (getWorkLogs)
                WorkLogs = GetWorklogs(issue);
            else
                WorkLogs = new List<EffortLog>();

            SubTickets = new List<ChildTicket>();
            EpicLink = GetCustomField(issue, "Epic Link");
            Updated = issue.Updated;

        }

        public ChildTicket()
        {
            SubTickets = new List<ChildTicket>();
            WorkLogs = new List<EffortLog>();
        }

        public string TicketStage { get; set; }

        public string EpicLink { get; set; }

        public List<ChildTicket> SubTickets { get; set; }
        
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
                var baseString = "{0}"; //Ticket Stage
                baseString += "<br><b>{1}</b><br><br>"; //Status
                baseString += @"<table>
                                    <tbody>
                                    <tr>
                                        <td style='padding:2px;'><b>Start Date</b></td>
                                        <td style='padding:2px;'>{2}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:2px;'><b>Last Update</b></td>
                                        <td style='padding:2px;'>{3}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:2px;'><b>Hours</b></td>
                                        <td style='padding:2px;'>{4}</td>
                                    </tr>
                                    </tbody>
                                </table>
                                {5}";


                string final = "";
                try
                {
                    final = string.Format(baseString,
                                                            TicketStage,
                                                            Status,
                                                            (FirstLogDate == null ? "--" : ((DateTime)FirstLogDate).ToString("MM/dd/yyyy")),
                                                            (TrueLastUpdate == null ? "--" : ((DateTime)TrueLastUpdate).ToString("MM/dd/yyyy")),
                                                            TotalHours.ToString("F"),
                                                            (!string.IsNullOrEmpty(Description) ? "<p style='text-align:left'><b>Last Comment:</b><br><i>" + WebUtility.HtmlEncode(ScrubComment(Description)) + "</i></p>" : "")
                                                            );
                } catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                
                return final;
                //    return  TicketStage +
            //        "<br><b>" +
            //        Status + "</b>" +
            //        "<p style='text-align:left'><b>Work Logs</b><br>" + TotalHours + " Hours <br>" + (FirstLogDate == null ? "?" : ((DateTime)FirstLogDate).ToString("MM/dd/yyyy"))+ " - " +
            //        (MostRecentLogDate == null ? "?" : ((DateTime)MostRecentLogDate).ToString("MM/dd/yyyy")) + "</p>" +
            //        (!string.IsNullOrEmpty(Description) ? "<br><p style='text-align:left'><strong>Last Comment:</strong><br><i>" + WebUtility.HtmlEncode(ScrubComment(Description)) + "</i></p>": "");
            }
        }

        new public string GetLatestCommentOrDescription(Issue issue, bool fallBackToDescription = true)
        {
            var comments = issue.GetComments();
            
            string comment = "";
            if (comments != null && comments.Count > 0)
            {
                var fullComment = comments.OrderByDescending(c => c.CreatedDate).FirstOrDefault();
                comment = fullComment.Body + "<br><br>" + ScrubJiraUsername(fullComment.Author) + " - " + (fullComment.CreatedDate != null ? ((DateTime)fullComment.CreatedDate).ToString("d") : "");
            }
            
            else if (fallBackToDescription)
                comment = issue.Description;

            return comment;
        }

        private string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        private string ScrubComment(string comment)
        {
            Regex rgx = new Regex(@"\[~(.*?)\]");
            var final = comment;
            foreach(Match match in rgx.Matches(comment))
            {
                var value = match.Groups[1].Value;
                final = final.Replace("[~" + value + "]", ScrubJiraUsername(value));
            }
            return final;
        }

        private string ScrubJiraUsername(string un)
        {
            var names = un.Split('.');
            StringBuilder sb = new StringBuilder("<b>");
            foreach (var name in names)
            {
                sb.Append(UppercaseFirst(name) + " ");
            }
            sb.Append("</b>");
            return sb.ToString();
        }

        
    }
}