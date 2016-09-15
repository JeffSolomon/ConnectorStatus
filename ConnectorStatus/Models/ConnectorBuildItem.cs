using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ConnectorBuildItem
    {

        public ConnectorBuildItem(ParentTicket ticket)
        {
            ParentTicket = ticket;
            //StageColors = new Dictionary<string, ChildTicket>();
        }
        public ParentTicket ParentTicket { get; set; }
        public Dictionary<string, ChildTicket> StageColors
        {
            get
            {
                var sc = new Dictionary<string, ChildTicket>();
                foreach(var ticket in ParentTicket.Stories)
                {
                    if(!sc.ContainsKey(ticket.TicketStage))
                        sc.Add(ticket.TicketStage, ticket);
                }
                return sc;
            }
            set
            {
                //StageColors = value;
            }
        }
        public string trClass
        {
            get
            {
                return ParentTicket.Client
                    + (ConnectorBuildInProgress ? " build-active" : " build-inactive")
                    + (AnythingInProgress ? " has-inprogress" : " nothing-inprogress")
                    + (AnythingOnHold ? " has-onhold": " nothing-onhold")
                    + (InDevelopment ? " in-dev": " not-in-dev");
            }
        }

        public int CompletenessScore{ get{ return StageColors.Values.Sum(x => x.StageScore); }}

        public double TotalHours
        {
            get
            {
                var hoursFromStages = ParentTicket.Stories.Select(s => s.TotalHours).Sum();
                return hoursFromStages + ParentTicket.TotalHours;
            }
        }

        public DateTime? FirstLogDate
        {
            get
            {
                var fromStages = ParentTicket.Stories.Where(s => s.FirstLogDate != null).Select(s => s.FirstLogDate).Min();

                if (fromStages != null)
                {
                    if (ParentTicket.FirstLogDate == null || fromStages < ParentTicket.FirstLogDate)
                        return fromStages;
                }

                return ParentTicket.FirstLogDate;

            }
        }

        public DateTime? MostRecentLogDate
        {
            get
            {
                var fromStages = ParentTicket.Stories.Where(s => s.MostRecentLogDate != null).Select(s => s.MostRecentLogDate).Max();

                if (fromStages != null)
                {
                    if (ParentTicket.MostRecentLogDate == null || fromStages > ParentTicket.MostRecentLogDate)
                        return fromStages;
                }

                return ParentTicket.MostRecentLogDate;

            }
        }

        public bool ConnectorBuildInProgress
        {
            get
            {
                var deliverToQAStage = BuildProcessConfig.Stages.Where(x => x.Value == "Deliver to QA").Select(x => x.Key).FirstOrDefault();
                List<int> scores = new List<int>();
                foreach(var stage in BuildProcessConfig.Stages.Where(x => x.Key <= deliverToQAStage))
                {
                    try
                    {
                        int stageScore = StageColors.Where(x => x.Key == stage.Value).Select(x => x.Value).FirstOrDefault().StageScore;
                        scores.Add(stageScore);
                    } catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                    
                }

                var averageScore = scores.Average(x => x);

                return averageScore > (int)BuildProcessConfig.StatusCode.BackLog //At least one ticket is open. 
                    && averageScore < (int)BuildProcessConfig.StatusCode.Closed; //Not all tickets up through Deliver to QA are closed. 

            }
        }
        public bool AnythingInProgress
        {
            get
            {
                var inProgressTickets = StageColors.Where(x => x.Value.StageScore == (int)BuildProcessConfig.StatusCode.InProgress);
                return inProgressTickets != null && inProgressTickets.Count() > 0;
            }
        }
        public bool AnythingOnHold
        {
            get
            {
                var onHoldTickets = StageColors.Where(x => x.Value.StageScore == (int)BuildProcessConfig.StatusCode.OnHoldExternal || x.Value.StageScore == (int)BuildProcessConfig.StatusCode.OnHoldInternal);
                return onHoldTickets != null && onHoldTickets.Count() > 0;
            }
        }

        public bool InDevelopment
        {
            get
            {
                var deliverToQAStage = BuildProcessConfig.Stages.Where(x => x.Value == "Deliver to QA").Select(x => x.Key).FirstOrDefault();
                var ETLStage = BuildProcessConfig.Stages.Where(x => x.Value == "Extract and Load").Select(x => x.Key).FirstOrDefault();

                foreach (var stage in BuildProcessConfig.Stages.Where(x => x.Key <= deliverToQAStage && x.Key >= ETLStage))
                {
                    int stageScore = StageColors.Where(x => x.Key == stage.Value).Select(x => x.Value).FirstOrDefault().StageScore;
                    if (stageScore == (int)BuildProcessConfig.StatusCode.Open || stageScore == (int)BuildProcessConfig.StatusCode.InProgress)
                        return true;
                }

                return false;

            }
        }


    }


}