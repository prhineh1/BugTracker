using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using BugTracker.Helper;
namespace BugTracker
{
    [Authorize (Roles = "Admin, Super User")]
    public class ArchivesController : Controller
    {
        private TicketHelper ticketHelper = new TicketHelper();
        private ProjectsHelper projHelper = new ProjectsHelper();
        private TicketDelta ticketDelta = new TicketDelta();
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Archives
        public ActionResult Projects()
        {
            return View(db.Archives.AsNoTracking().Include(p => p.Projects).Include(t => t.Tickets).ToList());
        }

        public ActionResult Ticket(int id)
        {
            var ticket = db.Archives.SelectMany(a => a.Tickets).FirstOrDefault(t => t.Id == id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            return View(ticket);
        }

        // GET: Archives/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Archive archive = db.Archives.Find(id);
            if (archive == null)
            {
                return HttpNotFound();
            }
            return View(archive);
        }

        // GET: Archives/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Archives/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(List<int> projectIds)
        {          
            if (projectIds != null)
            {
                foreach(var id in projectIds)
                {
                    db.Configuration.ProxyCreationEnabled = false;

                    projHelper.ArchiveProjectNotification(id);
                    var project = db.Projects.Include(t => t.Tickets).Include(u => u.Users).FirstOrDefault(p => p.Id == id);
                    var tickets = db.Tickets.Include(t => t.TicketAttachments).FirstOrDefault(p => p.ProjectId == id);
                    var projectCopy = project.Copy();

                    foreach (var user in  project.Users.ToList())
                    {
                        if (project.Users.Any(u => u.Id == user.Id))
                        {
                            var userCopy = user.Copy();
                            userCopy.UserName = string.Concat(user.UserName, project.Id);
                            userCopy.Id = string.Concat(user.Id, project.Id);
                            projectCopy.Users.Add(userCopy);
                        }
                    }

                    //Archive projects and their tickets
                    var archive = new Archive();

                    foreach (var ticket in project.Tickets.ToList())
                    {
                        var ticketCopy = ticket.Copy();
                        ticketCopy.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Closed");
                        var ticketAttachments = db.TicketAttachments.Where(t => t.TicketId == ticket.Id).ToList();
                        foreach (var attachment in ticketAttachments)
                        {
                            var attachmentCopy = attachment.Copy();
                            attachmentCopy.Ticket = null;
                            ticketCopy.TicketAttachments.Add(attachmentCopy);
                        }
                        archive.Tickets.Add(ticketCopy);
                        projectCopy.Tickets.Add(ticketCopy);
                    }

                    archive.Projects.Add(projectCopy);
                    archive.Created = DateTimeOffset.Now;

                    //Remove the project and its tickets from active status
                    foreach (var ticket in project.Tickets.ToList())
                    {
                        ticket.Active = false;
                        ticket.AssignedToUserId = null;
                        ticket.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned");
                        db.Entry(ticket).State = EntityState.Modified;
                    }

                    foreach (var user in project.Users.ToList())
                    {
                        project.Users.Remove(user);
                    }

                    project.ProjectManager = null;
                    project.Active = false;
                    db.Entry(project).State = EntityState.Modified;
                    db.Archives.Add(archive);
                }

                db.SaveChanges();
                return RedirectToAction("Index", "Projects");
            }

            return RedirectToAction("Index", "Projects");
        }

        // POST: Archives/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(List<int> ArchiveIds)
        {
            if (ArchiveIds != null)
            {
                foreach (var id in ArchiveIds)
                {
                    Archive archive = db.Archives.Find(id);

                    //return archives' projects and ticekts to active status
                    foreach (var projectArchive in archive.Projects.ToList())
                    {
                        var usersArchive = projectArchive.Users.ToList();
                        foreach (var user in usersArchive)
                        {
                            db.Users.Remove(user);
                        }
                        foreach (var ticket in projectArchive.Tickets.ToList())
                        {
                            projectArchive.Tickets.Remove(ticket);
                        }
                        var project = db.Projects.FirstOrDefault(p => p.Name == projectArchive.Name && p.Id != projectArchive.Id);
                        projHelper.hasNoDevs(project.Id);
                        projHelper.NoPmNotification(project.Id);
                        project.Active = true;
                        db.Entry(project).State = EntityState.Modified;
                        db.Projects.Remove(projectArchive);
                    }

                    foreach (var ticketArchive in archive.Tickets.ToList())
                    {
                        var ticket = db.Tickets.FirstOrDefault(t => t.Title == ticketArchive.Title && t.Id != ticketArchive.Id);
                        ticketDelta.AdminTicketNotify(ticket);
                        ticket.Active = true;
                        db.Entry(ticket).State = EntityState.Modified;
                        db.Tickets.Remove(ticketArchive);
                    }

                    //remove archive
                    db.Archives.Remove(archive);
                    db.SaveChanges();
                }

                return RedirectToAction("Projects");
            }

            return RedirectToAction("Projects");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
