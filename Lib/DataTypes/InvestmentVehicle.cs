using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public static class InvestmentVehiclesList
    {
        public static Dictionary<Guid, InvestmentVehicle> investmentVehicles { get; set; }
    }
    public class InvestmentVehicle : IEquatable<InvestmentVehicle>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public InvestmentVehicleType Type { get; set; }
        public string Symbol { get; set; } // only used if type is publicly traded
        public bool IsIndexFund { get; set; }
        public InvestmentBucket Bucket { get; set; }

        public InvestmentVehicle(string name)
        {
            Name = name;
            Type = InvestmentVehicleType.PRIVATELY_HELD;
            Symbol = "N/A";
            Id = Guid.NewGuid();
            IsIndexFund = false;
            Bucket = InvestmentBucket.NA;

            // write it to the DB
            DataAccessLayer.WriteNewInvestMentVehicleToDb(this);
        }
        public InvestmentVehicle(string name, string symbol)
        {
            Name = name;
            Symbol = symbol;
            Type = InvestmentVehicleType.PUBLICLY_TRADED;
            Id = Guid.NewGuid();
            IsIndexFund = false;
            Bucket = InvestmentBucket.NA;

            // write it to the DB
            DataAccessLayer.WriteNewInvestMentVehicleToDb(this);
        }
        [JsonConstructor]
        public InvestmentVehicle(Guid id, string name, string symbol, InvestmentVehicleType type, 
            bool isindexfund, InvestmentBucket investmentBucket)
        {
            Id = id; ;
            Name = name;
            Symbol = symbol;
            Type = type;
            IsIndexFund = isindexfund;
            Bucket = investmentBucket;
        }
        public bool Equals(InvestmentVehicle other)
        {
            if (other == null)
                return false;

            if(Type == InvestmentVehicleType.PUBLICLY_TRADED)
            {
                if(other.Symbol == Symbol && other.Type == Type) return true;
                return false;
            }
            if (Type == InvestmentVehicleType.PRIVATELY_HELD)
            {
                if (other.Name == Name && other.Type == Type) return true;
                return false;
            }
            return false;
        }
    }
    
}
