using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string NotifyReason { get; set; }
        public DateTimeOffset Created { get; set; }

        //FK's
        public int? TicketId { get; set; }
        public string UserId { get; set; }

        //Nav
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}