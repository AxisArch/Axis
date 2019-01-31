using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Grasshopper.GUI;
using Rhino.Geometry;

using StreamTools;
using Axis.Targets;
using Axis.Tools;

namespace Axis.Online
{
    public class OnlineIRC5 : GH_Component

        // Online control module for ABB industrial robots based on RoboOp from Madeline Gannong (MadLab)
    {
        // Class level variables for ensuring that we only create a single robot connection.
        public static StreamTools.Robot rob = null;
        public bool online = false;

        public OnlineIRC5() : base("Online", "Online", "Stream or upload data to an ABB IRC5 robot controller.", "Axis", "Core")
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconWarning;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{c3e16b33-544e-4d48-b2e1-f45aadb5c1fc}"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Connect", "Connect", "Search the network and connect to a robot controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the streaming component and clear the buffer.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "Controller IP address as string.", GH_ParamAccess.item, "192.168.125");
            pManager.AddIntegerParameter("Port", "Port", "Port number for connection", GH_ParamAccess.item, 1025);
            pManager.AddGenericParameter("Target", "Target", "Target to move to.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "Status of the connection.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Debug", "Debug", "Debug stuff.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            bool reset = false;
            string robIP = "127.0.0.1";
            int robPort = 1025;
            Target robTarg = null;

            // Initialise lists to store the messaging log and the debug information.
            List<string> log = new List<string>();
            List<object> debug = new List<object>();
                        
            if (!DA.GetData(0, ref connect)) return;
            if (!DA.GetData(1, ref reset)) return;
            if (!DA.GetData(2, ref robIP)) return;
            if (!DA.GetData(3, ref robPort)) return;
            if (!DA.GetData(4, ref robTarg)) return;

            if (reset)
            {
                // Potential issue with nulling the robot before allowing RoboOp to close the connection.
                connect = false;
                rob = null;
            }

            if (connect)
            {
                if (!online && rob == null)
                {
                    rob = new StreamTools.Robot(robIP, robPort);

                    string status = rob.connect();
                    log.Add(status);
                    
                    online = true;
                }

                if (rob != null && (online))
                {
                    // Move robot to target position.
                    rob.moveTo(robTarg.Position, robTarg.Quaternion);
                    ///string inPos = rob.waitUntilPos();
                    /*
                    // Get real position and orienation, display message on return of wait.
                    Point3d pos = rob.getPosition();
                    debug.Add(pos);
                    Quaternion orient = rob.getOrientation();
                    log.Add("Robot in position at " + pos.ToString() + ", " + orient.ToString() + "." );

                    // Create representative plane of current TCP position.
                    Plane endPlane = Util.QuaternionToPlane(pos, orient);
                    debug.Add(endPlane);
                    */
                }
            }

            else if (connect == false)
            {
                if (rob == null)
                {
                    log.Clear();
                    log.Add("No connection to robot controller.");
                    log.Add("Standing by.");
                }

                else if (rob != null && rob.isSetup == true)
                {
                    //rob.quit();
                    rob.closeClient();
                    log.Clear();
                    log.Add("Disconnected from robot controller.");
                    log.Add("Standing by.");
                    
                    online = false;
                }
            }

            DA.SetDataList(0, log);
            DA.SetDataList(1, debug);
        }
    }
}