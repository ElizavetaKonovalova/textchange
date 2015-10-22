//Gets how many tokens the user has from the database
public static string getTokens(string id)
{
    IFB299Entities db = new IFB299Entities();
    var user = db.AspNetUsers.Find(id);
    string tokenCount = user.Tokens.ToString();
    return tokenCount;
}

//Sets the users tokens to the value given
public void setTokens(string id, int quantity)
{
    var user = db.AspNetUsers.Find(id);
    user.Tokens = quantity;
    db.SaveChanges();
}

//Adds one to the users tokens
public void incrementTokens(string id)
{
    var user = db.AspNetUsers.Find(id);
    user.Tokens += 1;
    db.SaveChanges();
}

//Subtracts one from the users tokens
public void decrementTokens(string id)
{
    var user = db.AspNetUsers.Find(id);
    user.Tokens -= 1;
    db.SaveChanges();
}