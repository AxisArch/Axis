using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Set a module to an online IRC5 controller.
    /// </summary>
    public class GH_SetModule : GH_Component, IGH_VariableParameterComponent
    {
        public List<string> Status { get; set; }


        private bool send = false;
        private bool logOption = false;
        private bool logOptionOut = false;


        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        public GH_SetModule()
          : base("Set Module", "Set Mod",
              "Set the main module on the robot controller",
              AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter( controllerParam, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Send", "Send", "Send to module", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Module", "Module", "Module to be wtritten to the controller.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Kernal.Controller> controllers = new List<Kernal.Controller>();

            List<string> modFile = new List<string>();
            bool clear = false;

            DA.GetDataList("Controller", controllers);
            DA.GetData("Send", ref send);
            if (!DA.GetDataList("Module", modFile)) { return; }
            if (logOption) DA.GetData("Clear", ref clear);


            if (controllers == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected"); return; }


            if (send) foreach (var controller in controllers) controller.SetProgram(modFile);

            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            if (logOptionOut)
            {
                Status = log;
                DA.SetDataList("Log", log);
            }
            //ExpireSolution(true);
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

            ExpireSolution(true);
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