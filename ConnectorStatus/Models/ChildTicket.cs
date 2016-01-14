using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ChildTicket : JiraTicket
    {
        public SortedList<int, string> Stages = new SortedList<int, string>()
        {
            { 1, "Kick Off"},
            { 2, "Client Access"},
            { 3, "Requirements"},
            { 4, "Environment Build"},
            { 5, "Query Development"},
            { 6, "Code Review"},
            { 7, "Extract and Load"},
            { 8, "Seed Data Prep"},
            { 9, "Clinical Review"},
            { 10, "DQA"},
            { 11, "Verification"},
            { 12, "Deliver to QA"},
            { 13, "SIT Prep"},
            { 14, "SIT Execute"},
            { 15, "UAT Prep"},
            { 16, "UAT Execute"},
            { 17, "Go-Live Approval"},
            { 18, "Go-Live"},
            { 19, "Completed"}
        };

        public enum StatusCode
        {
            BackLog,
            OnHoldExternal,
            OnHoldInternal,
            Open,
            InProgress,
            Complete,
            Closed
        }


        public string TicketStage { get; set; }

        public StatusCode StageScore
        {
            get
            {
                //if (this.Status.ToLower() == "in progress")
                //    return Score.InProgress;
                //else if (this.Assignee == null)
                //    return Score.Unassigned;
                //else if (ConnectorTeamMembers.Contains(this.Assignee))
                //    return Score.InProgress;
                //else
                //    return Score.OnHold;
                foreach(StatusCode status in Enum.GetValues(typeof(StatusCode)))
                {
                    if (this.Status.ToLower().Replace(" ", "").Replace("-","") == status.ToString().ToLower())
                        return status;
                }
                return StatusCode.BackLog;
            }
        } 
                
        public string DisplayColor
        {
            get
            {
                string finalColor;
                if (StageScore == StatusCode.Complete)
                    finalColor = "#1565C0";
                else if (StageScore == StatusCode.Closed)
                    finalColor = "#1565C0";
                else if (StageScore == StatusCode.BackLog)
                    finalColor = "white";
                else if (StageScore == StatusCode.InProgress)
                    finalColor = "#4CAF50";
                else if (StageScore == StatusCode.Open)
                    finalColor = "#4CAF50";
                else
                    finalColor = "#F44336";
                
                return finalColor;
            }
        }
    }
}