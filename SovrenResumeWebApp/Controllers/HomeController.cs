using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly string _tokenRequestEndpointUrl = ConfigurationManager.AppSettings["TokenRequestEndpointUrl"];
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

        public async Task<ActionResult> Callback(string display, string code)
        {
            var auth = new AuthenticationClient();
            await auth.WebServerAsync(_consumerKey, _consumerSecret, _callbackUrl, code, _tokenRequestEndpointUrl);

            TempData["LoggedIn"] = true;
            TempData["InstanceUrl"] = auth.InstanceUrl;
            TempData["Token"] = auth.AccessToken;
            TempData["RefreshToken"] = auth.RefreshToken;
            TempData["User"] = auth.Id.Substring(auth.Id.LastIndexOf("/") + 1);

            return Redirect("Index");
        }

        public ActionResult Index()
        {

            if (TempData.ContainsKey("LoggedIn") && (bool)TempData["LoggedIn"] == true)
            {
                Session["InstanceUrl"] = TempData["InstanceUrl"];
                Session["Token"] = TempData["Token"];
                Session["RefreshToken"] = TempData["RefreshToken"];
                Session["User"] = TempData["User"];
                Session["LoggedIn"] = true;

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

        public async Task<bool> GetRefreshToken(string refresh)
        {
            var auth = new AuthenticationClient();
            await auth.TokenRefreshAsync(_consumerKey, refresh, _consumerSecret, _tokenRequestEndpointUrl);
            if (auth.AccessToken != null)
            {
                Session["Token"] = auth.AccessToken;
                return true;
            }
            return false;
        }

        [HttpPost]
        public string SovrenResumeApi(ResumeRequest resumeRequest)
        {
            if (Session["User"] == null)
            {
                Response.StatusCode = 401;
                return "Unauthorized";
            }

            resumeRequest.RecruiterName = (string)Session["User"];

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_sovrenUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(JsonConvert.SerializeObject(resumeRequest), Encoding.UTF8, "application/json");

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
        public async Task<string> SalesforceUpsert(Resumes resumes)
        {
            if (Session["InstanceUrl"] == null || Session["Token"] == null || Session["RefreshToken"] == null)
            {
                Response.StatusCode = 500;
                return "Session variables not found";
            }

            string instanceUrl = (string)Session["InstanceUrl"];
            string token = (string)Session["Token"];
            string refreshToken = (string)Session["RefreshToken"];

            bool refreshed = false;

            do
            {
                if (!string.IsNullOrEmpty(resumes.JobOrder))
                {
                    HttpResponseMessage jobOrderCheck = JobOrderLookup(instanceUrl, token, resumes.JobOrder);
                    if (jobOrderCheck.IsSuccessStatusCode)
                    {
                        var responseString = jobOrderCheck.Content.ReadAsStringAsync();
                        resumes.JobOrder = Regex.Replace(jobOrderCheck.Content.ReadAsStringAsync().Result, "['\"]", "");
                    }
                    else if (jobOrderCheck.ReasonPhrase == "Unauthorized")
                    {
                        if (refreshed == true) break;
                        refreshed = await GetRefreshToken(refreshToken);
                        continue;
                    }
                    else
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
                }
                else if (recordInserts.ReasonPhrase == "Unauthorized")
                {
                    if (refreshed == true) break;
                    refreshed = await GetRefreshToken(refreshToken);
                    continue;
                }
                else
                {
                    Response.StatusCode = (int)recordInserts.StatusCode;
                    return recordInserts.Content.ReadAsStringAsync().Result;
                }
            } while (refreshed == true);

            ViewBag.LoggedIn = false;
            Session["LoggedIn"] = false;
            Response.StatusCode = 401;
            return "Unauthorized";
            
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpContent content = new StringContent(resumeUpdate, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(_sovrenUrl + "/update", content).Result;

                return response;
            }
        }

    }

}