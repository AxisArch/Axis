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
                return Axis.Properties.Resources.Robot;
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
        public static string TabCore { get { return "1. Core"; } }
        public static string TabRobot { get { return "2. Robot"; } }
        public static string TabTargets { get { return "3. Targets"; } }
        public static string TabToolpath { get { return "4. Toolpath"; } }
        public static string TabGeometry { get { return "5. Geometry"; } }
        public static string TabIO { get { return "6. I/O"; } }
        public static string TabCode { get { return "7. RAPID"; } }
        public static string TabOnline { get { return "8. Online"; } }

    }
}
