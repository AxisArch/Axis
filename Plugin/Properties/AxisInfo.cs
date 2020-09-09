using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Axis
{
    public class AxisInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Axis";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Axis.Properties.Icons.Robot;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "A toolkit for industrial robot programming and simulation.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("011021d1-f033-4815-9ada-a12ad574343a");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Ryan Hughes";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "rhu@axisarch.tech";
            }
        }

        public static string Plugin { get { return "Axis"; } }
        public static string TabParam { get { return "0. Params"; } } //<--- Should not be visible
        public static string TabMain { get { return "1. Main"; } }
        public static string TabLive { get { return "2. Connection"; } }
        public static string TabConfiguration { get { return "3. Setup"; } }
        public static string TabDepricated { get { return "4. Depriacted"; } } //<--- Should not be visible
    }
}
