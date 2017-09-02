using BugTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Helper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.UI;

namespace BugTracker.Controllers
{
    public class HomeController : Controller
    {
        private UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
        private ApplicationDbContext db = new ApplicationDbContext();

        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin, Project Manager, Super User")]
        public ActionResult Users()
        {
            return View(userManager.Users.ToList());
        }
        
        [Authorize(Roles = "Admin, Super User")]
        public ActionResult Roles(string id)
        {
            var assignHelper = new AssignHelper();
            if(assignHelper.isDemoUser(id))
            {
                return RedirectToAction("Users");
            }

            var roleHelper = new UserRolesHelper();

            if(roleHelper.IsUserInRole(id,"Super User"))
            {
                return RedirectToAction("Users");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var roles = db.Roles.Where(r => r.Name != "Super User");
            var usrRoles = new UserRolesHelper();
            var assigned = "";

            if (usrRoles.ListUserRoles(id).Count() > 0)
            {
                assigned = usrRoles.ListUserRoles(id).First();
            }

            ViewBag.changeRoles = new SelectList(roles, "Name", "Name", assigned);


            var usr = db.Users.FirstOrDefault(p => p.Id == id);

            return View(usr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Roles(ApplicationUser user, List<string> changeRoles)
        {
            var usr = new UserRolesHelper();
            var assigned = usr.ListUserRoles(user.Id);
            var projHelper = new ProjectsHelper();
            var ticketHelper = new TicketHelper();
            assigned.Remove("Super User");

            foreach (var role in changeRoles)
            {
                if (usr.IsUserInRole(user.Id, "Project Manager") && role != "Project Manager")
                {
                    if (projHelper.ListUserProjects(user.Id).Count > 0)
                    {
                        TempData["warning"] = "remove";
                        return RedirectToAction("Users");
                    }
                }
                else if (usr.IsUserInRole(user.Id, "Developer") && role != "Developer")
                {
                    if (ticketHelper.GetAssignedTickets(user.Id).Count > 0 || projHelper.ListUserProjects(user.Id).Count > 0)
                    {
                        TempData["warning"] = "remove";
                        return RedirectToAction("Users");
                    }
                }
            }

            foreach (var role in assigned)
            {
                usr.RemoveUserFromRole(user.Id, role);
            }           

            if (changeRoles != null)
            {
                foreach (var role in changeRoles)
                {
                    usr.AddUserToRole(user.Id, role);
                }
            }
           return RedirectToAction("/Users");
        }

        [Authorize]
        public ActionResult Dashboard()
        {
            var projHelper = new ProjectsHelper();
            var ticketHelper = new TicketHelper();

            if (User.IsInRole("Project Manager"))
            {
                var pmId = User.Identity.GetUserId();
                var pmName = db.Users.FirstOrDefault(u => u.Id == pmId).FullName;

                ViewBag.projects = db.Projects.Where(p => p.ProjectManager == pmName && p.Active == true).ToList();
                ViewBag.tickets = db.Tickets.Where(t => t.Project.ProjectManager == pmName && t.Active == true).ToList();
                ViewBag.notifications = db.Notifications.Where(n => n.UserId == pmId).OrderByDescending(p => p.Created).ToList();
            }

            if (User.IsInRole("Developer"))
            {
                var devId = User.Identity.GetUserId();

                ViewBag.projects = projHelper.ListUserProjects(devId);
                ViewBag.tickets = ticketHelper.GetAssignedTickets(devId);
                ViewBag.notifications = db.Notifications.Where(n => n.UserId == devId).OrderByDescending(p => p.Created).ToList();
            }

            if (User.IsInRole("Submitter"))
            {
                var subId = User.Identity.GetUserId();

                ViewBag.tickets = ticketHelper.GetUserOwnedTickets(subId);
                ViewBag.notifications = db.Notifications.Where(n => n.UserId == subId).OrderByDescending(p => p.Created).ToList();
            }

            if (User.IsInRole("Admin") || User.IsInRole("Super User"))
            {
                var userId = User.Identity.GetUserId();

                ViewBag.tickets = db.Tickets.Where(t => t.Active == true).ToList();
                ViewBag.users = db.Users.ToList();
                ViewBag.projects = db.Projects.Where(t => t.Active == true).ToList();
                ViewBag.notifications = db.Notifications.Where(n => n.UserId == userId).OrderByDescending(p => p.Created).ToList();
            }

            return View();
        }

        [Authorize (Roles= "Super User, Admin, Project Manager")]
        public ActionResult Projects(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var roleHelper = new UserRolesHelper();
            var projHelper = new ProjectsHelper();
            var assignedProjList = projHelper.ListUserProjects(id).Select(p => p.Id);
            var usr = db.Users.FirstOrDefault(p => p.Id == id);

            #region permissions
            if (!roleHelper.IsUserInRole(id, "Developer"))
            {
                TempData["warning"] = "dev";
                return RedirectToAction("/Users");
            }
            if (User.IsInRole("Admin") || User.IsInRole("Super User"))
            {
                var projList = db.Projects.ToList();
                ViewBag.changeProjects = new MultiSelectList(projList, "Id", "Name", assignedProjList);
            }
            #endregion

            else
            {
                var projList = new List<Project>();
                var pmProj = projHelper.ListUserProjects(User.Identity.GetUserId());
                foreach (var project in pmProj)
                {
                    if (projHelper.IsProjectManager(User.Identity.GetUserId(),project.Id))
                    {
                        projList.Add(project);
                    }
                }

                ViewBag.changeProjects = new MultiSelectList(projList, "Id", "Name", assignedProjList);
            }

            return View(usr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Projects(ApplicationUser user, List<int> changeProjects)
        {
            var projHelper = new ProjectsHelper();
            var ticketHelper = new TicketHelper();
            var oldProjects = projHelper.ListUserProjects(user.Id).ToList();
            var oldProjectsIds = projHelper.ListUserProjects(user.Id).Select(p => p.Id).ToList();
            var changeProjectsCopy = new List<int>();

            if (changeProjects != null)
            {
                foreach (var id in changeProjects)
                {
                    changeProjectsCopy.Add(id);
                }
            }

            if (changeProjects == null)
            {
                projHelper.CompareProjectOneDev2(user.Id);
            }
            else 
            {
                projHelper.CompareProjectOneDev(oldProjectsIds, changeProjectsCopy, user.Id);
            }


            foreach (var proj in oldProjects)
            {
                projHelper.RemoveUserFromProject(user.Id, proj.Id);
            }

            if (changeProjects != null)
            {
                foreach (var proj in changeProjects)
                {
                    projHelper.AddUserToProject(user.Id, proj);
                }
            }

            //unassign user from tickets on projects they were removed from
            foreach (var project in projHelper.ListUserProjectsComplement(user.Id))
            {
                foreach (var ticket in project.Tickets)
                {
                    if (ticket.AssignedToUserId == user.Id)
                    {
                        await ticketHelper.UnassignTicketProjects(ticket.Id, user.Id, User.Identity.GetUserId());
                    }
                }
            }

            //check each project for no assigned developers
            foreach (var Id in oldProjectsIds)
            {
                projHelper.hasNoDevs(Id);
            }
            return RedirectToAction("/Users");
        }

        [Authorize (Roles ="Admin, Super User, Project Manager")]
        public ActionResult Tickets(string id)
        {
            var assignHelper = new AssignHelper();
            var roleHelper = new UserRolesHelper();
            var projHelper = new ProjectsHelper();
            var assignedProjects = projHelper.ListUserProjects(id);
            var listTickets = new List<Ticket>();
            var ticketHelper = new TicketHelper();
            var assignedTickets = ticketHelper.GetAssignedTickets(id).Select(t => t.Id).ToList();
            var user = db.Users.FirstOrDefault(u => u.Id == id);

            #region permissions
            //Can't assign tickets to non-developers
            if (!roleHelper.IsUserInRole(id, "Developer"))
            {
                TempData["warning"] = "dev";
                return RedirectToAction("/Users");
            }

            //can't assign tickets to developers who haven't yet been assigned projects
            if ((projHelper.ListUserProjects(user.Id).Count() == 0))
            {
                TempData["warning"] = "dev2";
                return RedirectToAction("/Users");
            }

            //can't assign tickets to developers whose projects don't have open tickets
            var count = 0;
            foreach (var project in projHelper.ListUserProjects(user.Id))
            {
                if (projHelper.ListProjectTickets(project.Id).Count() > 0)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                TempData["warning"] = "dev2";
                return RedirectToAction("/Users");
            }

            //project managers can't assign tickets to users not assigned to their projects
            if (User.IsInRole("Project Manager") && !(User.IsInRole("Super User") || User.IsInRole("Admin")))
            {
                var pmId = User.Identity.GetUserId();
                var fullName = db.Users.FirstOrDefault(u => u.Id == pmId).FullName;
                var PmProjects = db.Projects.Where(p => p.ProjectManager == fullName && p.Active == true).ToList();

                if (!assignHelper.CanAssignTicket(pmId,id))
                {
                    TempData["warning"] = "pm";
                    return RedirectToAction("/Users");
                }
            }
            #endregion

            if (User.IsInRole("Super User") || User.IsInRole("Admin"))
            {
                foreach (var project in assignedProjects)
                {
                    foreach (var ticket in projHelper.ListProjectTickets(project.Id))
                    {
                        listTickets.Add(ticket);
                    }
                }

                ViewBag.changeTickets = new MultiSelectList(listTickets, "Id", "Title", assignedTickets);
            }
            else
            {
                foreach (var project in assignedProjects)
                {
                    if (projHelper.IsProjectManager(User.Identity.GetUserId(),project.Id))
                    {
                        foreach (var ticket in projHelper.ListProjectTickets(project.Id))
                        {
                            listTickets.Add(ticket);
                        }
                    }
                }

                ViewBag.changeTickets = new MultiSelectList(listTickets, "Id", "Title", assignedTickets);
            }
            
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Tickets(ApplicationUser user, List<int> changeTickets)
        {
            var ticketHelper = new TicketHelper();
            var assignedTickets = ticketHelper.GetAssignedTickets(user.Id);
            var oldTicketList = new List<Ticket>();
            var newTicketList = new List<Ticket>();
            var ticketDelta = new TicketDelta();
            var projectTicketsOld = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == user.Id).Projects.SelectMany(t => t.Tickets).Where(t => t.Active == true);

            foreach(var ticket in projectTicketsOld)
            {
                oldTicketList.Add(ticket);
            }

            foreach(var ticket in assignedTickets)
            {
               ticketHelper.UnassignTicketRoles(ticket.Id, user.Id);
            }

            if (changeTickets != null)
            {
                foreach (var ticketId in changeTickets)
                {
                    var currentTicket = db.Tickets.Find(ticketId);
                    currentTicket.AssignedToUserId = user.Id;
                    currentTicket.TicketStatus = db.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Assigned");
                    db.Entry(currentTicket).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            var projectTicketsNew = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == user.Id).Projects.SelectMany(t => t.Tickets).Where(t => t.Active == true);

            foreach (var ticket in projectTicketsNew)
            {
                newTicketList.Add(ticket);
            }

            foreach (var oldTicket in oldTicketList)
            {
                foreach (var newTicket in newTicketList)
                {
                    if (newTicket.Id == oldTicket.Id)
                    {
                         await ticketDelta.objectCompare(oldTicket, newTicket, User.Identity.GetUserId());
                    }                    
                }
            }

            return RedirectToAction("/Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(List<int> Id)
        {
            if (Id != null)
            {
                foreach (var id in Id)
                {
                    var notification = db.Notifications.Find(id);
                    db.Notifications.Remove(notification);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Dashboard");
        }
    }
}