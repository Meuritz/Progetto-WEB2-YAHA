namespace Progetto_Web_2_IoT_Auth.Data.Model;

public class Access
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;

    public string AccessLevel { get; set; } = string.Empty;
}
