using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TextBooks.App_Start;
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
            List<Book> result = db.Books.Where(x=>x.BrwdBy == null).OrderBy(x => x.Title).ToList();
            return View(result);
        }

        [HttpPost]
        public ActionResult Index(string searchQuery)
        {
            var result = new List<Book>();
            if (searchQuery != "" && searchQuery != null)
            {
                result = (from table in db.Books
                                     where ((
                                     table.ISBN.Contains(searchQuery) ||
                                     table.Title.Contains(searchQuery) ||
                                     table.Author.Contains(searchQuery) ||
                                     table.Edition.Contains(searchQuery) ||
                                     table.Year.Contains(searchQuery)) && 
                                     table.BrwdBy == null
                                     )
                                     select table).OrderBy(x => x.Title).ToList();
                if (result != null)
                {
                    return View(result);
                }
            }
            result = db.Books.Where(x=>x.BrwdBy == null).OrderBy(x => x.Title).ToList();
            return View(result);
        }

        //Code that checks the validity of various fields relating to book adding/editing.
        bool bookErrorCheck(Book book)
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
            if (book.Year == null)
            {
                ModelState.AddModelError("", "Book Year field can't be empty.");
                failed = true;
            }
            else
            {
                if (book.Year.Length != 4)
                {
                    ModelState.AddModelError("", "Book Year field must be four characters.");
                    failed = true;
                }
                if (!AccountController.isDigitsOnly(book.Year))
                {
                    ModelState.AddModelError("", "Book Year field must be only numbers.");
                    failed = true;
                }
            }
            if (book.Edition == null)
            {
                ModelState.AddModelError("", "Book Edition field can't be empty.");
                failed = true;
            }
            return failed;
        }

        // GET: Books/Create
        public ActionResult Create()
        {
            return View();
        }

        // ActionResult GetAutocompleteList(string term)
        // This method gets the list of books for Autocomplete of the
        // "Create Books" form. It includes lists of titles, authors, ISBNs,
        // and Years of books. This data is attained by querying the Google
        // Books API with the private GoogleBooksQuery() method.
        public ActionResult GetAutocompleteList(string term)
        {
            // Check search term exists
            if (term == "" || term == " ") return null;
            // Query the Google Books API for possible matches
            var queryResult = GoogleBooksQuery(term);
            // Get relevant data from query result
            var dataLists = ExtractBookData(queryResult);
            // Format data as Json object and return
            return Json(dataLists, JsonRequestBehavior.AllowGet);
        }

        // List<string>[] ExtraBookData(Volumes volumes)
        // This method extracts the required data from Google Books API Volumes
        // data structure, for use in the Create view. In the case that this method
        // fails, it will return a list array of empty lists.
        List<string>[] ExtractBookData(Google.Apis.Books.v1.Data.Volumes volumes)
        {
            // Check volumes object is valid
            if (volumes == null || volumes.Items.Count == 0) return null;

            // Setup lists (we're going to package data into a Json object)
            var titlesList = new List<string>();
            var authorsList = new List<string>();
            var isbnList = new List<string>();
            var yearList = new List<string>();

            // For each book in the query result, add details to the data lists
            int i = 0;
            while (i < volumes.Items.Count && titlesList.Count < 5)
            {
                // Check required fields are included in record
                if ((volumes.Items[i].VolumeInfo.Title != null)
                    && (volumes.Items[i].VolumeInfo.Authors != null)
                    && (volumes.Items[i].VolumeInfo.IndustryIdentifiers != null)
                    && (volumes.Items[i].VolumeInfo.PublishedDate != null)
                    // and that we this isn't a duplicate record
                    && (!titlesList.Contains(volumes.Items[i].VolumeInfo.Title)))
                {
                    // Add the data to the prepared lists
                    titlesList.Add(volumes.Items[i].VolumeInfo.Title);
                    isbnList.Add(volumes.Items[i].VolumeInfo.IndustryIdentifiers.FirstOrDefault().Identifier);
                    yearList.Add(volumes.Items[i].VolumeInfo.PublishedDate.Substring(0, 4));
                    authorsList.Add(volumes.Items[i].VolumeInfo.Authors[0]);
                    
                    // Handle 2 or 3 authors
                    if (volumes.Items[i].VolumeInfo.Authors.Count > 1)
                    {
                        authorsList[authorsList.Count - 1] += (", " + (volumes.Items[i].VolumeInfo.Authors[1]));
                    }
                    if (volumes.Items[i].VolumeInfo.Authors.Count > 2)
                    {
                        authorsList[authorsList.Count - 1] += (", " + (volumes.Items[i].VolumeInfo.Authors[2]));
                    }
                }
                i++;
            }

            // Group the lists into another list for Json encoding
            List<string>[] resultsObj = { titlesList, authorsList, isbnList, yearList };
            return resultsObj;
        }

        // Volumes GoogleBooksQuery(string searchTerm)
        // This method queries the GoogleBooksAPI for volumes relating to searchTerm,
        // and returns the resulting Volumes object. In the case that the query fails,
        // this method will return null.
        Google.Apis.Books.v1.Data.Volumes GoogleBooksQuery(string searchTerm)
        {
            // Create a service object for the Google Books API
            var booksService = new Google.Apis.Books.v1.BooksService();

            // Create a query object (API auth has been provided from the Google apps side. 
            // Server set up for 2000 queries per day. If we want to exceed that, we need to
            // implement OAuth2.0 here.)
            var query = booksService.Volumes.List(searchTerm);

            // Restric to books in English
            query.LangRestrict = "en";

            // We don't need more than 10 results per query, this speeds up response time.
            query.MaxResults = 10;

            // Execute the query and record the results for user later. Also check for failure.
            var results = query.Execute();
            if (results == null || results.Items == null) return null;

            // Return results
            return results;
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

                    if (bookErrorCheck(book))
                    {
                        return View();
                    }
                    }

                    db.Books.Add(book);
                    book.Owner = name;
                    db.SaveChanges();
                    return RedirectToAction("Index");

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

                    if (bookErrorCheck(book))
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
            bool failed = false;
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
                return RedirectToAction("PublicProfile", "Account", new { username = toUsername, emailsent = "error", 
                    returnedBorrower = false, bookId = 0 });
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

                    if (db.Requests.Where(x => x.RequestFrom.Equals(fromUser.UserName) && x.BookId == bookDetails.B_ID).Count() == 0)
                    {
                        var request = shared.SendRequest(fromUser.UserName, toUser.Id, "Request to borrow "
                                + bookDetails.Title + ", " + bookDetails.Author + ", " + bookDetails.Year + ".", bookDetails.B_ID);

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
                            + "</em><br /><br />" + "You may Accept or Decline the request <a href =\"" + "http://texchange.xyz/Manage/RequestsToBorrow"
                            + "\">here</a>.<br/><b>You can reply to this email to contact "
                            + fromUser.FirstName + ".</b><br /><br />" + "Kind Regards,<br />The Texchange Team";

                        // Send the email
                        bool result = shared.SendEmailMessage(mailMessage);
                        if (result)
                        {
                            toUser.Notified += 1;
                            return RedirectToAction("PublicProfile", "Account", new
                            {
                                username = toUsername,
                                emailsent = "success",
                                returnedBorrower = false,
                                bookId = 0
                            });
                        }
                    }
                }
            }
            // One of the user accounts wasn't able to be received, or something else went wrong
            // Perhaps a user account was deleted while an message was being written?
            // Let's go to the home page and start again.
            return RedirectToAction("PublicProfile", "Account", new
            {
                username = toUsername,
                emailsent = "",
                returnedBorrower = false,
                bookId = 0
            });
        }

        // Action result to list all the book titles currently in the database
        public ActionResult ListAllBookTitles(int quantity)
        {
            // Query the database for a list of book titles
            List<string> titles = (from table in db.Books
                                   select table.Title).ToList();

            // Only send back the requested number of books
            if (quantity != 0) titles.RemoveRange(quantity, titles.Count - quantity);

            // Return the list of titles as a JSON object
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
