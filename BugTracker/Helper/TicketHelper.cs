using BugTracker.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BugTracker.Helper
{
    public class TicketHelper
    {
       private ApplicationDbContext db = new ApplicationDbContext();

        public ICollection<Ticket> GetUserOwnedTickets(string userId)
        {
            return db.Tickets.Where(t => t.OwnerUserId == userId && t.Active == true).ToList();
        }

        public ICollection<Ticket> GetAssignedTickets(string userId)
        {
            return db.Tickets.Where(t => t.AssignedToUserId == userId && t.Active == true).ToList();
        }

        public async Task UnassignTicketProjects(int ticketId, string userId, string userHistoryId)
        {
            var oldTicket = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticketId);
            var ticketDelta = new TicketDelta();

            Ticket tick = db.Tickets.Find(ticketId);
            tick.AssignedToUser = null;
            tick.AssignedToUserId = null;
            tick.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned");
            db.Entry(tick).State = EntityState.Modified;
            db.SaveChanges();

            var newTicket = db.Tickets.FirstOrDefault(t => t.Id == ticketId);
            await ticketDelta.objectCompare(oldTicket, newTicket, userHistoryId);
        }

        public void UnassignTicketRoles(int ticketId, string userId)
        {
            Ticket tick = db.Tickets.Find(ticketId);
            tick.AssignedToUser = null;
            tick.AssignedToUserId = null;
            tick.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned");
            db.Entry(tick).State = EntityState.Modified;
            db.SaveChanges();
        }

        public List<Ticket> openTickets()
        {
            return db.Tickets.Where(t => t.TicketStatus.Name == "Open/Unassigned" && t.Active == true).ToList();
        }

        public int getTicketsByType(ICollection<Ticket> tickets, string type)
        {
            return tickets.Where(t => t.Active == true).Select(t => t.TicketType).Where(t => t.Name == type).Count();
        }

        public int getTicketsByPriority(ICollection<Ticket> tickets, string priority)
        {
            return tickets.Where(t => t.Active == true).Select(t => t.TicketPriority).Where(t => t.Name == priority).Count();
        }
    }
}