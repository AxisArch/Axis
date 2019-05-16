using System;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using Grasshopper;

using Rhino.Geometry;
using Axis.Targets;
using Axis.Core;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;

namespace Axis.Robot
{
    public class DeconstructRobot : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("6ab28f95-e64b-4fe7-8c3b-95d7e7573bc7"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public DeconstructRobot() : base("Get Robot Base", "Base", "Get the base plane of a robot system.", "Axis", "2. Robot")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Base", "Base", "Robot base frame location.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Manipulator robot = new Manipulator();

            if (!DA.GetData(0, ref robot)) return;

            Plane basePlane = robot.RobBasePlane;

            DA.SetData(0, basePlane);
        }
    }
}