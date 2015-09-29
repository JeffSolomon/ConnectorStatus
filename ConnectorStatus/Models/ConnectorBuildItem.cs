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
            StageColors = new Dictionary<ChildTicket.Stage, ChildTicket>();
        }
        public ParentTicket ParentTicket { get; set; }
        public Dictionary<ChildTicket.Stage, ChildTicket> StageColors;
    }
}