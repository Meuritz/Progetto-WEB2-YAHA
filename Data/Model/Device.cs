namespace Progetto_Web_2_IoT_Auth.Data.Model
{
    public class Device
    {
        public int Id { get; set; }

        public int ZoneId { get; set; }
        public Zone Zone { get; set; } = null!;

        public int DeviceTypeId { get; set; }
        public DeviceType DeviceType { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public string? IpAddress { get; set; }

        public bool Power { get; set; }

        public int Level { get; set; }

        public ICollection<Automation> Automations { get; set; } = new List<Automation>();
    }
}
