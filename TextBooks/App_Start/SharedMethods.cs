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
    }
}