using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct Line : IShape
    {
        public double x1;
        public double x2;
        public double y1;
        public double y2;
        public double strokeWidthInPx;
        public string strokeColor;

        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<line");
            sb.AppendLine(string.Format("style=\"stroke-width:{0}px;stroke:#{1}\"",
                strokeWidthInPx, strokeColor));
            sb.AppendLine(string.Format("x1=\"{0}px\"", x1));
            sb.AppendLine(string.Format("y1=\"{0}px\"", y1));
            sb.AppendLine(string.Format("x2=\"{0}px\"", x2));
            sb.AppendLine(string.Format("y2=\"{0}px\"", y2));
            sb.AppendLine("/>");
            return sb.ToString();
        }
    }
}
