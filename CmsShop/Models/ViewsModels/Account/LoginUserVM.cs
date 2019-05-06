using System.ComponentModel.DataAnnotations;

namespace CmsShop.Models.ViewsModels.Account
{
    public class LoginUserVM
    {
        [Required]
        [Display(Name = "Nazwa użytkownika")]
        public string UserName { get; set; }
        [Required]
        [Display(Name = "Hasło")]
        public string Password { get; set; }
        [Display(Name = "Zapamietaj mnie")]
        public bool RememberMe { get; set; }
    }
}