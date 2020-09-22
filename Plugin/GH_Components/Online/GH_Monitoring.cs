using Axis.Kernal;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Online monitoring of an IRC5 controller object.
    /// </summary>
    public class GH_Monitoring : GH_Component, IGH_VariableParameterComponent
    {
        // Optionable Log
        private bool logOption = false;

        private bool logOptionOut = false;
        private bool autoUpdate = false;

        private List<string> Status { get; set; }

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        private List<string> IOstatus = new List<string>();
        private List<Plane> tcp = new List<Plane>();

        public GH_Monitoring() : base("Monitoring  ", "Monitoring", "This will monitor the robots position and IO's", AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controllerParam = new GH_Params.ContollerParam();
            pManager.AddParameter(controllerParam, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("IO", "IO", "IO status.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("TCP", "TCP", "TCP status.", GH_ParamAccess.list);
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Controller> controllers = new List<Controller>();

            DA.GetDataList("Controller", controllers);


            if (controllers == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }

            bool monitorTCP = true;
            bool monitorIO = true;
            bool clear = false;


            // Update TCP
            if (monitorTCP) tcp = controllers.Select(c => c.GetTCP()).ToList();

            // If active, update the status of the IO system.
            if (monitorIO) controllers.ForEach(c => c.GetIO());


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
            DA.SetDataList("TCP", tcp);

            if (autoUpdate)
            {
                ExpireSolution(true);
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

            //ExpireSolution(true);
        }

        private void autoUpdate_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("AutoUpdate");
            autoUpdate = !autoUpdate;
            ExpireSolution(true);
        }

        #endregion UI

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

        #endregion Component Settings
    }
}