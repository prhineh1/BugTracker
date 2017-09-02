using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class Ticket 
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }

        [AllowHtml]
        [Required]
        public string Description { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public bool Active { get; set; }

        //FK's
        public int ProjectId { get; set; }
        public int TicketTypeId { get; set; }
        public int TicketPriorityId { get; set; }
        public int TicketStatusId { get; set; }
        public int? ArchiveId { get; set; }
        public string OwnerUserId { get; set; }
        public string AssignedToUserId { get; set; }

        //Nav
        public virtual Project Project { get; set; }
        public virtual Archive Archive { get; set; }
        public virtual TicketType TicketType { get; set; }
        public virtual TicketPriority TicketPriority { get; set; }
        public virtual TicketStatus TicketStatus { get; set; }
        public virtual ApplicationUser OwnerUser { get; set; }
        public virtual ApplicationUser AssignedToUser { get; set; }

        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
        public virtual ICollection<TicketComment> TicketComments { get; set; }
        public virtual ICollection<TicketHistory> TicketHistorys { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        //Con
        public Ticket()
        {
            this.TicketAttachments = new HashSet<TicketAttachment>();
            this.TicketComments = new HashSet<TicketComment>();
            this.TicketHistorys = new HashSet<TicketHistory>();
            this.Notifications = new HashSet<Notification>();
        }

        public Ticket Copy()
        {
            Ticket other = (Ticket)this.MemberwiseClone();
            other.Active = false;
            other.Project = null;
            other.TicketAttachments = new HashSet<TicketAttachment>();
            return other;
        }
    }
}