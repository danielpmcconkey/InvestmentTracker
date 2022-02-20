using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib.Utilities
{
    public static class ColorHelper
    {
        public static string red = "ff4b39";
        public static string indigo = "3b5ab9";
        public static string green = "2cb45d";
        public static string orange = "ffa01a";
        public static string pink = "f799b6";
        public static string blue = "009ff1";
        public static string mint = "3ec593";
        public static string deeporange = "ff6024";
        public static string steelgrey = "3a464c";
        public static string lightblue = "b4d7e4";
        public static string lime = "88ef08";
        public static string brown = "875d4f";
        public static string purple = "ae30b4";
        public static string cyan = "00c1d7";
        public static string yellow = "ffeb4f";
        public static string grey = "a6a5a6";
        public static string deeppurple = "7343ba";
        public static string teal = "7343ba";
        public static string amber = "ffc525";
        public static string bluegrey = "618593";

        private static string[] _colorOrder;

        static ColorHelper()
        {
            _colorOrder = new string[] {red, indigo, green, orange, pink, blue, mint, deeporange, steelgrey,
                lightblue, lime, brown, purple, cyan, yellow, grey, deeppurple, teal, amber, bluegrey };
        }
        public static string GetColor(int ordinal)
        {
            return _colorOrder[ordinal % _colorOrder.Length];
        }
    }
}
