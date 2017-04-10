using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Tools;

namespace Axis.Targets
{
    public class HomePose : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconHome;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{a85de75e-48fe-4bd5-93da-0d0e11998c74}"); }
        }

        public HomePose() : base("Home Pose", "Home", "Create an instruction to go to the home pose.", "Axis", "Targets")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Speed", "Speed", "Desired robot speed [mm/s].", GH_ParamAccess.item, 300);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Pose", "Pose", "Home pose as MoveAbsJ.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double speed = 300;
            if (!DA.GetData(0, ref speed)) return;

            var strSpeed = "v" + speed.ToString();
            var strHome = String.Join(
                Environment.NewLine,
                "! Home Position",
                "MoveAbsJ" + "[[0, 0, 0, 0, 15, 0],extAxis]" + "," + strSpeed + "," + "fine" + "," + "tool0" + ";");

            DA.SetData(0, strHome);
        }
    }
}