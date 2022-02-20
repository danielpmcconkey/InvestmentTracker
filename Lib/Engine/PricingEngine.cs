using Lib.DataTypes;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Engine
{
    public class PricingEngine
    {
        public List<Valuation> valuations { get { return _valuations; }  }
        private List<Valuation> _valuations;
        private HashSet<string> _problemSymbols;


        public PricingEngine(List<Valuation> valuations)
        {
            _valuations = valuations;

            // skip these symbols when looking up prices
            // because it seems tehy're not the right symbol and it just throws everything off
            _problemSymbols = new HashSet<string>();
            _problemSymbols.Add("VITLX");
            _problemSymbols.Add("VINIX");
            _problemSymbols.Add("VEXMX");
        }
        public List<Valuation> BlendPricesWithRealTransactions(List<Account> accounts)
        {
            // use the real transactions to fill in days
            foreach (var account in accounts)
            {
                foreach (var transaction in account.Transactions)
                {
                    if (transaction.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED)
                    {
                        if (!IsPriceAtDate(transaction.InvestmentVehicle.Symbol, transaction.Date))
                        {
                            // price doesn't already exist. add this one
                            Valuation valuation = new Valuation()
                            {
                                Date = transaction.Date,
                                InvestmentVehicle = transaction.InvestmentVehicle,
                                Price = Math.Round(transaction.CashPriceTotalTransaction / transaction.Quantity, 4)
                            };
                            _valuations.Add(valuation);
                        }
                    }
                }
            }
            return _valuations;
        }
        public List<Valuation> BlendPricesDaily(List<Account> accounts)
        {
            // now go through each security and fill in the blanks with trend lines
            // get a list of distinct securities from the prices list
            List<InvestmentVehicle> vehiclesToCheck = new List<InvestmentVehicle>();

            // get unique symbols for publicly traded
            var symbols = _valuations.Where(t => t.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED)
                .Select(x => x.InvestmentVehicle.Symbol).Distinct().ToList();
            foreach (var symbol in symbols) vehiclesToCheck.Add(new InvestmentVehicle(symbol, symbol));
            
            // get unique names for private assets
            var names = _valuations.Where(t => t.InvestmentVehicle.Type == InvestmentVehicleType.PRIVATELY_HELD)
                .Select(x => x.InvestmentVehicle.Name).Distinct().ToList();
            foreach (var name in names) vehiclesToCheck.Add(new InvestmentVehicle(name));

            // add SPY for comparison
            if(!symbols.Contains("SPY"))
            {
                vehiclesToCheck.Add(new InvestmentVehicle("SPDR S&P 500 ETF Trust", "SPY"));
            }


            foreach (var vehicle in vehiclesToCheck)
            {
                var pricesForSymbol = _valuations.Where(x => 
                    (x.InvestmentVehicle.Type == InvestmentVehicleType.PRIVATELY_HELD 
                    && vehicle.Type == InvestmentVehicleType.PRIVATELY_HELD
                    && x.InvestmentVehicle.Name == vehicle.Name) 
                    ||
                    (x.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                    && vehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                    && x.InvestmentVehicle.Symbol == vehicle.Symbol)
                    )
                    .OrderBy(y => y.Date)
                    .ToArray();

                // iterate from first to second-from-last 
                // each time gram this one and the next
                // and fill in any dates between
                for (int i = 0; i < pricesForSymbol.Count() - 1; i++)
                {
                    var thisPrice = pricesForSymbol[i];
                    var nextPrice = pricesForSymbol[i + 1];

                    // only fill in if they're multiple days apart
                    if (nextPrice.Date.Date - thisPrice.Date.Date > new TimeSpan(1, 0, 0, 0))
                    {
                        DateTimeOffset firstDateToFillIn = new DateTimeOffset(thisPrice.Date.Date,
                            new TimeSpan(0, 0, 0)).AddDays(1);
                        DateTimeOffset lastDateToFillIn = new DateTimeOffset(nextPrice.Date.Date,
                            new TimeSpan(0, 0, 0)).AddDays(-1);
                        DateTimeOffset thisDateToFillIn = firstDateToFillIn;

                        // find the slope to tell us how much it moves in a day
                        // slope = rise over run
                        decimal rise = (nextPrice.Price - thisPrice.Price);
                        decimal run = (decimal)((nextPrice.Date - thisPrice.Date).TotalDays);
                        decimal slope = rise / run;


                        while (thisDateToFillIn <= lastDateToFillIn)
                        {
                            if (!IsPriceAtDate(vehicle, thisDateToFillIn))
                            {
                                // new price = first price + (slope * how many days since first price)
                                decimal numDays = (decimal)((thisDateToFillIn - thisPrice.Date).TotalDays);
                                decimal newPrice = thisPrice.Price + (slope * numDays);
                                Valuation valuation = new Valuation()
                                {
                                    Price = newPrice,
                                    Date = thisDateToFillIn,
                                    InvestmentVehicle = thisPrice.InvestmentVehicle,
                                };
                                _valuations.Add(valuation);
                            }


                            thisDateToFillIn = thisDateToFillIn.AddDays(1);
                        }
                    }
                }
            }

            return _valuations;
        }
        
        /// <summary>
        /// use this when you want to make sure you have all the prices
        /// you need for all the positions in your accounts list. It also
        /// updates the the pricing engine's valuations list
        /// </summary>
        /// <param name="accounts"></param>
        /// <returns>a caught up pricing list</returns>
        public List<Valuation> CatchUpPrices(List<Account> accounts)
        {
            // get a list of distinct securities from the accounts list
            List<InvestmentDuration> list = GetPublicInvestmentDurationsFromAccounts(accounts);
            
            // add SPY for comparison
            if (list.Where(x => x.investmentVehicle.Symbol == "SPY").Count() == 0)
            {
                var transactions = accounts.SelectMany(x => x.Transactions)
                    .Where(v => v.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED);
                var minDate = transactions.Min(t => t.Date);
                var maxDate = transactions.Max(t => t.Date);
                list.Add(new InvestmentDuration(
                    new InvestmentVehicle("SPDR S&P 500 ETF Trust", "SPY"), 
                    minDate, maxDate));
            }

            

            DateTimeOffset today = new DateTimeOffset(
                DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc),
                new TimeSpan(0, 0, 0));


            foreach (var row in list)
            {
                if (!_problemSymbols.Contains(row.investmentVehicle.Symbol))
                {
                    DateTimeOffset minDateFirst = DateTimeHelper.GetFirstOfMonth(row.minDate);
                    DateTimeOffset maxDateFirst = DateTimeHelper.GetFirstOfMonth(DateTime.Today.Date.AddMonths(1)); // first of next month

                    // don't go past today, though
                    
                    if (maxDateFirst > today) maxDateFirst = today;

                    // now catch the price up for this symbol
                    CatchUpPrice(row.investmentVehicle.Symbol, minDateFirst, maxDateFirst);
                }
            }

            // now do primary residence
            if(_valuations.Where(x => x.InvestmentVehicle.Type == InvestmentVehicleType.PRIVATELY_HELD
                && x.InvestmentVehicle.Name == "Primary residence"
                && x.Date == today
                ).Count() == 0)
            {
                string addressPortion = ConfigManager.GetString("ZillowAddressForUrl");
                var currentHousePrice = ZillowScraper.GetCurrentPrice(addressPortion);
                decimal mortgageBalance = ConfigManager.GetDecimal("PrimaryResidenceMortgageBalance");
                currentHousePrice.Price -= mortgageBalance;
                _valuations.Add(currentHousePrice);
            }

            
            return _valuations;
        }
        public Valuation GetPriceAtDate(InvestmentVehicle vehicle, DateTimeOffset dateTime)
        {
            // check if already present
            var valuations = _valuations.Where(x =>
                    (
                        (
                            x.InvestmentVehicle.Type == InvestmentVehicleType.PRIVATELY_HELD
                            && vehicle.Type == InvestmentVehicleType.PRIVATELY_HELD
                            && x.InvestmentVehicle.Name == vehicle.Name
                        )
                        ||
                        (
                            x.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && vehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && x.InvestmentVehicle.Symbol == vehicle.Symbol
                        )
                    ) 
                    && x.Date == dateTime
                    ).ToList();


            if (valuations.Count() > 0)
            {
                return valuations[0];
            }

            // not already present
            // go find one off the internet
            if (!_problemSymbols.Contains(vehicle.Symbol))
            {
                Logger.warn(string.Format("Price not found for {0}/{2} at {1}", vehicle.Symbol, dateTime, vehicle.Name));
            }
            return new Valuation()
            {
                Date = dateTime,
                InvestmentVehicle = vehicle,
                Price = 0
            };

        }
        public List<InvestmentDuration> GetPrivateInvestmentDurationsFromAccounts(List<Account> accounts)
        {
            List<InvestmentDuration> returnList = new List<InvestmentDuration>();

            var transactions = accounts.SelectMany(x => x.Transactions);
            var privateTransactions = transactions.Where(v =>
                v.InvestmentVehicle.Type == InvestmentVehicleType.PRIVATELY_HELD).ToList();
            var names = privateTransactions.Select(x => x.InvestmentVehicle.Name).Distinct();
            foreach (var name in names)
            {

                // find the first of the month dates to use to catch up
                var transactionsForName = privateTransactions.Where(
                    x => x.InvestmentVehicle.Name== name
                    );
                var minDate = transactionsForName.Min(x => x.Date);
                var maxDate = transactionsForName.Max(x => x.Date);
                InvestmentVehicle vehicle = new InvestmentVehicle(name);

                returnList.Add(new InvestmentDuration(vehicle, minDate, maxDate));
            }
            return returnList;
        }
        public List<InvestmentDuration>GetPublicInvestmentDurationsFromAccounts(List<Account>accounts)
        {
            List<InvestmentDuration> returnList = new List<InvestmentDuration>();

            var transactions = accounts.SelectMany(x => x.Transactions);
            var etfTransactions = transactions.Where(v =>
                v.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED);
            var symbols = etfTransactions.Select(x => x.InvestmentVehicle.Symbol).Distinct();
            foreach (var symbol in symbols)
            {
                
                // find the first of the month dates to use to catch up
                var transactionsForSymbol = etfTransactions.Where(x => x.InvestmentVehicle.Symbol == symbol);
                var minDate = transactionsForSymbol.Min(x => x.Date);
                var maxDate = transactionsForSymbol.Max(x => x.Date);
                InvestmentVehicle vehicle = new InvestmentVehicle(symbol, symbol);

                returnList.Add(new InvestmentDuration(vehicle, minDate, maxDate));
            }
            return returnList;
        }
        public string[] GetUniqueSymbolsFromValuations()
        {
            string[] symbols = _valuations.Select(x => x.InvestmentVehicle.Symbol).Distinct().ToArray();
            return symbols;
        }
        public void PrintPrices()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0}\t", "DATE"));

            // first set up a dictionary of int (position) and string (symbol) so we can always know
            // which column a price goes in
            string[] symbols = _valuations.Select(x => x.InvestmentVehicle.Symbol).Distinct().ToArray();
            Dictionary<string, int> symbolsDict = new Dictionary<string, int>();
            for(int i = 0; i < symbols.Length; i++)
            {
                symbolsDict.Add(symbols[i], i);
                // also add it to the header row
                sb.Append(string.Format("{0}\t", symbols[i]));
            }
            sb.Append(Environment.NewLine); // end of header

            // now set up a dictionary of datetime, decimal[length symbols.length]
            // and add each price into it in the right position of the decimal array
            Dictionary<DateTimeOffset, decimal[]> priceRows = new Dictionary<DateTimeOffset, decimal[]>();
            
            foreach (Valuation valuation in _valuations)
            {
                var symbol = valuation.InvestmentVehicle.Symbol;
                var date = valuation.Date;
                var price = valuation.Price;
                int symbolPosition = symbolsDict.GetValueOrDefault(symbol, -1);

                // add new array onto the dictionary if doesn't exist already
                if(!priceRows.ContainsKey(date))
                {
                    decimal[] newArray = new decimal[symbols.Length];
                    newArray[symbolPosition] = price;
                    priceRows.Add(date, newArray);
                }
                // or add the price to the array if it does exist already
                else
                {
                    priceRows[date][symbolPosition] = price;
                }
            }
            var sortedRows = priceRows.OrderBy(x => x.Key);
            foreach (var row in sortedRows)
            {
                sb.Append(string.Format("{0}\t", row.Key));//.ToString("yyyy-MM-dd HH:mm:ss")));
                foreach (decimal thisPrice in row.Value)
                {
                    string printPrice = (thisPrice == 0) ? string.Empty : thisPrice.ToString("0.00");
                    sb.Append(string.Format("{0}\t", thisPrice));
                }
                sb.Append(Environment.NewLine); // end of row
            }
            Logger.info("pricing dump:" + Environment.NewLine + sb.ToString());
        }


        private void CatchUpPrice(string symbol, DateTimeOffset begin, DateTimeOffset end)
        {
            DateTimeOffset thisDate = begin;
            bool isPriceScrapeNeeded = false;
            DateTimeOffset beginScrape = end.AddMonths(1); // make it out of range at the beginning so any month found missing will always be earlier than the initial set value
            
            // check back prices month over month to see if we need to fill in any gaps
            while (thisDate <= end)
            {
                if(!IsPriceNearDate(symbol, thisDate))
                {
                    isPriceScrapeNeeded = true;
                    if(thisDate < beginScrape) beginScrape = thisDate;
                }
                thisDate = thisDate.AddMonths(1);
            }
            // now check if we have a today's price
            if(!IsPriceAtDate(symbol, end))
            {
                isPriceScrapeNeeded = true;
                if (thisDate < beginScrape) beginScrape = thisDate;
            }
            if(isPriceScrapeNeeded)
            {
                
                // always run to present day
                DateTimeOffset endScrape = end.AddMonths(1);
                // just in case we did something weird and got our dates backward
                // set begin to a minimum of 6 months behind end
                if (beginScrape >= endScrape.AddMonths(-6)) beginScrape = endScrape.AddMonths(-6);

                List<Valuation> yahooPrices = YahooScraper.GetHistoryPrices(
                    symbol, beginScrape, endScrape);

                foreach(Valuation valuation in yahooPrices)
                {
                    if(!IsPriceAtDate(valuation.InvestmentVehicle.Symbol, valuation.Date))
                    {
                        _valuations.Add(valuation);
                    }
                    else
                    {
                        // update the price
                        Valuation oldValuation = _valuations.Where(x => 
                            x.InvestmentVehicle.Symbol == valuation.InvestmentVehicle.Symbol
                            && x.Date == valuation.Date
                            ).FirstOrDefault();
                        oldValuation.Price = valuation.Price;
                    }
                }

                


            }
        }
        private bool IsPriceAtDate(InvestmentVehicle vehicle, DateTimeOffset date)
        {
            var nearEnoughPrices = _valuations.Where(
                x => x.InvestmentVehicle == vehicle
                && x.Date == date);
            if (nearEnoughPrices.Count() > 0) return true;
            return false;
        }
        private bool IsPriceAtDate(string symbol, DateTimeOffset date)
        {
            var nearEnoughPrices = _valuations.Where(
                x => x.InvestmentVehicle.Symbol == symbol
                && x.Date == date);
            if (nearEnoughPrices.Count() > 0) return true;
            return false;
        }
        private bool IsPriceNearDate(string symbol, DateTimeOffset date)
        {
            TimeSpan nearEnough = ConfigManager.GetTimeSpan("TimeSpanForDateNearnessEvaluation");
            var nearEnoughPrices = _valuations.Where(
                x => x.InvestmentVehicle.Symbol == symbol
                && x.Date >= date - nearEnough
                && x.Date <= date + nearEnough
                );
            if (nearEnoughPrices.Count() > 0) return true;
            return false;
        }
    }
}
