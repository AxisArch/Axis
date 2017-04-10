using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.IO
{
    public class SetDO : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconSwitch;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{2230b30e-6aeb-4f55-a54a-b1ec7d8eb410}"); }
        }

        public SetDO() : base("Set DO", "Set DO", "Set the value of a digital output.", "Axis", "IO")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Output", "Output", "Name or number of the digital output to set.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Status", "Status", "Status of the signal to set. (1 = On, 0 = Off)", GH_ParamAccess.item, 0);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Command", "Rapid formatted SetDO command for signal control.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int outputNumber = 1;
            int status = 0;

            if (!DA.GetData(0, ref outputNumber)) return;
            if (!DA.GetData(1, ref status)) return;

            string strCommand = "SetDO " + "DO10_" + outputNumber.ToString() + ", " + status.ToString() + ";";

            DA.SetData(0, strCommand);
        }
    }
}