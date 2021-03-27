using System.ComponentModel.DataAnnotations;

namespace PriceFalcon.Web.ViewModels.Home
{
    public class CreateDraftJobViewModel
    {
        [Required]
        [Url]
        public string? Url { get; set; }
    }
}
