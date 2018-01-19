using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Salesforce.Common;
using Salesforce.Common.Models;
using SovrenResumeWebApp.Models;

namespace SovrenResumeWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _authorizationEndpointUrl = ConfigurationManager.AppSettings["AuthorizationEndpointUrl"];
        private readonly string _consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private readonly string _consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private readonly string _callbackUrl = ConfigurationManager.AppSettings["CallbackUrl"];
        private readonly string _sovrenUrl = ConfigurationManager.AppSettings["SovrenApi"];

        public HomeController()
        {
            ViewBag.Title = "BroadPath - Sovren Resume Parser";
            ViewBag.Header = "Resume Parser";
            ViewBag.LoggedIn = false;
        }

        public ActionResult Index()
        {
            if (Request.QueryString.HasKeys())
            {
                var instanceUrl = Request.QueryString["instance_url"];
                var token = Request.QueryString["token"];
                var user = Request.QueryString["user"];
                var refreshToken = Request.QueryString["refresh_token"];

                ViewBag.InstanceUrl = instanceUrl;
                ViewBag.Token = token;
                ViewBag.User = user;
                ViewBag.RefreshToken = refreshToken;

                ViewBag.LoggedIn = true;
            }
            return View();            
        }

        public ActionResult Login()
        {
            var url =
                Common.FormatAuthUrl(
                    _authorizationEndpointUrl,
                    ResponseTypes.Code,
                    _consumerKey,
                    HttpUtility.UrlEncode(_callbackUrl));

            return Redirect(url);
        }

        public async Task<ActionResult> GetRefreshToken(string refreshToken)
        {
            var auth = new AuthenticationClient();
            await auth.TokenRefreshAsync(_consumerKey, refreshToken, _consumerSecret);

            ViewBag.Token = auth.AccessToken;
            ViewBag.ApiVersion = auth.ApiVersion;
            ViewBag.InstanceUrl = auth.InstanceUrl;
            ViewBag.RefreshToken = auth.RefreshToken;

            ViewBag.LoggedIn = true;

            return View("Index");
        }

        [HttpPost]
        public string SovrenResumeApi(string resumeRequest)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_sovrenUrl);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            HttpContent content = new StringContent(resumeRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync(_sovrenUrl, content).Result;
            string result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Response.StatusCode = (int)response.StatusCode;
                result = response.StatusCode + response.ReasonPhrase;
            }

            return result;
        }

        [HttpPost]
        public string SovrenResumeUpdateApi(string resumeUpdate)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_sovrenUrl + "/update");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content = new StringContent(resumeUpdate, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync(_sovrenUrl + "/update", content).Result;
            string result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Response.StatusCode = (int)response.StatusCode;
                result = response.StatusCode + response.ReasonPhrase;
            }

            return result;
        }

        [HttpPost]
        public string SalesforceLookup(string instanceUrl, string token, string jobOrder)
        {
            string restQuery = instanceUrl + @"/services/apexrest/Sovren/" + jobOrder;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(restQuery);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = client.GetAsync(restQuery).Result;
            string result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
            } else
            {
                Response.StatusCode = (int) response.StatusCode;
                result = response.ReasonPhrase;
            }
            return result;
        }

        [HttpPost]
        public string SalesforceUpsert(string instanceUrl, string token, string parsedResumes)
        {
            string restQuery = instanceUrl + @"/services/apexrest/Sovren/";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(restQuery);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpContent content = new StringContent(parsedResumes, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync(restQuery, content).Result;
            string result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Response.StatusCode = (int)response.StatusCode;
                result = response.ReasonPhrase;
            }

            return result;
        }

    }
}