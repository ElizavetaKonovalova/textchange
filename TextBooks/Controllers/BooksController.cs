using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TextBooks.App_Start;
using TextBooks;
using TextBooks.Models;
using System.Security.Claims;


namespace TextBooks.Controllers
{
    public class BooksController : Controller
    {
        private IFB299Entities db = new IFB299Entities();
        private SharedMethods shared = new SharedMethods();

        public ActionResult Index()
        {
            return View(db.Books.OrderBy(x=>x.Title).ToList());
        }

        [HttpPost]
        public ActionResult Index(string searchQuery)
        {
            if (searchQuery != "" && searchQuery != null)
            {
                List<Book> result = (from table in db.Books
                                     where ((
                                     table.ISBN.Contains(searchQuery) ||
                                     table.Title.Contains(searchQuery) ||
                                     table.Author.Contains(searchQuery) ||
                                     table.Edition.Contains(searchQuery) ||
                                     table.Year.Contains(searchQuery)) && 
                                     table.BrwdBy.Equals(null)
                                     )
                                     select table).OrderBy(x => x.Title).ToList();
                if (result != null)
                {
                    return View(result);
                }
            }
            return View(db.Books.Where(x=>x.BrwdBy.Equals(null)).OrderBy(x => x.Title).ToList());
        }

        // GET: Books/Create
        public ActionResult Create()
        {
            return View();
        }

        // Query the GoogleBooksAPI for 
        public ActionResult GoogleBooksQuery(string term)
        {
            if (term == "" || term == " ") return null;

            var booksService = new Google.Apis.Books.v1.BooksService();
            
            var query = booksService.Volumes.List(term);
            query.LangRestrict = "en";
            query.MaxResults = 10;
            var results = query.Execute();


            // TODO Check for errors (here?)

            // Get required details
            var titlesList = new List<string>();
            var authorsList = new List<string>();
            var isbnList = new List<string>();
            var yearList = new List<string>();

            int i = 0;
            while(i < results.Items.Count && titlesList.Count < 5)
            {
                if ((results.Items[i].VolumeInfo.Title != null)
                    && (results.Items[i].VolumeInfo.Authors != null)
                    && (results.Items[i].VolumeInfo.IndustryIdentifiers != null)
                    && (results.Items[i].VolumeInfo.PublishedDate != null))
                {
                    if (!titlesList.Contains(results.Items[i].VolumeInfo.Title))
                    {
                        titlesList.Add(results.Items[i].VolumeInfo.Title);
                        authorsList.Add(results.Items[i].VolumeInfo.Authors[0]);
                        if (results.Items[i].VolumeInfo.Authors.Count > 1)
                        {
                            authorsList[authorsList.Count-1] += (", " + (results.Items[i].VolumeInfo.Authors[1]));
                        }
                        if (results.Items[i].VolumeInfo.Authors.Count > 2)
                        {
                            authorsList[authorsList.Count - 1] += (", " + (results.Items[i].VolumeInfo.Authors[2]));
                        }
                        isbnList.Add(results.Items[i].VolumeInfo.IndustryIdentifiers.FirstOrDefault().Identifier);
                        yearList.Add(results.Items[i].VolumeInfo.PublishedDate.Substring(0, 4));
                    }
                }
                i++;
            }

            List<string>[] resultsObj = { titlesList, authorsList, isbnList, yearList };
            return Json(resultsObj, JsonRequestBehavior.AllowGet);
        }


        // POST: Books/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //Use book.Title for example to get title of the book just submitted.
        //Bind takes all those arguments and makes a book object that it saves to DB
        public ActionResult Create([Bind(Include = "B_ID,ISBN,Title,Author,Edition,Year")] Book book)
        {
            var name = ClaimsPrincipal.Current.Identity.Name;
            if (ModelState.IsValid)
            {
                if (name != "")
                {

                    bool failed = false;

                    if (book.ISBN == null)
                    {
                        ModelState.AddModelError("", "Book ISBN field can't be empty.");
                        failed = true;
                    }
                    if (book.Title == null)
                    {
                        ModelState.AddModelError("", "Book Title field can't be empty.");
                        failed = true;
                    }
                    if (book.Author == null)
                    {
                        ModelState.AddModelError("", "Book Author field can't be empty.");
                        failed = true;
                    }
                    if (book.Year == null || book.Year.Length == 0|| book.Year.Length > 4)
                    {
                        ModelState.AddModelError("", "Book Year field can't be empty or contain more than 4 digits");
                        failed = true;
                    }
                    if (book.Edition == null)
                    {
                        ModelState.AddModelError("", "Book Edition field can't be empty.");
                        failed = true;
                    }

                    if (failed == true) {
                        return View();
                    }

                    db.Books.Add(book);
                    book.Owner = name;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "You must be logged in to submit books.");
                    return View();
                }
            }

            return View(book);
        }

        // GET: Books/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Book book = db.Books.Find(id);
            if (book == null)
            {
                return HttpNotFound();
            }
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "B_ID,ISBN,Title,Author,Edition,Year,Owner")] Book book)
        {
            var name = ClaimsPrincipal.Current.Identity.Name;
            if (ModelState.IsValid)
            {
                if (name != "")
                {

                    bool failed = false;

                    if (book.ISBN == null)
                    {
                        ModelState.AddModelError("", "Book ISBN field can't be empty.");
                        failed = true;
                    }
                    if (book.Title == null)
                    {
                        ModelState.AddModelError("", "Book Title field can't be empty.");
                        failed = true;
                    }
                    if (book.Author == null)
                    {
                        ModelState.AddModelError("", "Book Author field can't be empty.");
                        failed = true;
                    }
                    if (book.Year == null || book.Year.Length == 0 || book.Year.Length > 4)
                    {
                        ModelState.AddModelError("", "Book Year field can't be empty or contain more than 4 digits");
                        failed = true;
                    }
                    if (book.Edition == null)
                    {
                        ModelState.AddModelError("", "Book Edition field can't be empty.");
                        failed = true;
                    }

                    if (failed == true)
                    {
                        return View();
                    }
                }
                db.Entry(book).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("../Manage/ViewMyBooks");
            }
            return View(book);
        }

        // GET: Books/Delete/id
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Book book = db.Books.Find(id);
            if (book == null)
            {
                return HttpNotFound();
            }
            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Book book = db.Books.Find(id);
            db.Books.Remove(book);
            db.SaveChanges();
            return RedirectToAction("../Manage/ViewMyBooks");
        }

        // GET: Books/Details/5
        public ActionResult Details(int? id, string username)
        {
            var loggedIn = ClaimsPrincipal.Current.Identity.IsAuthenticated;
            if (!loggedIn)
            {
                return View("../Account/Register");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var books = (from book in db.Books
                         where book.B_ID == id
                         select new ViewMyBooks
                         {
                             Author = book.Author,
                             BookTitle = book.Title,
                             Edition = book.Edition,
                             ISBN = book.ISBN,
                             Year = book.Year,
                             B_ID = book.B_ID,
                             Owner = book.Owner
                         }).AsEnumerable();

            ViewMyBooks model = new ViewMyBooks
            {
                targetUser = (from table in db.AspNetUsers
                              where table.UserName == username
                              select table).FirstOrDefault(),
                BookDetails = books,
                B_ID = books.Select(x=>x.B_ID).Single()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(ViewMyBooks model, string toUsername, int bookID)
        {

            var bookDetails = db.Books.Find(bookID);

            // Check that the model has been passed in with a valid mail message
            Email mailMessage = model.contactEmail;

            // Get required details about the user receiving the email
            AspNetUser toUser = (from table in db.AspNetUsers
                                 where table.UserName == toUsername
                                 select table).FirstOrDefault();

            if (mailMessage.message == null)
            {
                // No email content to send, don't send it empty and let the view know it wasn't sent.
                return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "error" });
            }

            // Get the currently logged in user
            string fromUsername = ClaimsPrincipal.Current.Identity.Name;
            //&& !toUsername.Equals(fromUsername)
            if (fromUsername != "" && fromUsername != null)
            {
                if ((!fromUsername.Equals(toUsername) && String.IsNullOrEmpty(bookDetails.BrwdBy) == true) 
                    || String.IsNullOrEmpty(bookDetails.BrwdBy) == true)
                {
                    // Get required details about the user sending the email
                    AspNetUser fromUser = (from table in db.AspNetUsers
                                           where table.UserName == fromUsername
                                           select table).FirstOrDefault();

                    // Check the users were received successfully
                    if (fromUser == null || toUser == null)
                    {
                        return View("Error");
                    }

                    // Setup the Email with all the required info
                    mailMessage.fromName = fromUser.FirstName + " " + fromUser.LastName;
                    mailMessage.fromAddress = fromUser.Email;
                    mailMessage.toName = toUser.FirstName + " " + toUser.LastName;
                    mailMessage.toAddress = toUser.Email;
                    mailMessage.subject = "Contact from Texchange";

                    // Swap out our new lines chars for html line breaks in order to preserve formatting.
                    mailMessage.message = mailMessage.message.Replace(System.Environment.NewLine, "<br />");

                    // Wrap the message in a default template
                    mailMessage.message = "Hi " + toUser.FirstName + ",<br /><br />You've received a request from "
                        + fromUser.FirstName + " " + fromUser.LastName + " to borrow your book: <br/><br/> <strong>Title:</strong> "
                        + bookDetails.Title + "<br/> <strong>Year:</strong> " + bookDetails.Year
                        + "<br/><strong> Author: </strong>" + bookDetails.Author + "<br/><br/> on Texchange:<br /><br /><em>"
                        + "Hi " + toUser.FirstName + ",<br/>" + mailMessage.message
                        + "</em><br /><br />" + "You may <button class='btn btn-danger '>Accept</button> the request or <button>Decline</button> it. <br/><b>You can reply to this email to contact "
                        + fromUser.FirstName + ".</b><br /><br />" + "Kind Regards,<br />The Texchange Team";

                    // Send the email
                    bool result = shared.SendEmailMessage(mailMessage);
                    if (result)
                    {
                        toUser.Notified += 1;
                        var request = new Request();
                        request.RequestFrom = fromUser.UserName;
                        request.UserID = toUser.Id;
                        request.RequestText = "Request to borrow " + bookDetails.Title + ", " + bookDetails.Author + ", " + bookDetails.Year + ".";
                        request.BookId = bookDetails.B_ID;
                        db.Requests.Add(request);
                        db.SaveChanges();
                        return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "success" });
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Test Success!");
                    failed = true;
                    return RedirectToAction("Details", "Books", new { id = bookDetails.B_ID, username = toUser.UserName });
                }
            }
            // One of the user accounts wasn't able to be received, or something else went wrong
            // Perhaps a user account was deleted while an message was being written?
            // Let's go to the home page and start again.
            return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "yourself" });
        }

        public ActionResult ListAllBookTitles(int quantity)
        {
            List<string> titles = (from table in db.Books
                                   select table.Title).ToList();
            if (quantity != 0) titles.RemoveRange(quantity, titles.Count - quantity);
            return Json(titles.ToArray());
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
