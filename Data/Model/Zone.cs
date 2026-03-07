namespace Progetto_Web_2_IoT_Auth.Data.Model;

public class Zone
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public ICollection<Access> Accesses { get; set; } = new List<Access>();

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
