using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ChildTicket : JiraTicket
    {
        public enum Stage
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J
        }

        public enum Score
        {
            Unassigned,
            OnHold,
            InProgress,
            Complete
        }

        public Dictionary<Stage, string> StageNames = new Dictionary<Stage, string>
        {
            { Stage.A, "Receive Build Request" },
            { Stage.B, "Define Architecture" },
            { Stage.C, "Establish Project" },
            { Stage.D, "Prepare Infrastructure" },
            { Stage.E, "Perform Walkthrough" },
            { Stage.F, "Create Queries" },
            { Stage.G, "Review Code" },
            { Stage.H, "Implement Connector" },
            { Stage.I, "Test Connector" },
            { Stage.J, "Handoff to QA" }
        };

        public Stage TicketStage { get; set; }
        public Score StageScore
        {
            get
            {
                if (this.Status.ToLower() == "closed")
                    return Score.Complete;
                else if (this.Assignee == null)
                    return Score.Unassigned;
                else if (ConnectorTeamMembers.Contains(this.Assignee))
                    return Score.InProgress;
                else
                    return Score.OnHold;
            }
        } 
                
        public string DisplayColor
        {
            get
            {
                string finalColor = "green";
                if (StageScore == Score.Complete)
                    finalColor = "#1565C0";
                else if (StageScore == Score.Unassigned)
                    finalColor = "white";
                else if (StageScore == Score.InProgress)
                    finalColor = "#4CAF50";
                else
                    finalColor = "#F44336";
                
                return finalColor;
            }
        }
    }
}