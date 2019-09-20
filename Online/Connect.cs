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
        public bool updateVL = false;
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

        GH_DocumentIO docIO = new GH_DocumentIO();
        List<IGH_Param> delInputs = new List<IGH_Param>();
        Grasshopper.Kernel.Special.GH_ValueList vl = new Grasshopper.Kernel.Special.GH_ValueList();

        // Callbacks
        private void createValuelist(GH_Document doc)
        {
            //try { Params.Input[3].RemoveAllSources(); }
            //catch { }
            if (delInputs != null && delInputs.Count > 0)
            {
                doc.AddObject(vl, false, 1);

                for (int i = 0; i < delInputs.Count; ++i)
                {
                    Params.Input[3].RemoveSource(delInputs[i]);
                    delInputs[i].IsolateObject();
                    doc.RemoveObject(delInputs[i], false);
                    doc.AddObject(vl, false, 1);
                }
                Params.Input[3].AddSource(vl);
            }

            if (false)
            {
                // ?
            }
            delInputs.Clear();
        }

        public Connect() : base("Controller", "Controller", "Connect to an ABB controller", "Axis", "9. Online")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the online communication module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Scan", "Scan", "Scan the network for available controllers.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Index", "Index", "Index of the controller to connect to (if multiple connections are possible).", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process, logoff and dispose of network controllers.", GH_ParamAccess.item, false);

            for (int i = 0; i < 5; i++)
            {
                pManager[i].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Connection to Robot contoller", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            bool activate = false;
            bool scan = false;
            bool kill = false;
            bool clear = false;
            List<string> ipAddresses = new List<string>();
            int index = 0;         
          
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
                    updateVL = true;

                    if (controllers.Length > 0)
                    {
                        log.Add("Found controllers:");

                        // List the controller names that were found on the network.
                        for (int i = 0; i < controllers.Length; i++)
                        {
                            log.Add(controllers[i].ControllerName);
                        }
                    }
                    else { log.Add("Scan timed out. No controllers were found."); }
                }

                // Populate value list
                if (updateVL && (controllers != null))
                {
                    updateVL = false;
                    docIO.Document = new GH_Document();

                    //instantiate  new value list and clear it
                    vl.ListItems.Clear();
                    vl.NickName = "Controller";
                    vl.Name = "Controller";

                    //Create values for list and populate it
                    for (int i = 0; i < controllers.Length; ++i)
                    {
                        var item = new Grasshopper.Kernel.Special.GH_ValueListItem(controllers[i].ControllerName + " - " + controllers[i].Name, i.ToString());
                        vl.ListItems.Add(item);
                    }

                    //get active GH doc else abort
                    GH_Document doc = OnPingDocument();
                    if (docIO.Document == null) return;
                    doc.MergeDocument(docIO.Document);

                    //Create or replace input
                    if (Params.Input[3].Sources.Count == 0)
                    {
                        // place the object
                        docIO.Document.AddObject(vl, false, 1);

                        //get the pivot of the "accent" param
                        System.Drawing.PointF currPivot = Params.Input[3].Attributes.Pivot;
                        //set the pivot of the new object
                        vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 210, currPivot.Y - 11);
                        Params.Input[3].AddSource(vl);
                    }
                    else
                    {
                        IList<IGH_Param> sources = this.Params.Input[3].Sources;
                        for (int i = 0; i< sources.Count; ++i)
                        {
                            if (sources[i].Name == "Value List" | sources[i].Name == "Controller")
                            {
                                //get the pivot of the "source" value list
                                System.Drawing.PointF currPivot = sources[i].Attributes.Pivot;
                                //set the pivot of the new object
                                vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X, currPivot.Y);
                                delInputs.Add(sources[i]);
                            }
                        }
                    }

                    // Find out what this is doing and why
                    docIO.Document.SelectAll();
                    docIO.Document.ExpireSolution();
                    docIO.Document.MutateAllIds();
                    IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
                    doc.DeselectAll();
                    doc.UndoUtil.RecordAddObjectEvent("Create Accent List", objs);
                    doc.MergeDocument(docIO.Document);
                    doc.ScheduleSolution(10, createValuelist);
                }

                if (kill && controller != null)
                {
                    controller.Logoff();
                    controller.Dispose();
                    controller = null;
                    log.Add("Process killed, everything aborted!");
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
                        if (controller != null) { return; }
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
            }            
        }
               
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

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Connect;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("20538b5a-c2f6-4b3e-ab91-e59104ff2c71"); }
        }
    }
}