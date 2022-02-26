using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace Utilities
{
    public enum ParamDbType
    {
        Bigint = NpgsqlDbType.Bigint,
        Double = NpgsqlDbType.Double,
        Integer = NpgsqlDbType.Integer,
        Numeric = NpgsqlDbType.Numeric,
        Smallint = NpgsqlDbType.Smallint,
        Money = NpgsqlDbType.Money,
        Boolean = NpgsqlDbType.Boolean,
        Char = NpgsqlDbType.Char,
        Varchar = NpgsqlDbType.Varchar,
        Timestamp = NpgsqlDbType.Timestamp,
        Uuid = NpgsqlDbType.Uuid,
        Text = NpgsqlDbType.Text,
        Json = NpgsqlDbType.Json,
    }
    public class DbCommandParameter
    {
        public NpgsqlParameter npgsqlParameter;
        public string ParameterName;
        public ParamDbType DbType;
        public object Value;
    }
    public class DbCommand : IDisposable
    {
        public NpgsqlCommand npgsqlCommand;

        public void Dispose()
        {
            npgsqlCommand.Dispose();
        }
        public DbCommand()
        {
            npgsqlCommand = new NpgsqlCommand();
        }
        public DbCommand(string query, NpgsqlConnection connection)
        {
            npgsqlCommand = new NpgsqlCommand(query, connection);
        }
        public void AddParameter(DbCommandParameter parameter)
        {
            NpgsqlParameter param = new NpgsqlParameter();
            param.ParameterName = parameter.ParameterName;
            param.NpgsqlDbType = (NpgsqlDbType)parameter.DbType;
            param.Value = parameter.Value;
            npgsqlCommand.Parameters.Add(param);
        }

    }
    public static class PostgresDAL
    {
        // config values
        static int dbMaxRetries = 0;
        static int dbRetrySleepMilliseconds = 500;


        static PostgresDAL()
        {
            if (FEATURETOGGLE.NO_WRITE)
            {
#pragma warning disable CS0162 // Unreachable code detected
                string warning = Environment.NewLine
                           + "***************************************************" + Environment.NewLine
                           + "***   WARNING WARNING WARNING WARNING WARNING   ***" + Environment.NewLine
                           + "***           DATABASE NO WRITE IS ON           ***" + Environment.NewLine
                           + "***************************************************" + Environment.NewLine;
                Logger.warn(warning);
#pragma warning restore CS0162 // Unreachable code detected
            }
            dbMaxRetries = ConfigManager.GetInt("dbMaxRetries");
            dbRetrySleepMilliseconds = ConfigManager.GetInt("dbRetrySleepMilliseconds");
        }
        #region core PG functions
        public static async Task<NpgsqlBinaryExporter> beginBinaryExportReader_async(NpgsqlConnection openConnection,
            string queryString, int retries = 0)
        {
            try
            {
                Task<NpgsqlBinaryExporter> t = Task.Run(() => openConnection.BeginBinaryExport(queryString));
                return await t;
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in beginBinaryExportReader_async. Retrying. Total retries so far {++retries}", ex);
                    await Task.Delay(dbRetrySleepMilliseconds);
                    return await beginBinaryExportReader_async(openConnection, queryString, retries);
                }
                else throw;
            }
        }
        public static NpgsqlDataReader executeReader(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return (NpgsqlDataReader)cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in executeReader_async. Retrying. Total retries so far {++retries}", ex);
                    System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                    return executeReader(cmd, retries);
                }
                else throw;
            }
        }
        public static async Task<NpgsqlDataReader> executeReader_async(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return (NpgsqlDataReader)await cmd.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in executeReader_async. Retrying. Total retries so far {++retries}", ex);
                    await Task.Delay(dbRetrySleepMilliseconds);
                    return await executeReader_async(cmd, retries);
                }
                else throw;
            }
        }
        public static object executeScalar(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in execute scalar. Retrying. Total retries so far {++retries}", ex);
                    System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                    return executeScalar(cmd, retries);
                }
                else throw;
            }
        }
        public static async Task<object> executeScalar_async(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return await cmd.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in execute scalar. Retrying. Total retries so far {++retries}", ex);
                    await Task.Delay(dbRetrySleepMilliseconds);
                    return await executeScalar_async(cmd, retries);
                }
                else throw;
            }
        }
        public static int executeNonQuery(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23505")// duplicate key value violates unique constraint; happens in multi-threaded save ops when two strats share a trait
                {
                    Logger.warn("Primary key violation in executeNonQuery_async. Throwing.");
                    throw;
                }
                if (ex.SqlState == "22003")// numeric field overflow
                {
                    Logger.warn("Numeric field overflow in executeNonQuery_async. Throwing.");
                    throw;
                }
                else throw;
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in executeNonQuery. Retrying. Total retries so far {++retries}", ex);
                    System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                    return executeNonQuery(cmd, retries);
                }
                else throw;
            }
        }
        public static async Task<object> executeNonQuery_async(NpgsqlCommand cmd, int retries = 0)
        {
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "23505")// duplicate key value violates unique constraint; happens in multi-threaded save ops when two strats share a trait
                {
                    Logger.warn("Primary key violation in executeNonQuery_async. Throwing.");
                    throw;
                }
                if (ex.SqlState == "22003")// numeric field overflow
                {
                    Logger.warn("Numeric field overflow in executeNonQuery_async. Throwing.");
                    throw;
                }
                else throw;
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn($"Exception in executeNonQuery_async. Retrying. Total retries so far {++retries}", ex);
                    await Task.Delay(dbRetrySleepMilliseconds);
                    return await executeNonQuery_async(cmd, retries);
                }
                else throw;
            }
        }
        public static NpgsqlConnection getConnection()
        {
            string connectionString = ConfigManager.GetString("DbConnectionString");
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            return conn;
        }
        public static void openConnection(NpgsqlConnection conn, int retries = 0)
        {
            try
            {
                conn.Open();
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "53300") // too_many_connections
                {
                    if (retries >= dbMaxRetries)
                    {
                        Logger.error("PostgresException: Too many retries exceeded. Throwing.");
                        throw;
                    }
                    else
                    {
                        Logger.warn("PostgresException: Too many DB connections. Retrying after sleep period");
                        System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                        openConnection(conn, ++retries);
                    }
                }
                else
                {
                    if (retries < dbMaxRetries)
                    {
                        Logger.error("PostgresException exception on query. Retrying.");
                        System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                        openConnection(conn, ++retries);
                    }
                    else throw;
                }
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn("Exception in open connection. Retrying.", ex);
                    System.Threading.Thread.Sleep(dbRetrySleepMilliseconds);
                    openConnection(conn, ++retries);
                }
                else throw;
            }
        }

        public static async Task openConnection_async(NpgsqlConnection conn, int retries = 0)
        {
            try
            {
                await conn.OpenAsync(); //.ConfigureAwait(false); todo: investigate if configure 
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "53300") // too_many_connections
                {
                    if (retries >= dbMaxRetries)
                    {
                        Logger.error("PostgresException: Too many retries exceeded. Throwing.");
                        throw;
                    }
                    else
                    {
                        Logger.warn("PostgresException: Too many DB connections. Retrying after sleep period");
                        await Task.Delay(dbRetrySleepMilliseconds); // await here otherwise the async method will continue without it
                        Task t = openConnection_async(conn, ++retries);
                        t.Wait();
                    }
                }
                else
                {
                    if (retries < dbMaxRetries)
                    {
                        Logger.error("PostgresException exception on query. Retrying.");
                        await Task.Delay(dbRetrySleepMilliseconds);
                        Task t = openConnection_async(conn, ++retries);
                        t.Wait();
                    }
                    else throw;
                }
            }
            catch (Exception ex)
            {
                if (retries < dbMaxRetries)
                {
                    Logger.warn("Exception in open connection. Retrying.", ex);
                    await Task.Delay(dbRetrySleepMilliseconds);
                    Task t = openConnection_async(conn, ++retries);
                    t.Wait();
                }
                else throw;
            }
        }
        #endregion

        #region generic get functions

        public static bool getBool(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetBoolean(reader.GetOrdinal(fieldName));
            }
            return false;
        }
        public static DateTime getDateTime(NpgsqlDataReader reader, string fieldName, DateTimeKind kind)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                if (kind == DateTimeKind.Utc)
                {
                    NpgsqlDateTime dbTime = reader.GetTimeStamp(reader.GetOrdinal(fieldName));
                    NpgsqlDateTime dbTimeUtc = dbTime.ToUniversalTime();
                    return DateTime.SpecifyKind(dbTimeUtc.ToDateTime(), kind);
                }
                else
                {
                    var dbTime = reader.GetDateTime(reader.GetOrdinal(fieldName));
                    return DateTime.SpecifyKind(dbTime, kind);
                }
            }
            return DateTime.MinValue;
        }
        public static DateTimeOffset getDateTimeOffset(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {

                NpgsqlDateTime dbTime = reader.GetTimeStamp(reader.GetOrdinal(fieldName));
                //NpgsqlDateTime dbTimeUtc = dbTime.ToUniversalTime();
                return new DateTimeOffset(dbTime.ToDateTime(), new TimeSpan(0));
            }
            return new DateTimeOffset(DateTime.MinValue, new TimeSpan(0));
        }
        public static decimal getDecimal(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return (decimal)reader.GetDecimal(reader.GetOrdinal(fieldName));
            }
            return 0;
        }
        public static double getDouble(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetDouble(reader.GetOrdinal(fieldName));
            }
            return 0;
        }
        public static Guid getGuid(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetGuid(reader.GetOrdinal(fieldName));
            }
            return Guid.NewGuid();
        }
        // why do we have two versions? because somewhere in the sands of time I decided to create a new GUID where
        // it found null in the DB. I have no idea if something in this code relies on there being a new GUID
        // created when the DB shows null, so I just created this new version.
        public static Guid getGuid_nullable(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetGuid(reader.GetOrdinal(fieldName));
            }
            return Guid.Empty;
        }
        public static int getInt(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetInt32(reader.GetOrdinal(fieldName));
            }
            return 0;
        }
        public static long getLong(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetInt64(reader.GetOrdinal(fieldName));
            }
            return 0;
        }
        public static decimal? getNullableDecimal(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return (decimal?)reader.GetDecimal(reader.GetOrdinal(fieldName));
            }
            return null;
        }
        public static double? getNullableDouble(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetDouble(reader.GetOrdinal(fieldName));
            }
            return null;
        }
        public static string getString(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                return reader.GetString(reader.GetOrdinal(fieldName));
            }
            return string.Empty;
        }
        public static TimeSpan getTimeSpan(NpgsqlDataReader reader, string fieldName)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(fieldName)))
            {
                long dbVal = reader.GetInt64(reader.GetOrdinal(fieldName));
                return new TimeSpan(dbVal);
            }
            return new TimeSpan(0);
        }
        #endregion generic get functions


        public static async Task sendWriteScriptToDb_async(string writeScript)
        {
            await sendWriteScriptToDb_async(writeScript, null);
        }
        public static async Task sendWriteScriptToDb_async(string writeScript, NpgsqlConnection conn = null)
        {
            await sendWriteScriptToDb_async(writeScript, 0, conn);
        }
        public static async Task sendWriteScriptToDb_async(string writeScript, int retryNum, NpgsqlConnection conn = null)
        {
            if (FEATURETOGGLE.NO_WRITE)
            {
#pragma warning disable CS0162 // Unreachable code detected
                return;
#pragma warning restore CS0162 // Unreachable code detected
            }
            bool closeConnection = false;
            if (conn == null)
            {
                conn = getConnection();
                Task tOpen = openConnection_async(conn);
                tOpen.Wait();
                closeConnection = true;
            }
            try
            {


                using (NpgsqlCommand cmd = new NpgsqlCommand(writeScript, conn))
                {
                    cmd.CommandTimeout = 300;   // timeout in seconds
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                }

            }
            catch (PostgresException ex)
            {
                if (ex.SqlState == "53300")// too_many_connections
                {
                    if (retryNum > dbMaxRetries)
                    {
                        Logger.error("PostgresException: Too many retries exceeded. Throwing.");
                        throw;
                    }
                    Logger.warn("PostgresException: Too many DB connections. Retrying after sleep period");
                    await Task.Delay(dbRetrySleepMilliseconds);
                    await sendWriteScriptToDb_async(writeScript, ++retryNum, conn);
                }
                else
                {
                    Logger.error("PostgresException exception on query: " + writeScript);
                    throw;
                }
            }
            finally
            {
                if (closeConnection)
                {
                    conn.Close();
                    ((IDisposable)conn).Dispose();
                }
            }
        }

    }
}

