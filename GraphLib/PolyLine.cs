using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct PolyLine : IShape
    {
        public List<(double x, double y)> points;
        public double strokeWidthInPx;
        public string strokeColor;
        public bool shouldFill = false;
        public string fillColor;  // ignored if shouldFill = false
        public double strokeOpacity = 1;

        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<polyline");
            sb.Append(string.Format("style=\"stroke-width:{0}px;stroke:#{1};",
                strokeWidthInPx, strokeColor));
            sb.Append(string.Format("fill:{0}", (shouldFill) ? "#" + fillColor: "none"));
            sb.Append("\"" + Environment.NewLine);
            sb.AppendLine(string.Format("stroke-opacity=\"{0}\"", strokeOpacity.ToString("0.0")));
            sb.Append("points=\"");
            for(int i = 0; i < points.Count; i++)
            {
                string space = (i == points.Count - 1) ? "" : " ";
                sb.Append(string.Format("{0},{1}{2}", points[i].x, points[i].y, space));
            }
            sb.Append("\"" + Environment.NewLine);
            sb.AppendLine("/>");
            return sb.ToString();
        }
    }
}
