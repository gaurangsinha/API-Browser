using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace example.Controllers {

    /// <summary>
    /// Controller to test APIBrowser
    /// </summary>
    public class TestController : Controller {
        //
        // GET: /Test/

        public ActionResult Index() {
            return View();
        }


        /// <summary>
        /// Test method for HTTP GET
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Hello [Name]</returns>
        [HttpGet]
        public string HelloGet(string name) {
            return string.Format("Hello {0}", name);
        }

        /// <summary>
        /// Test method for HTTP POST
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Hello [Name]</returns>
        [HttpPost]
        public string HelloPost(string name) {
            return string.Format("Hello {0}", name);
        }

        /// <summary>
        /// Test method for HTTP GET to demostrate different datatypes
        /// </summary>
        /// <param name="intValue">The int value.</param>
        /// <param name="doubleValue">The double value.</param>
        /// <param name="stringValue">The string value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        /// <returns></returns>
        [HttpGet]
        public string DataTypeTestGet(int intValue, double doubleValue, string stringValue, DateTime dateTimeValue) {
            return string.Format("Interger: {0}\nDouble: {1:0.00}\nString: \"{2}\"\nDateTime: {3:yyyy-MMM-dd HH:mm:ss.ffff}", intValue, doubleValue, stringValue, dateTimeValue);
        }

        /// <summary>
        /// Test method for HTTP POST to demostrate different datatypes
        /// </summary>
        /// <param name="intValue">The int value.</param>
        /// <param name="doubleValue">The double value.</param>
        /// <param name="stringValue">The string value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        [HttpPost]
        public string DataTypeTestPost(int intValue, double doubleValue, string stringValue, DateTime dateTimeValue) {
            return string.Format("Interger: {0}\nDouble: {1:0.00}\nString: \"{2}\"\nDateTime: {3:yyyy-MMM-dd HH:mm:ss.ffff}", intValue, doubleValue, stringValue, dateTimeValue);
        }
    }
}
