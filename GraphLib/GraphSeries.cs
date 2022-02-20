using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct GraphSeries
    {
        public string name;
        public List<(object x, object y)> data;
        public Type xType;
        public Type yType;
        public SeriesPrefs seriesPrefs;
        public  GraphSeries(string name, Type xType, Type yType)
        {
            this.name = name;
            this.xType = xType;
            this.yType = yType;
            data = new List<(object x, object y)>();

            // default series prefs
            seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "FFFFFF",
                strokeWidthInPx = 1
            };
        }
        public void AddRow(object x, object y)
        {
            if (x.GetType() != xType)
            {
                throw new ArgumentException("x.Type must match xType");
            }
            if (y.GetType() != yType)
            {
                throw new ArgumentException("y.Type must match yType");
            }
            data.Add((x, y));
        }
    }
}
