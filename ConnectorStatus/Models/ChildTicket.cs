using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ChildTicket : JiraTicket
    {
        

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