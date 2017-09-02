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
using Microsoft.AspNet.Identity;

namespace BugTracker.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Projects
        [Authorize]
        public ActionResult Index()
        {
            return View(db.Projects.Where(p => p.Active == true).ToList());
        }

        // GET: Projects/Create
        [Authorize (Roles ="Admin, Project Manager, Super User")]
        public ActionResult Create()
        {
            var usr = new UserRolesHelper();
            var pmList = usr.UsersInRole("Project Manager").ToList();
            var devList = usr.UsersInRole("Developer").ToList();

            ViewBag.projectManager = new SelectList(pmList, "Id", "FullName");
            ViewBag.developers = new MultiSelectList(devList, "Id", "FullName");
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name")] Project project, string projectManager, List<string> developers)
        {
            var projectHelper = new ProjectsHelper();
            var roleHelper = new UserRolesHelper();

            if (ModelState.IsValid)
            {
                if (db.Projects.Any(p => p.Name.Trim().ToLower() == project.Name.Trim().ToLower()))
                {
                    TempData["name"] = "duplicate";
                    goto Permiss;
                }

                if (projectManager != null)
                {
                    project.ProjectManager = db.Users.FirstOrDefault(u => u.Id == projectManager).FullName;
                    project.Users.Add(db.Users.FirstOrDefault(u => u.Id == projectManager));
                }
                else
                {
                    var pmId = User.Identity.GetUserId();
                    project.ProjectManager = db.Users.FirstOrDefault(u => u.Id == pmId).FullName;
                    project.Users.Add(db.Users.FirstOrDefault(u => u.Id == pmId));
                }
                if (developers != null)
                {
                    foreach (string person in developers)
                    {
                        if (person != projectManager)
                        {
                            project.Users.Add(db.Users.FirstOrDefault(u => u.Id == person));
                        }                    
                    }
                }
            
                project.Created = DateTimeOffset.Now;
                project.Active = true;
                db.Projects.Add(project);
                db.SaveChanges();

                if (!(projectHelper.hasNoDevs(project.Id)))
                {
                    foreach(var user in project.Users)
                    {
                        if (roleHelper.IsUserInRole(user.Id, "Developer"))
                        {
                            projectHelper.AssignDevNotification(user.Id, project);
                        }
                    }
                }

                projectHelper.NewPmNotification(project);

                return RedirectToAction("Index");
            }
            Permiss:
            var usr = new UserRolesHelper();
            var pmList = usr.UsersInRole("Project Manager").ToList();
            var devList = usr.UsersInRole("Developer").ToList();

            ViewBag.projectManager = new SelectList(pmList, "Id", "FullName");
            ViewBag.developers = new MultiSelectList(devList, "Id", "FullName");

            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize (Roles ="Admin, Project Manager, Super User")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var projectId = id ?? default(int);
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            if (project.Active == false)
            {
                return RedirectToAction("Index");
            }

            var proj = new ProjectsHelper();
            var projUsrs = proj.UsersOnProject(projectId);
            var usr = new UserRolesHelper();

            //permissions
            if (User.IsInRole("Admin") || User.IsInRole("Super User"))
            {
                goto Permiss;
            }
            if (User.IsInRole("Project Manager"))
            {
                if(!(proj.IsProjectManager(User.Identity.GetUserId(), project.Id)))
                {
                    TempData["warning"] = "pm";
                    return RedirectToAction("index");
                }
            }

            Permiss:
            var devList = new List<ApplicationUser>();

            var assignedDevs = new List<string>();

            var pmList = usr.UsersInRole("Project Manager");

            string assignedPm = null;

            if (project.ProjectManager != null)
            {
                assignedPm = db.Users.FirstOrDefault(u => u.FullName == project.ProjectManager).Id;
            }

            //Creates a list of all developers
            foreach (var per in db.Users.ToList())
            {
                if (usr.IsUserInRole(per.Id, "Developer"))
                {
                    if (proj.IsProjectManager(per.Id, project.Id))
                    {
                        continue;
                    }
                    else
                    {
                        devList.Add(per);
                    }              
                }
            }
            //Creates a list of all developers assigned to this project
            foreach(var dev in projUsrs)
            {
                if (usr.IsUserInRole(dev.Id, "Developer"))
                {
                    if (proj.IsProjectManager(dev.Id, project.Id))
                    {
                        continue;
                    }
                    else
                    {
                        assignedDevs.Add(dev.Id);
                    }
                }
            }

            ViewBag.projectManager = new SelectList(pmList, "Id", "FullName", assignedPm);
            ViewBag.developers = new MultiSelectList(devList, "Id", "FullName", assignedDevs);
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Created")] Project project, string projectManager, List<string> developers)
        {
            if (ModelState.IsValid)
            {
                if (db.Projects.Where(p => p.Id != project.Id).Any(p => p.Name.Trim().ToLower() == project.Name.Trim().ToLower()))
                {
                    TempData["name"] = "duplicate";
                    goto Permiss;
                }

                var oldProject = db.Projects.AsNoTracking().Include(u => u.Users).FirstOrDefault(p => p.Id == project.Id);
                var oldProjectUsers = db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == project.Id).Users.ToList();
                var newProjectUsers = new List<ApplicationUser>();

                if (developers != null)
                {
                    foreach (var id in developers)
                    {
                        newProjectUsers.Add(db.Users.Find(id));
                    }
                }
                var proj = new ProjectsHelper();

                if (developers == null)
                {
                    proj.compareProjectMuliDev2(project.Id);
                }
                else
                {
                    proj.compareProjectMultiDev(oldProjectUsers, newProjectUsers, project.Id);
                }

                //Remove old assignments
                var oldDevs = proj.UsersOnProject(project.Id);

                foreach (var dev in oldDevs)
                {
                    proj.RemoveUserFromProject(dev.Id, project.Id);
                }

                //Create new assignments
                if (projectManager != null)
                {
                    project.ProjectManager = db.Users.FirstOrDefault(u => u.Id == projectManager).FullName;
                    proj.AddUserToProject(projectManager, project.Id);
                }
                else
                {
                    var pmId = User.Identity.GetUserId();
                    project.ProjectManager = db.Users.FirstOrDefault(u => u.Id == pmId).FullName;
                    proj.AddUserToProject(pmId, project.Id);
                }

                if (developers != null)
                {
                    foreach (string person in developers)
                    {
                        proj.AddUserToProject(person, project.Id);
                    }
                }

                project.Updated = DateTimeOffset.Now;
                project.Active = true;
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();

                proj.hasNoDevs(project.Id);
                proj.comparePms(oldProject, project);

                return RedirectToAction("Index");
            }

            Permiss:
            var devList = new List<ApplicationUser>();
            var usr = new UserRolesHelper();
            var projHelper = new ProjectsHelper();
            var projUsrs = projHelper.UsersOnProject(project.Id);

            var assignedDevs = new List<string>();

            var pmList = usr.UsersInRole("Project Manager");

            string assignedPm = null;

            if (project.ProjectManager != null)
            {
                assignedPm = db.Users.FirstOrDefault(u => u.FullName == project.ProjectManager).Id;
            }

            //Creates a list of all developers
            foreach (var per in db.Users.ToList())
            {
                if (usr.IsUserInRole(per.Id, "Developer"))
                {
                    if (projHelper.IsProjectManager(per.Id, project.Id))
                    {
                        continue;
                    }
                    else
                    {
                        devList.Add(per);
                    }
                }
            }
            //Creates a list of all developers assigned to this project
            foreach (var dev in projUsrs)
            {
                if (usr.IsUserInRole(dev.Id, "Developer"))
                {
                    if (projHelper.IsProjectManager(dev.Id, project.Id))
                    {
                        continue;
                    }
                    else
                    {
                        assignedDevs.Add(dev.Id);
                    }
                }
            }

            ViewBag.projectManager = new SelectList(pmList, "Id", "FullName", assignedPm);
            ViewBag.developers = new MultiSelectList(devList, "Id", "FullName", assignedDevs);

            return View(project);
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
