using System.ComponentModel.DataAnnotations;

namespace Voicer.Models
{
    public class ManageAccountViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }
}
