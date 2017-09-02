using BugTracker.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Helper
{
    public class UserRolesHelper
    {
        private UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
        private ApplicationDbContext db = new ApplicationDbContext();

        public bool IsUserInRole(string userId, string roleName)
        {
            return userManager.IsInRole(userId, roleName);
        }
        public ICollection<string> ListUserRoles(string userId)
        {
            return userManager.GetRoles(userId);
        }
        public bool AddUserToRole(string userId, string roleName)
        {
            var result = userManager.AddToRole(userId, roleName);
            return result.Succeeded;
        }
        public bool RemoveUserFromRole(string userId, string roleName)
        {
            var result = userManager.RemoveFromRole(userId, roleName);
            return result.Succeeded;
        }
        public ICollection<ApplicationUser> UsersInRole(string roleName)
        {
            var resultList = new List<ApplicationUser>();
            var List = userManager.Users.ToList();
            foreach (var user in List)
            {
                if (IsUserInRole(user.Id, roleName))
                    resultList.Add(user);
            }

            return resultList;
        }
        public ICollection<ApplicationUser> usersNotInRole(string roleName)
        {
            var resultList = new List<ApplicationUser>();
            var List = userManager.Users.ToList();
            foreach (var user in List)
            {
                if (!IsUserInRole(user.Id, roleName))
                    resultList.Add(user);
            }

            return resultList;
        }
        public Boolean UserWithNoRole(ApplicationUser user)
        {
            if (!(IsUserInRole(user.Id,"Admin") || IsUserInRole(user.Id,"Project Manager") || IsUserInRole(user.Id,"Developer") || IsUserInRole(user.Id,"Submitter")))
            {
                return true;
            }

            return false;
        }

        public void NoRoleAlert(ApplicationUser User)
        {
            foreach(var user in UsersInRole("Admin").Concat(UsersInRole("Super User")))
            {
                var userNotification = new Notification();
                userNotification.Created = DateTimeOffset.Now;
                userNotification.UserId = user.Id;
                userNotification.NotifyReason = User.FullName + " has no assigned role";
                db.Notifications.Add(userNotification);
            }

            db.SaveChanges();
        }
    }
}