using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct GraphData
    {
        public List<GraphSeries> series;
        public Type xType;
        public Type yType;

        public GraphData(Type xType, Type yType)
        {
            this.xType = xType;
            this.yType = yType;
            series = new List<GraphSeries>();
        }
        public void AddSeries(GraphSeries addSeries)
        {
            if (addSeries.xType != xType)
            {
                throw new ArgumentException("Series xType must match GraphData xType");
            }
            if (addSeries.yType != yType)
            {
                throw new ArgumentException("Series yType must match GraphData yType");
            }
            series.Add(addSeries);
        }
    }
}
