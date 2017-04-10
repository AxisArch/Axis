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
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "A toolkit for architectural robotic experimentation.";
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
                return "ryan.hughes.arch@gmail.com";
            }
        }
    }
}
