using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphLib
{
    public struct GraphPrefs
    {
        // main window
        public double pictureWidthInPx;
        public double pictureHeightInPx;
        public string pictureBagroundHexColor; // html color, exclude the #
        public (double top, double right, double bottom, double left) paddingInPx;

        // graph area
        public string graphFillHexColor; // html color, exclude the #
        public string graphStrokeHexColor; // html color, exclude the #
        public double graphBorderStrokeWidthInPx;

        // text
        public string labelsHexColor;
        public double labelsSizeInPx;
        public string title;
        public string titleHexColor;

    }
}
