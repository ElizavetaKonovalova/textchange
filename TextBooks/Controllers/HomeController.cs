using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Data;
using System.Data.SqlClient;

namespace TextBooks.Controllers
{
    public class HomeController : Controller
    {

        IFB299Entities dbcontext = new IFB299Entities();

        public ActionResult Index()
        {
            //test timmy = (from tbl in dbcontext.tests
            //                    where tbl.FirstName=="Taylor"
            //                   select tbl).FirstOrDefault();
            //timmy.LastName = "Stoneham";

            dbcontext.SaveChanges();


            
                int p = 5;


            dbcontext.Database.Connection.Open();

            ////dbcontext.Database.ExecuteSqlCommand("INSERT INTO test VALUES(3, 'Test', 'Name');");

            //String connectionString = dbcontext.Database.Connection.ConnectionString.ToString();
            //SqlCommand cmd = new SqlCommand();
            //cmd.Connection.ConnectionString = connectionString;
            //cmd.CommandText = "SELECT * FROM test";
            //cmd.CommandType = CommandType.Text;

            //SqlDataReader reader;
            //reader = cmd.ExecuteReader();

            //String result = reader.GetString(1); // get column 1


            dbcontext.Database.Connection.Close();
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}