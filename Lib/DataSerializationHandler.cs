using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Data;
using Lib.DataTypes;
using CsvHelper;
using System.Globalization;
using Utilities;
using Lib.Engine.MonteCarlo;

namespace Lib
{
    public static class DataSerializationHandler
    {
        private static string _dataDirectory;
        static DataSerializationHandler()
        {
            _dataDirectory = ConfigManager.GetString("DataDirectory");
        }
        public static List<Account> AddInitialTransactionsCSVToAccounts(List<Account> accounts)
        {
            string filePath = Path.Combine(_dataDirectory, 
                ConfigManager.GetString("InitialTransactionsFileName"));

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        TransactionType ttype = (csv.GetField<string>("Type") == "PURCHASE") ?
                                TransactionType.PURCHASE : TransactionType.SALE;
                        string fundName = csv.GetField<string>("Fund Name");
                        string symbol = csv.GetField<string>("Symbol");
                        InvestmentVehicle vehicle = new InvestmentVehicle(fundName, symbol);
                        DateTimeOffset date = new DateTimeOffset(
                            DateTime.Parse(csv.GetField<string>("Date")),
                            new TimeSpan(0, 0, 0));
                        Decimal quantity = csv.GetField<decimal>("Num Shares");
                        Decimal cashPrice = csv.GetField<decimal>("Cash Amount");
                        string accountName = csv.GetField<string>("Account Name");

                        if (vehicle.Symbol != "SPAXX")
                        {
                            Transaction transaction = new Transaction
                            {
                                TransactionType = ttype,
                                InvestmentVehicle = vehicle,
                                Date = date,
                                Quantity = quantity,
                                CashPriceTotalTransaction = cashPrice,
                            };
                            // find the right account to add this to
                            var account = accounts.Where(account => account.Name == accountName).FirstOrDefault();
                            account.Transactions.Add(transaction);
                        }                        
                    }
                }
            }
            return accounts;

        }
        public static List<Account> DeSerializeAccountsData()
        {
            string accountsPath = Path.Combine(_dataDirectory,
                ConfigManager.GetString("JsonAccountsFileName"));

            string jsonString = File.ReadAllText(accountsPath);
            List<Account> accounts = JsonSerializer.Deserialize<List<Account>>(jsonString);
            
            return accounts;
        }
        public static MonteCarloBatch DeserializeMonteCarloBatch(string serializedMonteCarlo)
        {
            return JsonSerializer.Deserialize<MonteCarloBatch>(serializedMonteCarlo);
        }
        public static List<Valuation> DeSerializePricingData()
        {
            string pricesPath = Path.Combine(_dataDirectory,
                ConfigManager.GetString("JsonPricesFileName"));

            // the prices file might not be built yet so return an empty list if so
            if(!File.Exists(pricesPath)) return new List<Valuation>();

            string jsonString = File.ReadAllText(pricesPath);
            List<Valuation> prices = JsonSerializer.Deserialize<List<Valuation>>(jsonString);

            //var housePrices = prices.Where(x => x.InvestmentVehicle.Name == "Primary residence")
            //    .OrderBy(y => y.Date).ToList();
            //foreach(var p in housePrices)
            //{
            //    Logger.debug(String.Format("Primary residence price: {0} = {1}", p.Date, p.Price ));
                
            //}

            return prices;
        }
        public static List<Account> ReadInitialAccountsCSV()
        {
            string filePath = Path.Combine(_dataDirectory,
                ConfigManager.GetString("InitialAccountsFileName"));

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    List<Account> accounts = csv.GetRecords<Account>().ToList();
                    foreach (var account in accounts)
                    {
                        account.Transactions = new List<Transaction>();
                    }
                    return accounts;
                }
            }
        }
        public static void SerializeData(List<Account> accounts, List<Valuation> prices)
        {
            // ensure the data directory exists
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            Logger.info("Serializing account data");
            string accountsJson = JsonSerializer.Serialize(accounts);
            string accountsPath = Path.Combine(_dataDirectory,
                ConfigManager.GetString("JsonAccountsFileName"));
            using (StreamWriter file = new(accountsPath, append: false))
            {
                file.Write(accountsJson);
            }

            Logger.info("Serializing price data");
            string pricesJson = JsonSerializer.Serialize(prices);
            string pricesPath = Path.Combine(_dataDirectory,
                ConfigManager.GetString("JsonPricesFileName"));
            using (StreamWriter file = new(pricesPath, append: false))
            {
                file.Write(pricesJson);
            }
        }
        public static string SerializeMonteCarloBatch(MonteCarloBatch batch)
        {
            return JsonSerializer.Serialize(batch);
        }


    }
}
