using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class FEATURETOGGLE
    {
        /* miscellaneous features */
        public static bool NO_WRITE = false; // should be false in a normal run
        public static bool MULTITHREAD = true;
        public static bool SHOULDDEBUGSIMLOGIC = false;


        public static bool SHOULDREADNEWTRANSACTIONFILES = true;

        /// <summary>
        /// check if we have prices from first needed transaction date to present and, if not, scrape them from Yahoo
        /// </summary>
        public static bool SHOULDCATCHUPPRICINGDATA = true;

        /// <summary>
        /// whether to fill in the gaps between prices with trend data. Yahoo only pulls monthly prices. This will fill in daily prices based on the slope between months
        /// </summary>
        public static bool SHOULDBLENDPRICINGDATA = true;

        /// <summary>
        /// whether to print a graph of net worth to the logs
        /// </summary>
        public static bool SHOULDPRINTNETWORTH = true;

        public static bool SHOULDRUNMONTECARLO = true;
        public static bool SHOULDRUNMONTECARLOBATCHES = true;

        /* command line args for default
         * shouldreadinitalcsvdata:false shouldwritejsondata:true shouldreadjsonaccountdata:true shouldreadjsonpricingdata:true shouldcatchuppricingdata:true shouldblendpricingdataa:true
         * */

    }
}
