// Note: 
//      This file represents the work relating to the Create Book
//      form on the /Books/Create page, and specifically to the
//      Google Books API -powered Autocomplete functionality.
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
    }
}
