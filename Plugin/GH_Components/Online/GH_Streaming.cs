using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Stream command instructions to a remote IRC5 controller.
    /// </summary>
    public class GH_Streaming : AxisLogin_Component, IGH_VariableParameterComponent
    {
        // Optionable Log
        private bool logOption = false;

        private bool logOptionOut = false;
        private bool lQOption = false;

        private List<string> Status { get; set; }

        private bool modOption = false;
        private bool stream;

        private Controller controller = null;

        

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        private List<string> IOstatus = new List<string>();
        private Plane tcp = new Plane();


        public GH_Streaming() : base("Live Connection", "Stream", "Stream instructions to a robot controller", AxisInfo.Plugin, AxisInfo.TabLive)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param controller = new GH_Params.ContollerParam();
            pManager.AddParameter(controller, "Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Stream", "Stream", "Begin streaming to the robot.", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target", "Target", "Target for robot positioning.", GH_ParamAccess.item);
            // Inputs optional
            for (int i = 1; i < 3; ++i) { pManager[i].Optional = true; }
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        protected override void SolveInternal(IGH_DataAccess DA)
        {
            bool clear = false;
            bool lqclear = false;
            bool stream = false;

            List<Kernal.Controller> controllers = new List<Kernal.Controller>();

            Target targ = null;

            DA.GetDataList("Controller",  controllers);
            DA.GetData("Stream", ref stream);
            DA.GetData("Target", ref targ);
            if (logOption) DA.GetData("Clear", ref clear);
            if (lQOption) DA.GetData("Clear Local Queue", ref lqclear);

            //Output module file to prime controller for straming
            if (modOption)
            {
                List<string> ModFile = new List<string>();

                string test = Folders.AppDataFolder;

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
            if (controllers != null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }

            // Stream a target to a controller
            if (stream)
            {
                for (int i =0; i< controllers.Count; ++i)
                {
                    if (targ != null) controllers[i].Stream(targ);
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

            //Output module file to prime controller for straming
            if (modOption)
            {
                List<string> ModFile = new List<string>();

                string test = Folders.AppDataFolder;

                // Use resource
                List<string> modfile;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Resources.RAPID_Modules.moduleFile))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    modfile = (List<string>)br.Deserialize(ms);
                }

                using (TextReader reader = File.OpenText(Folders.AppDataFolder + @"Libraries\Axis\Online\moduleFile.mod"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ModFile.Add(line);
                    }
                }
                DA.SetDataList("Steaming Module", ModFile);
            }
        }

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

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem LQueue = Menu_AppendItem(menu, "Local Queue", lQueue_Click, true, lQOption);
            LQueue.ToolTipText = "This will enable a local queue";
            ToolStripMenuItem modFile = Menu_AppendItem(menu, "Steaming Module", mod_Click, true, modOption);
            modFile.ToolTipText = "Activate the module file output";
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
                this.AddOutput(1, outputParams);
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

        private void mod_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Steaming Module");
            modOption = !modOption;

            if (modOption)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Steaming Module"), true);
            }

            //ExpireSolution(true);
        }

        private void lQueue_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Local Queue");
            lQOption = !lQOption;

            if (lQOption)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Output.FirstOrDefault(x => x.Name == "Steaming Module"), true);
            }

            //ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("LogOptionSetModule", this.logOption);
            writer.SetBoolean("LogOptionSetOutModule", this.logOptionOut);
            writer.SetBoolean("ModFileOption", this.modOption);
            writer.SetBoolean("Local Queue", this.lQOption);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionSetModule");
            this.logOptionOut = reader.GetBoolean("LogOptionSetOutModule");
            this.modOption = reader.GetBoolean("ModFileOption");
            this.lQOption = reader.GetBoolean("Local Queue");
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