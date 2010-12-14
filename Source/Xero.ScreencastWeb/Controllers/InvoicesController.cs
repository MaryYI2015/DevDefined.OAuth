using System.Web.Mvc;
using Xero.ScreencastWeb.Models;
using Xero.ScreencastWeb.Services;

namespace Xero.ScreencastWeb.Controllers
{
    public class InvoicesController : ControllerBase
    {
        //
        // GET: /Invoices/

        public ActionResult Index()
        {
            ApiListRequest<Invoice> listRequest = new ApiListRequest<Invoice>
            {
                OrderByClause = "Date DESC",
                WhereClause = "AmountDue > 0"
            };

            ApiRepository repository = new ApiRepository();
            var response = repository.ListItems(Session, listRequest);

            return View(response.Invoices);
        }

    }
}
