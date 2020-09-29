using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.RapidDomain;
using Axis.Kernal;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;

namespace Axis.Types
{
    /// <summary>
    /// Custom controller class that uses the ABB Controller type.
    /// </summary>
    public class AbbIRC5Contoller : Kernal.Controller
    {
        private ABB.Robotics.Controllers.Controller controller;
        private ABB.Robotics.Controllers.ControllerInfo controllerInfo;

        private IpcQueue robotQueue;
        private Task[] tasks;
        private int queueID;
        private string queueName;

        private ABB.Robotics.Controllers.ControllerState state { get 
            {
                if (controller == null) return ABB.Robotics.Controllers.ControllerState.SystemFailure;
                return this.controller.State; 
            } 
        }

        public override string Name { get
            { 
                if (controller != null) return this.controller.Name;
                return controllerInfo.ControllerName;
            } 
        }


        public override bool IsValid => (state == ABB.Robotics.Controllers.ControllerState.MotorsOn);
        public override string IsValidWhyNot => $"The controller state is {state}";
        public override Kernal.ControllerState State { get
            {
                switch (state)
                {
                    case ABB.Robotics.Controllers.ControllerState.EmergencyStop:
                        return Kernal.ControllerState.EmergencyStop;
                    case ABB.Robotics.Controllers.ControllerState.Init:
                        return Kernal.ControllerState.Init;
                    case ABB.Robotics.Controllers.ControllerState.MotorsOff:
                        return Kernal.ControllerState.MotorsOff;
                    case ABB.Robotics.Controllers.ControllerState.MotorsOn:
                        return Kernal.ControllerState.MotorsOn;
                    default:
                        return Kernal.ControllerState.UnknownMotorState;

                }
            }
        }


        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="controller"></param>
        public AbbIRC5Contoller() { }
        public AbbIRC5Contoller(ControllerInfo controllerInfo)
        {
            Manufacturer = Manufacturer.ABB;
            this.controllerInfo = controllerInfo;
        }

        public override bool Connect()
        {
            try
            {
                log.Add($"Selected robot controller: {controllerInfo.ControllerName}.");

                if (controllerInfo.Availability == Availability.Available)
                {
                    log.Add($"Robot controller {controllerInfo.ControllerName} is available.");
                    controller = ControllerFactory.CreateFrom(controllerInfo);
                    controller.Logon(UserInfo.DefaultUser);
                    log.Add("Connection to robot controller " + controller.SystemName + " established.");

                }
                else
                {
                    log.Add("Selected controller not available.");
                }

                string exceptionMessage = "No robot controllers found on network.";
                //ControllerID = "No Controller";
                log.Add(exceptionMessage);
            }
            catch { }
            return false;
        }
        public override bool LogOff()
        {
            if (controller == null) return false;
            if (controller.Connected)
            {
                try
                {
                    controller.Logoff();
                    controller.Dispose();
                    controller = null;
                    return true;
                }
                catch { };
            }

            return false;
        }
        public override bool Start()
        {
            // Execute robot tasks present on controller.
            try
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    using (Mastership m = Mastership.Request(controller.Rapid))
                    {
                        var tasks = controller.Rapid.GetTasks();
                        tasks[0].Start();
                        log.Add($"Robot program started on robot {controller.SystemName}.");
                        return true;
                    }
                }
                else log.Add("Automatic mode is required to start execution from a remote client.");

            }
            catch (System.InvalidOperationException ex) { log.Add($"Mastership is held by another client {ex.Message}.");}
            catch (System.Exception ex) { log.Add($"Unexpected error occurred: {ex.Message}");}

            return false;
        }
        public override bool Stop()
        {
            try
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    using (Mastership m = Mastership.Request(controller.Rapid))
                    {
                        var tasks = controller.Rapid.GetTasks();
                        //tasks[0].Start();
                        // Stop operation.
                        tasks[0].Stop(ABB.Robotics.Controllers.RapidDomain.StopMode.Immediate);
                        log.Add($"Robot program stopped on robot {controller.SystemName}.");
                        return true;
                    }
                }
                else
                {
                    log.Add("Automatic mode is required to stop execution from a remote client.");
                }
            }
            catch (System.InvalidOperationException ex) { log.Add("Mastership is held by another client." + ex.Message);}
            catch (System.Exception e) { log.Add("Unexpected error occurred: " + e.Message); }


            return false;
        }
        public override bool Reset()
        {
            using (Mastership m = Mastership.Request(controller.Rapid))
            {
                var tasks = controller.Rapid.GetTasks();
                // Reset program pointer to main.
                try
                {
                    tasks[0].ResetProgramPointer();
                    return true;
                }
                catch (System.Exception) { log.Add("Opperation not allowed in current state");}
            }

            return false;
        }
        public override void ClearCaches()
        {
            controller = null;
            controllerInfo = null;
            base.ClearCaches();
        }
        public override bool SetProgram(List<string> programm)
        {
            var file = GetFile(programm);

            log.Add("Sending Module to controller");
            try
            {
                using (Mastership m = Mastership.Request(controller.Rapid))
                {
                    var tasks = controller.Rapid.GetTasks();

                    if (controller.IsVirtual)
                    {
                        // Load program to virtual controller
                        tasks[0].LoadModuleFromFile(file.Name, ABB.Robotics.Controllers.RapidDomain.RapidLoadMode.Replace);
                        log.Add("Program has been loaded to virtual controler");

                    }
                    else
                    {
                        // Load program to physical controller
                        tasks = controller.Rapid.GetTasks();

                        // Missing Check if file and directory exist
                        if (controller.FileSystem.DirectoryExists("Axis"))
                        {
                            if (controller.FileSystem.FileExists($"Axis/AxisModule.mod"))
                            {                                    
                                controller.FileSystem.RemoveFile($"Axis/AxisModule.mod");
                            }
                        }
                        else
                        {
                            controller.FileSystem.CreateDirectory(@"Axis");
                        }

                        //Delete all previouse tasks
                        for (int j = 0; j < tasks.Length; ++j) { tasks[j].DeleteProgram(); }

                        //Code
                        controller.FileSystem.PutFile(file.Name, "Axis/AxisModule.mod");
                        var sucsess = tasks[0].LoadModuleFromFile("Axis/AxisModule.mod", ABB.Robotics.Controllers.RapidDomain.RapidLoadMode.Replace);

                        if (sucsess)
                        {
                            log.Add("Program has been loaded to controler");
                        }
                        else
                        {
                            log.Add("The program contains at least one error and cannot be loaded");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                log.Add("Can't write to controller"); 
                log.Add(e.ToString());
            }
            //log.Add("Program has been loaded");
            //throw new System.NotImplementedException();

            if (System.IO.File.Exists(file.Name)) { System.IO.File.Delete(file.Name); }

            return true;
        }
        public override Plane GetTCP()
        {
            if (tasks == null)  tasks = controller.Rapid.GetTasks();
            var cRobTarg = tasks[0].GetRobTarget();

            Point3d position = new Point3d() 
            {
                X = Math.Round(cRobTarg.Trans.X, 3),
                Y = Math.Round(cRobTarg.Trans.Y, 3),
                Z = Math.Round(cRobTarg.Trans.Z, 3),
            };
            Quaternion orientation = new Quaternion() 
            {
                A = Math.Round(cRobTarg.Rot.Q1, 5),
                B = Math.Round(cRobTarg.Rot.Q2, 5),
                C = Math.Round(cRobTarg.Rot.Q3, 5),
                D = Math.Round(cRobTarg.Rot.Q4, 5),
            };

            return Util.QuaternionToPlane(position, orientation);

        }
        public override bool Stream(Target target)
        {
            Quaternion quatTool = Util.QuaternionFromPlane(target.Tool.TCP);
            Pos posTool = new Pos()
            {
                X = (float)target.Tool.TCP.OriginX,
                Y = (float)target.Tool.TCP.OriginY,
                Z = (float)target.Tool.TCP.OriginZ,
            };
            Orient oriTool = new Orient()
            {
                Q1 = quatTool.A,
                Q2 = quatTool.B,
                Q3 = quatTool.C,
                Q4 = quatTool.D,
            };

            RobJoint robJoint = new RobJoint()
            {
                Rax_1 = (float)target.JointAngles[0],
                Rax_2 = (float)target.JointAngles[1],
                Rax_3 = (float)target.JointAngles[2],
                Rax_4 = (float)target.JointAngles[3],
                Rax_5 = (float)target.JointAngles[4],
                Rax_6 = (float)target.JointAngles[5],
            };


            Pose pose = new Pose();
            pose.Trans = posTool;
            pose.Rot = oriTool;

            ToolData streemingTool = new ToolData() 
            { };
            Speed speed = new Speed() 
            {
                TranslationSpeed = target.Speed.TranslationSpeed,
                RotationSpeed = target.Speed.RotationSpeed,
            };
            Zone zone = new Zone() 
            {
                PathRadius = target.Zone.PathRadius,
                PathOrient = target.Zone.PathOrient,
            };


            string content = string.Empty;
            switch (target.Method) 
            {
                case MotionType.Linear:
                case MotionType.Joint:
                    Pos pos = new Pos()
                    {
                        X = (float)target.Position.X,
                        Y = (float)target.Position.Y,
                        Z = (float)target.Position.Z,
                    };
                    Orient ori = new Orient()
                    {
                        Q1 = target.Quaternion.A,
                        Q2 = target.Quaternion.B,
                        Q3 = target.Quaternion.C,
                        Q4 = target.Quaternion.D,
                    };
                    string motion = target.Method.ToString();
                    content = $"SD;[\"{motion}\",{pos.ToString()},{ori.ToString()},{robJoint.ToString()},{speed.TranslationSpeed.ToString()},{speed.RotationSpeed.ToString()},{pose.ToString()}" +
                    //zone.PathRadius.ToString() + "," +
                    //zone.PathOrient.ToString() +
                    "]";
                    break;
                case MotionType.AbsoluteJoint:
                    break;
            }


            var data = new System.Text.UTF8Encoding().GetBytes(content);
            SendIPCMessage(data);
            
            return true;
        }
        public override void GetIO()
        {
            var ios = controller.IOSystem.GetSignals(ABB.Robotics.Controllers.IOSystemDomain.IOFilterTypes.Input);

            var list = new List<string>();
            foreach (ABB.Robotics.Controllers.IOSystemDomain.Signal io in ios) 
            {
                if (!io.InputAsPhysical) break;
                list.Add($"{io.Name} -  {io.State}");
            }
            //throw new NotImplementedException();
        }


        private FileStream GetFile(List<string> list)
        {
            var filename = "MyModule";
            var filepath = System.IO.Path.GetTempPath() + $"\\{filename}.mod";

            FileStream file = new FileStream(filepath, FileMode.Create);

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(file))
            {
                foreach (var line in list) writer.WriteLine(line);
            }
            return file;
        }

        private void SendIPCMessage(byte[] data) 
        {
            IpcMessage message = new IpcMessage();
            message.SetData(data);

            try { robotQueue.Send(message); }
            catch (System.Exception e)
            {
                // Clear queue if full
                log.Add("Error sending message.");
                log.Add($"The error was: {e.Message}");

                /*
                if (lQOption)
                    LocalQueue.Enqueue(message);
                    */
            }
            /*
            if (LocalQueue.Count != 0 && lQOption)
            {
                LocalQueue.Enqueue(message);
                try { RobotQueue.Send(LocalQueue.Dequeue()); }
                catch (Exception e) { }
            }
            else
            {
            }
            */
        }

        // Get T_ROB1 queue to send messages to the RAPID task.
        private void GetQueue()
        {
            if (!controller.Ipc.Exists("RMQ_T_ROB1"))
                controller.Ipc.CreateQueue("RMQ_T_ROB1", 10, Ipc.MaxMessageSize);

            // Get RobotQueue
            IpcQueue robotQueue = controller.Ipc.GetQueue("RMQ_T_ROB1");

            tasks = controller.Rapid.GetTasks();
            queueID = robotQueue.QueueId;
            queueName = robotQueue.QueueName;

            log.Add("Established RMQ for T_ROB1 on network controller.");
            log.Add($"Rapid Message Queue ID: {queueID.ToString()}.");
            log.Add($"Rapid Message Queue Name: {queueName}.");

        }
        private void ClearQueue()
        {
            try 
            { 
                controller.Ipc.DeleteQueue(controller.Ipc.GetQueue(robotQueue.QueueName).QueueId);
            } 
            catch { }

        }
    }
}