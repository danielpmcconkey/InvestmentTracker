using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes.Simulation
{
    public class SimulationFeatureToggles
    {
        public bool shouldLog { get; set; }
        public bool shouldWriteResultsToDB { get; set; }
        public bool shouldRunInParallel { get; set; }
       
        

    }
}
