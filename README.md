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


4) Run project and point browser to `http://[localhost]:[port]/APIBrowser.axd`

How is the API Browser page generated
-------------------------------------

* The project is reflected to retrieve all the types
* This list of types is then filtered to only consider the types that inherit from `System.Web.Mvc.Controller`
* Methods contained in the filtered types list are then considered only if they have the following attributes `HttpPostAttribute` or `HttpGetAttribute`.

