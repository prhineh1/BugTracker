using BugTracker.Models;
using BugTracker.Helper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace BugTracker.Helper
{
    public class ProjectsHelper
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public List<Ticket> ListProjectTickets(int projectId)
        {
            var project = db.Projects.Find(projectId);
            return project.Tickets.Where(t => t.TicketStatus.Name != "Resolved" || t.TicketStatus.Name != "Closed" && t.Active == true).ToList();
        }

        public bool IsProjectManager(string userID, int projectId)
        {
            var roleHelper = new UserRolesHelper();
            if (roleHelper.IsUserInRole(userID,"Project Manager") && IsUserOnProject(userID,projectId))
            {
                return true;
            }
            return false;
        }

        public bool IsUserOnProject(string userId, int projectId)
        {
            var project = db.Projects.Find(projectId);
            var flag = project.Users.Any(u => u.Id == userId);
            return (flag);
        }

        public ICollection<Project> ListUserProjects(string userId)
        {
            ApplicationUser user = db.Users.Find(userId);

            var projects = user.Projects.Where(p => p.Active == true).ToList();
            return (projects);
        }

        public List<Project> ListUserProjectsComplement(string userId)
        {
            var projects = db.Projects.Where(p => p.Active == true).ToList();
            var projList = new List<Project>();
            
            foreach (var project in projects)
            {
                if (IsUserOnProject(userId,project.Id)) 
                {
                    continue;
                }
                else
                {
                    projList.Add(project);
                }
            }

            return projList;
        }

        public void AddUserToProject(string userId, int projectId)
        {
            if (!IsUserOnProject(userId, projectId))
            {
                Project proj = db.Projects.Find(projectId);
                var newUser = db.Users.Find(userId);

                proj.Users.Add(newUser);
                db.SaveChanges();
            }
        }

        public void RemoveUserFromProject(string userId, int projectId)
        {
            if (IsUserOnProject(userId, projectId))
            {
                Project proj = db.Projects.Find(projectId);
                var delUser = db.Users.Find(userId);
                proj.Users.Remove(delUser);
                db.Entry(proj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public ICollection<ApplicationUser> UsersOnProject(int projectId)
        {
            return db.Projects.Find(projectId).Users.ToList();
        }

        public ICollection<ApplicationUser> UsersNotOnProject(int projectId)
        {
            return db.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToList();
        }

        public bool hasNoDevs(int projectId)
        {
            var project = db.Projects.Find(projectId);

            if (project.Users.Count() <= 1)
            {
                noDevNotification(project);
                return true;
            }
            return false;
        }

        public void noDevNotification(Project project)
        {
            var roleHelper = new UserRolesHelper();

            foreach (var user in roleHelper.UsersInRole("Project Manager")
                     .Concat(roleHelper.UsersInRole("Super User"))
                     .Concat(roleHelper.UsersInRole("Admin")))
            {
                if (roleHelper.IsUserInRole(user.Id,"Admin") || roleHelper.IsUserInRole(user.Id, "Super User"))
                {
                    var projectNotification = new Notification();
                    projectNotification.UserId = user.Id;
                    projectNotification.Created = DateTimeOffset.Now;
                    projectNotification.NotifyReason = project.Name + " has no assigned Developers";
                    db.Notifications.Add(projectNotification);
                    db.SaveChanges();
                }
                if (roleHelper.IsUserInRole(user.Id,"Project Manager") && IsProjectManager(user.Id,project.Id))
                {
                    var projectNotification = new Notification();
                    projectNotification.UserId = user.Id;
                    projectNotification.Created = DateTimeOffset.Now;
                    projectNotification.NotifyReason = project.Name + " has no assigned Developers";
                    db.Notifications.Add(projectNotification);
                    db.SaveChanges();
                }
            }         
        }

        public void NewPmNotification(Project project)
        {
            var projectNotification = new Notification();
            projectNotification.UserId = db.Users.FirstOrDefault(u => u.FullName == project.ProjectManager).Id;
            projectNotification.Created = DateTimeOffset.Now;
            projectNotification.NotifyReason = "You have been assigned as Project Manager on " + project.Name;
            db.Notifications.Add(projectNotification);
            db.SaveChanges();
        }

        public void NoPmNotification(int projectId)
        {
            var roleHelper = new UserRolesHelper();

            foreach (var user in roleHelper.UsersInRole("Super User").Concat(roleHelper.UsersInRole("Admin")))
            {
                var notification = new Notification();
                notification.UserId = user.Id;
                notification.Created = DateTimeOffset.Now;
                notification.NotifyReason = db.Projects.Find(projectId).Name + " has no Project Manager";
                db.Notifications.Add(notification);
                db.SaveChanges();
            }
        }

        public void oldPmNotification(Project project)
        {
            var projectNotification = new Notification();
            projectNotification.UserId = db.Users.FirstOrDefault(u => u.FullName == project.ProjectManager).Id;
            projectNotification.Created = DateTimeOffset.Now;
            projectNotification.NotifyReason = "You have been removed as Project Manager on " + project.Name;
            db.Notifications.Add(projectNotification);
            db.SaveChanges();
        }

        public void comparePms(Project oldProject, Project newProject)
        {
            if(oldProject.ProjectManager != newProject.ProjectManager && oldProject.ProjectManager != null)
            {
                oldPmNotification(newProject);
                NewPmNotification(newProject); 
            }
            else
            {
                NewPmNotification(newProject);
            }
        }

        public void CompareProjectOneDev(List<int> oldProjectIds, List<int> changeProjectIds, string userId)
        {
            for (var i = 0; i < oldProjectIds.Count(); i++)
            {
                if (changeProjectIds.Contains(oldProjectIds[i]))
                {
                    changeProjectIds.Remove(oldProjectIds[i]);
                }
                else
                {
                    UnassignDevNotification(userId, db.Projects.Find(oldProjectIds[i]));
                }
            }

            foreach(var id in changeProjectIds)
            {
                AssignDevNotification(userId, db.Projects.Find(id));
            }
        }

        public void CompareProjectOneDev2(string userId)
        {
            if (ListUserProjects(userId).Count() > 0)
            {
                foreach (var project in ListUserProjects(userId))
                {
                    UnassignDevNotification(userId, project);
                }
            }
        }

        public void compareProjectMultiDev(List<ApplicationUser> oldProjectUsers, List<ApplicationUser> newProjectusers, int projectId)
        {   
            for (var i = 0; i < oldProjectUsers.Count(); i++)
            {
                if (newProjectusers.Contains(oldProjectUsers[i]))
                {
                    newProjectusers.Remove(oldProjectUsers[i]);
                }
                else
                {
                    UnassignDevNotification(oldProjectUsers[i].Id, db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == projectId));
                }
            }

            foreach (var user in newProjectusers)
            {
                AssignDevNotification(user.Id, db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == projectId));
            }
        }

        public void compareProjectMuliDev2(int projectId)
        {
           if (db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == projectId).Users.Count() > 0)
            {
                foreach (var user in db.Projects.Find(projectId).Users.ToList())
                {
                    UnassignDevNotification(user.Id, db.Projects.AsNoTracking().FirstOrDefault(p => p.Id == projectId));
                }
            }
        }

        public void UnassignDevNotification(string userId, Project project)
        {
            var userNotification = new Notification();
            userNotification.UserId = userId;
            userNotification.Created = DateTimeOffset.Now;
            userNotification.NotifyReason = "You have been removed from project " + project.Name;
            db.Notifications.Add(userNotification);
            db.SaveChanges();
        }

        public void AssignDevNotification(string userId, Project project)
        {
            var userNotification = new Notification();
            userNotification.UserId = userId;
            userNotification.Created = DateTimeOffset.Now;
            userNotification.NotifyReason = "You have been added to project " + project.Name;
            db.Notifications.Add(userNotification);
            db.SaveChanges();
        }

        public void ArchiveProjectNotification(int projectId)
        {
            foreach (var user in db.Projects.Find(projectId).Users.ToList())
            {
                var notification = new Notification();
                notification.UserId = user.Id;
                notification.Created = DateTimeOffset.Now;
                notification.NotifyReason = db.Projects.Find(projectId).Name + " has been archived.";
                db.Notifications.Add(notification);
                db.SaveChanges();
            }
        }
    }
}
