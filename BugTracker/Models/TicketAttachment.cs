using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }

        public string MediaUrl { get; set; }

        [AllowHtml]
        public string Description { get; set; }
        public DateTimeOffset Created { get; set; }

        //FK's
        public int TicketId { get; set; }
        public string UserId { get; set; }

        //Nav
        public virtual Ticket Ticket { get; set; }
        public virtual ApplicationUser User { get; set; }

        public TicketAttachment Copy()
        {
            TicketAttachment other = (TicketAttachment)this.MemberwiseClone();
            return other;
        }
    }
}