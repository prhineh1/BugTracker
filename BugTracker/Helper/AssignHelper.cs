using BugTracker.Models;
using BugTracker.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace BugTracker.Helper
{
    public class AssignHelper
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private ProjectsHelper projHelper = new ProjectsHelper();

        public bool CanAssignTicket(string projectManagerId, string userId)
        {
            var userProjects = projHelper.ListUserProjects(userId);
            var fullName = db.Users.FirstOrDefault(u => u.Id == projectManagerId).FullName;
            var PmProjects = db.Projects.Where(p => p.ProjectManager == fullName && p.Active == true).ToList();

            foreach (var project in userProjects)
            {
                foreach (var pmProject in PmProjects)
                {
                    if (project.Id == pmProject.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool isDemoUser(string userId)
        {
            var email = db.Users.Find(userId).Email;
            
            if (email == "minister@mailinator.com" ||
                email == "ProjectManager@mailinator.com" ||
                email == "Submitter@mailinator.com" ||
                email == "Developer@mailinator.com")
            {
                return true;
            }

            return false;
        }
    }
}