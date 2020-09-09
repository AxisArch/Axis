using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    /// <summary>
    /// Add a simple RAPID-formatted comment.
    /// </summary>
    public class Comment_Obsolete : GH_Component
    {
        public Comment_Obsolete() : base("Comment", "C", "Generates a RAPID comment.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "Text", "Comment as string.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Comment in RAPID format.", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strComm = null;
            if (!DA.GetData(0, ref strComm)) return;

            DA.SetData(0, "! " + strComm);
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
            get { return new Guid("{ef081106-87c1-4521-8b0b-d1e296270867}"); }
        }
        #endregion
    }
}