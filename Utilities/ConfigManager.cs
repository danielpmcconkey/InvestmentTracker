using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Utilities
{
    public static class ConfigManager
    {
        private static Dictionary<string, string> _cache;
        
        private static void ReadAllConfig()
        {
            _cache = new Dictionary<string, string>();
                var appSettings = ConfigurationManager.AppSettings;

            foreach (var key in appSettings.AllKeys)
            {
                _cache.Add(key, appSettings[key]);
            }
        }
        public static string GetString(string key)
        {
            if (_cache == null)
            {
                ReadAllConfig();
            }
            if (_cache.ContainsKey(key))
                return _cache[key];
            else
            {
                throw new Exception("Config key not found in config file");
            }
        }
        public static int GetInt(string key)
        {
            return int.Parse(ConfigManager.GetString(key));
        }
        public static bool GetBool(string key)
        {
            return bool.Parse(ConfigManager.GetString(key));
        }
        public static DateTime GetDateTime(string key)
        {
            return DateTime.Parse(ConfigManager.GetString(key));
        }
        public static Decimal GetDecimal(string key)
        {
            return Decimal.Parse(GetString(key));
        }
        public static TimeSpan GetTimeSpan(string key)
        {
            return TimeSpan.Parse(ConfigManager.GetString(key));
        }        
    }
}
