using universal_print.Helpers;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace universal_print.Controllers
{
    public class PrinterController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            IEnumerable<PrinterShare> printerShares = null;

            try
            {
                printerShares = await GraphHelper.GetRegisteredPrinterSharesAsync();
            }
            catch (ServiceException ex)
            {
                Flash("Error getting printer shares", ex.Message);
                return RedirectToAction("Error", "Home");
            }

            return View(printerShares);
        }


        [HttpPost]
        public async Task<ActionResult> UploadFile(string selectedPrinterShareId,HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength <= 0)
            {
                throw new Exception("File upload empty.");
            }
            if (string.IsNullOrEmpty(selectedPrinterShareId))
            {
                throw new Exception("PrinterShareID empty.");
            }

            var result = await GraphHelper.CreatePrintJobAsync(selectedPrinterShareId, file);

            return RedirectToAction("Index");
        }

    }
}
