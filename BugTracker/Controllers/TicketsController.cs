using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using Microsoft.AspNet.Identity;
using BugTracker.Helper;
using System.IO;
using System.Threading.Tasks;

namespace BugTracker.Controllers
{   [Authorize]
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Tickets
        public ActionResult Index()
        {
            return View(db.Tickets.Where(p => p.Active == true).ToList());
        }

        // GET: Tickets/Details/5
        public ActionResult Details(int? id)
        {
            var projHelper = new ProjectsHelper();

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (ticket.Active == false)
            {
                return RedirectToAction("Index");
            }

            #region permissions

            #endregion

            Permiss:

            ViewBag.ticketAttachment = db.TicketAttachments.FirstOrDefault(t => t.TicketId == id);

            return View(ticket);
        }
        [Authorize (Roles ="Submitter, Super User")]
        // GET: Tickets/Create
        public ActionResult Create()
        {
            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");            
            ViewBag.TicketPriorityId = new SelectList(db.TicketPrioritys, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId, MediaUrl")] Ticket ticket)
        {
            var ticketDelta = new TicketDelta();

            if (ModelState.IsValid)
            {
                if (db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == ticket.ProjectId).Tickets.Any(t => t.Title.Trim().ToLower() == ticket.Title.Trim().ToLower()))
                {
                    TempData["title"] = "duplicated";
                    goto Permiss;
                }

                ticket.Created = DateTimeOffset.Now;
                ticket.OwnerUserId = User.Identity.GetUserId();
                ticket.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned");
                ticket.Active = true;
                db.Tickets.Add(ticket);
                db.SaveChanges();

                ticketDelta.AdminTicketNotify(ticket);

                return RedirectToAction("Index");
            }

            Permiss:

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPrioritys, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        [Authorize (Roles ="Super User, Admin, Project Manager, Developer, Submitter")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (ticket.Active == false)
            {
                return RedirectToAction("Index");
            }

            var ticketId = id ?? default(int);
            var projHelper = new ProjectsHelper();
            var roleHelper = new UserRolesHelper();

            #region permissions

            //Admin, Super User
            if (User.IsInRole("Admin") || User.IsInRole("Super User"))
            {
                goto Permiss;
            }

            //Project Manager
            if (User.IsInRole("Project Manager"))
            {
                var pmId = User.Identity.GetUserId();
                var pmName = db.Users.FirstOrDefault(p => p.Id == pmId).FullName;

                if (!(db.Projects.Where(p => p.ProjectManager == pmName)
                    .SelectMany(t => t.Tickets)
                    .Any(i => i.Id == ticketId)))
                {
                    return RedirectToAction("index");
                }
            }

            //Developer
            if (User.IsInRole("Developer"))
            {
                var devId = User.Identity.GetUserId();

                if (!(db.Tickets.Where(u => u.AssignedToUserId == devId && u.Active == true)
                    .Any(t => t.Id == ticketId)))
                {
                    return RedirectToAction("index");
                }
            }

            //Submitter
            if (User.IsInRole("Submitter"))
            {
                var subId = User.Identity.GetUserId();

                if (!(db.Tickets.Where(u => u.OwnerUserId == subId && u.Active == true)
                    .Any(t => t.Id == ticketId)))
                {
                    return RedirectToAction("index");
                }
            }

            #endregion

            Permiss:

            var userList = ticket.Project.Users.ToList();
            var assignedList = new List<ApplicationUser>();

            foreach (var dev in userList)
            {
                if (roleHelper.IsUserInRole(dev.Id,"Developer"))
                {
                    assignedList.Add(dev);
                }
            }

            ViewBag.AssignedToUserId = new SelectList(assignedList, "Id", "FullName", ticket.AssignedToUserId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPrioritys, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId")] Ticket ticket, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {

                if (db.Tickets.Where(t => t.Id != ticket.Id).Any(t => t.Title.Trim().ToLower() == ticket.Title.Trim().ToLower()))
                {
                    TempData["title"] = "duplicated";
                    goto Permiss;
                }

                var ticketDelta = new TicketDelta();

                var oldTicket = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id);

                ticket.Updated = DateTimeOffset.Now;
                ticket.Active = true;

                if (ticket.AssignedToUserId == null)
                {
                    ticket.TicketStatus = ticket.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Unassigned");
                }
               
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();

               await ticketDelta.objectCompare(oldTicket, ticket, User.Identity.GetUserId());

                return RedirectToAction("Index");
            }

            Permiss:

            var userList = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id).Project.Users.ToList();
            var assignedList = new List<ApplicationUser>();
            var roleHelper = new UserRolesHelper();

            foreach (var dev in userList)
            {
                if (roleHelper.IsUserInRole(dev.Id, "Developer"))
                {
                    assignedList.Add(dev);
                }
            }           

            var tickStat = db.TicketStatuses.Where(t => t.Name == "Resolved" || t.Name == "Closed").ToList();

            ViewBag.AssignedToUserId = new SelectList(assignedList, "Id", "FullName", ticket.AssignedToUserId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPrioritys, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(tickStat, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
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
