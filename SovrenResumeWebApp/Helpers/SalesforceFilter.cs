using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace SovrenResumeWebApp.Helpers
{
    public class SalesforceFilter : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext loggedIn)
        {
            if (loggedIn.HttpContext.Session["LoggedIn"] == null
                || (bool)loggedIn.HttpContext.Session["LoggedIn"] == false)
            {
                loggedIn.Result = new RedirectResult("/Login");
            }
        }
    }
}