using Lib.DataTypes;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;

namespace Lib.Engine
{
    public static class FidelityScraper
    {
        public static List<Account> GetTransactions(List<Account> accounts)
        {
            string dataDirectory = ConfigManager.GetString("DataDirectory");
            string fidelityFile = ConfigManager.GetString("FidelityTransactionsFile");
            string filePath = Path.Combine(dataDirectory,fidelityFile);
            

            Logger.info(string.Format("Pulling Fidelity transactions from {0}.", filePath));

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        DateTimeOffset date = new DateTimeOffset(
                            DateTime.Parse(csv.GetField<string>("Run Date")),
                            new TimeSpan(0, 0, 0));
                        DateTime settlementDate = DateTime.Now;
                        if (DateTime.TryParse(csv.GetField<string>("Settlement Date"), out settlementDate))
                        {
                            date = new DateTimeOffset(settlementDate, new TimeSpan(0, 0, 0));
                        }
                        string accountName = csv.GetField<string>("Account").Trim();
                        string action = csv.GetField<string>("Action").Trim();
                        string symbol = csv.GetField<string>("Symbol").Trim();
                        string strQty = csv.GetField<string>("Quantity").Trim();
                        string strPrice = csv.GetField<string>("Amount ($)").Trim();
                        if (symbol != String.Empty && symbol != "SPAXX")
                        {
                            Decimal cashAmount = csv.GetField<decimal>("Amount ($)");
                            string securityDescription = csv.GetField<string>("Security Description").Trim();

                            var matchingVehicles = InvestmentVehiclesList.investmentVehicles.Where(x =>
                                x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                && x.Value.Symbol == symbol);
                            InvestmentVehicle vehicle = null;
                            if (matchingVehicles.Count() > 0)
                            {
                                vehicle = matchingVehicles.FirstOrDefault().Value;
                            }
                            else
                            {
                                vehicle = new InvestmentVehicle(securityDescription, symbol);
                                InvestmentVehiclesList.investmentVehicles.Add(vehicle.Id, vehicle);
                            } 

                            TransactionType tType = (cashAmount > 0) ? TransactionType.SALE : TransactionType.PURCHASE;

                            // sometimes the quantity is missing, like on dividends
                            Decimal quantity = 0M;
                            if (strQty == String.Empty)
                            {
                                try
                                {
                                    decimal todaysPrice = PricingEngine.GetPriceAtDate(vehicle, date).Price;
                                    quantity = Math.Round(cashAmount / todaysPrice,4);
                                }
                                catch (Exception)
                                {

                                    throw;
                                }

                                // these transactions a positive is a credit to the 
                                if (cashAmount > 0) tType = TransactionType.PURCHASE;
                                else tType = TransactionType.SALE;
                            }
                            else quantity = csv.GetField<decimal>("Quantity");
                            
                            var absCash = Math.Abs(cashAmount);
                            var absQty = Math.Abs(quantity);


                            var account = accounts.Where(account => account.Name == accountName).FirstOrDefault();
                            if (account.Transactions.Where(t => t.InvestmentVehicle.Equals(vehicle)
                                 && t.TransactionType == tType
                                 && t.Date == date
                                 && t.CashPriceTotalTransaction == absCash
                                 && Math.Round(t.Quantity,4) == absQty)
                                .Count() == 0)
                            {

                                Transaction t = new Transaction(tType, vehicle, date, absCash, absQty);
                                account.Transactions.Add(t);
                                DataAccessLayer.WriteNewTransactionToDb(t, account.Id);
                            }
                        }
                    }
                }
            }
            return accounts;
        }
    }
}
