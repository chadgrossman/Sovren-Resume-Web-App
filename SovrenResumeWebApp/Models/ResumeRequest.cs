using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SovrenResumeWebApp.Models
{
    public class ResumeRequest
    {
        public string RecruiterName { get; set; }
        public IEnumerable<ResumeRequestObject> Resumes { get; set; }
    }

    public class ResumeRequestObject
    {
        public string EmailAddress { get; set; }
        public string SalesforceId { get; set; }
        public string FileName { get; set; }
        public string DocumentAsBase64String { get; set; }
    }
}