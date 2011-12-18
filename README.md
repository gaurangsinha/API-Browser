API Browser
===========

This library automatically creates a page containing all the REST API calls present in the controller files of an ASP.NET MVC3 project.

Setting up API Browser
----------------------

1) Add APIBrowser reference to MVC project

2) Add entry in `httpHandler` section in `web.config`

    <system.web>
        <httpHandlers>
            <add verb="GET" path="apibrowser.axd" type="Webtools.APIBrowser, APIBrowser" />
        </httpHandlers>
    </system.web>
    
	<system.webServer>
		<handlers>
			<add name="APIBrowser" verb="GET" path="apibrowser.axd" type="Webtools.APIBrowser, APIBrowser" resourceType="Unspecified" />
		</handlers>
	</system.webServer>


3) Add following line at the begining of `RegisterRoutes` method in `Global.asax` file.

    routes.IgnoreRoute("apibrowser.axd");

The `Global.asax` file should now look like this:

    public class MvcApplication : System.Web.HttpApplication {
        ...
        
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("apibrowser.axd");            
            ...            
        }
        ...
    }


4) Build & Run the project. Point the browser to `http://[localhost]:[port]/APIBrowser.axd`

How is the API Browser page generated
-------------------------------------

* The project is reflected to retrieve all the types
* This list of types is then filtered to only consider the types that inherit from `System.Web.Mvc.Controller`
* Methods contained in the filtered types list are then considered only if they have the following attributes `HttpPostAttribute` or `HttpGetAttribute`.

License
-------

**MIT License**

Copyright (c) 2011 Gaurang Sinha

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.