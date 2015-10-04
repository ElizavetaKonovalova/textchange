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

        [AllowAnonymous]
        public ActionResult VerifyEmail()
        {
            return View();
        }

        public static string TokensCount()
        {
            return "123";
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
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

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email.Split('@')[0], Email = model.Email };

                // Check email address is okay
                string addressSuffix = user.Email.Split('@')[1];
                if (!addressSuffix.Equals("connect.qut.edu.au") || user.Email == "ifb299books@gmail.com")
                {
                    AddErrors("The email address is not valid! Use @connect.qut.edu.au");
                    return View(model);
                }
                
                // Create the account
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Create Entities object for database access
                    IFB299Entities db = new IFB299Entities();
                    var currentUser = (from table in db.AspNetUsers
                                       where table.Email == user.Email
                                       select table).FirstOrDefault();

                    if (currentUser != null)
                    {                      
                        // Add details to database record
                        currentUser.FirstName = model.FirstName;
                        currentUser.LastName = model.LastName;
                        currentUser.PhoneNumber = model.ContactNumber;
                        currentUser.Tokens = 1;
                        db.SaveChanges();

                        // Send an email with link to a confirmation code, used to verify their account
                        string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        SendVerificationEmail(code, callbackUrl, currentUser.FirstName, currentUser.Email);

                        // If the page user is the admin show them the accounts, otherwise prompt them to check their email.
                        if (user.UserName.Equals("ifb299books"))
                        {
                            return RedirectToAction("ViewAccounts", "Account");
                        }
                        else
                        {
                            return RedirectToAction("VerifyEmail", "Account");
                        }
                    }

                }
                // If we failed to create the user account
                // (or if the account was created but then couldn't be found (i.e. database errors))
                AddErrors(result);
            }
            // If we got this far, something failed. Let's redisplay the Register form (with errors).
            return View(model);
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

        private void SendResetPasswordEmail(string code, string callbackURL, string email)
        {

            var user = db.AspNetUsers.Where(x => x.Email == email).Select(x => x.FirstName).First().ToString();
            try
            {
                var body = "Hello " +user + ", click the link to reset your password <a href =\"" + callbackURL + "\">here</a>.";
                var message = new MailMessage();
                message.To.Add(new MailAddress(email));  // replace with valid value 
                message.From = new MailAddress("ifb299books@gmail.com", "noreply");  // replace with valid value
                message.Subject = "Verify Texchange Account";
                message.Body = string.Format(body, "noreply", "ifb299books@gmail.com", message);
                message.IsBodyHtml = true;

                // Init SmtpClient and send
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                NetworkCredential credentials = new NetworkCredential("ifb299", "IFB299Password");
                smtpClient.Credentials = credentials;
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Email.Contains("connect.qut.edu.au") || !model.Email.Equals("ifb299books@gmail.com"))
                {
                    var user = await UserManager.FindByEmailAsync(model.Email);
                    if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return View("ForgotPasswordConfirmation");
                    }

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                    SendResetPasswordEmail(code, callbackUrl, user.Email);

                    return RedirectToAction("ForgotPasswordConfirmation", "Account");
                }
                else 
                {
                    AddErrors("You have typed the wrong email! Use @connect.qut.edu.au");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        // don't sign in
                        //await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        //GET: /Account/ViewAccounts
        public ActionResult ViewAccounts()
        {
            ViewAccounts returnView = new ViewAccounts()
            {
                ifbEntity = GetAllAccounts().OrderBy(x=>x.FirstName)
            };

            return View(returnView);
        }

        //
        //POST: /Account/ViewAccounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewAccounts(ViewAccounts model, string id)
        {
            var findUser = db.AspNetUsers.Find(id);
            var findBook = db.Books.Where(x => x.Owner.Equals(findUser.UserName)).Select(x => x);

            if (!findUser.UserName.Equals("ifb299books")) 
            {
                var deleteUser = db.AspNetUsers.Remove(findUser);
                if (findBook != null) 
                {
                    var deleteBook = db.Books.RemoveRange(findBook);
                }
                db.SaveChanges();
            }

            model = new ViewAccounts
            {
                ifbEntity = GetAllAccounts().OrderBy(x=>x.FirstName)    
            };

            return View(model);
        }

        public static string getTokens(string id)
        {
            IFB299Entities db = new IFB299Entities();
            var user = db.AspNetUsers.Find(id);
            string tokenCount = user.Tokens.ToString();
            return tokenCount;
        }

        public ActionResult Edit(ViewAccounts model, string id)
        {
            var user = db.AspNetUsers.Find(id);

            if( user == null)
            {
                return HttpNotFound();
            }

            model = new ViewAccounts
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Email = user.Email
            };

            return View(model);
        }

        //
        // POST: /Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,FirstName,LastName,Phone,Email")]ViewAccounts model)
        {
            var editedUser = db.AspNetUsers.Find(model.Id);
            editedUser.Email = model.Email;
            editedUser.PhoneNumber = model.Phone;
            editedUser.LastName = model.LastName;
            editedUser.FirstName = model.FirstName;
            editedUser.Id = model.Id;

            if (ModelState.IsValid)
            {
                db.Entry(editedUser).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("/ViewAccounts");
            }
            else 
            {
                return HttpNotFound();
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

        public IEnumerable<ViewAccounts> GetAllAccounts()
        {
            return db.AspNetUsers.Select(x => new ViewAccounts 
        {
                Phone = x.PhoneNumber.ToString(), 
                Email = x.Email, Id = x.Id, 
                FirstName = x.FirstName,
                LastName = x.LastName

            }).AsEnumerable();
        }
        
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private void AddErrors(string customError)
        {
            ModelState.AddModelError("", customError);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}