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
            if (model.Number.Length > 14)
            {
                AddErrors("Please type the correct number! Not longer then 13 characters.");
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
            return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
        }

        private void AddErrors(string p)
        {
            ModelState.AddModelError("", p);
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

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }