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
    public static class TRoweScraper
    {
        private static Dictionary<string, string> _symbolReference;
        private static MoneyTypeConverter<Decimal> _moneyConverter;

        static TRoweScraper()
        {
            _symbolReference = new Dictionary<string, string>();
            _symbolReference.Add("VANGUARD DVLPD MRKTS IND INST", "VTMNX");
            _symbolReference.Add("TORONTO DOMINION BANK STOCK", "TD");
            _symbolReference.Add("VANGUARD INST INDEX   PLUS", "VIIIX");

            _moneyConverter = new MoneyTypeConverter<Decimal>();
        }
        public static List<Account> GetTransactions(List<Account> accounts)
        {
            string dataDirectory = ConfigManager.GetString("DataDirectory");
            string transactionsFile = ConfigManager.GetString("TRowePriceTransactionsFile");
            string filePath = Path.Combine(dataDirectory, transactionsFile);


            Logger.info(string.Format("Pulling T.Rowe Price transactions from {0}.", filePath));

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        DateTimeOffset date = new DateTimeOffset(
                            DateTime.Parse(csv.GetField<string>("Date")),
                            new TimeSpan(0, 0, 0));
                        
                        string accountName = "TD 401K Contributions";
                        string activityType = csv.GetField<string>("Activity Type").Trim();
                        if (activityType != "Market Fluctuation")
                        {
                            // market fluctuations have a cash amount, but no quantity
                            // so far these are really small, so just ignore them
                            string investment = csv.GetField<string>("Investment").Trim();
                            Decimal cashAmount = csv.GetField<decimal>("Amount", _moneyConverter);
                            Decimal quantity = csv.GetField<decimal>("Shares");
                            Decimal price = csv.GetField<decimal>("Price", _moneyConverter);
                            string symbol = _symbolReference[investment];

                            var matchingVehicles = InvestmentVehiclesList.investmentVehicles.Where(x =>
                                x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                && x.Value.Symbol == symbol);
                            InvestmentVehicle vehicle = null;
                            if (matchingVehicles.Count() > 0) vehicle = matchingVehicles.FirstOrDefault().Value;
                            else
                            {
                                vehicle = new InvestmentVehicle(investment, symbol);
                                InvestmentVehiclesList.investmentVehicles.Add(vehicle.Id, vehicle);
                            }

                            TransactionType tType = (activityType == "Fee") ?
                                TransactionType.SALE : TransactionType.PURCHASE;

                            var absCash = Math.Abs(cashAmount);
                            var absQty = Math.Abs(quantity);


                            var account = accounts.Where(account => account.Name == accountName).FirstOrDefault();
                            if (account.Transactions.Where(t => t.InvestmentVehicle.Equals(vehicle)
                                 && t.TransactionType == tType
                                 && t.Date == date
                                 && t.CashPriceTotalTransaction == absCash
                                 && t.Quantity == absQty)
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
