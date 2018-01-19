using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SovrenResumeWebApp.Models
{
    public class ParsedResume
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SalesforceId { get; set; }
        public string CandidateId { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
    }
}