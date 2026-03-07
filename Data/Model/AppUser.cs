namespace Progetto_Web_2_IoT_Auth.Data.Model;

public class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Mail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public ICollection<Access> Accesses { get; set; } = new List<Access>();
}
