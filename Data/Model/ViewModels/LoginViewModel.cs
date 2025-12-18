using System.ComponentModel.DataAnnotations;

namespace Progetto_Web_2_IoT_Auth.Data.Model.WievModels
{
    public class LoginViewModel
    {
        
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "Insert an Username!")]
        public string? Username { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Insert an Username!")] 
        public string Password { get; set; }
    }
}
