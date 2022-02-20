using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct LineChartPrefs
    {
        public string gridFillHexColor; // html color, exclude the #
        public string gridStrokeHexColor; // html color, exclude the #
        public double gridBorderStrokeWidthInPx;
        public string legendBGHexColor;

        //public int gridDivisionsX;
        //public int gridDivisionsY;
        public string gridLineStrokeHexColor;
        public double gridLineStrokeWidthInPx;

        public string xColumnLabelsTextFormat = "default";
        public string yColumnLabelsTextFormat = "default";

        public double resolution = 7d; // minimum pixels between X values 

        public bool shouldPrintLegend = true;

        public object maxX = null;
        public object maxY = null;
    }
}
