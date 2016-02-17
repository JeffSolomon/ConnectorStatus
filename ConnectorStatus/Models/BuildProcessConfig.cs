using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class BuildProcessConfig
    {
        public static SortedList<int, string> Stages = new SortedList<int, string>()
        {
            { 1, "Kick off"},
            { 2, "Client Access"},
            { 3, "Requirements"},
            { 4, "Environment Build"},
            { 5, "Extract and Load"},
            { 6, "Query Development"},
            { 7, "Code Review"},  
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
           // { 19, "Completed"}
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

        public static Dictionary<StatusCode, string> StageColors = new Dictionary<StatusCode, string>()
        {
            { StatusCode.BackLog, "white" },
            { StatusCode.OnHoldExternal, "rgba(228,65,69,1)" },
            { StatusCode.OnHoldInternal, "rgba(228,65,69,.8)" },
            { StatusCode.Open, "rgba(123,193,67,.4)" },
            { StatusCode.InProgress, "rgba(123,193,67,1)" },
            { StatusCode.Complete, "rgba(27,117,188,.4)" },
            { StatusCode.Closed, "rgba(27,117,188,1)" }
        };

    }
}