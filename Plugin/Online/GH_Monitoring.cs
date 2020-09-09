using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
{
    /// <summary>
    /// Online monitoring of an IRC5 controller object.
    /// </summary>
    public class GH_Monitoring : GH_Component, IGH_VariableParameterComponent
    {
        // Optionable Log
        bool logOption = false;
        bool logOptionOut = false;
        bool autoUpdate = false;

        List<string> Status { get; set; }

        Controller controller = null;
        Task[] tasks = null;

        // State switch variables for TCP monitoring.
        int ioMonitoringOn = 0; int ioMonitoringOff = 0;
        int tcpMonitoringOn = 0; int tcpMonitoringOff = 0;

        ABB.Robotics.Controllers.RapidDomain.RobTarget cRobTarg;
        System.Byte[] data;

        // Create a list of string to store a log of the connection status.
        List<string> log = new List<string>();
        List<string> IOstatus = new List<string>();
        Plane tcp = new Plane();

        public GH_Monitoring() : base("Monitoring  ", "Monitoring", "This will monitor the robots position and IO's", AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("IO", "IO", "IO status.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("TCP", "TCP", "TCP status.", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper controller = new GH_ObjectWrapper();
            Controller abbController = null;

            if (!DA.GetData("Controller", ref controller)) ;

            //Check for valid input, else top execuion
            AxisController myAxisController = controller.Value as AxisController;
            if ((myAxisController != null) && (myAxisController.axisControllerState == true))
                abbController = myAxisController;
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }

            bool monitorTCP = true;
            bool monitorIO = true;
            bool clear = false;

            // Current Robot task
            tasks = abbController.Rapid.GetTasks();

            // Current robot positions and rotations
            double cRobX = 0; double cRobY = 0; double cRobZ = 0;
            double cRobQ1 = 0; double cRobQ2 = 0; double cRobQ3 = 0; double cRobQ4 = 0;
            Quaternion cRobQuat = new Quaternion();

            // Update TCP
            if (monitorTCP)
            {
                if (tcpMonitoringOn == 0)
                    log.Add("TCP monitoring started.");

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

                tcpMonitoringOn += 1; tcpMonitoringOff = 0;
            }
            else if (tcpMonitoringOn > 0)
            {
                if (tcpMonitoringOff == 0)
                    log.Add("TCP monitoring stopped.");

                tcpMonitoringOff += 1; tcpMonitoringOn = 0;
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

                ioMonitoringOn += 1; ioMonitoringOff = 0; // Update state switch variables for IO monitoring.
                //ExpireSolution(true);
            }
            else if (ioMonitoringOn > 0)
            {
                if (ioMonitoringOff == 0)
                {
                    log.Add("Signal monitoring stopped.");
                }

                ioMonitoringOff += 1; ioMonitoringOn = 0;
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

            if (autoUpdate)
            {
                ExpireSolution(true);
            }
        }

        #region UI
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
            ToolStripMenuItem update = Menu_AppendItem(menu, "Auto Update", autoUpdate_Click, true, autoUpdate);
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

            //ExpireSolution(true);
        }
        private void autoUpdate_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("AutoUpdate");
            autoUpdate = !autoUpdate;
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
        #endregion

        #region Serialization
        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOptionSetModule", this.logOption);
            writer.SetBoolean("LogOptionSetOutModule", this.logOptionOut);
            writer.SetBoolean("AutoUpdate", this.autoUpdate);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionSetModule");
            this.logOptionOut = reader.GetBoolean("LogOptionSetOutModule");
            this.autoUpdate = reader.GetBoolean("AutoUpdate");
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Icons.DigitalIn;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("288C0F4E-542A-4A37-A947-4B541436BEFB"); }
        }
        #endregion
    }
}