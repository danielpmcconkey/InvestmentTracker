using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public enum ObjectComparison
    {
        GREATER,
        LESS,
        EQUAL
    }
    public static class TypeHelper
    {
        // type definitions
        public static Type int32Type;
        public static Type doubleType;
        public static Type decimalType;
        public static Type floatType;
        public static Type int64Type;
        public static Type dateTimeOffsetType;
        static TypeHelper()
        {
            int32Type = ((int)1).GetType();
            int64Type = ((long)1).GetType();
            doubleType = ((double)1).GetType();
            decimalType = ((decimal)1).GetType();
            floatType = ((float)1).GetType();
            dateTimeOffsetType = new DateTimeOffset().GetType();
        }
        public static ObjectComparison CompareBoxedObjects(object val1, object val2, Type t)
        {
            if (t == int32Type)
            {
                if ((int)val1 < (int)val2) return ObjectComparison.LESS;
                if ((int)val1 > (int)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            if (t == int64Type)
            {
                if ((long)val1 < (long)val2) return ObjectComparison.LESS;
                if ((long)val1 > (long)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            if (t == doubleType)
            {
                if ((double)val1 < (double)val2) return ObjectComparison.LESS;
                if ((double)val1 > (double)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            if (t == decimalType)
            {
                if ((decimal)val1 < (decimal)val2) return ObjectComparison.LESS;
                if ((decimal)val1 > (decimal)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            if (t == floatType)
            {
                if ((float)val1 < (float)val2) return ObjectComparison.LESS;
                if ((float)val1 > (float)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            if (t == dateTimeOffsetType)
            {
                if ((DateTimeOffset)val1 < (DateTimeOffset)val2) return ObjectComparison.LESS;
                if ((DateTimeOffset)val1 > (DateTimeOffset)val2) return ObjectComparison.GREATER;
                return ObjectComparison.EQUAL;
            }
            throw new NotImplementedException();
        }


    }
}
