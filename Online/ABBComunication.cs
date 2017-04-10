using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;


namespace Axis.Online
{
    
    public class ABBCommunication
    {
        public string ControllerID { get; set; }
        public List<string> Status { get; set; }
        public Controller controller = null;
        private Task[] tasks = null;
        public IpcQueue RobotQueue { get; set; }

        private bool connect;
        private int controllerIndex;
        private bool start;
        private bool stream;
        private string command;

        public ABBCommunication(bool connect, int controllerIndex, bool start)
        {
            this.connect = connect;
            this.controllerIndex = controllerIndex;
            this.start = start;

            // Create a list of string to store a log of the connection status.
            List<string> log = new List<string>();

            NetworkScanner scanner = new NetworkScanner();
            ControllerInfo[] controllers = scanner.GetControllers();

            if (connect)
            {
                if (controllers.Length > 0)
                {
                    string controllerID = controllers[controllerIndex].ControllerName;
                    log.Add("Connected to robot controller " + controllerID + ".");

                    if (controllers[controllerIndex].Availability == Availability.Available)
                    {
                        if (this.controller != null)
                        {
                            this.controller.Logoff();
                            this.controller.Dispose();
                            this.controller = null;
                        }
                        this.controller = ControllerFactory.CreateFrom(controllers[controllerIndex]);
                        this.controller.Logon(UserInfo.DefaultUser);
                        log.Add("Robot controller " + controllerID + " is available.");

                        // Get T_ROB1 queue to send messages to the RAPID task.
                        IpcQueue robotQueue = controller.Ipc.GetQueue("RMQ_T_ROB1");
                        this.RobotQueue = robotQueue;
                    }
                    else
                    {
                        log.Add("Selected controller not available.");
                    }

                    this.ControllerID = controllerID;

                    if (start)
                    {
                        // Execute robot tasks present on controller.
                        try
                        {
                            if (controller.OperatingMode == ControllerOperatingMode.Auto)
                            {
                                tasks = controller.Rapid.GetTasks();
                                while (start == true)
                                {
                                    using (Mastership m = Mastership.Request(controller.Rapid))
                                    {
                                        // Perform operation.
                                        tasks[0].Start();
                                        log.Add("Robot program started on robot " + controllerID + ".");
                                    }
                                }
                                using (Mastership m = Mastership.Request(controller.Rapid))
                                {
                                    // Stop operation.
                                    tasks[0].Stop();
                                    log.Add("Robot program stopped on robot " + controllerID + ".");
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
                }

                else
                {
                    string exceptionMessage = "No robot controllers found on network.";
                    this.ControllerID = "No Controller";
                    log.Add(exceptionMessage);
                }
            }
            
            // Output the status of the connection.
            this.Status = log;
        }
    }
    */
}