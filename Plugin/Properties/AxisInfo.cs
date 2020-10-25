using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Axis
{
    public class AxisInfo : GH_AssemblyInfo
    {
        public override string Name => "Axis";
        public override Bitmap Icon => Axis.Properties.Icons.Robot;
        public override string Description => "A toolkit for industrial robot programming and simulation.";
        public override Guid Id => new Guid("011021d1-f033-4815-9ada-a12ad574343a");

        public override string AuthorName => "Ryan Hughes, Povl Filip Sonne-Frederiksen";
        public override string AuthorContact => "https://github.com/AxisArch/Axis/";

        public static string Plugin { get { return "Axis"; } }
        public static string TabParam { get { return "0. Params"; } } //<--- Should not be visible
        public static string TabMain { get { return "1. Main"; } }
        public static string TabLive { get { return "2. Connection"; } }
        public static string TabConfiguration { get { return "3. Setup"; } }
        public static string TabDepricated { get { return "4. Depriacted"; } } //<--- Should not be visible
    }
}