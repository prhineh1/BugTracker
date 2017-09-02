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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BugTracker.Controllers
{   [Authorize]
    public class TicketCommentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private TicketDelta ticketDelta = new TicketDelta();

        // POST: TicketComments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Comment,TicketId")] TicketComment ticketComment)
        {
            if (ticketComment.Comment == null)
            {
                return RedirectToAction("details", "Tickets", new { id = ticketComment.TicketId });
            }

            Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
            if (tagRegex.IsMatch(ticketComment.Comment))
            {
                TempData["warning"] = "warn";
                return RedirectToAction("details", "Tickets", new { id = ticketComment.TicketId });
            }

            var oldComment = db.Tickets.AsNoTracking().Include(t => t.TicketComments).FirstOrDefault(p => p.Id == ticketComment.TicketId);

            if (ModelState.IsValid)
            {
                ticketComment.UserId = User.Identity.GetUserId();
                ticketComment.Created = DateTimeOffset.Now;
                db.TicketComments.Add(ticketComment);
                db.SaveChanges();

                var newComment = db.Tickets.Include(t => t.TicketComments).FirstOrDefault(p => p.Id == ticketComment.TicketId);

                ticketDelta.CommentCompare(oldComment, newComment, User.Identity.GetUserId());

                return RedirectToAction("details", "Tickets", new { id = ticketComment.TicketId});
            }

            return RedirectToAction("details", "Tickets", new { id = ticketComment.TicketId });
        }

        //// GET: TicketComments/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    TicketComment ticketComment = db.TicketComments.Find(id);
        //    if (ticketComment == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketComment.TicketId);
        //    ViewBag.UserId = new SelectList(db.Users, "Id", "FirstName", ticketComment.UserId);
        //    return View(ticketComment);
        //}

        //// POST: TicketComments/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Id,Comment,Created,TicketId,UserId")] TicketComment ticketComment)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(ticketComment).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketComment.TicketId);
        //    ViewBag.UserId = new SelectList(db.Users, "Id", "FirstName", ticketComment.UserId);
        //    return View(ticketComment);
        //}

        // GET: TicketComments/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    TicketComment ticketComment = db.TicketComments.Find(id);
        //    if (ticketComment == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(ticketComment);
        //}

        // POST: TicketComments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var ticketId = db.TicketComments.FirstOrDefault(i => i.Id == id).TicketId;
            TicketComment ticketComment = db.TicketComments.Find(id);
            var oldComment = db.Tickets.AsNoTracking().Include(t => t.TicketComments).FirstOrDefault(p => p.Id == ticketId);
            db.TicketComments.Remove(ticketComment);
            db.SaveChanges();

            var newComment = db.Tickets.Include(t => t.TicketComments).FirstOrDefault(p => p.Id == ticketId);

            ticketDelta.CommentCompare(oldComment, newComment, User.Identity.GetUserId());

            return RedirectToAction("Details", "Tickets", new { id = ticketId });
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
