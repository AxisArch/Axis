using Axis.Kernal;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Stream command instructions to a remote IRC5 controller.
    /// </summary>
    public class GH_Streaming : AxisLogin_Component, IGH_VariableParameterComponent
    {
        public GH_Streaming() : base("Live Connection", "Stream", "Stream instructions to a robot controller", AxisInfo.Plugin, AxisInfo.TabLive)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            IsMutiManufacture = false;

            this.UI_Elements = new Kernal.IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentToggle("Stream"){ LeftClickAction = Stream, Toggle = new Tuple<string, string>("Start","Stop") },
            };

            attr.Update(UI_Elements);



            modFile = new ToolStripMenuItem("Steaming Module", null, mod_Click) 
            {
                ToolTipText = "Activate the module file output",
            };
            logOption = new ToolStripMenuItem("Log", null, log_Click) 
            {
                ToolTipText = "Activate the log output",
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                modFile,
                logOption,
            };
        }

        protected override void SolveInternal(IGH_DataAccess DA)
        {
            bool clear = false;
            bool lqclear = false;

            controllers.Clear();
            targ = null;

            DA.GetDataList("Controller", controllers);
            DA.GetData("Target", ref targ);

            if (logOption.Checked) DA.GetData("Clear", ref clear);
            if (logOption.Checked) DA.GetData("Clear Local Queue", ref lqclear);

            //Output module file to prime controller for straming
            if (modFile.Checked)
            {
                List<string> ModFile = new List<string>();

                // Use file from resource
                using (TextReader reader = new StreamReader(new System.IO.MemoryStream(Resources.RAPID_Modules.moduleFile)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ModFile.Add(line);
                    }
                }

                DA.SetDataList("Steaming Module", ModFile);
            }

            //Check for valid input, else top execuion
            if (controllers != null) if (controllers.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }

            if (stream) for (int i = 0; i < controllers.Count; ++i) if (targ != null) controllers[i].Stream(targ);
            // Clear log
            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            //Output log
            if (logOption.Checked)
            {
                DA.SetDataList("Log", log);
            }
        }

        #region Method

        private void Stream(object sender, object e)
        {
            var toggel = sender as IToggle;
            toggel.State = !toggel.State;
            stream = toggel.State;
        }

        #endregion Method

        #region Variables
        ToolStripMenuItem modFile;
        ToolStripMenuItem logOption;



        private bool stream = false;

        private List<string> Status { get; set; }


        private Target targ = null;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        private List<Controller> controllers = new List<Kernal.Controller>();

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controller = new GH_Params.ContollerParam();
            pManager.AddParameter(controller, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            IGH_Param instruction = new GH_Params.InstructionParam();
            pManager.AddParameter(instruction, "Target", "Target", "Target for robot positioning.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[2]
        {
            new Param_Boolean() { Name = "Clear", NickName = "Clear", Description = "Clear the communication log.", Access = GH_ParamAccess.item, Optional = true},
            new Param_Boolean() { Name = "Clear Local Queue", NickName = "Clear LQ", Description = "Clear the the local Queue", Access = GH_ParamAccess.item, Optional = true},
        };

        // Build a list of optional output parameters
        private IGH_Param[] outputParams = new IGH_Param[2]
        {
            new Param_String() { Name = "Steaming Module", NickName = "SMod", Description = "Module that needsa to be running on the controller for streaming live targets"},
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
                this.AddOutput(1, outputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Clear"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
            }

            ExpireSolution(true);
        }

        private void mod_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("Steaming Module");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Steaming Module"), true);
            }

            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOption", this.logOption.Checked);
            writer.SetBoolean("ModFileOption", this.modFile.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("LogOption")) this.logOption.Checked = reader.GetBoolean("LogOption");
            if(reader.ItemExists("ModFileOption")) this.modFile.Checked = reader.GetBoolean("ModFileOption");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Icons.Streaming;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b2f9edfc-14d6-4db0-a569-6d3a0ddec76a"); }
        }

        #endregion Component Settings
    }
}