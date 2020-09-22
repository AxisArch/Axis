using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using Axis.GH_Params;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Controller = Axis.Kernal.Controller;

namespace Axis.GH_Components
{
    /// <summary>
    /// Handles online connections to an IRC5 controller.
    /// </summary>
    public class GH_Connect : Kernal.AxisLogin_Component, IGH_VariableParameterComponent
    {
        public List<string> Status { get; set; }
        public List<Controller> controllers = new List<Controller>();

        public bool updateVL = false;

        private bool logOption;
        private bool logOptionOut;



        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();


        public GH_Connect()
          : base("Controller", "Controller",
              "Connect to an ABB controller",
              AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Scan", "Scan", "Scan the network for available controllers.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Index", "Index", "Index of the controller to connect to (if multiple connections are possible).", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process; logoff and dispose of network controllers.", GH_ParamAccess.item, false);

            for (int i = 0; i < 5; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param controllerParam = new ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Connection to Robot contoller", GH_ParamAccess.list);
        }

        #endregion IO

        protected override void SolveInternal(IGH_DataAccess DA)
        {
            bool scan = false;
            bool kill = false;
            bool clear = false;
            List<string> ipAddresses = new List<string>();
            List<int> indecies = new List<int>();
            bool connect = false;

            DA.GetData("Scan", ref scan);
            if (!DA.GetDataList("IP", ipAddresses)) { return; }
            DA.GetDataList("Index", indecies);
            DA.GetData("Connect", ref connect);
            DA.GetData("Kill", ref kill);

            if (logOption)
            {
                DA.GetData("Clear", ref clear);
            }


            if (scan)
            {
                //Reset Controllers
                if (controllers != null)
                {
                    foreach (var controller in controllers) controller.LogOff();
                    controllers = new List<Controller>();
                }

                // Find ABB robots on the network
                NetworkScanner scanner = new NetworkScanner();
                scanner.Scan();

                if (ipAddresses != null)
                {
                    // Scan the network for controllers and add them to our controller array.
                    foreach (string ip in ipAddresses) { NetworkScanner.AddRemoteController(ip); }
                }
                ControllerInfo[] controllersID = scanner.GetControllers();
                foreach (ControllerInfo id in controllersID) controllers.Add(new AbbIRC5Contoller(id));


                // Possibility to scan for other robots


                // Allow value list to be updated
                updateVL = true;


                if (controllers != null) { 
                    if (controllers.Count > 0)
                    {
                        log.Add("Controllers found:");

                        // List the controller names that were found on the network.
                        for (int i = 0; i < controllers.Count; i++)
                        {
                            log.Add(controllers[i].Name);
                        }
                    }
                    else { log.Add("Scan timed out. No controllers were found."); }
                }
            }

            // Populate value list
            if (updateVL)
            {
                if (controllers != null) { 
                    updateVL = false;

                    GH_Document doc = OnPingDocument();
                    List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();

                    //Create values for list and populate it
                    for (int i = 0; i < controllers.Count; ++i)
                    {
                        values.Add(new KeyValuePair<string, string>(controllers[i].Name, i.ToString()));
                    }

                    Canvas.Component.SetValueList(doc, this, 2, values, "Controller");
                }
            }

            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            foreach (var idx in indecies)
            {
                if (kill && controllers[idx] != null) if (controllers[idx].LogOff()) log.Add("Process killed! Abandon ship!");

                // Make the actual connection.
                if (connect && controllers[idx] != null) controllers[idx].Connect();

            }

            if (logOptionOut)
            {
                Status = log;
                DA.SetDataList("Log", log);
            }

            if (controllers != null) DA.SetDataList(0, controllers.FindAll(c => indecies.Contains(controllers.IndexOf(c))).ToList() );

        }
        

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[1]
        {
            new Param_Boolean() { Name = "Clear", NickName = "Clear", Description = "Clear the communication log.", Access = GH_ParamAccess.item, Optional = true},
        };

        // Build a list of optional output parameters
        private IGH_Param[] outputParams = new IGH_Param[1]
        {
            new Param_String() { Name = "Log", NickName = "Log", Description = "Log checking the connection status"},
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem log = Menu_AppendItem(menu, "Log", log_Click, true, logOption);
            log.ToolTipText = "Activate the log output";

            //ToolStripSeparator seperator = Menu_AppendSeparator(menu);
        }

        private void log_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Log");
            logOption = !logOption;

            if (logOption)
            {
                this.AddInput(0, inputParams);
                this.AddOutput(0, outputParams);
                logOptionOut = true;
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Clear"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
                logOptionOut = false;
            }

            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOptionConnect", this.logOption);
            writer.SetBoolean("LogOptionOutConnect", this.logOptionOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionConnect");
            this.logOptionOut = reader.GetBoolean("LogOptionOutConnect");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance(){}

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Icons.Connect;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("20538b5a-c2f6-4b3e-ab91-e59104ff2c71"); }
        }

        #endregion Component Settings
    }
}