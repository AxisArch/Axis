using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Start and stop robot tasks on a controller.
    /// </summary>
    public class GH_StartStop : GH_Component, IGH_VariableParameterComponent
    {
        // Optionable Log
        private bool logOption = false;

        private bool logOptionOut = false;
        private List<string> log = new List<string>();
        public List<string> Status { get; set; }

        public Controller controllers = null;

        private Axis.Kernal.ControllerState motorState = Axis.Kernal.ControllerState.Init;

        public GH_StartStop()
          : base("Start/Stop", "Start/Stop",
              "Controll a programm running on a robot controller",
              AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Reset PP", "Reset PP", "Set the program pointed back to the main entry point for the current task.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Begin", "Begin", "Start the default task on the controller.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Stop", "Stop", "Stop the default task on the controller.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Axis.Kernal.Controller> controllers = new List<Axis.Kernal.Controller>();
            bool resetPP = false;
            bool begin = false;
            bool stop = false;
            bool clear = false;
            DA.GetDataList("Controller", controllers);
            DA.GetData("Reset PP", ref resetPP);
            DA.GetData("Begin", ref begin);
            DA.GetData("Stop", ref stop);
            if (logOption) DA.GetData("Clear", ref clear);

            // Check for input
            if (controllers.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active contoller connected."); }

            // Body of the code
            for (int i = 0; i < controllers.Count; i++)
            {
                if(!controllers[i].IsValid) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }



                // Check motor state and set icon.
                if (motorState != controllers[i].State)
                {
                    motorState = controllers[i].State;
                    DestroyIconCache();
                }

                if (resetPP && controllers[i] != null) controllers[i].Reset();

                if (begin) controllers[i].Start();

                if (stop) controllers[i].Stop();
            }

            // Clear log
            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            // Output log
            if (logOptionOut)
            {
                Status = log;
                DA.SetDataList("Log", log);
            }
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
        new Param_String() { Name = "Log", NickName = "Log", Description = "Connection status log."},
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem log = Menu_AppendItem(menu, "Show Log", log_Click, true, logOption);
            log.ToolTipText = "Activate the log output.";

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
        }

        #endregion UI

        #region Serialization

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

        #endregion Serialization

        #region Component Settings

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                switch (motorState)
                {
                    case Kernal.ControllerState.MotorsOn:
                        return Properties.Icons.Start_Stop;
                    case Kernal.ControllerState.MotorsOff:
                        return Properties.Icons.MotorOff;
                    case Kernal.ControllerState.EmergencyStop:
                        return Properties.Icons.EmergencyStop;
                    default:
                        return Properties.Icons.UnknownMotorState;
                }
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("1dca8994-0a96-4454-a5bb-28c8bd911829"); }
        }

        #endregion Component Settings
    }
}