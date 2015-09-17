// Note: 
//      This file represents the work relating to public profiles
//      and sending messages to contact users.
//
//      All unrelated content have been stripped out to make it
//      easier to read. For the full file, see 
//      AccountViewModels.cs in ~/Models/
//
//      Andrew Cooper 2015

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TextBooks.Models
{
    public class PublicProfileViewModel
    {
        // The User for the Profile page
        public AspNetUser targetUser { get; set; }

        // Associated Books
        public List<Book> booksOwned { get; set; }
        public List<Book> booksBorrowed { get; set; }

        // Contact the user through email
        public Email contactEmail { get; set; }
    }

    public class Email
    {
        // To
        public string toAddress { get; set; }
        public string toName { get; set; }

        // From
        public string fromAddress { get; set; }
        public string fromName { get; set; }

        // Subject & Message
        public string subject { get; set; }
        public string message { get; set; }

        // Success / fail
        public bool success { get; set; }
    }
}
