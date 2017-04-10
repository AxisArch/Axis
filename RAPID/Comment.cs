using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    public class Comment : GH_Component
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
            get { return new Guid("{ef081106-87c1-4521-8b0b-d1e296270867}"); }
        }

        public Comment() : base("Comment", "Comment", "Generates a RAPID comment.", "Axis", "RAPID")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("String", "String", "Comment as string.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Comment", "Comment", "Comment in RAPID format.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strComm = null;
            if (!DA.GetData(0, ref strComm)) return;

            DA.SetData(0, "! " + strComm);
        }
    }
}