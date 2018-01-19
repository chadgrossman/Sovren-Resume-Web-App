using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Salesforce.Common;

namespace SovrenResumeWebApp.Controllers
{
    public class CallbackController : ApiController
    {
        private readonly string _consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private readonly string _consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private readonly string _callbackUrl = ConfigurationManager.AppSettings["CallbackUrl"];
        private readonly string _tokenRequestEndpointUrl = ConfigurationManager.AppSettings["TokenRequestEndpointUrl"];

        public async Task<HttpResponseMessage> Get(string display, string code)
        {
            var auth = new AuthenticationClient();
            await auth.WebServerAsync(_consumerKey, _consumerSecret, _callbackUrl, code, _tokenRequestEndpointUrl);

            var url = string.Format("/?instance_url={0}&token={1}&user={2}&refresh_token={3}",
                auth.InstanceUrl,
                auth.AccessToken,
                auth.Id.Substring(auth.Id.LastIndexOf("/") + 1),
                auth.RefreshToken);

            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri(url, UriKind.Relative);

            return response;
        }
    }
}
