using System.ComponentModel.DataAnnotations;

namespace PriceFalcon.Web.ViewModels.Home
{
    public class IndexViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
