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
    public static class HealthEquityScraper
    {
        private static MoneyTypeConverter<Decimal> _moneyConverter;

        static HealthEquityScraper()
        {
            _moneyConverter = new MoneyTypeConverter<Decimal>();
        }
        public static List<Account> GetTransactions(List<Account> accounts)
        {
            string dataDirectory = ConfigManager.GetString("DataDirectory");
            string transactionsFile = ConfigManager.GetString("HealthEquityTransactionsFile");
            string filePath = Path.Combine(dataDirectory, transactionsFile);


            Logger.info(string.Format("Pulling Health Equity transactions from {0}.", filePath));

            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        string accountName = "Dan's HSA";

                        DateTimeOffset date = new DateTimeOffset(
                            DateTime.Parse(csv.GetField<string>("Date")),
                            new TimeSpan(0, 0, 0));
                        string symbol = csv.GetField<string>("Fund").Trim();
                        string category = csv.GetField<string>("Category").Trim();
                        Decimal price = csv.GetField<decimal>("Price", _moneyConverter);
                        Decimal cashAmount = csv.GetField<decimal>("Amount", _moneyConverter);
                        Decimal quantity = csv.GetField<decimal>("Shares");
                        string fundName = (symbol == "VSMAX") ?
                            "Vanguard Small-Cap Index Fund Admiral Shares" : symbol;

                        TransactionType tType = TransactionType.PURCHASE;

                        var matchingVehicles = InvestmentVehiclesList.investmentVehicles.Where(x =>
                                x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                && x.Value.Symbol == symbol);
                        InvestmentVehicle vehicle = null;
                        if (matchingVehicles.Count() > 0) vehicle = matchingVehicles.FirstOrDefault().Value;
                        else
                        {
                            vehicle = new InvestmentVehicle(fundName, symbol);
                            InvestmentVehiclesList.investmentVehicles.Add(vehicle.Id, vehicle);
                        }

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
            return accounts;
        }
    }
}
