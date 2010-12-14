using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace Xero.ScreencastWeb.Controllers
{
    public class ContactsController : ControllerBase
    {
        //
        // GET: /Contacts/

        public ActionResult Index()
        {
            return View();
        }

    }
}
