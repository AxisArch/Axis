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
    public class SetModule : GH_Component
    {
        public string ControllerID { get; set; }
        public List<string> Status { get; set; }
        public Controller controller = null;
        private Task[] tasks = null;
        public IpcQueue RobotQueue { get; set; }

        //Copied
        private bool scan;
        private bool kill;
        private bool connect;
        private int controllerIndex;
        private bool start;
        private bool stream;
        private string command;

        //New 
        private bool logOption;


        NetworkScanner scanner = new NetworkScanner();
        ControllerInfo[] controllers = null;

        ABB.Robotics.Controllers.RapidDomain.RobTarget cRobTarg;
        System.Byte[] data;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();
        private List<string> IOstatus = new List<string>();
        private Plane tcp = new Plane();


        /// <summary>
        /// Initializes a new instance of the WriteModuleToControler class.
        /// </summary>
        public SetModule()
          : base("Set Module", "Set Mod",
              "Set the main module on the robot controller",
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
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process; logoff and dispose of network controllers.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Clear", "Clear", "Clear the communication log.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Send", "Send", "Send to module", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Moduel", "Module", "Module to be wtritten to the controller.", GH_ParamAccess.list);

            for (int i = 0; i < 7; i++)
            {
                pManager[i].Optional = true;
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
            string ip = System.String.Empty;
            int index = 0;
            bool connect = false;
            bool resetPP = false;
            bool begin = false;

            if (!DA.GetData(0, ref activate)) ;
            if (!DA.GetData(1, ref scan)) ;
            if (!DA.GetData(2, ref kill)) ;
            if (!DA.GetData(3, ref clear)) ;
            if (!DA.GetData(4, ref ip)) ;
            if (!DA.GetData(5, ref index)) ;
            if (!DA.GetData(6, ref connect)) ;
            if (!DA.GetData(7, ref resetPP)) ;
            if (!DA.GetData(8, ref begin)) ;


            this.controllerIndex = index;
            this.start = begin;

            double cRobX = 0;
            double cRobY = 0;
            double cRobZ = 0;

            double cRobQ1 = 0;
            double cRobQ2 = 0;
            double cRobQ3 = 0;
            double cRobQ4 = 0;

            Quaternion cRobQuat = new Quaternion();

            if (activate)
            {
                if (scan)
                {
                    // Scan the network for controllers and add them to our controller array
                    scanner.Scan();
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

                            if (controller != null)
                            {
                                controller.Logoff();
                                controller.Dispose();
                                controller = null;
                            }

                            controller = ControllerFactory.CreateFrom(controllers[index]);
                            controller.Logon(UserInfo.DefaultUser);
                            log.Add("Connection to robot controller " + controller.SystemName + " established.");

                            // Get T_ROB1 queue to send messages to the RAPID task.
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

                if (resetPP && controller != null)
                {
                    using (Mastership m = Mastership.Request(controller.Rapid))
                    {
                        // Reset program pointer to main.
                        tasks[0].ResetProgramPointer();
                        log.Add("Program pointer set to main on the active task.");
                    }
                }

                // To be changed in to optional logoutput
                // Output the status of the connection.
                /*Status = log;

                DA.SetDataList(0, log);
                DA.SetDataList(1, IOstatus);
                DA.SetData(2, new GH_Plane(tcp));
                */

                ExpireSolution(true);
            }

        }
        


        /// <summary>
        /// Additional Input and Output parameters for the component
        /// </summary>
        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[0]
        {
            //new Param_Integer() { Name = "Method", NickName = "Method", Description = "A list of target interpolation types [0 = Linear, 1 = Joint]. If one value is supplied it will be applied to all targets.", Access = GH_ParamAccess.list },
        };
        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Log", NickName = "Log", Description = "Log checking the connection status" },
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
            RecordUndoEvent("KukaTargets");
            logOption = !logOption;
            //ExpireSolution(true);
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
            get { return new Guid("676a28f1-9320-4a02-a9bc-59c617dd04d0"); }
        }
    }
}