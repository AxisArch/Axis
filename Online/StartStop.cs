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
    public class StartStop : GH_Component, IGH_VariableParameterComponent
    {
        //Optionable Log
        private bool logOption = false;
        private bool logOptionOut = false;
        private List<string> log = new List<string>();
        public List<string> Status { get; set; }

        public Controller controllers = null;
        private Task[] tasks = null;

        ControllerState motorState = ControllerState.Init;

        public StartStop()
          : base("Start/Stop", "Start/Stop",
              "Controll a programm running on a robot controller",
              AxisInfo.Plugin, AxisInfo.TabOnline)
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Reset PP", "Reset PP", "Set the program pointed back to the main entry point for the current task.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Begin", "Begin", "Start the default task on the controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Stop", "Stop", "Stop the default task on the controller.", GH_ParamAccess.item, false);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_ObjectWrapper> controllers = new List<GH_ObjectWrapper>();
            Controller abbController = null;
            bool resetPP = false;
            bool begin = false;
            bool stop = false;
            bool clear = false;
            if (!DA.GetDataList("Controller", controllers)) ;
            if (!DA.GetData("Reset PP", ref resetPP)) ;
            if (!DA.GetData("Begin", ref begin)) ;
            if (!DA.GetData("Stop", ref stop)) ;
            if (logOption)
            {
                if (!DA.GetData("Clear", ref clear)) ;
            }
            // Check for input
            if (controllers.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active contoller conected"); }

            // Body of the code  
            for (int i = 0; i < controllers.Count; i++)
            {
                //Check for valid input, else top execuion
                AxisController myAxisController = controllers[i].Value as AxisController;
                if ((myAxisController != null) && (myAxisController.axisControllerState == true))
                {
                    abbController = myAxisController;
                }
                else { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected"); return; }

                tasks = abbController.Rapid.GetTasks();

                // Check motor state and set icon
                if (motorState != abbController.State)
                {
                    motorState = abbController.State;
                    DestroyIconCache();
                }



                if (resetPP && abbController != null)
                {
                    using (Mastership m = Mastership.Request(abbController.Rapid))
                    {
                        // Reset program pointer to main.
                        try
                        {
                            tasks[0].ResetProgramPointer();                            
                        }
                        catch (Exception){log.Add("Opperation not allowed in current state");}

                    }
                }

                if (begin)
                {
                    // Execute robot tasks present on controller.
                    try
                    {
                        if (abbController.OperatingMode == ControllerOperatingMode.Auto)
                        {
                            using (Mastership m = Mastership.Request(abbController.Rapid))
                            {
                                // Perform operation.
                                tasks[0].Start();
                                log.Add("Robot program started on robot " + abbController.SystemName + ".");
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
                        if (abbController.OperatingMode == ControllerOperatingMode.Auto)
                        {
                            using (Mastership m = Mastership.Request(abbController.Rapid))
                            {
                                // Stop operation.
                                tasks[0].Stop(StopMode.Immediate);
                                log.Add("Robot program stopped on robot " + abbController.SystemName + ".");
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
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionSetModule");
            this.logOptionOut = reader.GetBoolean("LogOptionSetOutModule");
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
                if (motorState == ControllerState.MotorsOn)
                {
                    return Properties.Resources.Star_Stop;
                }
                if (motorState == ControllerState.MotorsOff)
                {
                    return Properties.Resources.MotorOff;
                }
                if (motorState == ControllerState.EmergencyStop)
                {
                    return Properties.Resources.EmergencyStop;
                }
                else
                {
                    return Properties.Resources.UnknownMotorState;
                }
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("1dca8994-0a96-4454-a5bb-28c8bd911829"); }
        }
    }
}