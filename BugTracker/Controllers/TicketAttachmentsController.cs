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
using System.Text.RegularExpressions;

namespace BugTracker.Controllers
{   [Authorize]
    public class TicketAttachmentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private TicketDelta ticketDelta = new TicketDelta();

        // POST: TicketAttachments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Description,TicketId")] TicketAttachment ticketAttachment, HttpPostedFileBase Attachment)
        {
            if (ModelState.IsValid)
            {
                if (Attachment == null)
                {
                    return RedirectToAction("details", "Tickets", new { id = ticketAttachment.TicketId });
                }

                Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
                if (tagRegex.IsMatch(ticketAttachment.Description))
                {
                    TempData["warning"] = "warn";
                    return RedirectToAction("details", "Tickets", new { id = ticketAttachment.TicketId });
                }

                var oldAttachment = db.Tickets.AsNoTracking().Include(t => t.TicketAttachments).FirstOrDefault(p => p.Id == ticketAttachment.TicketId);
                
                if (db.TicketAttachments.Where(t => t.TicketId == ticketAttachment.TicketId).Count() > 4)
                {
                    TempData["warning"] = "max";
                    return RedirectToAction("details", "tickets", new { id = ticketAttachment.TicketId });
                }

                if (ImageUploadValidator.IsWebFriendly(Attachment))
                {

                    var notStored = true;
                    try
                    {

                        foreach (var img in Directory.GetFiles(Path.Combine(Server.MapPath("~/Uploads/"))))
                        {
                            var justImg = Path.GetFileName(img);
                            if (Path.GetFileName(Attachment.FileName) == justImg)
                            {
                                ticketAttachment.MediaUrl = "/Uploads/" + Path.GetFileName(Attachment.FileName);
                                notStored = false;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return RedirectToAction("index", "tickets");
                    }

                    if (notStored)
                    {
                        var fileName = Path.GetFileName(Attachment.FileName);
                        string completeName = DateTime.Now.ToString("hh.mm.ss.ffffff") + "_" + fileName;
                        Attachment.SaveAs(Path.Combine(Server.MapPath("~/Uploads/"), completeName));
                        ticketAttachment.MediaUrl = "/Uploads/" + completeName;
                    }
                }

                ticketAttachment.Created = DateTimeOffset.Now;
                ticketAttachment.UserId = User.Identity.GetUserId();
                db.TicketAttachments.Add(ticketAttachment);
                db.SaveChanges();

                var newAttachment = db.Tickets.Include(t => t.TicketAttachments).FirstOrDefault(p => p.Id == ticketAttachment.TicketId);

                ticketDelta.AttachmentCompare(oldAttachment, newAttachment, User.Identity.GetUserId());

                return RedirectToAction("details", "Tickets", new { id = ticketAttachment.TicketId});
            }

            return RedirectToAction("details", "Tickets", new { id = ticketAttachment.TicketId });
        }

        // GET: TicketAttachments/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    TicketAttachment ticketAttachment = db.TicketAttachments.Find(id);
        //    if (ticketAttachment == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketAttachment.TicketId);
        //    ViewBag.UserId = new SelectList(db.Users, "Id", "FirstName", ticketAttachment.UserId);
        //    return View(ticketAttachment);
        //}

        // POST: TicketAttachments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Id,MediaUrl,Description,Created,TicketId,UserId")] TicketAttachment ticketAttachment)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(ticketAttachment).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.TicketId = new SelectList(db.Tickets, "Id", "Title", ticketAttachment.TicketId);
        //    ViewBag.UserId = new SelectList(db.Users, "Id", "FirstName", ticketAttachment.UserId);
        //    return View(ticketAttachment);
        //}

        // GET: TicketAttachments/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    TicketAttachment ticketAttachment = db.TicketAttachments.Find(id);
        //    if (ticketAttachment == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(ticketAttachment);
        //}

        // POST: TicketAttachments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var ticketId = db.TicketAttachments.FirstOrDefault(i => i.Id == id).TicketId;
            var oldAttachment = db.Tickets.AsNoTracking().Include(t => t.TicketAttachments).FirstOrDefault(p => p.Id == ticketId);
            TicketAttachment ticketAttachment = db.TicketAttachments.Find(id);
            db.TicketAttachments.Remove(ticketAttachment);
            db.SaveChanges();

            var newAttachment = db.Tickets.Include(t => t.TicketAttachments).FirstOrDefault(p => p.Id == ticketId);

            ticketDelta.AttachmentCompare(oldAttachment, newAttachment, User.Identity.GetUserId());

            return RedirectToAction("Details", "Tickets", new { id = ticketId});
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
