using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes.Simulation
{
    public class SimulationParameters
    {
        public DateTime startDate { get; set; }
        public DateTime retirementDate { get; set; }
        public DateTime birthDate { get; set; }
        //public int riskAppetite { get; set; }
        //public decimal retirementDrawDownPercent { get; set; }
        //public List<Asset> assets { get; set; }
        public decimal monthlyGrossIncomePreRetirement { get; set; }
        public decimal monthlyNetSocialSecurityIncome { get; set; }
        public decimal monthlySpendLifeStyleToday { get; set; }
        public decimal monthlyInvestRoth401k { get; set; }
        public decimal monthlyInvestTraditional401k { get; set; }
        public decimal monthlyInvestBrokerage { get; set; }
        public decimal monthlyInvestHSA { get; set; }
        public decimal annualRSUInvestment { get; set; }
        public decimal minBondPercentPreRetirement { get; set; }
        public decimal maxBondPercentPreRetirement { get; set; }
        public decimal xMinusAgeStockPercentPreRetirement { get; set; }
        public decimal numYearsCashBucketInRetirement { get; set; }
        public decimal numYearsBondBucketInRetirement { get; set; }
        public decimal recessionRecoveryPercent { get; set; }
        public bool shouldMoveEquitySurplussToFillBondGapAlways { get; set; }
        public int deathAgeOverride { get; set; }
        public decimal recessionLifestyleAdjustment { get; set; } // when there's been a recession, spend this percent of normal monthly spend
        public decimal maxSpendingPercentWhenBelowRetirementLevelEquity { get; set; }



    }
}
