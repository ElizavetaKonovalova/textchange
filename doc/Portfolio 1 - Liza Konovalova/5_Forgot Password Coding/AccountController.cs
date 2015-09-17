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