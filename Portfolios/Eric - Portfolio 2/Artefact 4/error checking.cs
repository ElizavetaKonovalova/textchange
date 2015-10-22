//Code that checks the validity of various fields relating to book adding/editing.
bool bookErrorCheck(Book book)
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
    else
    {
        if (book.Year.Length != 4)
        {
            ModelState.AddModelError("", "Book Year field must be four characters.");
            failed = true;
        }
        if (!AccountController.isDigitsOnly(book.Year))
        {
            ModelState.AddModelError("", "Book Year field must be only numbers.");
            failed = true;
        }
    }
    if (book.Edition == null)
    {
        ModelState.AddModelError("", "Book Edition field can't be empty.");
        failed = true;
    }
    return failed;
}


//In the book edit and create functions below, this function is implemented like so.
if (bookErrorCheck(book))
    {
        return View();
    }
//If any of the errors occur then it will return true and so return the view with the error
//information that was added from bookErrorCheck()