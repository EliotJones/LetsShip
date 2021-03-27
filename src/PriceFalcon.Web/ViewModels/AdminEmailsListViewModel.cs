using System;
using System.Collections.Generic;

namespace PriceFalcon.Web.ViewModels
{
    public class AdminEmailsListViewModel
    {
        public List<EmailViewModel> Emails { get; set; } = new List<EmailViewModel>();
    }

    public class EmailViewModel
    {
        public string Subject { get; set; } = string.Empty;

        public string Recipient { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime Created { get; set; }
    }
}
