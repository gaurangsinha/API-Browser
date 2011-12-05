using System;
using System.Web;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;

namespace Webtools {

    /// <summary>
    /// API Browser
    /// </summary>
    public class APIBrowser : IHttpHandler {

        /// <summary>
        /// Assemblies to ignore
        /// </summary>
        private static readonly string[] ASSEMBLIES_TO_IGNORE = { "Microsoft", "System", "mscorlib" };

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

        /// <summary>
        /// Checks if the assembly should be ignored.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        private bool IgnoreAssembly(Assembly assembly) {
            foreach (var name in ASSEMBLIES_TO_IGNORE)
                if (assembly.FullName.StartsWith(name))
                    return true;
            return false;
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
        /// Renders the page from assembly.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="assemblies">The assemblies.</param>
        public void RenderPageFromAssembly(Stream outputStream, Assembly[] assemblies) {
            List<Type> types = new List<Type>();
            foreach (var assembly in assemblies) {
                if (!IgnoreAssembly(assembly)) {
                    types.AddRange(assembly.GetTypes());
                }
            }

            //using(TextWriter tx = new StringWriter(str))
            using (StreamWriter streamWriter = new StreamWriter(outputStream))
            using (HtmlTextWriter htmlWriter = new HtmlTextWriter(streamWriter)) {

                

                //begin page
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Head);
                
                //title
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Title);
                htmlWriter.Write("API Browser");
                htmlWriter.RenderEndTag();

                //css
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Style);                
                htmlWriter.Write(RetrieveEmbeddedResource("Webtools.APIBrowser.Styles.css"));
                htmlWriter.RenderEndTag();

                //jQuery
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Src, "http://ajax.googleapis.com/ajax/libs/jquery/1.7.0/jquery.min.js");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Script);
                htmlWriter.RenderEndTag();

                //custom javascript
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Script);
                htmlWriter.Write(RetrieveEmbeddedResource("Webtools.APIBrowser.Async.js"));
                htmlWriter.RenderEndTag();

                //begin body
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                //header
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H1);
                htmlWriter.Write("API Browser");
                htmlWriter.RenderEndTag();

                ////Retrieve Javascript & Stylesheet
                //string css = RetrieveEmbeddedResource("Webtools.APIBrowser.Styles.css");
                //string javascript = RetrieveEmbeddedResource("Webtools.APIBrowser.Async.js");

                //StringBuilder page = new StringBuilder();
                //page.Append("<html>")
                //    .Append("<head>")
                //    .Append("<title>API Browser</title>")
                //    .AppendFormat("<style type=\"text/css\">{0}</style>", css)
                //    .Append("<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.7.0/jquery.min.js\"></script>")
                //    .AppendFormat("<script type=\"text/javascript\">{0}</script>", javascript)
                //    .Append("</head>")
                //    .Append("<body><h1>API Browser</h1>");

                //Find all the controller types
                //page.Append("<div id=\"content\">");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, "content");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                foreach (var type in types) {
                    if (IsController(type)) {
                        RenderController(htmlWriter, type);
                        //string controllerHtml = RenderController(htmlWriter, type);
                        //if (!string.IsNullOrEmpty(controllerHtml))
                        //    page.AppendFormat("<div>{0}</div>", controllerHtml);
                    }
                }

                //footer line
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
                htmlWriter.RenderEndTag();

                //footer text
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "footer");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.Write("Powered by ");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);                
                htmlWriter.Write("API Browser");
                htmlWriter.RenderEndTag();
                htmlWriter.Write(", Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                htmlWriter.WriteBreak();
                htmlWriter.Write("Copyright (c) 2011 Gaurang Sinha. All rights reserved. Licensed under GPL version 2");
                htmlWriter.RenderEndTag();
                htmlWriter.RenderEndTag();

                //close body
                htmlWriter.RenderEndTag();

                //close html
                htmlWriter.RenderEndTag();

                //page.Append("<hr/>");
                //page.Append("<div class=\"footer\">");
                //page.AppendFormat("Powered by <a href=\"#\">API Browser</a>, Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                //page.Append("<br/>");
                //page.Append("Copyright (c) 2011 Gaurang Sinha. All rights reserved. Licensed under GPL version 2");
                //page.Append("</div>");
                //page.Append("</div>");

                //page.Append("</body></html>");
                //return page.ToString();
            }
        }

        /// <summary>
        /// Determines whether the specified type is controller.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is controller; otherwise, <c>false</c>.
        /// </returns>
        public bool IsController(Type type) {
            return (null == type || null == type.BaseType)
                ? false
                : (type.BaseType.FullName != "System.Web.Mvc.Controller")
                    ? IsController(type.BaseType)
                    : true;
        }

        /// <summary>
        /// Renders the controller.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="controller">The controller.</param>
        public void RenderController(HtmlTextWriter htmlWriter, Type controller) {
            bool titleHeader = false;
            //StringBuilder html = new StringBuilder();
            MethodInfo[] methods = controller.GetMethods();
            if (null != methods && methods.Length > 0) {
                foreach (var method in methods) {
                    var mType = IsMvcActionMethod(method);
                    if (null != mType) {
                        if (false == titleHeader) {
                            //htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.H2);
                            htmlWriter.Write("/" + controller.Name.Replace("Controller", string.Empty));
                            htmlWriter.RenderEndTag();
                            titleHeader = true;
                        }
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                        RenderMethod(htmlWriter, method);
                        htmlWriter.RenderEndTag();
                        //html.AppendFormat("<div>{0}</div>", RenderMethod(htmlWriter, method));
                    }
                }
                if (titleHeader) {
                    htmlWriter.WriteBreak();
                }
                //if (html.Length > 0) {
                //    html.Insert(0, string.Format("<h2>/{0}</h2>", controller.Name.Replace("Controller", string.Empty)));
                //    html.Append("<br/>");
                //}
            }
            //return html.ToString();
        }


        /// <summary>
        /// Renders the method.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="method">The method.</param>
        public void RenderMethod(HtmlTextWriter htmlWriter, MethodInfo method) {
            //StringBuilder html = new StringBuilder();

            string formAction = string.Format("/{0}/{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string formId = string.Format("{0}_{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string httpMethod = IsMvcActionMethod(method);

            //header
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "heading_" + httpMethod.ToLower());
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);            
            
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H3);

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "http_method");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);

                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#" + formAction);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                            htmlWriter.Write(IsMvcActionMethod(method));
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "path");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);
                            
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#" + formAction);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);");            
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                        htmlWriter.Write(formAction);
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();

            //header
            //html.AppendFormat("<div class=\"heading_{0}\">", httpMethod.ToLower())
            //    .Append("<h3>")
            //    .Append("<span class=\"http_method\">")
            //    .AppendFormat("<a href=\"#{0}\" onclick=\"$('#{1}_content').slideToggle(500);\">{2}</a>", formAction, formId, IsMvcActionMethod(method))
            //    .Append("</span>")
            //    .Append("<span class=\"path\">")
            //    .AppendFormat("<a href=\"#{0}\" onclick=\"$('#{1}_content').slideToggle(500);\">{0}</a>", formAction, formId)
            //    .Append("</span>")
            //    .Append("</h3>")
            //    .Append("</div>");
            
            //render parameters
            RenderMethodParameters(htmlWriter, method.GetParameters(), formId, httpMethod, formAction);

            //return html.ToString();
        }

        /// <summary>
        /// Renders the method parameters.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="formId">The form id.</param>
        /// <param name="method">The method.</param>
        /// <param name="action">The action.</param>
        public void RenderMethodParameters(HtmlTextWriter htmlWriter, ParameterInfo[] parameters, string formId, string method, string action) {

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_content");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "content_" + method.ToLower());
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId);
                htmlWriter.AddAttribute("action", action);
                htmlWriter.AddAttribute("method", method);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Form);

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Thead);
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Parameter");
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Value");
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Type");
                            htmlWriter.RenderEndTag();
                        htmlWriter.RenderEndTag();
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);


            //StringBuilder table = new StringBuilder();
            //table.AppendFormat("<div id=\"{0}_content\" class=\"content_{1}\" style=\"display: none;\">", formId, method.ToLower())
            //    .AppendFormat("<form id=\"{0}\" action=\"{1}\" method=\"{2}\">", formId, action, method)
            //    .Append("<table>")
            //    .Append("<thead>")
            //        .Append("<tr>")
            //            .Append("<th>Parameter</th>")
            //            .Append("<th>Value</th>")
            //            .Append("<th>Type</th>")
            //        .Append("</tr>")
            //    .Append("</thead>")
            //    .Append("<tbody>");
            foreach (var parameter in parameters) {
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                        htmlWriter.Write(parameter.Name);
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_" + parameter.Name);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Name, parameter.Name);
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Input);
                        htmlWriter.RenderEndTag();
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                        htmlWriter.Write(parameter.ParameterType.Name);
                    htmlWriter.RenderEndTag();
                htmlWriter.RenderEndTag();

                //table.AppendFormat("<tr><td>{0}</td><td><input id=\"{1}_{0}\" type=\"text\" name=\"{0}\" /></td><td>{2}</td></tr>",
                //    parameter.Name,
                //    formId,
                //    parameter.ParameterType.Name);
            }

            //close tbody
            htmlWriter.RenderEndTag();

            //close table
            htmlWriter.RenderEndTag();

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "submit");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Value, "Submit");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Input);
            htmlWriter.RenderEndTag();

            htmlWriter.Write("&nbsp;");

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_hider");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_response').slideUp();$(this).fadeOut();return false;");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
            htmlWriter.Write("Hide Responses");
            htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();

            //table.Append("</tbody></table>")
            //    .AppendFormat("<input type=\"submit\" value=\"Submit\" onclick=\"submitForm('{0}')\" />", formId)
            //    .AppendFormat("&nbsp;<a id=\"{0}_response_hider\" href=\"#\" onclick=\"$('#{0}_response').slideUp();$(this).fadeOut();return false;\" style=\"display: none\">Hide Responses</a>", formId)
            //    .Append("</form>");


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
            htmlWriter.Write("Response Body");
            htmlWriter.RenderEndTag();

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_body");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_body");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
            htmlWriter.RenderEndTag();

            htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
            htmlWriter.Write("Response Status");
            htmlWriter.RenderEndTag();

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_header");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_header");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
            htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();

            //reponse
            //table.AppendFormat("<div id=\"{0}_response\" class=\"response\" style=\"display: none\">", formId)                
            //    .Append("<h4>URL</h4>")
            //    .AppendFormat("<div class=\"response_url\" id=\"{0}_response_url\"></div>", formId)
            //    .Append("<h4>Response Body</h4>")
            //    .AppendFormat("<div class=\"response_body\" id=\"{0}_response_body\"></div>", formId)
            //    .Append("<h4>Response Status</h4>")
            //    .AppendFormat("<div class=\"response_header\" id=\"{0}_response_header\"></div>", formId)
            //    .Append("</div>");

            //return table.Append("</div>").ToString();
        }

        /// <summary>
        /// Determines whether the specified methos is a MVC action method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public static string IsMvcActionMethod(MethodInfo method) {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attrib in attributes) {
                if (attrib.GetType().Name == "HttpPostAttribute")
                    return "POST";
                if (attrib.GetType().Name == "HttpGetAttribute")
                    return "GET";
            }
            return null;
        }        
    }
}
