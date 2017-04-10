using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    public class Acceleration : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRapid;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{06575491-6164-4b5d-82a8-a31d2a5ed75f}"); }
        }

        public Acceleration() : base("Acceleration", "Acceleration", "Override the robot acceleration and deceleration settings.", "Axis", "RAPID")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Acceleration", "Acceleration", "Desired robot acceleration value. [As % of default values]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Deceleration", "Deceleration", "Desired robot deceleration value. [As % of default values]", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Acceleration Settings", "Settings", "RAPID-formatted robot acceleration override settings.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double accVal = 35;
            double decVal = 60;

            if (!DA.GetData(0, ref accVal)) return;
            if (!DA.GetData(1, ref decVal)) return;

            string strAccSet = "AccSet " + accVal.ToString() + ", " + decVal.ToString() + ";";

            DA.SetData(0, strAccSet);
        }
    }
}