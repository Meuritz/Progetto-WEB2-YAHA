using Microsoft.EntityFrameworkCore;
using Progetto_Web_2_IoT_Auth.Data;
using Progetto_Web_2_IoT_Auth.Data.Model;

namespace Progetto_Web_2_IoT_Auth.Services
{
    public class SettingsService
    {
        private readonly DbContextSQLite _dbContext;

        public SettingsService(DbContextSQLite dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new AppSetting { Key = key, Value = value };
                _dbContext.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                _dbContext.AppSettings.Update(setting);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
