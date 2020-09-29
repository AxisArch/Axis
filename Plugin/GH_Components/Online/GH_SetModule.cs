using Axis.Kernal;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Set a module to an online IRC5 controller.
    /// </summary>
    public class GH_SetModule : Axis_Component, IGH_VariableParameterComponent
    {
        public GH_SetModule()
          : base("Set Module", "Set Mod",
              "Set the main module on the robot controller",
              AxisInfo.Plugin, AxisInfo.TabLive)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            IsMutiManufacture = false;

            this.UI_Elements = new Kernal.IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentButton("Send"){ LeftClickAction = Send },
            };

            attr.Update(UI_Elements);

            logOption = new ToolStripMenuItem("Log", null, log_Click) 
            {
                ToolTipText = "Activate the log output",
            };
            RegularToolStripItems = new ToolStripMenuItem[] { logOption };
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool clear = false;

            controllers.Clear();
            modFile.Clear();

            DA.GetDataList("Controller", controllers);
            if (!DA.GetDataList("Module", modFile)) { return; }
            if (logOption.Checked) DA.GetData("Clear", ref clear);

            if (controllers == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected"); return; }

            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            if (logOption.Checked)
            {
                DA.SetDataList("Log", log);
            }
        }

        #region Methods

        private void Send(object sender, object e)
        {
            foreach (var controller in controllers) controller.SetProgram(modFile);
        }

        #endregion Methods

        #region Variables

        ToolStripMenuItem logOption;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        private List<Kernal.Controller> controllers = new List<Kernal.Controller>();
        private List<string> modFile = new List<string>();

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            IGH_Param programParam = new GH_Params.ProgramParam();
            pManager.AddParameter(programParam, "Module", "Module", "Module to be wtritten to the controller.", GH_ParamAccess.list);
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
        new Param_String() { Name = "Log", NickName = "Log", Description = "Log checking the connection status"},
        };


        private void log_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("Log");
            button.Checked = !button.Checked;

            if (button.Checked)
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
            if(reader.ItemExists("LogOption"))this.logOption.Checked = reader.GetBoolean("LogOption");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Set_Module;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("676a28f1-9320-4a02-a9bc-59c617dd04d0"); }
        }

        #endregion Component Settings
    }
}