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
        public decimal monthlyGrossIncomePreRetirement { get; set; }
        public decimal monthlyNetSocialSecurityIncome { get; set; }
        public decimal monthlySpendLifeStyleToday { get; set; }
        public decimal monthlySpendCoreToday { get; set; }
        public decimal monthlyInvestRoth401k { get; set; }
        public decimal monthlyInvestTraditional401k { get; set; }
        public decimal monthlyInvestBrokerage { get; set; }
        public decimal monthlyInvestHSA { get; set; }
        public decimal annualRSUInvestmentPreTax { get; set; }
        public decimal xMinusAgeStockPercentPreRetirement { get; set; }
        public decimal numYearsCashBucketInRetirement { get; set; }
        public decimal numYearsBondBucketInRetirement { get; set; }
        public decimal recessionRecoveryPercent { get; set; }
        public bool shouldMoveEquitySurplussToFillBondGapAlways { get; set; }
        public int deathAgeOverride { get; set; }
        public decimal recessionLifestyleAdjustment { get; set; } // when there's been a recession, spend this percent of normal monthly spend
        public decimal retirementLifestyleAdjustment { get; set; } // come retirement day, permanently drop your lifestyle spend this much
        public decimal maxSpendingPercentWhenBelowRetirementLevelEquity { get; set; }
        public decimal annualInflationLow { get; set; }// = 0.01m;
        public decimal annualInflationHi { get; set; }// = 0.075m;
        public decimal socialSecurityCollectionAge { get; set; }
        public decimal livingLargeThreashold { get; set; } // if your total equities are greater that retirement level * this value, start living large
        public decimal livingLargeLifestyleSpendMultiplier { get; set; }
        public decimal beansAndWeeniesThreshold { get; set; }
        public decimal beansAndWeeniesCoreSpendMultiplier { get; set; }


    }
}
