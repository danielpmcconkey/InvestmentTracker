using GraphLib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GraphLib
{
    public class PieChart : IShape
    {
        protected GraphPrefs _graphPrefs;
        
        protected string _xmlId;

        // chart measurements
        protected double _gridX;
        protected double _gridY;
        protected double _gridWidth;
        protected double _gridHeight;
        protected double _titleHeight;
        protected double _seriesLegendWidth;
        protected double _yLabelsWidth;
        protected double _xLabelsHeight;
        private GraphScale _xScale;
        private GraphScale _yScale;



        public PieChart(GraphPrefs graphPrefs, string xmlId)
        {
            //_graphPrefs = graphPrefs;
            
            //_xmlId = xmlId;

            //// measure things out for future placement

            //_seriesLegendWidth = (_lineChartPrefs.shouldPrintLegend) ? _graphPrefs.pictureWidthInPx * 0.25 : 0;
            //_titleHeight = _graphPrefs.pictureHeightInPx * 0.1;

            //_yLabelsWidth = _graphPrefs.labelsSizeInPx * 10;
            //_xLabelsHeight = _graphPrefs.labelsSizeInPx * 4;

            //_gridWidth = _graphPrefs.pictureWidthInPx
            //    - _seriesLegendWidth
            //    - _yLabelsWidth
            //    - graphPrefs.paddingInPx.left
            //    - graphPrefs.paddingInPx.right;

            //_gridHeight = _graphPrefs.pictureHeightInPx
            //    - _titleHeight
            //    - _xLabelsHeight
            //    - graphPrefs.paddingInPx.top
            //    - graphPrefs.paddingInPx.bottom;

            //_gridX = graphPrefs.paddingInPx.left + _yLabelsWidth;
            //_gridY = graphPrefs.paddingInPx.top + _titleHeight;




            //SetGraphScaling();

        }
        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            ////sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF - 8\" standalone =\"no\" ?>");
            ////sb.AppendLine("< !--Created with Dan's Graph Lib -->");
            //sb.AppendLine("<svg");
            //sb.AppendLine(string.Format("width=\"{0}px\"", _graphPrefs.pictureWidthInPx));
            //sb.AppendLine(string.Format("height=\"{0}px\"", _graphPrefs.pictureHeightInPx));
            //sb.AppendLine("xmlns=\"http://www.w3.org/2000/svg\"");
            //sb.AppendLine("xmlns:svg=\"http://www.w3.org/2000/svg\">");
            //sb.AppendLine(string.Format("<g id=\"{0}\">", _xmlId));

            //Rectangle window = new Rectangle()
            //{
            //    fillColor = _graphPrefs.pictureBagroundHexColor,
            //    heightInPx = _graphPrefs.pictureHeightInPx,
            //    widthInPx = _graphPrefs.pictureWidthInPx,
            //    strokeColor = _graphPrefs.pictureBagroundHexColor,
            //    strokeWidthInPx = 1,
            //    position = (0, 0),
            //};
            //sb.AppendLine(window.MakeXML());
            //Rectangle mainRect = new Rectangle()
            //{
            //    fillColor = _graphPrefs.graphFillHexColor,
            //    heightInPx = _graphPrefs.pictureHeightInPx - _graphPrefs.paddingInPx.top
            //        - _graphPrefs.paddingInPx.bottom,
            //    widthInPx = _graphPrefs.pictureWidthInPx - _graphPrefs.paddingInPx.left
            //        - _graphPrefs.paddingInPx.right,
            //    strokeColor = _graphPrefs.graphStrokeHexColor,
            //    strokeWidthInPx = _graphPrefs.graphBorderStrokeWidthInPx,
            //    position = (_graphPrefs.paddingInPx.left, _graphPrefs.paddingInPx.top),
            //};
            //sb.AppendLine(mainRect.MakeXML());
            //sb.AppendLine(MakeTitle());
            //sb.AppendLine(MakeGrid());
            //sb.AppendLine(MakeSeriesLines());
            //sb.AppendLine(MakeSeriesLegend());


            //sb.AppendLine(string.Format("</g><!-- end of group {0} -->", _xmlId));
            //sb.AppendLine("</svg>");

            return sb.ToString();
        }



        protected virtual string MakeSeriesLegend()
        {
            //if (!_lineChartPrefs.shouldPrintLegend) return string.Empty;
            StringBuilder sb = new StringBuilder();

            //double top = _gridY;
            //double height = _graphPrefs.paddingInPx.top
            //    + (_graphData.series.Count * _graphPrefs.labelsSizeInPx * 2)
            //    + _graphPrefs.paddingInPx.bottom;
            //double left = _gridX + _gridWidth + _graphPrefs.paddingInPx.left;
            //double width = _seriesLegendWidth - _graphPrefs.paddingInPx.left - _graphPrefs.paddingInPx.right;
            //double lineWidth = width * 0.2d;

            //Rectangle boundingBox = new Rectangle()
            //{
            //    fillColor = _lineChartPrefs.legendBGHexColor,
            //    heightInPx = height,
            //    widthInPx = width,
            //    strokeColor = _graphPrefs.graphStrokeHexColor,
            //    strokeWidthInPx = 1,
            //    position = (left, top),
            //};
            //sb.AppendLine(boundingBox.MakeXML());
            //for (int i = 0; i < _graphData.series.Count; i++)
            //{
            //    var series = _graphData.series[i];
            //    // draw the line
            //    Line line = new Line()
            //    {
            //        x1 = left + _graphPrefs.paddingInPx.left,
            //        y1 = top + (_graphPrefs.paddingInPx.top * 2) + (i * 2 * _graphPrefs.labelsSizeInPx),
            //        x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
            //        y2 = top + (_graphPrefs.paddingInPx.top * 2) + (i * 2 * _graphPrefs.labelsSizeInPx),
            //        strokeWidthInPx = series.seriesPrefs.strokeWidthInPx,
            //        strokeColor = series.seriesPrefs.strokeHexColor,
            //    };
            //    sb.AppendLine(line.MakeXML());
            //    // add the label
            //    Text xLabel = new Text()
            //    {
            //        text = series.name,
            //        fillColor = _graphPrefs.labelsHexColor,
            //        fontSize = _graphPrefs.labelsSizeInPx,
            //        position = (
            //            left + _graphPrefs.paddingInPx.left + lineWidth,
            //            top + (_graphPrefs.paddingInPx.top * 3) + (i * 2 * _graphPrefs.labelsSizeInPx)
            //            ),
            //        textAnchor = "left",
            //    };
            //    sb.AppendLine(xLabel.MakeXML());

            //}

            return sb.ToString();
        }
        
        private string MakeTitle()
        {
            Text title = new Text()
            {
                text = _graphPrefs.title,
                fillColor = _graphPrefs.titleHexColor,
                fontSize = _graphPrefs.labelsSizeInPx * 2.5,
                position = (_graphPrefs.pictureWidthInPx / 2d,
                        _titleHeight),
                textAnchor = "middle",
            };
            return title.MakeXML();
        }
        private void SetGraphScaling()
        {
            //// start with reasonable values
            //var minXValue = _graphData.series.SelectMany(s => s.data).Min(d => d.x);
            //var maxXValue = _graphData.series.SelectMany(s => s.data).Max(d => d.x);
            //var minYValue = _graphData.series.SelectMany(s => s.data).Min(d => d.y);
            //var maxYValue = _graphData.series.SelectMany(s => s.data).Max(d => d.y);

            //if (_lineChartPrefs.maxX != null) maxXValue = _lineChartPrefs.maxX;
            //if (_lineChartPrefs.maxY != null) maxYValue = _lineChartPrefs.maxY;

            //// use those reasonable values to calculate actual scale
            //_xScale = GraphScaler.GetAxisScale(minXValue, maxXValue, _graphData.xType);
            //_yScale = GraphScaler.GetAxisScale(minYValue, maxYValue, _graphData.yType);
        }

    }
}
