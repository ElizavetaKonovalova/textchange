﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TextBooks.Models;
using TextBooks.App_Start;
using System.Security.Claims;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace TextBooks.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private IFB299Entities db = new IFB299Entities();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private SharedMethods shared = new SharedMethods();
        private static int requestID;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            bool failed = false;

            if (model.Number.Length > 14)
            {               
                ModelState.AddModelError("", "Please type the correct number. Not longer than 13 characters.");
                failed = true;
            }

            if (model.Number.Length < 8)
            {
                ModelState.AddModelError("", "Please type the correct number. Not shorter than 8 characters.");
                failed = true;
            }

            if (failed == true)
            {
                return View();
            }

            else
            {

                string currentLoggedInUser = null;
                if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
                    currentLoggedInUser = ClaimsPrincipal.Current.Identity.Name;

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                else
                {
                    var userID = db.AspNetUsers.FirstOrDefault(x => x.UserName == currentLoggedInUser);
                    userID.PhoneNumber = model.Number;
                    userID.ContactNumber = model.Number;
                    try
                    {
                        db.Entry(userID).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    catch (DbEntityValidationException dbEx)
                    {
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                Trace.TraceInformation("Property: {0} Error: {1}",
                                                        validationError.PropertyName,
                                                        validationError.ErrorMessage);
                            }
                        }
                    }
                }
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
        }

        private void AddErrors(string p)
        {
            ModelState.AddModelError("", p);
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // GET: /Manage/RemovePhoneNumber
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        //GET: /Manage/ViewMyBooks
        public ActionResult ViewMyBooks()
        {
            string currentLoggedInUser = null;
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
                currentLoggedInUser = ClaimsPrincipal.Current.Identity.Name;

            var books = (from book in db.Books
                         where book.Owner == currentLoggedInUser
                       select new ViewMyBooks { Author = book.Author, BookTitle = book.Title,
                           Edition = book.Edition, ISBN = book.ISBN, Year = book.Year, B_ID = book.B_ID, 
                           Owner = book.Owner }).AsEnumerable();

            ViewMyBooks model = new ViewMyBooks
            {
                BookDetails = books
            };

            return View(model);
        }

        private void setRequestID(int id)
        {
            requestID = id;
        }

        private int getRequestID()
        {
            return requestID;
        }

        public ActionResult RateBorrower(int id, bool rated)
        {
            if (id == 0)
            {
                var requests = db.Requests.Find(getRequestID());

                if (requests != null)
                {
                    //Remove the found request from the database.
                    db.Requests.Remove(requests);
                    db.SaveChanges();
                }

                return Redirect("../RequestsToBorrow");
            }

            else
            {
                var bookS = db.Books.Find(id);
                int rid = RateLoaner(bookS);
                setRequestID(rid);
                ViewMyBooksBorrower(id, rated);
                return Redirect("../ViewMyBooksBorrower");
            }
        }

        public int RateLoaner( Book book)
        {
            shared = new SharedMethods();
            Request request = new Request();
            //Get owner information of the book from the database.
            AspNetUser owner = db.AspNetUsers.Where(x => x.UserName == book.Owner)
                .Select(x => x).FirstOrDefault();

            //Get borrower information of the book from the database.
            AspNetUser borrow = db.AspNetUsers.Where(x => x.UserName == book.BrwdBy)
                .Select(x => x).FirstOrDefault();

            request = shared.SendRequest(borrow.UserName, owner.Id, "Please, rate "+borrow.FirstName+"!", 0);

            int requestID = request.Id;

            return requestID;
        }

        //
        //GET: /Manage/ViewMyBooksBorrower/
        [HttpGet]
        public ActionResult ViewMyBooksBorrower()
        {
            string currentLoggedInUser = null;
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
                currentLoggedInUser = ClaimsPrincipal.Current.Identity.Name;

            return View(getBorrowed(currentLoggedInUser));
        }

        private ViewMyBooks getBorrowed(string user)
        {
            var books = (from book in db.Books
                         where book.BrwdBy == user
                         select new ViewMyBooks
                         {
                             Author = book.Author,
                             BookTitle = book.Title,
                             Edition = book.Edition,
                             ISBN = book.ISBN,
                             Year = book.Year,
                             B_ID = book.B_ID,
                             Borrower = book.BrwdBy
                         }).AsEnumerable();

            AspNetUser target = db.AspNetUsers.Where(x=>x.UserName == user).Select(x=>x).FirstOrDefault();

            ViewMyBooks model = new ViewMyBooks
            {
                BookDetails = books,
                targetUser = target
            };

            return model;
        }

        //
        //POST: /Manage/ViewMyBooksBorrower/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewMyBooksBorrower(int id, bool rated)
        {
            string currentLoggedInUser = null;
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
                currentLoggedInUser = ClaimsPrincipal.Current.Identity.Name;

            var book = db.Books.Find(id);

            //Get owner information of the book from the database.
            AspNetUser owner = db.AspNetUsers.Where(x => x.UserName == book.Owner )
                .Select(x => x).FirstOrDefault();

            //Get borrower information of the book from the database.
            AspNetUser borrow = db.AspNetUsers.Where(x => x.UserName == book.BrwdBy)
                .Select(x => x).FirstOrDefault();

            if(rated == true)
            {
                Email confirmation = new Email();
                shared = new SharedMethods();
                AccountController account = new AccountController();

                confirmation.fromAddress = borrow.Email;
                confirmation.fromName = borrow.FirstName;
                confirmation.toAddress = owner.Email;
                confirmation.toName = owner.FirstName;
                confirmation.subject = "Texchange: Book returned";

                if (book.BrwdBy.Equals(currentLoggedInUser))
                {
                    book.BrwdBy = null;
                    db.SaveChanges();
                }

                confirmation.message = "Hello " + owner.FirstName + " " + owner.LastName
                + ",<br /><br/> Your book has been returned by " + borrow.FirstName + " " + borrow.LastName
                + "  :<br/><br/><b>Title:</b> " + book.Title + "<br/><b>Author</b>: "
                + book.Author + "<br/><b>Year:</b> " + book.Year + "<br/><br/>Do you think there was a <b>mistake</b>??<br/>" +
                "Contact admin ifb299books@gmail.com <br/><br/>Kind regards,<br/><b>Texchange</b>.";

                //Send request reply to the borrower.
                bool sent = shared.SendEmailMessage(confirmation);

                if (sent)
                {
                    return RedirectToAction("ViewMyBooksBorrower");
                }
            }

            return RedirectToAction("PublicProfile", "Account", new { 
                username = owner.UserName, emailsent = "", returnedBorrower = true, bookId = book.B_ID });
        }

        //
        //GET: /Manage/ViewMyBooksBorrowed/
        public ActionResult ViewMyBooksBorrowed()
        {
            string currentLoggedInUser = null;
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
                currentLoggedInUser = ClaimsPrincipal.Current.Identity.Name;

            var books = (from book in db.Books
                         where book.Owner == currentLoggedInUser && book.BrwdBy != null
                         select new ViewMyBooks
                         {
                             Author = book.Author,
                             BookTitle = book.Title,
                             Edition = book.Edition,
                             ISBN = book.ISBN,
                             Year = book.Year,
                             B_ID = book.B_ID,
                             Borrower = book.BrwdBy
                         }).AsEnumerable();

            ViewMyBooks model = new ViewMyBooks
            {
                BookDetails = books
            };

            return View(model);
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        public ActionResult RequestsToBorrow(RequestsToBorrowView request)
        {
            var userId = db.AspNetUsers.Where(x=>x.UserName == User.Identity.Name).Select(x=>x.Id).FirstOrDefault();
            var newRequest = db.Requests.Where(x => x.UserID == userId )
                .Select(x => 
                    new RequestsToBorrowView { 
                        message = x.RequestText, 
                        sender = x.RequestFrom, 
                        bookID = x.BookId, 
                        borrower = x.RequestFrom,
                        requestID = x.Id
                    }).ToList();

            request = new RequestsToBorrowView
            {
                RequestsAll = newRequest
            };

            return View(request);
        }

        //
        //POST: Manage/RequestsToBorrow/
        [HttpPost]
        public ActionResult RequestsToBorrow(string responce, int bookValue, string borrower, int requestID)
        {
            //Get borrower information of the book from the database.
            AspNetUser borrow = db.AspNetUsers.Where(x => x.UserName == borrower)
                .Select(x => x).FirstOrDefault();

            if (true)
            {

                if (responce.Equals("Rate"))
                {
                    setRequestID(requestID);

                    return RedirectToAction("PublicProfile", "Account", new
                    {
                        username = borrow.UserName,
                        emailsent = "",
                        returnedBorrower = true,
                        bookID = 0
                    });
                }
                else
                {

                    Email confirmation = new Email();
                    shared = new SharedMethods();
                    AccountController account = new AccountController();

                    //Find requested book in the database.
                    var results = db.Books.Find(bookValue);

                    //Get owner information of the book from the database.
                    AspNetUser owner = db.AspNetUsers.Where(x => x.UserName == results.Owner)
                        .Select(x => x).FirstOrDefault();

                    switch (responce)
                    {
                        case "Accept":
                            //Assign book's borrower
                            results.BrwdBy = borrower;
                            db.SaveChanges();

                            //Create message body for request reply on a successful application.
                            confirmation.message = "Hello " + borrow.FirstName + " " + borrow.LastName +
                                ",<br /><br/> You have recently sent a request to " + owner.FirstName +
                                " " + owner.LastName + " to borrow this book:<br/><br/><b>Title</b>:" + results.Title + "<br/><b>Author</b>: "
                                + results.Author + "<br/><b>Year:</b> " + results.Year +
                                "<br/><br/>Congratulations! Your request has been <b>accepted</b>"
                                + "<br/><br/>Enjoy the book!<br/>Kind regards,<br/><b>Texchange</b>.";

                            //Increase number of tokens of the book's owner.
                            account.incrementTokens(owner.Id);

                            //Decrease number of tokens the book's borrower.
                            account.decrementTokens(borrow.Id);

                            if (owner.Notified >= 1)
                            {
                                //Decrease owner's number of notifications sent.
                                owner.Notified -= 1;
                            }
                            break;
                        case "Decline":
                            //Create message body for request reply on an unsuccessful application.
                            confirmation.message = "Hello " + borrow.FirstName + " " + borrow.LastName
                                + ",<br /><br/> You have recently sent a request to " + owner.FirstName + " " + owner.LastName
                                + " to borrow this book:<br/><br/><b>Title</b>:" + results.Title + "<br/><b>Author</b>: "
                                + results.Author + "<br/><b>Year:</b> " + results.Year +
                                "<br/><br/>We are sorry, but your request was <b>rejected</b><br/><br/>Good luck!<br/>Kind regards,<br/>"
                                + "<b>Texchange</b>.";
                            break;
                    }

                    confirmation.fromAddress = owner.Email;
                    confirmation.fromName = owner.FirstName;
                    confirmation.toAddress = borrow.Email;
                    confirmation.toName = borrow.FirstName;
                    confirmation.subject = "Texchange: Book request confirmation";

                    //Send request reply to the borrower.
                    shared.SendEmailMessage(confirmation);

                    //Find this request in the database.
                    var requests = db.Requests.Find(requestID);

                    if (requests != null)
                    {
                        //Remove the found request from the database.
                        db.Requests.Remove(requests);
                        db.SaveChanges();
                    }

                    return Redirect("../Manage/RequestsToBorrow");
                }
            }
            else 
            {
                return Redirect("../Manage/RequestsToBorrow");
            }
        }

        public ActionResult Accepted(int bookValue, string borrower, int requestID)
        {
            Email confirmation = new Email();
            shared = new SharedMethods();
            AccountController account = new AccountController();

            //Find requested book in the database.
            var results = db.Books.Find(bookValue);

            //Get owner information of the book from the database.
            AspNetUser owner = db.AspNetUsers.Where(x => x.UserName == results.Owner)
                .Select(x => x).FirstOrDefault();

            //Get borrower information of the book from the database.
            AspNetUser borrow = db.AspNetUsers.Where(x => x.UserName == borrower)
                .Select(x => x).FirstOrDefault();

            //Assign book's borrower
            results.BrwdBy = borrower;
            db.SaveChanges();

            //Create message body for request reply on a successful application.
            confirmation.message = "Hello " + borrow.FirstName + " " + borrow.LastName +
                ",<br /><br/> You have recently sent a request to " + owner.FirstName +
                " " + owner.LastName + " to borrow this book:<br/><br/><b>Title</b>:" + results.Title + "<br/><b>Author</b>: "
                + results.Author + "<br/><b>Year:</b> " + results.Year +
                "<br/><br/>Congratulations! Your request has been <b>accepted</b>"
                + "<br/><br/>Enjoy the book!<br/>Kind regards,<br/><b>Texchange</b>.";

            //Increase number of tokens of the book's owner.
            account.incrementTokens(owner.Id);

            //Decrease number of tokens the book's borrower.
            account.decrementTokens(borrow.Id);

            if (owner.Notified >= 1)
            {
                //Decrease owner's number of notifications sent.
                owner.Notified -= 1;
            }

            confirmation.fromAddress = owner.Email;
            confirmation.fromName = owner.FirstName;
            confirmation.toAddress = borrow.Email;
            confirmation.toName = borrow.FirstName;
            confirmation.subject = "Texchange: Book request confirmation";

            //Send request reply to the borrower.
            shared.SendEmailMessage(confirmation);

            //Find this request in the database.
            var requests = db.Requests.Find(requestID);

            if (requests != null)
            {
                //Remove the found request from the database.
                db.Requests.Remove(requests);
                db.SaveChanges();
            }

            return Redirect("../Manage/RequestsToBorrow");
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
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

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}