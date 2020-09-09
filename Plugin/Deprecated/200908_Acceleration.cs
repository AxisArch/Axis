using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    /// <summary>
    /// Create custom RAPID code to control the acceleration.
    /// </summary>
    public class Acceleration_Obsolete : GH_Component
    {
        public Acceleration_Obsolete() : base("Acceleration", "Acc", "Override the robot acceleration and deceleration settings.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Acceleration", "Acc", "Desired robot acceleration value. [As % of default values]", GH_ParamAccess.item, 35);
            pManager.AddNumberParameter("Deceleration", "Dec", "Desired robot deceleration value. [As % of default values]", GH_ParamAccess.item, 60);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "RAPID-formatted robot acceleration override settings.", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double accVal = 35;
            double decVal = 60;

            if (!DA.GetData(0, ref accVal)) return;
            if (!DA.GetData(1, ref decVal)) return;

            string strAccSet = "AccSet " + accVal.ToString() + ", " + decVal.ToString() + ";";

            DA.SetData(0, strAccSet);
        }

        #region Component Settings
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.RAPID;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{06575491-6164-4b5d-82a8-a31d2a5ed75f}"); }
        }
        #endregion
    }
}