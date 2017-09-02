using BugTracker.Migrations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class Project 
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string ProjectManager { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public bool Active { get; set; }

        //Fk
        public int? ArchiveId { get; set; }

        //Nav
        public virtual ICollection<Ticket> Tickets { get; set; }
        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual Archive Archive { get; set; }

        //Con
        public Project()
        {
            this.Tickets = new HashSet<Ticket>();
            this.Users = new HashSet<ApplicationUser>();
        }

        public Project Copy()
        {
            Project other = (Project)this.MemberwiseClone();
            other.Tickets = new HashSet<Ticket>();
            other.Users = new HashSet<ApplicationUser>();
            other.Active = false;
            return other;
        }
    }
}