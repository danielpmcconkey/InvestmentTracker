using Lib.DataTypes;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Lib.Engine
{
    internal static class ZillowScraper
    {
        public static Valuation GetCurrentPrice(string ZillowAddressForUrl)
        {
            Logger.info(string.Format("Scraping Zillow price for {0}.", ZillowAddressForUrl));

            // form the URL
            string url = string.Format("https://www.zillow.com/homedetails/{0}/6352178_zpid/", ZillowAddressForUrl);

            try
            {
                string chromeDriverDir = "./";

                using (IWebDriver driver = new ChromeDriver(chromeDriverDir)) // ChromeDriver())
                {
                    driver.Navigate().GoToUrl(url);
                    IWebElement values = driver.FindElement(By.Id("home-details-home-values"));

                    var allH3s = values.FindElements(By.XPath("//h3[contains(@class, \"Text-c11n-8-63-0__sc-aiai24-0\")]"));
                    string zestimate = allH3s[0].Text;

                    decimal mortgageBalance = ConfigManager.GetDecimal("PrimaryResidenceMortgageBalance");
                    decimal currentHousePrice = Decimal.Parse(zestimate.Replace('$', ' ').Trim());
                    currentHousePrice -= mortgageBalance;

                    Valuation valToday = new Valuation(
                        InvestmentVehiclesList.investmentVehicles.Where(x =>
                            x.Value.Type == InvestmentVehicleType.PRIVATELY_HELD
                            && x.Value.Name == "Primary residence").FirstOrDefault().Value,
                        new DateTimeOffset(
                            DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc),
                            new TimeSpan(0, 0, 0)),
                        currentHousePrice
                        );

                    driver.Close();

                    return valToday;


                }
            }
            catch (Exception ex)
            {
                Logger.fatal("Unhandled exception in ZillowScraper.GetCurrentPrice. Re-throwing", ex);
                throw;
            }
        }
    }
}
