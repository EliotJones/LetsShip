using System;
using PriceFalcon.Domain;

namespace PriceFalcon.Web.ViewModels.Home
{
    public class TrackDraftJobLogViewModel
    {
        public DraftJobStatus Status { get; set; }

        public DateTime Created { get; set; }

        public string? Message { get; set; }
    }
}