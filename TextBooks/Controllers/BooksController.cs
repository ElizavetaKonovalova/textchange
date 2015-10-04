using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TextBooks;
using System.Security.Claims;


namespace TextBooks.Controllers
{
    public class BooksController : Controller
    {
        private IFB299Entities db = new IFB299Entities();

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
                                     where (
                                     table.ISBN.Contains(searchQuery) || 
                                     table.Title.Contains(searchQuery) ||
                                     table.Author.Contains(searchQuery) ||
                                     table.Edition.Contains(searchQuery) ||
                                     table.Year.Contains(searchQuery)
                                     )
                                     select table).OrderBy(x=>x.Title).ToList();
                if (result != null)
                {
                    return View(result);
                }
            }
            return View(db.Books.OrderBy(x => x.Title).ToList());
        }

        // GET: Books/Details/5
        public ActionResult Details(int? id)
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
            if (ModelState.IsValid)
            {
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
