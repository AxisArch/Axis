using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;


using Rhino.Geometry;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
{
    public class Streaming : GH_Component, IGH_VariableParameterComponent
    {
        //Optionable Log
        private bool logOption = false;
        private bool logOptionOut = false;
        public List<string> Status { get; set; }

        private bool modOption = false;

        public Controller controller = null;
        private Task[] tasks = null;
        public IpcQueue RobotQueue { get; set; }

        private bool stream;
        private int ioMonitoringOn = 0;
        private int ioMonitoringOff = 0;
        private int tcpMonitoringOn = 0;
        private int tcpMonitoringOff = 0;


        ABB.Robotics.Controllers.RapidDomain.RobTarget cRobTarg;
        System.Byte[] data;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();
        private List<string> IOstatus = new List<string>();
        private Plane tcp = new Plane();

        Pos pos = new Pos();
        Orient ori = new Orient();
        Pos posTool = new Pos();
        Orient oriTool = new Orient();
        Pose pose = new Pose();
        RobJoint robJoint = new RobJoint();
        ToolData streemingTool = new ToolData();
        Speed speed = new Speed();
        Zone zone = new Zone();



        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Streaming()
          : base("Live Strem Code", "Strem",
              "Stream instructions to a robot controller",
              "Axis", "9. Online")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Stream", "Stream", "Begin streaming to the robot.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("TCP", "TCP", "Opional monitoring of the TCP.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("IO", "IO", "Opional monitoring of the IO system. (Only signals registered as common will be monitored.)", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target", "Target", "Target for robot positioning.", GH_ParamAccess.item);

            //Input optional
            for (int i = 0; i < 5; ++i){pManager[i].Optional = true;}
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("IO", "IO", "IO status.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("TCP", "TCP", "TCP status.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper controller = new GH_ObjectWrapper();
            Controller abbController = null;
            bool clear = false;
            bool monitorTCP = false;
            bool monitorIO = false;
            bool stream = false;
            Target targ = null;

            if (!DA.GetData("Controller", ref controller)) ;
            if (!DA.GetData("Stream", ref stream)) ;
            if (!DA.GetData("TCP", ref monitorTCP)) ;
            if (!DA.GetData("IO", ref monitorIO)) ;
            if (!DA.GetData("Target", ref targ)) ;
            if (logOption)
            {
                if (!DA.GetData("Clear", ref clear)) ;
            }


            double cRobX = 0;
            double cRobY = 0;
            double cRobZ = 0;

            double cRobQ1 = 0;
            double cRobQ2 = 0;
            double cRobQ3 = 0;
            double cRobQ4 = 0;

            Quaternion cRobQuat = new Quaternion();

            //Check for valid input, else top execuion
            AxisController myAxisController = controller.Value as AxisController;
            if ((myAxisController != null) && (myAxisController.axisControllerState == true))
            {
                abbController = myAxisController;
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected"); return; }

            // Get T_ROB1 queue to send messages to the RAPID task.
            if (!abbController.Ipc.Exists("RMQ_T_ROB1"))
            {
                abbController.Ipc.CreateQueue("RMQ_T_ROB1", 10, Ipc.MaxMessageSize);
            }

            // Get RobotQueue
            if (RobotQueue== null)
            {
                tasks = abbController.Rapid.GetTasks();
                IpcQueue robotQueue = abbController.Ipc.GetQueue("RMQ_T_ROB1");
                int queueID = robotQueue.QueueId;
                string queueName = robotQueue.QueueName;

                log.Add("Established RMQ for T_ROB1 on network controller.");
                log.Add("Rapid Message Queue ID:" + queueID.ToString() + ".");
                log.Add("Rapid Message Queue Name:" + queueName + ".");
                RobotQueue = robotQueue;
            }

            // If active, update the status of the TCP.
            if (monitorTCP)
            {
                if (tcpMonitoringOn == 0)
                {
                    log.Add("TCP monitoring started.");
                }

                cRobTarg = tasks[0].GetRobTarget();

                cRobX = Math.Round(cRobTarg.Trans.X, 3);
                cRobY = Math.Round(cRobTarg.Trans.Y, 3);
                cRobZ = Math.Round(cRobTarg.Trans.Z, 3);

                Point3d cRobPos = new Point3d(cRobX, cRobY, cRobZ);

                cRobQ1 = Math.Round(cRobTarg.Rot.Q1, 5);
                cRobQ2 = Math.Round(cRobTarg.Rot.Q2, 5);
                cRobQ3 = Math.Round(cRobTarg.Rot.Q3, 5);
                cRobQ4 = Math.Round(cRobTarg.Rot.Q4, 5);

                cRobQuat = new Quaternion(cRobQ1, cRobQ2, cRobQ3, cRobQ4);

                tcp = Util.QuaternionToPlane(cRobPos, cRobQuat);

                tcpMonitoringOn += 1;
                tcpMonitoringOff = 0;
            }
            else if (tcpMonitoringOn > 0)
            {
                if (tcpMonitoringOff == 0)
                {
                    log.Add("TCP monitoring stopped.");
                }

                tcpMonitoringOff += 1;
                tcpMonitoringOn = 0;
            }

            // If active, update the status of the IO system.
            if (monitorIO)
            {
                if (ioMonitoringOn == 0)
                {
                    log.Add("Signal monitoring started.");
                }

                // Filter only the digital IO system signals.
                IOFilterTypes dSignalFilter = IOFilterTypes.Common;
                SignalCollection dSignals = abbController.IOSystem.GetSignals(dSignalFilter);

                IOstatus.Clear();
                // Iterate through the found collection and print them to the IO monitoring list.
                foreach (Signal signal in dSignals)
                {
                    string sigVal = signal.ToString() + ": " + signal.Value.ToString();
                    IOstatus.Add(sigVal);
                }

                ioMonitoringOn += 1;
                ioMonitoringOff = 0;
            }
            else if (ioMonitoringOn > 0)
            {
                if (ioMonitoringOff == 0)
                {
                    log.Add("Signal monitoring stopped.");
                }

                ioMonitoringOff += 1;
                ioMonitoringOn = 0;
            }

            // Steam a target to a controller
            if (stream)
            {
                if (targ != null)
                {
                    IpcMessage message = new IpcMessage();

                    string motion = targ.Method.ToString();

                    pos.X = (float)targ.Position.X;
                    pos.Y = (float)targ.Position.Y;
                    pos.Z = (float)targ.Position.Z;

                    posTool.X = (float)targ.Tool.TCP.OriginX;
                    posTool.Y = (float)targ.Tool.TCP.OriginY;
                    posTool.Z = (float)targ.Tool.TCP.OriginZ;

                    ori.Q1 = targ.Quaternion.A;
                    ori.Q2 = targ.Quaternion.B;
                    ori.Q3 = targ.Quaternion.C;
                    ori.Q4 = targ.Quaternion.D;

                    Quaternion quatTool = Util.QuaternionFromPlane(targ.Tool.TCP);
                    oriTool.Q1 = quatTool.A;
                    oriTool.Q2 = quatTool.B;
                    oriTool.Q3 = quatTool.C;
                    oriTool.Q4 = quatTool.D;

                    if (targ.JointAngles != null)
                    {
                        robJoint.Rax_1 = (float)targ.JointAngles[0];
                        robJoint.Rax_2 = (float)targ.JointAngles[1];
                        robJoint.Rax_3 = (float)targ.JointAngles[2];
                        robJoint.Rax_4 = (float)targ.JointAngles[3];
                        robJoint.Rax_5 = (float)targ.JointAngles[4];
                        robJoint.Rax_6 = (float)targ.JointAngles[5];
                    }

                    pose.Trans = posTool;
                    pose.Rot = oriTool;

                    
                    speed.TranslationSpeed = targ.Speed.TranslationSpeed;
                    speed.RotationSpeed = targ.Speed.RotationSpeed;
                    zone.PathRadius = targ.Zone.PathRadius;
                    zone.PathOrient = targ.Zone.PathOrient;

                    //streemingTool

                    string content = "SD;[" +
                            "\"" + 
                            motion +
                            "\"," +
                            pos.ToString() + "," +
                            ori.ToString() + "," +
                            robJoint.ToString() + "," +
                            speed.TranslationSpeed.ToString() + "," +
                            speed.RotationSpeed.ToString() + "," +
                            pose.ToString() +
                            //zone.PathRadius.ToString() + "," +
                            //zone.PathOrient.ToString() +
                            "]";
                    


                    byte[] data = new UTF8Encoding().GetBytes(content);

                    message.SetData(data);
                    try
                    {
                        RobotQueue.Send(message);
                        //Reset PP and restart if if error on controller(?)
                    }
                    catch (Exception e)
                    {
                        // Clear que if full
                        return;
                        //throw;
                    }
                }
            }

            // Clear log
            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            //Output log
            if (logOptionOut)
            {
                Status = log;
                DA.SetDataList("Log", log);
            }

            // Output IO & TCP
            DA.SetDataList("IO", IOstatus);
            DA.SetData("TCP", new GH_Plane(tcp));

            //Output module file to prime controller for straming
            if (modOption)
            {
                List<string> ModFile = new List<string>();

                using (TextReader reader = File.OpenText(@"Online\moduleFile.mod"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ModFile.Add(line);
                    }
                }
                DA.SetDataList("Steaming Module", ModFile);

            }

            ExpireSolution(true);

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
        IGH_Param[] outputParams = new IGH_Param[2]
        {
            new Param_String() { Name = "Steaming Module", NickName = "SMod", Description = "Module that needsa to be running on the controller for streaming live targets"},
            new Param_String() { Name = "Log", NickName = "Log", Description = "Log checking the connection status"},
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem modFile = Menu_AppendItem(menu, "Steaming Module", mod_Click, true, modOption);
            modFile.ToolTipText = "Activate the module file output";
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
                AddOutput(1);
                logOptionOut = true;
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Clear"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
                logOptionOut = false;
            }

            //ExpireSolution(true);
        }
        private void mod_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Steaming Module");
            modOption = !modOption;

            if (modOption)
            {
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Steaming Module"), true);
            }

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

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOptionSetModule", this.logOption);
            writer.SetBoolean("LogOptionSetOutModule", this.logOptionOut);
            writer.SetBoolean("ModFileOption", this.modOption); 
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionSetModule");
            this.logOptionOut = reader.GetBoolean("LogOptionSetOutModule");
            this.modOption = reader.GetBoolean("ModFileOption");
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
            get { return new Guid("b2f9edfc-14d6-4db0-a569-6d3a0ddec76a"); }
        }
    }
}