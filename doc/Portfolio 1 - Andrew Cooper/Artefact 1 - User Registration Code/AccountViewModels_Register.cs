// Note: 
//      This file represents the work relating to registering
//      a new user account with a qut email address.
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
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Surname")]
        public string LastName { get; set; }

        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }
    }
}
