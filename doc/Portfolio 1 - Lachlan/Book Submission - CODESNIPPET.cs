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