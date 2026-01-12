using System.ComponentModel.DataAnnotations;

namespace Progetto_Web_2_IoT_Auth.Data.Model.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Inserire un nome"), StringLength(100)]
        public string Name { get; set; } = "";
        
        public string Password { get; set; } = "";
        
        [Required, Compare(nameof(Password), ErrorMessage = "Le password non corrispondono.")]
        public string ConfirmPassword { get; set; } = "";

        [Required, AllowedValues(values: ["User", "Admin"])]
        public string Role { get; set; } = "User";
    }
}