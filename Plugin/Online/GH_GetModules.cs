using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Rhino.Geometry;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
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
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the online communication module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Scan", "Scan", "Scan the network for available controllers.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process; logoff and dispose of network controllers.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Clear", "Clear", "Clear the communication log.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);

            for (int i = 0; i < 6; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        #endregion

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
        #endregion
    }
}