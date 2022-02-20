using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes.Simulation
{
    public class Asset
    {
        public DateTime created { get; set; }
        public Int64 amountContributed { get; set; }
        public Int64 amountCurrent { get; set; }
        public InvestmentIndex investmentIndex { get; set; }
        public TaxBucket taxBucket { get; set; }
        public DateTime? rmdDate { get; set; }
        public Asset clone()
        {
            Asset newAsset = new Asset();
            newAsset.created = created;
            newAsset.amountContributed = amountContributed;
            newAsset.amountCurrent = amountCurrent;
            newAsset.investmentIndex = investmentIndex;
            newAsset.taxBucket = taxBucket;
            newAsset.rmdDate = rmdDate;
            return newAsset;
        }
    }
}
