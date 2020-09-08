using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    /// <summary>
    /// Control the SoftMove option.
    /// </summary>
    public class SoftMove_Obsolete : GH_Component
    {
        public SoftMove_Obsolete() : base("SoftMove", "Soft", "Control the ABB SoftMove option.", AxisInfo.Plugin, AxisInfo.TabCode)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Control", "Control", "Turn the Cartesian Soft Servo option on or off.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Stiffness", "Stiffness", "Turn the Cartesian Soft Servo option on or off.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Cartesian Soft Servo command code.", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // string cssPlane = inPlane
            // string cssAct = "CSSAct ";
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
            get { return new Guid("{56593858-817a-42a0-aa34-3277a5f75c24}"); }
        }
        #endregion
    }
}