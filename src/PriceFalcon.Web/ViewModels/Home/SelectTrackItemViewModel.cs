using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PriceFalcon.Web.ViewModels.Home
{
    public class SelectTrackItemViewModel
    {
        public string Element { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public List<LineageItem> Lineage { get; set; } = new List<LineageItem>();

        public class LineageItem
        {
            public string Tag { get; set; } = string.Empty;

            public string? Id { get; set; }

            public string? Classes { get; set; }

            public string? Name { get; set; }
        }
    }

    public class SelectTrackItemResponseViewModel
    {
        public bool IsValid { get; set; }

        public decimal? Price { get; set; }

        public string? Reason { get; set; }
    }

    public class CreateJobViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string SelectionJson { get; set; } = string.Empty;
    }
}
