using Grasshopper.Kernel;
using System;

namespace Axis.GH_Components
{
    /// <summary>
    /// Get all of the available modules from a robot controller.
    /// </summary>
    public class GH_GetModules : GH_Component
    {
        public GH_GetModules() : base("Get Modules", "Get Mods", "Get all available modules from a robot controller.", AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Connection to Robot contoller", GH_ParamAccess.list);


            for (int i = 0; i < 1; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        #region Component Settings

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Get_Module;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9503f909-fd58-4a57-8900-0f9c83c04846"); }
        }

        #endregion Component Settings
    }
}