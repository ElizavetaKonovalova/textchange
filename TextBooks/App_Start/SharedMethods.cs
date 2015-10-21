using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TextBooks.Models;
using System.Net.Mail;
using System.Net;

namespace TextBooks.App_Start
{
    public class SharedMethods : Controller
    {
        private IFB299Entities db = new IFB299Entities();

        // GET: SharedMethods
        public ActionResult Index()
        {
            return View();
        }

        public bool SendEmailMessage(Email model)
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

                string templatesJson = "{\"filters\": {\"templates\": {\"settings\": {\"enable\": 1, \"template_id\": \"1f7bf5b2-1ad2-4c63-b0b4-b9898905ea4d\"}}}}";
                message.Headers.Add("X-SMTPAPI", templatesJson);

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

        public void AddErrors(string customError)
        {
            ModelState.AddModelError("", customError);
        }

        public Request SendRequest(string fromUser, string toUserID, string text, int bookID)
        {
            Request request = new Request();
            request.RequestFrom = fromUser;
            request.UserID = toUserID;
            request.RequestText = text;
            request.BookId = bookID;
            db.Requests.Add(request);
            db.SaveChanges();

            return request;
        }
    }
}