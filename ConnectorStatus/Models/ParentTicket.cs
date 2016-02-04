using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConnectorStatus.Models
{
    public class ParentTicket : JiraTicket
    {
        public ParentTicket()
        {
            Stories = new List<ChildTicket>();
        }
        public List<ChildTicket> Stories { get; set; }

        public string Description { get; set; }

        public int TotalScore
        {
            get
            {
                var score = (from s in Stories
                             select s.StageScore).Sum(x => (int)x);
                System.Diagnostics.Debug.WriteLine(this.Client + "-" + this.Source + ", Score: " + score);
                return score;        
            }
        }
    }
}