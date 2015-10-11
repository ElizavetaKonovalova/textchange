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
            return View();
        }


        public ActionResult Terms()
        {
            ViewBag.Message = "Terms and Conditions";

            return View();
        }
    }
}