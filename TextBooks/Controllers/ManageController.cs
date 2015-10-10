using System;
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
        private SharedMethods shared;

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

        //
        //GET: /Manage/ViewMyBooks
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
                    });

            if (newRequest.ToList().Count() < 1 )
            {
                newRequest = db.Requests.Select(
                    x => new RequestsToBorrowView
                    {
                        message = "Currently, you have no requests.",
                        sender = ""
                    });
            }

            request = new RequestsToBorrowView
            {
                RequestsAll = newRequest.AsEnumerable()
            };

            return View(request);
        }

        [HttpPost]
        public ActionResult RequestsToBorrow(string responce, int bookValue, string borrower, int requestID)
        {
            Email confirmation = new Email();
            shared = new SharedMethods();
            AccountController account = new AccountController();

            var results = db.Books.Find(bookValue);

            AspNetUser owner = db.AspNetUsers.Where(x => x.UserName == results.Owner)
                .Select(x => x).FirstOrDefault();

            AspNetUser borrow = db.AspNetUsers.Where(x => x.UserName == results.BrwdBy)
                .Select(x => x).FirstOrDefault();

            if (responce.Equals("Accept"))
            {
                results.BrwdBy = borrower;
                db.SaveChanges();                

                confirmation.message = "Hello " + borrow.FirstName + " " + borrow.LastName+
                    ",<br /><br/> You have recently sent a request to "+owner.FirstName+
                    " "+owner.LastName+" to borrow this book:<br/><br/><b>Title</b>:"+results.Title+"<br/><b>Author</b>: "
                    + results.Author + "<br/><b>Year:</b> " + results.Year + 
                    "<br/><br/>Congratulations! Your request has been <b>accepted</b>"
                    + "<br/><br/>Enjoy the book!<br/>Kind regards,<br/><b>Texchange</b>.";

                account.incrementTokens(owner.Id);
                account.decrementTokens(borrow.Id);

                if (owner.Notified >= 1)
                {
                    owner.Notified -= 1;
                }
            }
            else
            {
                confirmation.message = "Hello " + borrow.FirstName + " " + borrow.LastName 
                    + ",<br /><br/> You have recently sent a request to " + owner.FirstName +" " + owner.LastName 
                    + " to borrow this book:<br/><br/><b>Title</b>:" + results.Title + "<br/><b>Author</b>: "
                    + results.Author + "<br/><b>Year:</b> " + results.Year +
                    "<br/><br/>We are sorry, but your request was <b>regected</b><br/><br/>Good luck!<br/>Kind regards,<br/>"
                    + "<b>Texchange</b>.";
            }

            confirmation.fromAddress = owner.Email;
            confirmation.fromName = owner.FirstName;
            confirmation.toAddress = borrow.Email;
            confirmation.toName = borrow.FirstName;
            confirmation.subject = "Texchange: Book request confirmation";

            shared.SendEmailMessage(confirmation);

            var requests = db.Requests.Find(requestID);
            
            if(requests != null)
            {
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