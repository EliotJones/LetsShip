using System.ComponentModel.DataAnnotations;

namespace PriceFalcon.Web.ViewModels
{
    public class CreateJobViewModel
    {
        [Required]
        [Url]
        public string Url { get; set; }
    }
}
