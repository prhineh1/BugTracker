using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        [AllowHtml]
        public string Comment { get; set; }
        public DateTimeOffset Created { get; set; }

        //FK's
        public int TicketId { get; set; }
        public string UserId { get; set; }

        //Nav
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}