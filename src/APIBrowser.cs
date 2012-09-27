using System;
using System.Web;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.Routing;
using System.Xml.Linq;

namespace Webtools {

    /// <summary>
    /// API Browser
    /// </summary>
    public class APIBrowser : IHttpHandler {

        #region Properties
        /// <summary>
        /// Assemblies to ignore
        /// </summary>
        private static readonly string[] ASSEMBLIES_TO_IGNORE = { "Microsoft", "System", "mscorlib" };

        /// <summary>
        /// Assemblies reflected
        /// </summary>
        private List<Assembly> AssembliesDisplayed = new List<Assembly>();
        #endregion

        #region IHttpHandler Members

        /// <summary>
        /// You will need to configure this handler in the web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.</returns>
        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context) {
            RenderPageFromAssembly(
                context.Response.OutputStream,
                AppDomain.CurrentDomain.GetAssemblies());
        }
        #endregion

        #region Util Methods
        /// <summary>
        /// Checks if the assembly should be ignored.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        private bool IgnoreAssembly(Assembly assembly) {
            return ASSEMBLIES_TO_IGNORE.Any(s => assembly.FullName.StartsWith(s));
        }

        /// <summary>
        /// Retrieves the embedded resource.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns></returns>
        private string RetrieveEmbeddedResource(string resourceName) {
            string data = string.Empty;
            if (!string.IsNullOrEmpty(resourceName)) {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                using (var resourceStream = currentAssembly.GetManifestResourceStream(resourceName)) {
                    if (null != resourceStream) {
                        using (var resourceStreamReader = new StreamReader(resourceStream)) {
                            data = resourceStreamReader.ReadToEnd();
                        }
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Determines whether the specified type is controller.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is controller; otherwise, <c>false</c>.
        /// </returns>
        private bool IsController(Type type) {
            return (null == type || null == type.BaseType)
                ? false
                : (type.BaseType.FullName != "System.Web.Mvc.Controller")
                    ? IsController(type.BaseType)
                    : true;
        }

        /// <summary>
        /// Determines whether the specified methos is a MVC action method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        private string IsMvcActionMethod(MethodInfo method) {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attrib in attributes) {
                if (attrib.GetType().Name == "HttpPostAttribute")
                    return "POST";
                if (attrib.GetType().Name == "HttpGetAttribute")
                    return "GET";
            }
            return null;
        }

        /// <summary>
        /// Fetch XML documentation
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns>LINQ XDocument</returns>
        private XDocument FetchDocumentation(string assemblyPath) {
            assemblyPath = assemblyPath.Replace("file:///", string.Empty);
            return (!string.IsNullOrEmpty(assemblyPath) 
                && File.Exists(assemblyPath)
                && File.Exists(Path.ChangeExtension(assemblyPath, "xml"))) 
                    ? XDocument.Load(Path.ChangeExtension(assemblyPath, "xml"))
                    : null;
        }

        /// <summary>
        /// Checks if documentation exists.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns></returns>
        private bool DocumentationExists(string assemblyPath) {
            return null != FetchDocumentation(assemblyPath);
        }

        /// <summary>
        /// Fetches the documentation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        private T FetchDocumentation<T>(string assemblyPath, Func<XDocument, T> handler) {
            var xDoc = FetchDocumentation(assemblyPath);
            return null != xDoc ? handler(xDoc) : default(T);
        }

        /// <summary>
        /// Fetches the documentation for the controller.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="controller">The controller.</param>
        /// <returns></returns>
        private string FetchDocumentation(string assemblyPath, Type controller) {
            return FetchDocumentation<string>(assemblyPath,
                    xDoc => {
                        var doc = from m in xDoc.Descendants("member")
                                  where m.Attribute("name").Value == string.Format("T:{0}", controller.FullName)
                                  select m.Descendants("summary").FirstOrDefault().Value;
                        return null != doc ? doc.FirstOrDefault() : null;
                    });
        }

        /// <summary>
        /// Fetches the documentation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="method">The method.</param>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        private T FetchDocumentation<T>(string assemblyPath, MethodInfo method, Func<XElement, T> handler) {
            return FetchDocumentation<T>(assemblyPath,
                xDoc => {
                    var parameters = method.GetParameters();
                    var doc = from m in xDoc.Descendants("member")
                              where m.Attribute("name").Value == string.Format(parameters.Length > 0 ? "M:{0}.{1}({2})" : "M:{0}.{1}{2}",
                                                                      method.DeclaringType.FullName,
                                                                      method.Name,
                                                                        string.Join(",",
                                                                            Array.ConvertAll<ParameterInfo, string>(
                                                                                parameters,
                                                                                p => p.ParameterType.FullName)))
                              select m;
                    return null != doc ? handler(doc.FirstOrDefault()) : default(T);
                });
        }

        /// <summary>
        /// Fetches documentation for the method.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="method">The method.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <returns></returns>
        private string FetchDocumentation(string assemblyPath, MethodInfo method, string elementName="summary") {
            return FetchDocumentation<string>(
                assemblyPath, 
                method,
                doc => {
                    var summary = from s in doc.Descendants(elementName) select s;
                    return null != summary && summary.Count() > 0 ? summary.FirstOrDefault().Value : null;
                });
        }

        /// <summary>
        /// Fetches documentation for the method parameter.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        private string FetchDocumentation(string assemblyPath, MethodInfo method, ParameterInfo parameter) {
            return FetchDocumentation<string>(
                assemblyPath,
                method,
                doc => {
                    var para = from p in doc.Descendants("param")
                               where p.Attribute("name").Value == parameter.Name
                               select p.Value;
                    return null != para ? para.FirstOrDefault() : null;
                });
        }

        /// <summary>
        /// Fetches the routes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        private string FetchRoutes(MethodInfo method) {
            VirtualPathData defaultPath = RouteTable.Routes.GetVirtualPath(null, new RouteValueDictionary { 
                    { "controller", method.DeclaringType.Name.Replace("Controller", string.Empty) }, 
                    { "action", method.Name } 
            });
            return null != defaultPath ? defaultPath.VirtualPath : null;
        }

        /// <summary>
        /// Get specified assembly attribute
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <returns></returns>
        private static T GetAssemblyAttribute<T>() where T : Attribute {
            object[] attributes = Assembly.GetCallingAssembly().GetCustomAttributes(typeof(T), false);
            return (null == attributes || 0 == attributes.Length)
                    ? default(T)
                    : (T)attributes[0];
        }
        #endregion

        #region Render Methods
        /// <summary>
        /// Renders the page from assembly.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="assemblies">The assemblies.</param>
        private void RenderPageFromAssembly(Stream outputStream, Assembly[] assemblies) {
            List<Type> types = new List<Type>();
            foreach (var assembly in assemblies.Where(a => !IgnoreAssembly(a))) {
                types.AddRange(assembly.GetTypes().Where(t => IsController(t)));
            }
            
            using (StreamWriter streamWriter = new StreamWriter(outputStream))
            using (HtmlTextWriter htmlWriter = new HtmlTextWriter(streamWriter)) {
               
                //begin page
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                    //Render head
                    RenderPageHead(htmlWriter);
                    //begin body
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);
                        //header
                        RenderTag(htmlWriter, HtmlTextWriterTag.H1, "API Browser");
                        //Render all the controller types
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, "content");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                            foreach (var type in types) {
                                RenderController(htmlWriter, type);
                            }

                            //footer line
                            RenderTag(htmlWriter, HtmlTextWriterTag.Hr);
                            //assemblies displayed
                            RenderTag(htmlWriter, HtmlTextWriterTag.H3, "Reflected Assemblies");
                            htmlWriter.WriteBreak();
                            foreach (var ass in AssembliesDisplayed) {
                                RenderTag(htmlWriter, HtmlTextWriterTag.A, ass.EscapedCodeBase, Tuple.Create(HtmlTextWriterAttribute.Href, ass.EscapedCodeBase));
                                htmlWriter.WriteBreak();
                                htmlWriter.Write(ass.FullName);
                                htmlWriter.WriteBreak();
                                htmlWriter.Write("Documentation : {0}", DocumentationExists(ass.CodeBase) ? "Found" : "Not Found");
                                htmlWriter.WriteBreak();
                            }
                            //footer line
                            RenderTag(htmlWriter, HtmlTextWriterTag.Hr);
                            //footer text
                            RenderPageFooter(htmlWriter);

                        htmlWriter.RenderEndTag();

                    //close body
                    htmlWriter.RenderEndTag();
                //close html
                htmlWriter.RenderEndTag();
            }
        }

        /// <summary>
        /// Renders the page head.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        private void RenderPageHead(HtmlTextWriter htmlWriter) {
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Head);

            //title
            RenderTag(htmlWriter, HtmlTextWriterTag.Title, "API Browser");

            //css
            RenderTag(htmlWriter,
                HtmlTextWriterTag.Style,
                RetrieveEmbeddedResource("Webtools.APIBrowser.Styles.css"),
                Tuple.Create(HtmlTextWriterAttribute.Type, "text/css"));

            //prettify css
            RenderTag(htmlWriter, HtmlTextWriterTag.Link, null,
                Tuple.Create(HtmlTextWriterAttribute.Type, "text/css"),
                Tuple.Create(HtmlTextWriterAttribute.Href, "http://google-code-prettify.googlecode.com/svn/branches/release-1-Jun-2011/src/prettify.css"),
                Tuple.Create(HtmlTextWriterAttribute.Rel, "stylesheet"));

            //jQuery
            RenderTag(htmlWriter, HtmlTextWriterTag.Script, null,
                Tuple.Create(HtmlTextWriterAttribute.Type, "text/javascript"),
                Tuple.Create(HtmlTextWriterAttribute.Src, "http://ajax.googleapis.com/ajax/libs/jquery/1.7.0/jquery.min.js"));

            //google code pretty print
            RenderTag(htmlWriter, HtmlTextWriterTag.Script, null,
                Tuple.Create(HtmlTextWriterAttribute.Type, "text/javascript"),
                Tuple.Create(HtmlTextWriterAttribute.Src, "http://google-code-prettify.googlecode.com/svn/branches/release-1-Jun-2011/src/prettify.js"));

            //custom javascript
            RenderTag(htmlWriter,
                HtmlTextWriterTag.Script,
                RetrieveEmbeddedResource("Webtools.APIBrowser.Async.js"),
                Tuple.Create(HtmlTextWriterAttribute.Type, "text/javascript"));

            htmlWriter.RenderEndTag();
        }

        /// <summary>
        /// Renders the page footer.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        private void RenderPageFooter(HtmlTextWriter htmlWriter) {
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "footer");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
            htmlWriter.Write("Powered by ");

            RenderTag(htmlWriter, 
                HtmlTextWriterTag.A, 
                "API Browser", 
                Tuple.Create(HtmlTextWriterAttribute.Href, "https://github.com/gaurangsinha/API-Browser"));
            
            htmlWriter.Write(", Version {0}", GetAssemblyAttribute<AssemblyFileVersionAttribute>().Version ?? string.Empty);
            htmlWriter.WriteBreak();
            htmlWriter.Write(GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright);

            RenderTag(htmlWriter,
                HtmlTextWriterTag.A,
                "Gaurang Sinha",
                Tuple.Create(HtmlTextWriterAttribute.Href, "http://gaurangs.com"));
            
            htmlWriter.RenderEndTag();            
        }

        /// <summary>
        /// Renders the controller.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="controller">The controller.</param>
        private void RenderController(HtmlTextWriter htmlWriter, Type controller) {
            bool titleHeaderExists = false;            
            MethodInfo[] methods = controller.GetMethods();
            if (null != methods && methods.Length > 0) {
                foreach (var method in methods) {
                    var mType = IsMvcActionMethod(method);
                    if (null != mType) {
                        if (false == titleHeaderExists) {
                            if (DocumentationExists(controller.Assembly.CodeBase))
                                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Title, FetchDocumentation(controller.Assembly.CodeBase, controller));
                            RenderTag(htmlWriter, HtmlTextWriterTag.H2, "/" + controller.Name.Replace("Controller", string.Empty));
                            titleHeaderExists = true;
                            if (!AssembliesDisplayed.Contains(controller.Assembly)) {
                                AssembliesDisplayed.Add(controller.Assembly);
                            }
                        }
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                        RenderMethod(htmlWriter, method);
                        htmlWriter.RenderEndTag();
                    }
                }
                if (titleHeaderExists) {
                    htmlWriter.WriteBreak();
                }
            }
        }

        /// <summary>
        /// Renders the method.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="method">The method.</param>
        private void RenderMethod(HtmlTextWriter htmlWriter, MethodInfo method) {
            string formAction = FetchRoutes(method); //string.Format("/{0}/{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string formId = string.Format("{0}_{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string httpMethod = IsMvcActionMethod(method);

            //header
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "heading_" + httpMethod.ToLower());
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
            
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H3);

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "http_method");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);

                        RenderTag(htmlWriter,
                            HtmlTextWriterTag.A,
                            IsMvcActionMethod(method),
                            Tuple.Create(HtmlTextWriterAttribute.Name, formAction),
                            Tuple.Create(HtmlTextWriterAttribute.Href, "#" + formAction),
                            Tuple.Create(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);"));

                    htmlWriter.RenderEndTag();

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "path");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);
        
                        if (DocumentationExists(method.DeclaringType.Assembly.CodeBase))
                            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Title, FetchDocumentation(method.DeclaringType.Assembly.CodeBase, method));
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#" + formAction);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);");            
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                        htmlWriter.Write(formAction);
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();

            //render parameters
            RenderMethodParameters(htmlWriter, method, method.GetParameters(), formId, httpMethod, formAction);
        }

        /// <summary>
        /// Renders the method parameters.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="methodInfo">The method info.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="formId">The form id.</param>
        /// <param name="method">The method.</param>
        /// <param name="action">The action.</param>
        private void RenderMethodParameters(HtmlTextWriter htmlWriter, MethodInfo methodInfo, ParameterInfo[] parameters, string formId, string method, string action) {
            bool addComments = false;

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_content");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "content_" + method.ToLower());
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                string summary = FetchDocumentation(methodInfo.DeclaringType.Assembly.CodeBase, methodInfo);
                if (!string.IsNullOrEmpty(summary)) {
                    htmlWriter.Write(summary);
                    htmlWriter.WriteBreak();
                    string returnSummary = FetchDocumentation(methodInfo.DeclaringType.Assembly.CodeBase, methodInfo, "returns");
                    if (!string.IsNullOrEmpty(returnSummary)) {
                        RenderTag(htmlWriter, HtmlTextWriterTag.B, "Returns: ");
                        htmlWriter.Write(returnSummary);
                        htmlWriter.WriteBreak();
                    }
                    htmlWriter.WriteBreak();
                }

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId);
                htmlWriter.AddAttribute("action", action);
                htmlWriter.AddAttribute("method", method);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Form);

                if (null != parameters && parameters.Length > 0) {
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Thead);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                    RenderTag(htmlWriter, HtmlTextWriterTag.Th, "Parameter");
                    RenderTag(htmlWriter, HtmlTextWriterTag.Th, "Value");
                    RenderTag(htmlWriter, HtmlTextWriterTag.Th, "Type");
                    if (DocumentationExists(methodInfo.DeclaringType.Assembly.CodeBase)) {
                        RenderTag(htmlWriter, HtmlTextWriterTag.Th, "Comment");
                        addComments = true;
                    }
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);

                    foreach (var parameter in parameters) {
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Title, FetchDocumentation(methodInfo.DeclaringType.Assembly.CodeBase, methodInfo, parameter));

                        RenderTag(htmlWriter, HtmlTextWriterTag.Td, parameter.Name);
                        
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);

                        RenderTag(htmlWriter, HtmlTextWriterTag.Input, null,
                            Tuple.Create(HtmlTextWriterAttribute.Id, formId + "_" + parameter.Name),
                            Tuple.Create(HtmlTextWriterAttribute.Type, "text"),
                            Tuple.Create(HtmlTextWriterAttribute.Name, parameter.Name));
                        
                        htmlWriter.RenderEndTag();

                        RenderTag(htmlWriter, HtmlTextWriterTag.Td, parameter.ParameterType.Name);
                        
                        if (addComments) {
                            RenderTag(htmlWriter, HtmlTextWriterTag.Td, FetchDocumentation(methodInfo.DeclaringType.Assembly.CodeBase, methodInfo, parameter));
                        }
                        htmlWriter.RenderEndTag();
                    }

                    //close tbody
                    htmlWriter.RenderEndTag();

                    //close table
                    htmlWriter.RenderEndTag();
                }
                else {
                    //Show method requires no parameters
                    RenderTag(htmlWriter, HtmlTextWriterTag.I, "Method has no paramaters");
                    htmlWriter.WriteBreak();
                }

                RenderTag(htmlWriter, HtmlTextWriterTag.Input, null,
                    Tuple.Create(HtmlTextWriterAttribute.Type, "submit"),
                    Tuple.Create(HtmlTextWriterAttribute.Value, "Submit"));

                htmlWriter.Write("&nbsp;");

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_time");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.Write("-");
                htmlWriter.RenderEndTag();

                htmlWriter.Write("&nbsp;");

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_hider");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_response').slideUp();$('#" + formId + "_response_time').fadeOut();$(this).fadeOut();return false;");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                    htmlWriter.Write("Hide Responses");
                htmlWriter.RenderEndTag();
                
            //close form
            htmlWriter.RenderEndTag();

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlWriter.Write("URL");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_url");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_url");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                htmlWriter.Write("Response Status");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_header");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_header");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlWriter.Write("Response Body");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_body");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_body");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();
        }
        #endregion

        #region Render Helpers

        /// <summary>
        /// Renders the tag.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="value">The value.</param>
        /// <param name="attributes">The attributes.</param>
        private void RenderTag(HtmlTextWriter htmlWriter, HtmlTextWriterTag tag, string value=null, params Tuple<HtmlTextWriterAttribute, string>[] attributes) {
            //Add attributes
            if (null != attributes && attributes.Length > 0) {
                foreach (var attr in attributes) {
                    htmlWriter.AddAttribute(attr.Item1, attr.Item2);
                }
            }
            
            htmlWriter.RenderBeginTag(tag);

            if (null != value) {
                htmlWriter.Write(value);
            }

            htmlWriter.RenderEndTag();
        }

        #endregion
    }
}
