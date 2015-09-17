// Note: 
//      This file represents the work relating to the public
//      profile view, and the contact user (email) functionality.
//
//      All unrelated methods have been stripped out to make it
//      easier to read. For the full file, see 
//      AccountController.cs in ~/Controllers/Accounts/
//
//      Andrew Cooper 2015

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TextBooks.Models;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using System.Net.Mime;
using System.Data.Entity;

namespace TextBooks.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private IFB299Entities db = new IFB299Entities();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        //
        // GET: /Account/PublicProfile
        [AllowAnonymous]
        public ActionResult PublicProfile(string username, string emailsent)
        {
            // Check user is logged in. If not, send them to the Register page.
            var loggedIn = ClaimsPrincipal.Current.Identity.IsAuthenticated;
            if (!loggedIn)
            {
                return View("Register");
            }
            
            // Create instance of Entities object for database access
            IFB299Entities db = new IFB299Entities();

            // Create emtpy model
            PublicProfileViewModel result = new PublicProfileViewModel();

            // Get the target user
            result.targetUser = (from table in db.AspNetUsers
                        where table.UserName == username
                        select table).FirstOrDefault();

            // Check the user was found
            if (result.targetUser != null)
            {
                // List all the books owned by the target user
                result.booksOwned = (from table in db.Books
                              where table.Owner == username
                              select table).ToList();
                if (result.booksOwned.Count == 0) result.booksOwned = null;

                // List all the books being borrowed by the target user
                result.booksBorrowed = (from table in db.Books
                                         where table.BrwdBy == username
                                         select table).ToList();
                if (result.booksBorrowed.Count == 0) result.booksBorrowed = null;

                // If we've already tried to send a contact email, save this into 
                // the model so the view can display an appropriate message
                if (emailsent == "success")
                {
                    result.contactEmail = new Email();
                    result.contactEmail.success = true;
                }
                if (emailsent == "error")
                {
                    result.contactEmail = new Email();
                    result.contactEmail.success = false;
                }
                
                // Done
                return View(result);
            }
            // There should be no links to users that don't exist!
            // If for some reason there is one, error so that we fix it.
            return View("Error");
        }
        
        private bool SendEmailMessage(Email model)
        {
            try
            {
                // Get message from Email class
                string body = model.message;

                // Setup a new MailMessage to send to target user
                var message = new MailMessage();
                message.To.Add(new MailAddress(model.toAddress, model.toName));
                message.From = new MailAddress(model.fromAddress, model.fromName);
                message.Subject = model.subject;
                message.Body = string.Format(body);
                message.IsBodyHtml = true;

                // Init SmtpClient with credentials for the SendGrid Account
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                NetworkCredential credentials = new NetworkCredential("ifb299", "IFB299Password");
                smtpClient.Credentials = credentials;

                // Send the email
                smtpClient.Send(message);

                // If we got this far, the email has been sent. 
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult ContactUser(PublicProfileViewModel model, string toUsername)
        {
            // Check that the model has been passed in with a valid mail message
            Email mailMessage = model.contactEmail;
            if (mailMessage.message == null)
            {
                // No email content to send, don't send it empty and let the view know it wasn't sent.
                return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "error" });
            }

            // Get the currently logged in user
            string fromUsername = ClaimsPrincipal.Current.Identity.GetUserName();
            if (fromUsername != "" && fromUsername != null)
            {
                // Create Entity object for database access
                IFB299Entities db = new IFB299Entities();

                // Get required details about the user sending the email
                AspNetUser fromUser = (from table in db.AspNetUsers
                                 where table.UserName == fromUsername
                                 select table).FirstOrDefault();

                // Get required details about the user receiving the email
                AspNetUser toUser = (from table in db.AspNetUsers
                              where table.UserName == toUsername
                              select table).FirstOrDefault();

                // Check the users were received successfully
                if (fromUser == null || toUser == null)
                {
                    return View("Error");
                }

                // Setup the Email with all the required info
                mailMessage.fromName = fromUser.FirstName + " " + fromUser.LastName;
                mailMessage.fromAddress = fromUser.Email;
                mailMessage.toName = toUser.FirstName+ " " + toUser.LastName;
                mailMessage.toAddress = toUser.Email;
                mailMessage.subject = "Contact from Texchange";

                // Swap out our new lines chars for html line breaks in order to preserve formatting.
                mailMessage.message = mailMessage.message.Replace(System.Environment.NewLine, "<br />");

                // Wrap the message in a default template
                mailMessage.message = "Hi " + toUser.FirstName + ",<br /><br />You've received a message from "
                    + fromUser.FirstName + " " + fromUser.LastName + " on Texchange:<br /><br /><em>"
                    + "Hi " + toUser.FirstName + ",<br/>" + mailMessage.message
                    + "</em><br /><br />" + "<b>You can reply to this email to contact " + fromUser.FirstName
                    + ".</b><br /><br />" + "Kind Regards,<br />The Texchange Team";

                // Send the email
                bool result = SendEmailMessage(mailMessage);
                if (result)
                {
                    return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "success" });
                }
            }

            // One of the user accounts wasn't able to be received, or something else went wrong
            // Perhaps a user account was deleted while an message was being written?
            // Let's go to the home page and start again.
            return RedirectToAction("Index", "Home");
        }
    }
}