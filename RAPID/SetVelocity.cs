using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    public class SetVelocity : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid
        {
            get { return new Guid("9ef915c7-893c-4d9d-84cc-4f4919392f97"); }
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.RAPID;
            }
        }

        public SetVelocity() : base("Velocity Override", "Velocity Override", "Override all following programmed velocities to a percentage of their value.", AxisInfo.Plugin, AxisInfo.TabCode)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Override %", "Override %", "Desired robot speed as a percentage of programmed speed.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Speed Limit", "Speed Limit", "Desired robot deceleration value. [As % of default values]", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Velocity Override", "Velocity Override", "RAPID-formatted speed acceleration override settings.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double velPct = 100;
            double maxSpeed = 900;

            if (!DA.GetData(0, ref velPct)) return;
            if (!DA.GetData(1, ref maxSpeed)) return;

            string strVelSet = "VelSet " + velPct.ToString() + ", " + maxSpeed.ToString() + ";";

            DA.SetData(0, strVelSet);
        }
    }
}