﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class JiraTicket
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string TicketType { get; set; }
        public string Assignee { get; set; }
        public string Status { get; set; }

        public string Client { get; set; }
        public string Source { get; set; }
        public DateTime? DueDate { get; set; }
        public static List<string> ConnectorTeamMembers = new List<string>
        {
            "mayur.sampath",
            "romel.gupta",
            "jeff.solomon",
            "joshua.aschheim",
            "oren.shapira",
            "joseph.cho",
            "alexander.dillaire",
            "wendong.tang",
            "kelsey.wade",
            "omar.nema"
        };
    }
}