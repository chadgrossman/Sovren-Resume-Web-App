using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
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

        public string GetRefreshToken(string refresh)
        {
            var auth = new AuthenticationClient();
            auth.TokenRefreshAsync(_consumerKey, refresh, _consumerSecret);

            return auth.AccessToken;
        }

        [HttpPost]
        public string SovrenResumeApi(string resumeRequest)
        {
            using (var client = new HttpClient())
            {
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
                    result = response.ReasonPhrase;
                }

                return result;
            }
        }

        [HttpPost]
        public string SalesforceUpsert(string instanceUrl, string token, string refreshToken, Resumes resumes)
        {
            if (!string.IsNullOrEmpty(resumes.JobOrder))
            {
                HttpResponseMessage jobOrderCheck = JobOrderLookup(instanceUrl, token, resumes.JobOrder);
                if (jobOrderCheck.IsSuccessStatusCode)
                {
                    var responseString = jobOrderCheck.Content.ReadAsStringAsync();
                    resumes.JobOrder = Regex.Replace(jobOrderCheck.Content.ReadAsStringAsync().Result, "['\"]", "");
                } else
                {
                    Response.StatusCode = (int)jobOrderCheck.StatusCode;
                    return jobOrderCheck.ReasonPhrase;
                }
            }

            HttpResponseMessage recordInserts = RecordInserts(instanceUrl, token, resumes);

            if (recordInserts.IsSuccessStatusCode)
            {
                string updateJson = recordInserts.Content.ReadAsStringAsync().Result;
                HttpResponseMessage sovrenResumeUpdate = SovrenResumeUpdateApi(updateJson);
                if (sovrenResumeUpdate.IsSuccessStatusCode)
                {
                    return sovrenResumeUpdate.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Response.StatusCode = (int)sovrenResumeUpdate.StatusCode;
                    return sovrenResumeUpdate.StatusCode + sovrenResumeUpdate.ReasonPhrase;
                }
            } else
            {
                Response.StatusCode = (int)recordInserts.StatusCode;
                return recordInserts.Content.ReadAsStringAsync().Result;
            }
        }

        public HttpResponseMessage JobOrderLookup(string instanceUrl, string token, string jobOrder)
        {
            string restQuery = instanceUrl + @"/services/apexrest/Sovren/" + jobOrder;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(restQuery);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = client.GetAsync(restQuery).Result;

                return response;
            }
        }

        public HttpResponseMessage RecordInserts(string instanceUrl, string token, Resumes parsedResumes)
        {
            string restQuery = instanceUrl + @"/services/apexrest/Sovren/";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(restQuery);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpContent content = new StringContent(JsonConvert.SerializeObject(parsedResumes), Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(restQuery, content).Result;
                
                return response;
            }
        }

        public HttpResponseMessage SovrenResumeUpdateApi(string resumeUpdate)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_sovrenUrl + "/update");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(resumeUpdate, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(_sovrenUrl + "/update", content).Result;

                return response;
            }
        }

    }

}