using GraphLib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GraphLib
{
    public class LineChart : IShape
    {
        protected GraphPrefs _graphPrefs;
        protected LineChartPrefs _lineChartPrefs;
        protected GraphData _graphData;
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
        

        
        public LineChart(GraphPrefs graphPrefs, LineChartPrefs lineChartPrefs, GraphData graphData, string xmlId)
        {
            _graphPrefs = graphPrefs;
            _lineChartPrefs = lineChartPrefs;
            _graphData = graphData;
            _xmlId = xmlId;

            // measure things out for future placement

            _seriesLegendWidth = (_lineChartPrefs.shouldPrintLegend) ? _graphPrefs.pictureWidthInPx * 0.25 : 0;
            _titleHeight = _graphPrefs.pictureHeightInPx * 0.1;

            _yLabelsWidth = _graphPrefs.labelsSizeInPx * 10;
            _xLabelsHeight = _graphPrefs.labelsSizeInPx * 4;

            _gridWidth = _graphPrefs.pictureWidthInPx
                - _seriesLegendWidth
                - _yLabelsWidth
                - graphPrefs.paddingInPx.left
                - graphPrefs.paddingInPx.right;

            _gridHeight = _graphPrefs.pictureHeightInPx
                - _titleHeight
                - _xLabelsHeight
                - graphPrefs.paddingInPx.top
                - graphPrefs.paddingInPx.bottom;

            _gridX = graphPrefs.paddingInPx.left + _yLabelsWidth;
            _gridY = graphPrefs.paddingInPx.top + _titleHeight;




            SetGraphScaling();

        }
        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF - 8\" standalone =\"no\" ?>");
            //sb.AppendLine("< !--Created with Dan's Graph Lib -->");
            sb.AppendLine("<svg");
            sb.AppendLine(string.Format("width=\"{0}px\"", _graphPrefs.pictureWidthInPx));
            sb.AppendLine(string.Format("height=\"{0}px\"", _graphPrefs.pictureHeightInPx));
            sb.AppendLine("xmlns=\"http://www.w3.org/2000/svg\"");
            sb.AppendLine("xmlns:svg=\"http://www.w3.org/2000/svg\">");
            sb.AppendLine(string.Format("<g id=\"{0}\">", _xmlId));

            Rectangle window = new Rectangle()
            {
                fillColor = _graphPrefs.pictureBagroundHexColor,
                heightInPx = _graphPrefs.pictureHeightInPx,
                widthInPx = _graphPrefs.pictureWidthInPx,
                strokeColor = _graphPrefs.pictureBagroundHexColor,
                strokeWidthInPx = 1,
                position = (0, 0),
            };
            sb.AppendLine(window.MakeXML());
            Rectangle mainRect = new Rectangle()
            {
                fillColor = _graphPrefs.graphFillHexColor,
                heightInPx = _graphPrefs.pictureHeightInPx - _graphPrefs.paddingInPx.top
                    - _graphPrefs.paddingInPx.bottom,
                widthInPx = _graphPrefs.pictureWidthInPx - _graphPrefs.paddingInPx.left
                    - _graphPrefs.paddingInPx.right,
                strokeColor = _graphPrefs.graphStrokeHexColor,
                strokeWidthInPx = _graphPrefs.graphBorderStrokeWidthInPx,
                position = (_graphPrefs.paddingInPx.left, _graphPrefs.paddingInPx.top),
            };
            sb.AppendLine(mainRect.MakeXML());
            sb.AppendLine(MakeTitle());
            sb.AppendLine(MakeGrid());
            sb.AppendLine(MakeSeriesLines());
            sb.AppendLine(MakeSeriesLegend());


            sb.AppendLine(string.Format("</g><!-- end of group {0} -->", _xmlId));
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
        

        
        private (double x, double y) GetCoordinate(object x, object y)
        {
            // this is the opposite of GetValuesAtPoint

            // what percentage between x min and x max are we
            double percentX = GetPercentDistance(x, _xScale.min, _xScale.max, _graphData.xType);
            // what is the int value that matches
            double xVal = _gridX + (_gridWidth * percentX);

            // what percentage between y min and y max are we
            double percentY = GetPercentDistance(y, _yScale.min, _yScale.max, _graphData.yType);
            // what is the int value that matches
            // y goes "down" as the number gets larger, so must invert
            double yVal = (_gridY + _gridHeight) - (_gridHeight * percentY);

            return(xVal, yVal);
        }
        private double GetPercentDistance(object actual, object min, object max, Type type)
        {
            // this is the opposite of GetValueAtPercentDistance

            double actualDouble = 0d;
            double minDouble = 0d;
            double maxDouble = 0d;

            if (type == TypeHelper.int32Type)
            {
                actualDouble = (double)((int)(actual));
                minDouble = (double)((int)(min));
                maxDouble = (double)((int)(max));
            }
            else if (type == TypeHelper.int64Type)
            {
                actualDouble = (double)((long)(actual));
                minDouble = (double)((long)(min));
                maxDouble = (double)((long)(max));
            }
            else if (type == TypeHelper.floatType)
            {
                actualDouble = (double)((float)(actual));
                minDouble = (double)((float)(min));
                maxDouble = (double)((float)(max));
            }
            else if (type == TypeHelper.doubleType)
            {
                actualDouble = (double)actual;
                minDouble = (double)min;
                maxDouble = (double)max;
            }
            else if (type == TypeHelper.decimalType)
            {
                actualDouble = (double)((decimal)(actual));
                minDouble = (double)((decimal)(min));
                maxDouble = (double)((decimal)(max));
            }
            else if (type == TypeHelper.dateTimeOffsetType)
            {
                // cast all dates to unix seconds
                actualDouble = (double)((DateTimeOffset)actual).ToUnixTimeSeconds();
                minDouble = (double)((DateTimeOffset)min).ToUnixTimeSeconds();
                maxDouble = (double)((DateTimeOffset)max).ToUnixTimeSeconds();

                
            }
            // anything else not yet implemented
            else throw new NotImplementedException();

            if(actualDouble > maxDouble) actualDouble = maxDouble;
            return (actualDouble - minDouble) / (maxDouble - minDouble);
        }
        private object GetValueAtPercentDistance(double percentDistance, object min, object max, Type type)
        {
            // this is the opposite of GetPercentDistance

            // percentDistance = (actual - min) / (max - min);
            // percentDistance * (max - min) = actual - min;
            // (percentDistance * (max - min)) + min = actual;
            // actual = (percentDistance * (max - min)) + min;

            if (type == TypeHelper.int32Type)
            {
                int minInt = (int)min;
                int maxInt = (int)max;
                return (int)(Math.Round((percentDistance * (maxInt - minInt)) + minInt, 0));
            }
            else if (type == TypeHelper.int64Type)
            {
                long minInt = (long)min;
                long maxInt = (long)max;
                return (long)(Math.Round((percentDistance * (maxInt - minInt)) + minInt, 0));
            }
            else if (type == TypeHelper.floatType)
            {
                float minCast = (float)min;
                float maxCast = (float)max;
                return (float)((percentDistance * (maxCast - minCast)) + minCast);
            }
            else if (type == TypeHelper.doubleType)
            {
                double minCast = (double)min;
                double maxCast = (double)max;
                return (double)((percentDistance * (maxCast - minCast)) + minCast);
            }
            else if (type == TypeHelper.decimalType)
            {
                decimal minCast = (decimal)min;
                decimal maxCast = (decimal)max;
                return (decimal)(((decimal)percentDistance * (maxCast - minCast)) + minCast);
            }
            else if (type == TypeHelper.dateTimeOffsetType)
            {
                // cast all dates to unix seconds

                long minCast = ((DateTimeOffset)min).ToUnixTimeSeconds();
                long maxCast = ((DateTimeOffset)max).ToUnixTimeSeconds();
                long actualLong = (int)(Math.Round((percentDistance * (maxCast - minCast)) + minCast, 0));
                return DateTimeOffset.FromUnixTimeSeconds(actualLong);

            }
            // anything else not yet implemented
            else throw new NotImplementedException();

        }
        private (object x, object y) GetValuesAtPoint(double x, double y)
        {
            // this is the opposite of GetCoordinate

            // what percentage between x min and x max are we
            // actual / (max - min)
            double percentX = (x - _gridX) / ((_gridX + _gridWidth) - _gridX);
            object xVal = GetValueAtPercentDistance(percentX, _xScale.min, _xScale.max, _graphData.xType);

            // what percentage between y min and y max are we
            double percentYOnAxis = (y - _gridY) / ((_gridY + _gridHeight) - _gridY);
            // y goes "down" as the number gets larger, so must invert
            double percentY = 1 - percentYOnAxis;
            object yVal = GetValueAtPercentDistance(percentY, _yScale.min, _yScale.max, _graphData.yType);

            return (xVal, yVal);
        }
        private string MakeGrid()
        {
            StringBuilder sb = new StringBuilder();
            string gridId = Guid.NewGuid().ToString();
            sb.AppendLine(string.Format("<g id=\"{0}\"", gridId));
            sb.AppendLine(string.Format("x=\"{0}px\"", _gridX));
            sb.AppendLine(string.Format("y=\"{0}px\"", _gridY));
            sb.AppendLine(">");
            Rectangle gridRect = new Rectangle()
            {
                fillColor = _lineChartPrefs.gridFillHexColor,
                heightInPx = _gridHeight,
                widthInPx = _gridWidth,
                strokeColor = _lineChartPrefs.gridStrokeHexColor,
                strokeWidthInPx = _lineChartPrefs.gridBorderStrokeWidthInPx,
                position = (_gridX, _gridY),
            };
            sb.AppendLine(gridRect.MakeXML());

            // make the x grid divisions
            double divisionWidth = _gridWidth / (double)_xScale.numDivisions;
            for(int i = 0; i <= _xScale.numDivisions; ++i)
            {
                double divisionLineX = _gridX + (divisionWidth * i);
                Line line = new Line()
                {
                    x1 = divisionLineX,
                    x2 = divisionLineX,
                    y1 = _gridY,
                    y2 = _gridY + _gridHeight,
                    strokeWidthInPx = _lineChartPrefs.gridLineStrokeWidthInPx,
                    strokeColor = _lineChartPrefs.gridLineStrokeHexColor,
                };
                sb.AppendLine(line.MakeXML());
                object xLabelVal = GetValuesAtPoint(divisionLineX, _gridY).x;
                if(_graphData.xType == TypeHelper.dateTimeOffsetType)
                {
                    // need to do special datetime rounding
                    xLabelVal = _xScale.dateRoundingFuction((DateTimeOffset)xLabelVal, RoundDateDirection.CLOSEST);
                }
                double xLabelYPosition = _gridY + _gridHeight + (_graphPrefs.labelsSizeInPx * 1.5);
                if (i % 2 == 0) xLabelYPosition += _graphPrefs.labelsSizeInPx * 1.25;
                Text xLabel = new Text()
                {
                    text = TextHelper.FormatGraphLabel(xLabelVal, _graphData.xType, _lineChartPrefs.xColumnLabelsTextFormat),
                    fillColor = _graphPrefs.labelsHexColor,
                    fontSize = _graphPrefs.labelsSizeInPx,
                    position = (divisionLineX, xLabelYPosition),
                    textAnchor = "middle",
                };
                sb.AppendLine(xLabel.MakeXML());
            }
            // make y grid divisions
            double divisionHeight = _gridHeight / (double)_yScale.numDivisions;
            for (int i = 0; i <= _yScale.numDivisions; ++i)
            {
                double divisionLineY = _gridY + (divisionHeight * i);
                Line line = new Line()
                {
                    y1 = divisionLineY,
                    y2 = divisionLineY,
                    x1 = _gridX,
                    x2 = _gridX + _gridWidth,
                    strokeWidthInPx = _lineChartPrefs.gridLineStrokeWidthInPx,
                    strokeColor = _lineChartPrefs.gridLineStrokeHexColor,
                };
                sb.AppendLine(line.MakeXML());
                object yLabelVal = GetValuesAtPoint(_gridX, divisionLineY).y;
                Text yLabel = new Text()
                {
                    text = TextHelper.FormatGraphLabel(yLabelVal, _graphData.yType, _lineChartPrefs.yColumnLabelsTextFormat),
                    fillColor = _graphPrefs.labelsHexColor, 
                    fontSize = _graphPrefs.labelsSizeInPx, 
                    position = (_gridX - _graphPrefs.labelsSizeInPx, 
                        divisionLineY + (_graphPrefs.labelsSizeInPx * 0.5)),    // move it down half way to have the middle align to the grid line
                    textAnchor = "end",
                };
                sb.AppendLine(yLabel.MakeXML());
            }

            sb.AppendLine(string.Format("</g><!-- end of grid {0} -->", gridId));
            return sb.ToString();
        }
        protected virtual string MakeSeriesLegend()
        {
            if(!_lineChartPrefs.shouldPrintLegend) return string.Empty;

            double top = _gridY;
            double height = _graphPrefs.paddingInPx.top
                + (_graphData.series.Count * _graphPrefs.labelsSizeInPx * 2)
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
            for(int i = 0; i < _graphData.series.Count; i++)
            {
                var series = _graphData.series[i];
                // draw the line
                Line line = new Line()
                {
                    x1 = left + _graphPrefs.paddingInPx.left,
                    y1 = top + (_graphPrefs.paddingInPx.top * 2) + (i * 2 * _graphPrefs.labelsSizeInPx),
                    x2 = left + _graphPrefs.paddingInPx.left + lineWidth - 5,
                    y2 = top + (_graphPrefs.paddingInPx.top * 2) + (i * 2 * _graphPrefs.labelsSizeInPx),
                    strokeWidthInPx = series.seriesPrefs.strokeWidthInPx,
                    strokeColor = series.seriesPrefs.strokeHexColor,
                };
                sb.AppendLine(line.MakeXML());
                // add the label
                Text xLabel = new Text()
                {
                    text = series.name,
                    fillColor = _graphPrefs.labelsHexColor,
                    fontSize = _graphPrefs.labelsSizeInPx,
                    position = (
                        left + _graphPrefs.paddingInPx.left + lineWidth, 
                        top + (_graphPrefs.paddingInPx.top * 3) + (i * 2 * _graphPrefs.labelsSizeInPx)
                        ),
                    textAnchor = "left",
                };
                sb.AppendLine(xLabel.MakeXML());

            }

            return sb.ToString();
        }
        private string MakeSeriesLines()
        {
            StringBuilder sb = new StringBuilder();
            string seriesId = Guid.NewGuid().ToString();
            sb.AppendLine(string.Format("<g id=\"{0}\"", seriesId));
            sb.AppendLine(string.Format("x=\"{0}px\"", _gridX));
            sb.AppendLine(string.Format("y=\"{0}px\"", _gridY));
            sb.AppendLine(">");

            foreach (GraphSeries series in _graphData.series)
            {
                // we don't want more than 1 point every [resolution] px
                // so we need to thin the list a bit
                int maxElements = (int) Math.Round(_gridWidth / _lineChartPrefs.resolution, 0);
                List<(object x,object y)> thinnedList = new List<(object,object)> ();

                // if we already have a small list, keep it
                if (series.data.Count <= maxElements) thinnedList.AddRange(series.data);

                // if not create a thinned list
                else
                {
                    int moduloValue = (int)Math.Round(series.data.Count / (double)maxElements, 0);
                    for (int i = 0; i < series.data.Count; ++i)
                    {
                        bool shouldAdd = false;
                        if (i == 0) shouldAdd = true;    // always add the first, no matter what
                        if (i == series.data.Count - 1) shouldAdd = true;    // always add the last, no matter what
                        if (i % moduloValue == 0) shouldAdd = true;

                        if (shouldAdd)
                        {
                            thinnedList.Add(series.data[i]);
                        }
                    }
                }

                
                PolyLine line = new PolyLine()
                {
                    strokeWidthInPx = series.seriesPrefs.strokeWidthInPx,
                    strokeColor = series.seriesPrefs.strokeHexColor,
                    points = new List<(double x, double y)>(),
                };
                for (int i = 0; i < thinnedList.Count; ++i)
                {
                    line.points.Add(GetCoordinate(thinnedList[i].x, thinnedList[i].y));
                }
                sb.AppendLine(line.MakeXML());

            }

            sb.AppendLine(string.Format("</g><!-- end of graph series list {0} -->", seriesId));
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
            // start with reasonable values
            var minXValue = _graphData.series.SelectMany(s => s.data).Min(d => d.x);
            var maxXValue = _graphData.series.SelectMany(s => s.data).Max(d => d.x);
            var minYValue = _graphData.series.SelectMany(s => s.data).Min(d => d.y);
            var maxYValue = _graphData.series.SelectMany(s => s.data).Max(d => d.y);

            if (_lineChartPrefs.maxX != null) maxXValue = _lineChartPrefs.maxX;
            if (_lineChartPrefs.maxY != null) maxYValue = _lineChartPrefs.maxY;

            // use those reasonable values to calculate actual scale
            _xScale = GraphScaler.GetAxisScale(minXValue, maxXValue, _graphData.xType);
            _yScale = GraphScaler.GetAxisScale(minYValue, maxYValue, _graphData.yType);
        }
        
    }
}
