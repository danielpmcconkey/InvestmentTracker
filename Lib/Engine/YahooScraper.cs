using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Lib.DataTypes;
using Utilities;

namespace Lib.Engine
{
    public static class YahooScraper
    {
        private static DateTimeOffset _lastScrapeTime;
        private static TimeSpan _minTimeBetweenScrapes;

        static YahooScraper()
        {
            // set the last scrape time to five minutes ago
            _lastScrapeTime = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.BaseUtcOffset).AddMinutes(-5);
            _minTimeBetweenScrapes = ConfigManager.GetTimeSpan("MinTimeSpanBetweenYahooScrapes");
        }
        public static List<Valuation> GetHistoryPrices(
            string symbol, DateTimeOffset beginRange, DateTimeOffset endRange, int retries = 0)
        {
            Logger.info(string.Format("Scraping history prices for {0} from {1} to {2}.", symbol, beginRange, endRange));
            List<Valuation> prices = new List<Valuation>();

            // yahoo needs timestamps 
            long beginTimestamp = beginRange.ToUnixTimeSeconds();
            long endTimestamp = endRange.ToUnixTimeSeconds();

            // form the URL
            string urlWithVariables = "https://finance.yahoo.com/quote/{0}/history?";
            //urlWithVariables += "period1={1}&period2={2}&interval=1mo&filter=history&frequency=1mo&includeAdjustedClose=true";
            urlWithVariables += "period1={1}&period2={2}&interval=1d&filter=history&frequency=1mo&includeAdjustedClose=true";
            string url = string.Format(urlWithVariables, symbol, beginTimestamp, endTimestamp);

            // make sure we're not blitzing Yahoo
            WaitForNecessaryCoolDown();

            try
            {
                // scrape the page
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);

                _lastScrapeTime = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.BaseUtcOffset);

                // grab the security name while we're here
                var h1Nodes = doc.DocumentNode.SelectNodes("//h1");
                string securityName = h1Nodes[0].InnerText;

                // grab the current price
                var finStreamerNodes = doc.DocumentNode.SelectNodes("//fin-streamer [@class='Fw(b) Fz(36px) Mb(-4px) D(ib)']");
                //< fin - streamer class="Fw(b) Fz(36px) Mb(-4px) D(ib)" data-symbol="VSMAX" data-test="qsp-price" data-field="regularMarketPrice" data-trend="none" data-pricehint="2" value="97.18" active="">97.18</fin-streamer>
                string todaysPriceString = finStreamerNodes[0].InnerText;
                Valuation valToday = new Valuation();
                valToday.InvestmentVehicle = new InvestmentVehicle(securityName, symbol);
                valToday.Price = Decimal.Parse(todaysPriceString);
                valToday.Date = new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc), 
                    new TimeSpan(0, 0, 0));


                prices.Add(valToday);

                // now grab the table
                var rows = doc.DocumentNode.SelectNodes("//*[@id='Col1-1-HistoricalDataTable-Proxy'] / section / div[2] / table / tbody / tr");
                if(rows == null)
                {
                    Logger.warn("No data rows found on page. Skipping.");
                    return new List<Valuation>(); ;
                }
                foreach (var row in rows)
                {
                    if (row.ChildNodes.Count > 2)
                    {
                        // dividend rows only have 2 TDs
                        Valuation val = new Valuation();
                        val.InvestmentVehicle = new InvestmentVehicle(securityName, symbol);
                        var dateSpan = row.SelectSingleNode("td[1] / span");
                        var openSpan = row.SelectSingleNode("td[2] / span");

                        val.Date = new DateTimeOffset(DateTime.Parse(dateSpan.InnerText).Date, 
                            new TimeSpan(0, 0, 0));
                        val.Price = Decimal.Parse(openSpan.InnerText);
                        prices.Add(val);
                    }
                }
            }
            catch (Exception ex)
            {
                if(retries < ConfigManager.GetInt("dbMaxRetries"))
                {
                    Logger.warn("Exception thrown in YahooScraper.GetHistoryPrices. Re-trying.");
                    GetHistoryPrices(symbol, beginRange, endRange, retries + 1);
                }
                Logger.fatal("Unhandled exception in YahooScraper.GetHistoryPrices. Re-throwing", ex);
                throw;
            }
            return prices;
        }

        private static void WaitForNecessaryCoolDown()
        {
            var nowTime = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.BaseUtcOffset);
            TimeSpan distance = nowTime - _lastScrapeTime;
            if (distance < _minTimeBetweenScrapes)
            {
                double randomAdd = (double)DateTime.Now.Millisecond / (double)500;
                TimeSpan sleepTime = _minTimeBetweenScrapes - distance;
                double sleepSeconds = sleepTime.TotalSeconds + randomAdd;
                Logger.info(string.Format("Sleeping for {0} seconds to keep Yahoo from getting angry.",
                   sleepSeconds));
                int sleepMiliseconds = (int)(Math.Round(sleepSeconds * 1000,0));
                System.Threading.Thread.Sleep(sleepMiliseconds);
            }
        }
    }
}
