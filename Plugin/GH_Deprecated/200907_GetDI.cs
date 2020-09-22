using Grasshopper.Kernel;
using System;

namespace Axis.GH_Components
{
    /// <summary>
    /// Get the value of a specific digital input.
    /// </summary>
    public class GetDI_Obsolete : GH_Component
    {
        public GetDI_Obsolete() : base("Get Digital Input", "Get DI", "Get the value of a specific digital input.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        #region Component Settings

        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Icons.DigitalIn;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("d3f8a44d-ea83-4740-8511-de8b4725dec5"); }
        }

        #endregion Component Settings
    }
}