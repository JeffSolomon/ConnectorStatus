using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ConnectorBuildItem
    {
        public enum SortOrder
        {
            Client
            , Source
            , Default
        }
        public ConnectorBuildItem(ParentTicket ticket)
        {
            ParentTicket = ticket;
            StageColors = new Dictionary<ChildTicket.Stage, ChildTicket>();
        }
        public ParentTicket ParentTicket { get; set; }
        public Dictionary<ChildTicket.Stage, ChildTicket> StageColors;
        public SortOrder sortOrder { get; set; }
    }


}