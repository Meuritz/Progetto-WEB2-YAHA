namespace Progetto_Web_2_IoT_Auth.Data.Model;

public class Automation
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public bool Power { get; set; }

    public int Level { get; set; }

    public string TimeCondition { get; set; } = string.Empty;

    public string WeatherCondition { get; set; } = string.Empty;
}
