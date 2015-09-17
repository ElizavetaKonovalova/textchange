// This is the controller used to create new books. I did a little bit of working adding some checks
// to ensure that various fields couldn't be empty and that the user had to be logged in before submitting a book.
// For full code see controllers/BookControllers.cs
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
                    if (book.Year == null)
                    {
                        ModelState.AddModelError("", "Book Year field can't be empty.");
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

