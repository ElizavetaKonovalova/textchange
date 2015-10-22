public static bool isDigitsOnly(string str)
{
    foreach (char c in str)
    {
        if (c < '0' || c > '9')
            return false;
    }

    return true;
}

//Registration controller error checks               
string password = model.Password;
string confirmPassword = model.ConfirmPassword;
string fName = model.FirstName;
string lName = model.LastName;
string email = model.Email;
string number = model.ContactNumber;
// Check fields are ok

bool failed = false;
if (email == null)
{
    ModelState.AddModelError("", "Email field may not be empty.");
    failed = true;
}

if (email != null && !email.Contains('@'))
{
    ModelState.AddModelError("", "Please use a valid email format.");
    failed = true;
}

if (email != null && email.Contains('@'))
{
    string addressSuffix = email.Split('@')[1];
    if (!addressSuffix.Equals("connect.qut.edu.au") && email != "ifb299books@gmail.com")
    {
        ModelState.AddModelError("", "Please use valid email (@connect.qut.edu.au)");
        failed = true;
    }
}

if (password == null)
{
    ModelState.AddModelError("", "Password field may not be empty.");
    failed = true;
}

if (confirmPassword == null)
{
    ModelState.AddModelError("", "Confirm password field may not be empty.");
    failed = true;
}

if (fName == null)
{
    ModelState.AddModelError("", "First name field may not be empty.");
    failed = true;
}

if (lName == null)
{
    ModelState.AddModelError("", "Last name field may not be empty.");
    failed = true;
}

if (password != null && password.Length < 6)
{
    ModelState.AddModelError("", "Password must be longer than 6 characters.");
    failed = true;
}
if (password != confirmPassword)
{
    ModelState.AddModelError("", "Passwords do not match.");
    failed = true;
}

if (number != null)
{
    if (!isDigitsOnly(number))
    {
        ModelState.AddModelError("", "The contact number may only be digits.");
        failed = true;
    }
}

if (failed == true)
{
    return View();
}

