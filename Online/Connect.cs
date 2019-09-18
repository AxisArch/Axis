using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
{
    public class Connect : GH_Component, IGH_VariableParameterComponent
    {
        public string ControllerID { get; set; }
        public List<string> Status { get; set; }
        public Controller controller = null;
        private Task[] tasks = null;
        public IpcQueue RobotQueue { get; set; }

        private bool scan;
        private bool kill;
        private bool connect;
        private int controllerIndex;
        private bool start;
        private bool stream;
        private string command;
        private bool logOption;
        private bool logOptionOut;


        NetworkScanner scanner = new NetworkScanner();
        ControllerInfo[] controllers = null;


        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        GH_Document GrasshopperDocument;
        IGH_Component Component;


        /// <summary>
        /// Initializes a new instance of the Controller class.
        /// </summary>
        public Connect()
          : base("Controller", "Controller",
              "Connect to an ABB controller",
              "Axis", "9. Online")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the online communication module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Scan", "Scan", "Scan the network for available controllers.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Index", "Index", "Index of the controller to connect to (if multiple connections are possible).", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process; logoff and dispose of network controllers.", GH_ParamAccess.item, false);

            for (int i = 0; i < 5; i++)
            {
                pManager[i].Optional = true;
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Connection to Robot contoller", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool activate = false;
            bool scan = false;
            bool kill = false;
            bool clear = false;
            List<string> ipAddresses = new List<string>();
            int index = 0;
            bool connect = false;
         
          
            if (!DA.GetData("Activate", ref activate)) ;
            if (!DA.GetData("Scan", ref scan)) ;
            if (!DA.GetDataList("IP",  ipAddresses)){return;}
            if (!DA.GetData("Index", ref index)) ;
            if (!DA.GetData("Connect", ref connect)) ;
            if (!DA.GetData("Kill", ref kill)) ;

            if (logOption)
            {
                if (!DA.GetData("Clear", ref clear)) ;
            }
            
            this.controllerIndex = index;

            if (activate)
            {
                if (scan)
                {
                    // Scan the network for controllers and add them to our controller array       
                    scanner.Scan();

                    if (ipAddresses != null)
                    {
                        foreach (string ip in ipAddresses) { NetworkScanner.AddRemoteController(ip); }
                    }

                    controllers = scanner.GetControllers();

                    if (controllers.Length > 0)
                    {
                        log.Add("Controllers found:");

                        // List the controller names that were found on the network.
                        for (int i = 0; i < controllers.Length; i++)
                        {
                            log.Add(controllers[i].ControllerName);
                        }
                    }
                    else { log.Add("Scan timed out. No controllers were found."); }
                }
                
                // Populate value list
                if (controllers.Length > 0 && controllers!= null)
                {
                    //instantiate  new value list and clear it
                    var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
                    vallist.ListItems.Clear();

                    //Create values for list and populate it
                    for (int i = 0; i < controllers.Length; ++i)
                    {
                        var item = new Grasshopper.Kernel.Special.GH_ValueListItem(controllers[i].Name, i.ToString());
                        vallist.ListItems.Add(item);
                    }
                    List<IGH_Param> sources = new List<IGH_Param>();

                    IList<IGH_Param> inputs = this.Component.Params.Input;
                    foreach (IGH_Param input in inputs)
                    {
                        if (input.Name == "Index") { sources.Add(input); }
                    }

                    foreach (IGH_Param source in sources)
                    {
                        source.ReplaceSource(source, vallist);
                        //Until now, the slider is a hypothetical object.
                        // This command makes it 'real' and adds it to the canvas.
                        //GrasshopperDocument.AddObject(vallist, false);
                        //GrasshopperDocument.AddObject(vallist, false, 0);

                        //Connect the new slider to this component
                        //this.Component.Params.Input[3].AddSource(vallist);
                        //this.Component.Params.Input[3].AddSource(vallist);

                    }

                }

                if (kill && controller != null)
                {
                    controller.Logoff();
                    controller.Dispose();
                    controller = null;

                    log.Add("Process killed! Abandon ship!");
                }

                if (clear)
                {
                    log.Clear();
                    log.Add("Log cleared.");
                }

                if (connect)
                {
                    if (controller == null && controllers.Length > 0)
                    {
                        string controllerID = controllers[index].ControllerName;
                        log.Add("Selected robot controller: " + controllers[index].ControllerName + ".");

                        if (controllers[index].Availability == Availability.Available)
                        {
                            log.Add("Robot controller " + controllers[index].ControllerName + " is available.");

                            // Shound never be the case see base if statment
                            if (controller != null)
                            {
                                controller.Logoff();
                                controller.Dispose();
                                controller = null;
                            }

                            controller = ControllerFactory.CreateFrom(controllers[index]);
                            controller.Logon(UserInfo.DefaultUser);
                            log.Add("Connection to robot controller " + controller.SystemName + " established.");

                            /*
                            // Get T_ROB1 queue to send messages to the RAPID task.
                            // Needs to be moved later
                            if (!controller.Ipc.Exists("RMQ_T_ROB1"))
                            {
                                controller.Ipc.CreateQueue("RMQ_T_ROB1", 10, Ipc.MaxMessageSize);
                            }

                            tasks = controller.Rapid.GetTasks();
                            IpcQueue robotQueue = controller.Ipc.GetQueue("RMQ_T_ROB1");
                            int queueID = robotQueue.QueueId;
                            string queueName = robotQueue.QueueName;

                            log.Add("Established RMQ for T_ROB1 on network controller.");
                            log.Add("Rapid Message Queue ID:" + queueID.ToString() + ".");
                            log.Add("Rapid Message Queue Name:" + queueName + ".");
                            RobotQueue = robotQueue;
                            */
                        }
                        else
                        {
                            log.Add("Selected controller not available.");
                        }

                        ControllerID = controllerID;
                    }
                    else
                    {
                        if (controller != null)
                        {
                            return;
                        }
                        else
                        {
                            string exceptionMessage = "No robot controllers found on network.";
                            ControllerID = "No Controller";
                            log.Add(exceptionMessage);
                        }
                    }
                }

                if (logOptionOut)
                {
                    Status = log;
                    DA.SetDataList("Log", log);
                }

                

                if (controller != null)
                {
                    AxisController myAxisController = new AxisController(controller);
                    DA.SetData(0, myAxisController);

                }
                else { DA.SetData(0, "No active connection"); }

                //ExpireSolution(true);
            }
            
        }



        /// <summary>
        /// Additional Input and Output parameters for the component
        /// </summary>
        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[1]
        {
            new Param_Boolean() { Name = "Clear", NickName = "Clear", Description = "Clear the communication log.", Access = GH_ParamAccess.item, Optional = true},
        };
        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
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
                AddInput(0);
                AddOutput(0);
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


        // Register the new input parameters to our component.
        private void AddInput(int index)
        {
            IGH_Param parameter = inputParams[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(inputParams, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        // Register the new output parameters to our component.
        private void AddOutput(int index)
        {
            IGH_Param parameter = outputParams[index];

            if (Params.Output.Any(x => x.Name == parameter.Name))
                Params.UnregisterOutputParameter(Params.Output.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Output.Count;
                for (int i = 0; i < Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(outputParams, x => x.Name == Params.Output[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterOutputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();

            ExpireSolution(true);
        }

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

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Online;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("20538b5a-c2f6-4b3e-ab91-e59104ff2c71"); }
        }
    }
}