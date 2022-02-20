using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class InvestmentVehicle : IEquatable<InvestmentVehicle>
    {
        public string Name { get; set; }
        public InvestmentVehicleType Type { get; set; }
        public string Symbol { get; set; } // only used if type is publicly traded

        public InvestmentVehicle(string name)
        {
            Name = name;
            Type = InvestmentVehicleType.PRIVATELY_HELD;
            Symbol = "N/A";
        }
        public InvestmentVehicle(string name, string symbol)
        {
            Name = name;
            Symbol = symbol;
            Type = InvestmentVehicleType.PUBLICLY_TRADED;
        }
        [JsonConstructor]
        public InvestmentVehicle(string name, string symbol, InvestmentVehicleType type)
        {
            Name = name;
            Symbol = symbol;
            Type = type;
        }
        public bool Equals(InvestmentVehicle other)
        {
            if (other == null)
                return false;

            if(Type == InvestmentVehicleType.PUBLICLY_TRADED)
            {
                if(other.Symbol == Symbol && other.Type == Type) return true;
                return false; ;
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
