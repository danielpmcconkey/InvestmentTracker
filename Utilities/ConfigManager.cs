using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Text.Json;

namespace Utilities
{
    public static class ConfigManager
    {
        private static Dictionary<string, string> _cache;
        const string _secretsEnvVarName = "InvestmentTrackerVars";

        
        public static void ReWriteSecrets()
        {
            string secretsString = Environment.GetEnvironmentVariable
                (_secretsEnvVarName, EnvironmentVariableTarget.User);
            var secretsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(secretsString);
            /*
             * add your new secrets just below this comment
             * use the format: 
             *  secretsDict.Add("name", "value");
            */

            /*
             * MAKE DAMN SURE YOU REMOVE IT BEFORE COMMITTING
             * 
             * 
             * */

            string secretsJson = JsonSerializer.Serialize(secretsDict);
            Environment.SetEnvironmentVariable(_secretsEnvVarName, secretsJson, EnvironmentVariableTarget.User);
        }
        public static void WriteDbConfigValue(string name, string value)
        {
            string query = "update " + _cache["dbConfigTable"] + " set value = @value where name = @name;";
            using (var conn = PostgresDAL.getConnection())
            {
                PostgresDAL.openConnection(conn);

                using (var cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        DbType = ParamDbType.Varchar,
                        ParameterName = "name",
                        Value = name
                    }
                    );
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        DbType = ParamDbType.Varchar,
                        ParameterName = "value",
                        Value = value
                    }
                    );

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("ConfigManager.WriteDbConfigValue data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }                
            }
        }

        private static void ReadAllConfig()
        {
            _cache = new Dictionary<string, string>();
            var appSettings = ConfigurationManager.AppSettings;

            foreach (var key in appSettings.AllKeys)
            {
                _cache.Add(key, appSettings[key]);
            }

            // read the secrets into cache, too
            string secretsString = Environment.GetEnvironmentVariable
                (_secretsEnvVarName, EnvironmentVariableTarget.User);
            Dictionary<string, string> secretsDict = JsonSerializer
                .Deserialize<Dictionary<string, string>>(secretsString);
            foreach (var secret in secretsDict)
            {
                _cache.Add(secret.Key, secret.Value);
            }

            // overwrite with DB config
            string query = "select name, value from " + _cache["dbConfigTable"];
            using (var conn = PostgresDAL.getConnection())
            {
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            var name = PostgresDAL.getString(reader, "name");
                            var value = PostgresDAL.getString(reader, "value");
                            if(_cache.ContainsKey(name))
                            {
                                // overwrite
                                _cache[name] = value;
                            }
                            else
                            {
                                // add
                                _cache.Add(name, value);
                            }
                        }
                    }
                }
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
