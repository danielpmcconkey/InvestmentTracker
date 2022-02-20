using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public class Text : IShape
    {
        public (double x, double y) position;
        public string fontFamily = "sans-serif";
        public double fontSize;
        public string fillColor;
        public string text;
        public string fontWeight = "normal";
        public string textAnchor = "start";
        public string MakeXML()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<text");
            sb.AppendLine(string.Format("style=\"font-style:normal; font-weight:{0}; font-size:{1}px; font-family:{2}; fill:#{3};stroke:none;\"",
                fontWeight, fontSize, fontFamily, fillColor));
            sb.AppendLine(string.Format("x=\"{0}px\"", position.x));
            sb.AppendLine(string.Format("y=\"{0}px\"", position.y));
            sb.AppendLine(string.Format("text-anchor=\"{0}\"", textAnchor));
            sb.AppendLine(string.Format(">{0}</text>", text));
            return sb.ToString();
        }
    }
}
