﻿// Note: 
//      This file represents the work relating to registering
//      a new user account with a qut email address.
//
//      All unrelated methods have been stripped out to make it
//      easier to read. For the full file, see 
//      AccountController.cs in ~/Controllers/Accounts/
//
//      Andrew Cooper 2015

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TextBooks.Models;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using System.Net.Mime;
using System.Data.Entity;

namespace TextBooks.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private IFB299Entities db = new IFB299Entities();
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

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
    }
}