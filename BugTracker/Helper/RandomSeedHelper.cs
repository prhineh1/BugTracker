using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BugTracker.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity.Migrations;
using BugTracker.Helper;

namespace BugTracker.Helper
{
    public class RandomSeedHelper
    {
        private static ApplicationDbContext context = new ApplicationDbContext();
        private UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

        public void CreateRandomUsers(int number)
        {
            var userManager = new UserManager<ApplicationUser>(
             new UserStore<ApplicationUser>(context));

            for (var i = 0; i < number; i++)
            {
                var firstName = "Test";
                var lastName = string.Concat("User", i + 1);
                var random = new Random();

                var email = string.Concat(firstName, lastName, "@mailinator.com");

                if (!context.Users.Any(u => u.Email == email))
                {
                    userManager.Create(new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                    }, "Abc&123!");
                }

                //Assign created user a random role
               var rolesList = context.Roles.Where(r => r.Name != "Admin" && r.Name != "Super User").ToList();
               var userId = userManager.FindByEmail(email).Id;
               userManager.AddToRole(userId, rolesList[random.Next(0,rolesList.Count())].Name);
            }
        }

        public void CreateRandomProjects (int number, int tickets)
        {
            for (var i=0; i < number; i++)
            {
                var name = string.Concat("Project ", i + 1);
                var random = new Random();

                var project = new Project();
                project.Created = DateTimeOffset.Now;
                project.Active = true;
                project.Name = name;
                project.Id = i + 1;

                var userList = userManager.Users.ToList();
               
                var devList = new List<ApplicationUser>();
                foreach(var user in userList)
                {
                    if (userManager.IsInRole(user.Id,"Developer"))
                    {
                        devList.Add(user);
                    }
                }

                //demo developer gets at least two projects
                if (i < 2)
                {
                    var demoDev = userManager.FindByEmail("Developer@mailinator.com");
                    project.Users.Add(demoDev);
                    devList.Remove(demoDev);
                    var dev2 = devList[random.Next(0, devList.Count())];
                    project.Users.Add(dev2);
                    devList.Remove(dev2);
                    project.Users.Add(devList[random.Next(0, devList.Count())]);
                }
                //assign 3 developers to each project
                else
                {
                    var dev = devList[random.Next(0, devList.Count())];
                    project.Users.Add(dev);
                    devList.Remove(dev);
                    var dev2 = devList[random.Next(0, devList.Count())];
                    project.Users.Add(dev2);
                    project.Users.Add(devList[random.Next(0, devList.Count())]);
                }

                //assign the project manager
                var pmList = new List<ApplicationUser>();
                foreach (var user in userList)
                {
                    if (userManager.IsInRole(user.Id, "Project Manager"))
                    {
                        pmList.Add(user);
                    }
                }
                //demo project Manager gets at least two projects
                if (i < 2)
                {
                    var demoPm = userManager.FindByEmail("ProjectManager@mailinator.com");
                    project.Users.Add(demoPm);
                    project.ProjectManager = demoPm.FullName;
                }
                else
                {
                    var pm = pmList[random.Next(0, pmList.Count())];
                    project.Users.Add(pm);
                    project.ProjectManager = pm.FullName;
                }

                context.Projects.Add(project);
                context.SaveChanges();

                CreateRandomTickets(project.Id, tickets);
            }
        }

        public void CreateRandomTickets(int projectId, int number)
        {
            //Create 'number' tickets per project
            for (var i = 0; i < number; i++)
            {
                var title = string.Concat("Ticket ", i + 1," ", context.Projects.Find(projectId).Name);
                var random = new Random();

                var ticket = new Ticket();
                ticket.Created = DateTimeOffset.Now;
                ticket.Title = title;
                ticket.Active = true;
                ticket.Id = i + 1;
                ticket.ProjectId = projectId;
                ticket.Description = "This is a seeded ticket.";

                var userList = userManager.Users.ToList();

                //assign the ticket a random submitter
                var subList = new List<ApplicationUser>();
                foreach (var user in userList)
                {
                    if (userManager.IsInRole(user.Id, "Submitter"))
                    {
                        subList.Add(user);
                    }
                }

                //demo submitter owns at least 2*number tickets
                if (projectId < 3)
                {
                    var demoSub = userManager.FindByEmail("Submitter@mailinator.com");
                    ticket.OwnerUserId = demoSub.Id;
                }
                else
                {
                    ticket.OwnerUserId = subList[random.Next(0, subList.Count())].Id;
                }

                //assign the ticket a random developer and set initial status
                var devList = new List<ApplicationUser>();
                foreach (var user in userList)
                {
                    if (userManager.IsInRole(user.Id, "Developer") && context.Projects.FirstOrDefault(p => p.Id == projectId).Users.Any(u => u.Id == user.Id))
                    {
                        devList.Add(user);
                    }
                }

                //demo developer gets at least 2*number tickets
                if (projectId < 3)
                {
                    var demoDev = userManager.FindByEmail("Developer@mailinator.com");
                    ticket.AssignedToUserId = demoDev.Id;
                    ticket.TicketStatusId = context.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Assigned").Id;
                }
                else
                {
                    ticket.AssignedToUserId = devList[random.Next(0, devList.Count())].Id;
                    ticket.TicketStatusId = context.TicketStatuses.FirstOrDefault(t => t.Name == "Open/Assigned").Id;
                }

                //assign a random type
                var typeList = context.TicketTypes.Select(t => t.Id).ToList();
                ticket.TicketTypeId = typeList[random.Next(0, typeList.Count())];

                //assign a random priority
                var priorityList = context.TicketPrioritys.Select(t => t.Id).ToList();
                ticket.TicketPriorityId = priorityList[random.Next(0, priorityList.Count())];

                context.Tickets.Add(ticket);
                context.SaveChanges();
            }        
        }
    }
}