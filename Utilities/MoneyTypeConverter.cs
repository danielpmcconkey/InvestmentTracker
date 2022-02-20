using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class MoneyTypeConverter<T> : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (typeof(T) == TypeHelper.decimalType)
            {
                // removes dollar signs and commas
                return Decimal.Parse(text.Replace("$", "").Replace(",", ""));
            }
            else return text;
        }
    }
}
