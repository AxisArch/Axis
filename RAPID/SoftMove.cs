using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    public class SoftMove : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRapid;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{56593858-817a-42a0-aa34-3277a5f75c24}"); }
        }

        public SoftMove() : base("SoftMove", "SoftMove", "Control the ABB SoftMove option.", "Axis", "RAPID")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Control", "Control", "Turn the Cartesian Soft Servo option on or off.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Stiffness", "Stiffness", "Turn the Cartesian Soft Servo option on or off.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Command", "Cartesian Soft Servo command code.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // string cssPlane = inPlane
            // string cssAct = "CSSAct ";
        }
    }
}