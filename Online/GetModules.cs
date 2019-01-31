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
    public class GetModules : GH_Component
    {
        public GetModules() : base("Get Modules", "Get Mods", "Get all available modules from a robot controller.", "Axis", "9. Online")
        {
        }

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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Online;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("9503f909-fd58-4a57-8900-0f9c83c04846"); }
        }
    }
}