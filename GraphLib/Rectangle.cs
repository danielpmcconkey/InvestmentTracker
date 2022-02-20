using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{

    public struct Rectangle : IShape
    {
        public double widthInPx;
        public double heightInPx;
        public string fillColor;
        public double strokeWidthInPx;
        public string strokeColor;
        public (double x, double y) position;

        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<rect");
            sb.AppendLine(string.Format("style=\"fill:#{0};stroke-width:{1}px;stroke:#{2}\"",
                fillColor, strokeWidthInPx, strokeColor));
            sb.AppendLine(string.Format("width=\"{0}px\"", widthInPx));
            sb.AppendLine(string.Format("height=\"{0}px\"", heightInPx));
            sb.AppendLine(string.Format("x=\"{0}px\"", position.x));
            sb.AppendLine(string.Format("y=\"{0}px\"", position.y));
            sb.AppendLine("/>");
            return sb.ToString();
        }
    }
}
