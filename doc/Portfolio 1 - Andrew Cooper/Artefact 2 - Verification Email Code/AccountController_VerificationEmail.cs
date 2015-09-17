// Note: 
//      This file represents the work relating to verifying the
//      user's account by sending them a verification email.
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
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user account is verified
            IFB299Entities db = new IFB299Entities();
            var user = (from table in db.AspNetUsers
                        where model.Username == table.UserName
                        select table).FirstOrDefault();
            if (user != null)
            {
                if (!user.EmailConfirmed)
                {
                    // If they're unverified, send them to the appropriate view.
                    return View("VerifyEmail");
                }
            }

            // "shouldLockout: false" means that users won't get locked out for repeated incorrect passwords. This is okay for now.
            var result = await SignInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        private void SendVerificationEmail(string code, string callbackURL, string firstName, string email)
        {
            try
            {
                string body = "Hi " + firstName + ",<br />Please confirm your account by clicking <a href =\""
                    + callbackURL + "\">here</a>.<br/>Kind Regards,<br/>The Texchange Team";

                // Setup MailMessage ready for sending verification email
                MailMessage message = new MailMessage();
                message.To.Add(new MailAddress(email, firstName)); 
                message.From = new MailAddress("ifb299books@gmail.com", "noreply");
                message.Subject = "Verify Texchange Account";
                message.Body = string.Format(body);
                message.IsBodyHtml = true;
                
                // Init SmtpClient with credentials from the SendGrid account
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                NetworkCredential credentials = new NetworkCredential("ifb299", "IFB299Password");
                smtpClient.Credentials = credentials;

                // Send the email
                smtpClient.Send(message);
            }
            catch (Exception e)
            {
                // If somethign went wrong, write the error to the console for debugging.
                Console.WriteLine(e.Message);
            }
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [AllowAnonymous]
        public ActionResult VerifyEmail()
        {
            return View();
        }
    }
}