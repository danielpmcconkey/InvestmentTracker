using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphLib;
using GraphLib.Utilities;

namespace Lib.Engine.MonteCarlo
{
    public class MonteCarloLineChart : LineChart
    {
        public MonteCarloLineChart(GraphPrefs graphPrefs, LineChartPrefs lineChartPrefs, 
            GraphData graphData, string xmlId) : base(graphPrefs, lineChartPrefs, graphData, xmlId)
        {
            
        }
        protected override string MakeSeriesLegend()
        {
            if (!_lineChartPrefs.shouldPrintLegend) return string.Empty;

            double top = _gridY;
            double height = _graphPrefs.paddingInPx.top
                + (5 * _graphPrefs.labelsSizeInPx * 2)
                + _graphPrefs.paddingInPx.bottom;
            double left = _gridX + _gridWidth + _graphPrefs.paddingInPx.left;
            double width = _seriesLegendWidth - _graphPrefs.paddingInPx.left - _graphPrefs.paddingInPx.right;
            double lineWidth = width * 0.2d;

            Rectangle boundingBox = new Rectangle()
            {
                fillColor = _lineChartPrefs.legendBGHexColor,
                heightInPx = height,
                widthInPx = width,
                strokeColor = _graphPrefs.graphStrokeHexColor,
                strokeWidthInPx = 1,
                position = (left, top),
            };
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(boundingBox.MakeXML());


            // fill in the legend values
            // start with the runs
            int position = 0;   // used to replace i
            Line line = new Line()
            {
                x1 = left + _graphPrefs.paddingInPx.left,
                y1 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
                y2 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                strokeWidthInPx = 1,
                strokeColor = ColorHelper.steelgrey,
            };
            sb.AppendLine(line.MakeXML());
            // add the label
            Text xLabel = new Text()
            {
                text = String.Format("Simulation runs (X{0})",_graphData.series.Count - 3),
                fillColor = _graphPrefs.labelsHexColor,
                fontSize = _graphPrefs.labelsSizeInPx,
                position = (
                    left + _graphPrefs.paddingInPx.left + lineWidth,
                    top + (_graphPrefs.paddingInPx.top * 3) + (position * 2 * _graphPrefs.labelsSizeInPx)
                    ),
                textAnchor = "left",
            };
            sb.AppendLine(xLabel.MakeXML());


            // now do the median
            position = 1;   // used to replace i
            Line line2 = new Line()
            {
                x1 = left + _graphPrefs.paddingInPx.left,
                y1 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
                y2 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                strokeWidthInPx = 3,
                strokeColor = ColorHelper.amber,
            };
            sb.AppendLine(line2.MakeXML());
            // add the label
            Text xLabel2 = new Text()
            {
                text = "Median",
                fillColor = _graphPrefs.labelsHexColor,
                fontSize = _graphPrefs.labelsSizeInPx,
                position = (
                    left + _graphPrefs.paddingInPx.left + lineWidth,
                    top + (_graphPrefs.paddingInPx.top * 3) + (position * 2 * _graphPrefs.labelsSizeInPx)
                    ),
                textAnchor = "left",
            };
            sb.AppendLine(xLabel2.MakeXML());

            // now do the 90th %ile min
            position = 2;   // used to replace i
            Line line3 = new Line()
            {
                x1 = left + _graphPrefs.paddingInPx.left,
                y1 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
                y2 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                strokeWidthInPx = 3,
                strokeColor = ColorHelper.deeporange,
            };
            sb.AppendLine(line3.MakeXML());
            // add the label
            Text xLabel3 = new Text()
            {
                text = "90th percentile min",
                fillColor = _graphPrefs.labelsHexColor,
                fontSize = _graphPrefs.labelsSizeInPx,
                position = (
                    left + _graphPrefs.paddingInPx.left + lineWidth,
                    top + (_graphPrefs.paddingInPx.top * 3) + (position * 2 * _graphPrefs.labelsSizeInPx)
                    ),
                textAnchor = "left",
            };
            sb.AppendLine(xLabel3.MakeXML());

            // now do the 90th %ile max
            position = 3;   // used to replace i
            Line line4 = new Line()
            {
                x1 = left + _graphPrefs.paddingInPx.left,
                y1 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
                y2 = top + (_graphPrefs.paddingInPx.top * 2) + (position * 2 * _graphPrefs.labelsSizeInPx),
                strokeWidthInPx = 3,
                strokeColor = ColorHelper.indigo,
            };
            sb.AppendLine(line4.MakeXML());
            // add the label
            Text xLabel4 = new Text()
            {
                text = "90th percentile max",
                fillColor = _graphPrefs.labelsHexColor,
                fontSize = _graphPrefs.labelsSizeInPx,
                position = (
                    left + _graphPrefs.paddingInPx.left + lineWidth,
                    top + (_graphPrefs.paddingInPx.top * 3) + (position * 2 * _graphPrefs.labelsSizeInPx)
                    ),
                textAnchor = "left",
            };
            sb.AppendLine(xLabel4.MakeXML());


            return sb.ToString();
        }
    }
}
