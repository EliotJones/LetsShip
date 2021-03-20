using System.ComponentModel.DataAnnotations;

namespace PriceFalcon.Web.ViewModels
{
    public class IndexViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
