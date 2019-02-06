using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;


namespace Axis.Online
{
    public class IRC5Online : GH_Component
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
        private int ioMonitoringOn = 0;
        private int ioMonitoringOff = 0;
        private int tcpMonitoringOn = 0;
        private int tcpMonitoringOff = 0;

        NetworkScanner scanner = new NetworkScanner();
        ControllerInfo[] controllers = null;

        ABB.Robotics.Controllers.RapidDomain.RobTarget cRobTarg;
        ABB.Robotics.Controllers.RapidDomain.Byte[] data;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();
        private List<string> IOstatus = new List<string>();
        private Plane tcp = new Plane();

        Pos pos = new Pos();
        Orient ori = new Orient();
        Pose pose = new Pose();

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Online;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("6e6ff838-aad7-4224-986d-6cba047e8a41"); }
        }

        public IRC5Online() : base("Online", "Online", "Online control and communcation for ABB IRC5 controllers.", "Axis", "Core")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the online communication module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Scan", "Scan", "Scan the network for available controllers.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Kill", "Kill", "Kill the process; logoff and dispose of network controllers.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Clear", "Clear", "Clear the communication log.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("IP", "IP", "IP adress of the controller to connect to.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Index", "Index", "Index of the controller to connect to (if multiple connections are possible).", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Connect", "Connect", "Connect to the network controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset PP", "Reset PP", "Set the program pointed back to the main entry point for the current task.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Begin", "Begin", "Start the default task on the controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Stop", "Stop", "Stop the default task on the controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("TCP", "TCP", "Opional monitoring of the TCP.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("IO", "IO", "Opional monitoring of the IO system. (Only signals registered as common will be monitored.)", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Stream", "Stream", "Begin streaming to the robot.", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target", "Target", "Target for robot positioning.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
            pManager[12].Optional = true;
            pManager[13].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Communication status.", GH_ParamAccess.list);
            pManager.AddTextParameter("IO", "IO", "IO status.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("TCP", "TCP", "TCP status.", GH_ParamAccess.item);
        }

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
            bool stop = false;
            bool monitorTCP = false;
            bool monitorIO = false;
            bool stream = false;
            Target targ = null;

            if (!DA.GetData(0, ref activate)) ;
            if (!DA.GetData(1, ref scan)) ;
            if (!DA.GetData(2, ref kill)) ;
            if (!DA.GetData(3, ref clear)) ;
            if (!DA.GetData(4, ref ip)) ;
            if (!DA.GetData(5, ref index)) ;
            if (!DA.GetData(6, ref connect)) ;
            if (!DA.GetData(7, ref resetPP)) ;
            if (!DA.GetData(8, ref begin)) ;
            if (!DA.GetData(9, ref stop)) ;
            if (!DA.GetData(10, ref monitorTCP)) ;
            if (!DA.GetData(11, ref monitorIO)) ;
            if (!DA.GetData(12, ref stream)) ;
            if (!DA.GetData(13, ref targ)) ;

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

                if (begin)
                {
                    // Execute robot tasks present on controller.
                    try
                    {
                        if (controller.OperatingMode == ControllerOperatingMode.Auto)
                        {
                            using (Mastership m = Mastership.Request(controller.Rapid))
                            {
                                // Perform operation.
                                tasks[0].Start();
                                log.Add("Robot program started on robot " + controller.SystemName + ".");
                            }
                        }
                        else
                        {
                            log.Add("Automatic mode is required to start execution from a remote client.");
                        }
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        log.Add("Mastership is held by another client." + ex.Message);
                    }
                    catch (System.Exception ex)
                    {
                        log.Add("Unexpected error occurred: " + ex.Message);
                    }
                }

                if (stop)
                {
                    try
                    {
                        if (controller.OperatingMode == ControllerOperatingMode.Auto)
                        {
                            using (Mastership m = Mastership.Request(controller.Rapid))
                            {
                                // Stop operation.
                                tasks[0].Stop(StopMode.Immediate);
                                log.Add("Robot program stopped on robot " + controller.SystemName + ".");
                            }
                        }
                        else
                        {
                            log.Add("Automatic mode is required to stop execution from a remote client.");
                        }
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        log.Add("Mastership is held by another client." + ex.Message);
                    }
                    catch (System.Exception ex)
                    {
                        log.Add("Unexpected error occurred: " + ex.Message);
                    }
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
                    SignalCollection dSignals = controller.IOSystem.GetSignals(dSignalFilter);

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

                /*
                if (stream)
                {
                    if (targ != null)
                    {
                        IpcMessage message = new IpcMessage();

                        pos.X = (float)targ.Position.X;
                        pos.Y = (float)targ.Position.Y;
                        pos.Z = (float)targ.Position.Z;


                        ori.Q1 = targ.Quaternion.A;
                        ori.Q2 = targ.Quaternion.B;
                        ori.Q3 = targ.Quaternion.C;
                        ori.Q4 = targ.Quaternion.D;

                        pose.Trans = pos;
                        pose.Rot = ori;

                        //string content = "SD;" + targ.Method.ToString() + "," + pose.ToString();
                        string content = @"SD;" + "Test";

                        byte[] msgdata = new UTF8Encoding().GetBytes(content);

                        for (int i = 0; i < msgdata.Length; i++)
                        {
                            data[i] = (ABB.Robotics.Controllers.RapidDomain.Byte[])msgdata[i];
                        }

                        message.SetData(data);
                        RobotQueue.Send(message);
                    }
                }
                */

            // Output the status of the connection.
            Status = log;

            DA.SetDataList(0, log);
            DA.SetDataList(1, IOstatus);
            DA.SetData(2, new GH_Plane(tcp));

            ExpireSolution(true);
        }
    }
}