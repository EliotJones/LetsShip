using System;
using System.Collections.Generic;

namespace PriceFalcon.Web.ViewModels
{
    public class AdminEmailsListViewModel
    {
        public List<EmailViewModel> Emails { get; set; }
    }

    public class EmailViewModel
    {
        public string Subject { get; set; }

        public string Recipient { get; set; }

        public string Body { get; set; }

        public DateTime Created { get; set; }
    }
}
