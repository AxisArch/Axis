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
    public class GH_StartStop : Axis_Component, IGH_VariableParameterComponent
    {
        public GH_StartStop()
          : base("Start/Stop", "Start/Stop",
              "Controll a programm running on a robot controller",
              AxisInfo.Plugin, AxisInfo.TabLive)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            // Set up the UI buttons for the component
            this.UI_Elements = new IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentToggle("Start"){ LeftClickAction = StartStop, Toggle = new Tuple<string, string>("Start", "Stop") },
                new Kernal.UIElements.ComponentButton("Reset PP"){ LeftClickAction = ResetPP },
            };
            attr.Update(UI_Elements);

            // Add option to the context menu
            logOption = new ToolStripMenuItem("Show Log", null, log_Click) { ToolTipText = "Activate the log output." };
            this.RegularToolStripItems = new ToolStripItem[]
            {
                logOption,
            };
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool clear = false;

            controllers.Clear();
            DA.GetDataList("Controller", controllers);
            if (logOption.Checked) DA.GetData("Clear", ref clear);

            // Check for input
            if (controllers.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active controller connected."); }

            // Body of the code
            for (int i = 0; i < controllers.Count; i++)
            {
                if(!controllers[i].IsValid) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{controllers[i].Name} is not active."); }

                // Check motor state and set icon.
                if (motorState != controllers[i].State)
                {
                    motorState = controllers[i].State;
                    DestroyIconCache();
                }
            }

            // Clear log
            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            // Output log
            if (logOption.Checked)
            {
                DA.SetDataList("Log", log);
            }
        }

        # region Methods

        void StartStop(object sender, object e) 
        {
            var toggel = sender as IToggle;

            switch (toggel.State) 
            {
                case true:
                    for (int i = 0; i < controllers.Count; i++) toggel.State = !controllers[i].Stop();

                    break;
                case false:
                    for (int i = 0; i < controllers.Count; i++) toggel.State = controllers[i].Start();
                    break;
            }
            
            ExpireSolution(true);
        }

        void ResetPP(object sender, object e) 
        {
            for (int i = 0; i < controllers.Count; i++) if (controllers[i] != null) controllers[i].Reset();
            ExpireSolution(true);
        }

        # endregion Methods

        #region Variables

        // Optionable Log
        ToolStripMenuItem logOption;


        private List<string> log = new List<string>();

        List<Controller> controllers = new List<Controller>();
        private ControllerState motorState = ControllerState.Init;

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

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


        private void log_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            RecordUndoEvent("Log");
            item.Checked = !item.Checked;

            if (item.Checked)
            {
                this.AddInput(0, inputParams);
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Clear"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
            }

            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOption", this.logOption.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if (reader.ItemExists("LogOption")) this.logOption.Checked = reader.GetBoolean("LogOption");
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