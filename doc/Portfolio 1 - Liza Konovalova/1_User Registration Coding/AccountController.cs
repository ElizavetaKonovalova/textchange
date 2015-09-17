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
                        currentUser.ContactNumber = model.ContactNumber;
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

        private void AddErrors(string customError)
        {
            ModelState.AddModelError("", customError);
        }