// Note: 
//      This file represents the work relating to the autocomplete
//
//      All unrelated methods have been stripped out to make it
//      easier to read. For the full file, see 
//      AccountController.cs in ~/Controllers/Books
//
//      Andrew Cooper 2015

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
    }
}
