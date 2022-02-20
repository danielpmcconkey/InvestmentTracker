using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GraphLib.Utilities
{
    internal static class TextHelper
    {
        internal static string FormatGraphLabel(object raw, Type type, string format = "default")
        {
            if (type == TypeHelper.int32Type)
            {
                return ((int)raw).ToString(format == "default" ? "#,##0" : format);
            }
            else if (type == TypeHelper.int64Type)
            {
                return ((long)raw).ToString(format == "default" ? "#,##0" : format);
            }
            else if (type == TypeHelper.floatType)
            {
                return ((float)raw).ToString(format == "default" ? "#,##0.00" : format);
            }
            else if (type == TypeHelper.doubleType)
            {
                return ((double)raw).ToString(format == "default" ? "#,##0.00" : format);
            }
            else if (type == TypeHelper.decimalType)
            {
                return ((decimal)raw).ToString(format == "default" ? "#,##0.00" : format);
            }
            else if (type == TypeHelper.dateTimeOffsetType)
            {
                return ((DateTimeOffset)raw).ToString(format == "default" ? "MMM \"'\"yy" : format);
            }
            // anything else not yet implemented
            else throw new NotImplementedException();
        }
    }
}
