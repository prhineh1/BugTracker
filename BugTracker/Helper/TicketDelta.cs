using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using BugTracker.Models;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Security.Policy;
using BugTracker.Helpers;

namespace BugTracker.Helper
{
    public class TicketDelta
    {
        private UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesHelper roleHelper = new UserRolesHelper();
        private ProjectsHelper projectHelper = new ProjectsHelper();
        private EmailHelper EmailHelper = new EmailHelper();
        private string userId = HttpContext.Current.User.Identity.GetUserId();

        public async Task objectCompare(Ticket oldTicket, Ticket newTicket, string userId)
        {

            if (oldTicket.AssignedToUserId != newTicket.AssignedToUserId)
            {
                createHistory(oldTicket, newTicket, "AssignedToUserId", userId);
                await createNotification(oldTicket, newTicket, "AssignedToUserId");
            }
            if (oldTicket.Description != newTicket.Description)
            {
                createHistory(oldTicket, newTicket, "Description", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Description");
                }
            }
            if (oldTicket.TicketPriorityId != newTicket.TicketPriorityId)
            {
                createHistory(oldTicket, newTicket, "TicketPriorityId", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "TicketPriorityId");
                }
            }
            if (oldTicket.TicketStatusId != newTicket.TicketStatusId)
            {
                createHistory(oldTicket, newTicket, "TicketStatusId", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "TicketStatusId");
                }
            }
            if (oldTicket.TicketTypeId != newTicket.TicketTypeId)
            {
                createHistory(oldTicket, newTicket, "TicketTypeId", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "TicketTypeId");
                }
            }
            if (oldTicket.Title != newTicket.Title)
            {
                createHistory(oldTicket, newTicket, "Title", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Title");
                }
            }
        }

        public void AttachmentCompare(Ticket oldTicket, Ticket newTicket, string userId)
        {
            if (oldTicket.TicketAttachments.Count() > newTicket.TicketAttachments.Count())
            {
                createHistory(oldTicket, newTicket, "Ticket Attachments", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Ticket Attachments");
                }
            }
            if (oldTicket.TicketAttachments.Count() < newTicket.TicketAttachments.Count())
            {
                createHistory(oldTicket, newTicket, "Ticket Attachments", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Ticket Attachments");
                }
            }
        }

        public void CommentCompare(Ticket oldTicket, Ticket newTicket, string userId)
        {
            if (oldTicket.TicketComments.Count() > newTicket.TicketComments.Count())
            {
                createHistory(oldTicket, newTicket, "Ticket Comments", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Ticket Comments");
                }
            }
            if (oldTicket.TicketComments.Count() < newTicket.TicketComments.Count())
            {
                createHistory(oldTicket, newTicket, "Ticket Comments", userId);
                if (!(roleHelper.IsUserInRole(userId, "Developer")))
                {
                     createNotification(oldTicket, newTicket, "Ticket Comments");
                }
            }
        }

        private void createHistory(Ticket oldTicket, Ticket newTicket, string property, string userId)
        {
            if (property == "AssignedToUserId")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Assigned developer";
                if (oldTicket.AssignedToUserId != null)
                {
                    ticketHistory.OldValue = oldTicket.AssignedToUser.FullName;
                }
                else
                {
                    ticketHistory.OldValue = "not assigned";
                } 
                if (newTicket.AssignedToUserId != null)
                {
                    ticketHistory.NewValue = db.Users.FirstOrDefault(u => u.Id == newTicket.AssignedToUserId).FullName;
                }
                else
                {
                    ticketHistory.NewValue = "not assigned";
                }
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "Description")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Description";
                if (oldTicket.Description != null)
                {
                    ticketHistory.OldValue = oldTicket.Description;
                }
                else
                {
                    ticketHistory.OldValue = "no description";
                }
                ticketHistory.NewValue = newTicket.Description;
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "TicketPriorityId")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Ticket Priority";
                if (oldTicket.TicketPriority != null)
                {
                    ticketHistory.OldValue = oldTicket.TicketPriority.Name;
                }
                ticketHistory.NewValue = db.TicketPrioritys.FirstOrDefault(t => t.Id == newTicket.TicketPriorityId).Name;
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "TicketStatusId")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Ticket Status";
                if (oldTicket.TicketStatus != null)
                {
                    ticketHistory.OldValue = oldTicket.TicketStatus.Name;
                }
                ticketHistory.NewValue = db.TicketStatuses.FirstOrDefault(t => t.Id == newTicket.TicketStatusId).Name;
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "TicketTypeId")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Ticket Type";
                if (oldTicket.TicketType != null)
                {
                    ticketHistory.OldValue = oldTicket.TicketType.Name;
                }
                ticketHistory.NewValue = db.TicketTypes.FirstOrDefault(t => t.Id == newTicket.TicketTypeId).Name;
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "Title")
            {
                var ticketHistory = new TicketHistory();
                ticketHistory.Property = "Title";
                if (oldTicket.AssignedToUserId != null)
                {
                    ticketHistory.OldValue = oldTicket.Title;
                }
                else
                {
                    ticketHistory.OldValue = "no title";
                }
                ticketHistory.NewValue = newTicket.Title;
                ticketHistory.UserId = userId;
                ticketHistory.TicketId = newTicket.Id;
                ticketHistory.ChangedDt = DateTimeOffset.Now;
                db.TicketHistorys.Add(ticketHistory);
            }
            if (property == "Ticket Attachments")
            {
                if (oldTicket.TicketAttachments.Count() > newTicket.TicketAttachments.Count())
                {
                    var ticketHistory = new TicketHistory();
                    ticketHistory.Property = property;
                    ticketHistory.OldValue = "An attachment was removed";
                    ticketHistory.NewValue = "N/A";
                    ticketHistory.ChangedDt = DateTimeOffset.Now;
                    ticketHistory.UserId = userId;
                    ticketHistory.TicketId = newTicket.Id;
                    db.TicketHistorys.Add(ticketHistory);
                }
                if (oldTicket.TicketAttachments.Count() < newTicket.TicketAttachments.Count())
                {
                    var ticketHistory = new TicketHistory();
                    ticketHistory.Property = property;
                    ticketHistory.OldValue = "An attachment was added";
                    ticketHistory.NewValue = "N/A";
                    ticketHistory.ChangedDt = DateTimeOffset.Now;
                    ticketHistory.UserId = userId;
                    ticketHistory.TicketId = newTicket.Id;
                    db.TicketHistorys.Add(ticketHistory);
                }
            }
            if (property == "Ticket Comments")
            {
                if (oldTicket.TicketComments.Count() > newTicket.TicketComments.Count())
                {
                    var ticketHistory = new TicketHistory();
                    ticketHistory.Property = property;
                    ticketHistory.OldValue = "A comment was removed";
                    ticketHistory.NewValue = "N/A";
                    ticketHistory.ChangedDt = DateTimeOffset.Now;
                    ticketHistory.UserId = userId;
                    ticketHistory.TicketId = newTicket.Id;
                    db.TicketHistorys.Add(ticketHistory);
                }
                if (oldTicket.TicketComments.Count() < newTicket.TicketComments.Count())
                {
                    var ticketHistory = new TicketHistory();
                    ticketHistory.Property = property;
                    ticketHistory.OldValue = "A comment was added";
                    ticketHistory.NewValue = "N/A";
                    ticketHistory.ChangedDt = DateTimeOffset.Now;
                    ticketHistory.UserId = userId;
                    ticketHistory.TicketId = newTicket.Id;
                    db.TicketHistorys.Add(ticketHistory);
                }
            }

            db.SaveChanges();
        }

        private async Task createNotification(Ticket oldTicket, Ticket newTicket, string property)
        {
            if (property == "AssignedToUserId")
            {
                if (oldTicket.AssignedToUserId == null)
                {
                    var ticketNotification = new Notification();
                    ticketNotification.TicketId = oldTicket.Id;
                    ticketNotification.UserId = newTicket.AssignedToUserId;
                    ticketNotification.Created = DateTimeOffset.Now;
                    ticketNotification.NotifyReason = "You have been assigned " + newTicket.Title;
                    db.Notifications.Add(ticketNotification);

                    var emailNotificaiton = new NotifcationMessage();
                    emailNotificaiton.Body = ticketNotification.NotifyReason;
                    emailNotificaiton.SourceId = userId;
                    emailNotificaiton.SourceName = db.Users.Find(userId).FullName;
                    emailNotificaiton.Subject = "Ticket Notification";
                    emailNotificaiton.DestinationEmail = db.Users.Find(newTicket.AssignedToUserId).Email;

                    await EmailHelper.SendNotificationEmailAsync(emailNotificaiton);

                    //notification for the submitter
                    var ticketNotificationSub = new Notification();
                    ticketNotificationSub.TicketId = oldTicket.Id;
                    ticketNotificationSub.UserId = newTicket.OwnerUserId;
                    ticketNotificationSub.Created = DateTimeOffset.Now;
                    ticketNotificationSub.NotifyReason = newTicket.Title + " has been assigned to " + db.Users.Find(newTicket.AssignedToUserId).FullName;
                    db.Notifications.Add(ticketNotificationSub);
                }
                if (newTicket.AssignedToUserId == null)
                {
                    var ticketNotification = new Notification();
                    ticketNotification.TicketId = oldTicket.Id;
                    ticketNotification.UserId = oldTicket.AssignedToUserId;
                    ticketNotification.Created = DateTimeOffset.Now;
                    ticketNotification.NotifyReason = "You have been unassigned from " + oldTicket.Title;
                    db.Notifications.Add(ticketNotification);
                    AdminTicketNotify(oldTicket);

                    var emailNotificaiton = new NotifcationMessage();
                    emailNotificaiton.Body = ticketNotification.NotifyReason;
                    emailNotificaiton.SourceId = userId;
                    emailNotificaiton.SourceName = db.Users.Find(userId).FullName;
                    emailNotificaiton.Subject = "Ticket Notification";
                    emailNotificaiton.DestinationEmail = db.Users.Find(ticketNotification.UserId).Email;

                    await EmailHelper.SendNotificationEmailAsync(emailNotificaiton);

                }
                if (newTicket.AssignedToUserId != null && oldTicket.AssignedToUserId != null)
                {   
                    //notification for the assigned
                    var ticketNotificationAssign = new Notification();
                    ticketNotificationAssign.TicketId = oldTicket.Id;
                    ticketNotificationAssign.UserId = newTicket.AssignedToUserId;
                    ticketNotificationAssign.Created = DateTimeOffset.Now;
                    ticketNotificationAssign.NotifyReason = "You have been assigned " + newTicket.Title;
                    db.Notifications.Add(ticketNotificationAssign);

                    var emailNotificaiton = new NotifcationMessage();
                    emailNotificaiton.Body = ticketNotificationAssign.NotifyReason;
                    emailNotificaiton.SourceId = userId;
                    emailNotificaiton.SourceName = db.Users.Find(userId).FullName;
                    emailNotificaiton.Subject = "Ticket Notification";
                    emailNotificaiton.DestinationEmail = db.Users.Find(ticketNotificationAssign.UserId).Email;

                    await EmailHelper.SendNotificationEmailAsync(emailNotificaiton);


                    //notification for the unassigned
                    var ticketNotificationUnassign = new Notification();
                    ticketNotificationUnassign.TicketId = oldTicket.Id;
                    ticketNotificationUnassign.UserId = oldTicket.AssignedToUserId;
                    ticketNotificationUnassign.Created = DateTimeOffset.Now;
                    ticketNotificationUnassign.NotifyReason = "You have been unassigned from " + oldTicket.Title;
                    db.Notifications.Add(ticketNotificationUnassign);

                    var emailNotificaitonUnassign = new NotifcationMessage();
                    emailNotificaiton.Body = ticketNotificationUnassign.NotifyReason;
                    emailNotificaiton.SourceId = userId;
                    emailNotificaiton.SourceName = db.Users.Find(userId).FullName;
                    emailNotificaiton.Subject = "Ticket Notification";
                    emailNotificaiton.DestinationEmail = db.Users.Find(ticketNotificationUnassign.UserId).Email;

                    await EmailHelper.SendNotificationEmailAsync(emailNotificaiton);


                    //notification for the submitter
                    var ticketNotificationSub = new Notification();
                    ticketNotificationSub.TicketId = oldTicket.Id;
                    ticketNotificationSub.UserId = newTicket.OwnerUserId;
                    ticketNotificationSub.Created = DateTimeOffset.Now;
                    ticketNotificationSub.NotifyReason = newTicket.Title + " has been assigned to " + db.Users.Find(newTicket.AssignedToUserId).FullName;
                    db.Notifications.Add(ticketNotificationSub);
                }
            }
            if (oldTicket.TicketAttachments.Count() > newTicket.TicketAttachments.Count())
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "An Attachment has been deleted";
                db.Notifications.Add(ticketNotification);
            }
            if (oldTicket.TicketAttachments.Count() < newTicket.TicketAttachments.Count())
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "An Attachment has been added";
                db.Notifications.Add(ticketNotification);
            }
            if (oldTicket.TicketComments.Count() > newTicket.TicketComments.Count())
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "A comment has been deleted";
                db.Notifications.Add(ticketNotification);
            }
            if (oldTicket.TicketComments.Count() < newTicket.TicketComments.Count())
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "A comment has been added";
                db.Notifications.Add(ticketNotification);
            }
            if (property == "Description")
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "The description on " + newTicket.Title + " has been changed";
                db.Notifications.Add(ticketNotification);
            }
            if (property == "TicketPriorityId")
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "Ticket Priority for " + newTicket.Title + " has been changed from " + oldTicket.TicketPriority.Name + " to " + newTicket.TicketPriority.Name;
                db.Notifications.Add(ticketNotification);
            }
            if (property == "TicketStatusId" && newTicket.TicketStatusId != db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Assigned").Id && newTicket.TicketStatusId != db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned").Id)
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "Ticket Status for " + newTicket.Title + " has been changed from " + oldTicket.TicketStatus.Name + " to " + newTicket.TicketStatus.Name;
                ticketNotification.Created = DateTimeOffset.Now;
                db.Notifications.Add(ticketNotification);
            }
            if (property == "TicketTypeId")
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.Created = DateTimeOffset.Now;
                ticketNotification.NotifyReason = "Ticket Type for " + newTicket.Title + " has been changed from " + oldTicket.TicketType.Name + " to " + newTicket.TicketType.Name;
                db.Notifications.Add(ticketNotification);
            }
            if (property == "Title")
            {
                var ticketNotification = new Notification();
                ticketNotification.TicketId = oldTicket.Id;
                ticketNotification.UserId = oldTicket.AssignedToUserId;
                ticketNotification.NotifyReason = oldTicket.Title + " name changed to " + newTicket.Title;
                ticketNotification.Created = DateTimeOffset.Now;
                db.Notifications.Add(ticketNotification);
            }

            db.SaveChanges();
        }

        public void AdminTicketNotify(Ticket oldTicket)
        {
            var projectId = db.Tickets.FirstOrDefault(t => t.Id == oldTicket.Id).ProjectId;

            foreach (var user in roleHelper.UsersInRole("Project Manager")
                     .Concat(roleHelper.UsersInRole("Super User"))
                     .Concat(roleHelper.UsersInRole("Admin")))
            {
                if (roleHelper.IsUserInRole(user.Id,"Super User") || roleHelper.IsUserInRole(user.Id,"Admin"))
                {
                    var ticketNotification = new Notification();
                    ticketNotification.TicketId = oldTicket.Id;
                    ticketNotification.UserId = user.Id;
                    ticketNotification.NotifyReason = oldTicket.Title + " is Currently Unassigned";
                    ticketNotification.Created = DateTimeOffset.Now;
                    db.Notifications.Add(ticketNotification);
                    db.SaveChanges();
                }
                if (roleHelper.IsUserInRole(user.Id,"Project Manager") && projectHelper.IsProjectManager(user.Id,projectId))
                {
                    var ticketNotification = new Notification();
                    ticketNotification.TicketId = oldTicket.Id;
                    ticketNotification.UserId = user.Id;
                    ticketNotification.NotifyReason = oldTicket.Title + " is Currently Unassigned";
                    ticketNotification.Created = DateTimeOffset.Now;
                    db.Notifications.Add(ticketNotification);
                    db.SaveChanges();
                }
            }
        }
    }
}