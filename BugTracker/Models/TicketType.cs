using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class TicketType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        //Nav
        public virtual ICollection<Ticket> Tickets { get; set; }

        //Con
        public TicketType()
        {
            this.Tickets = new HashSet<Ticket>();
        }
    }
}