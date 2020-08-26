using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Axis.Targets;

namespace Axis.Core
{
    /// <summary>
    /// Stepwise simulation of a robotic program.
    /// </summary>
    public class Simulation : GH_Component, IGH_VariableParameterComponent
    {
        DateTime strat = new DateTime();
        Toolpath toolpath;
        Target cTarget;

        bool timeline = false;
        bool showSpeed = false;
        bool showAngles = false;
        bool showMotion = false;
        bool showExternal = false;

        public Simulation() : base("Simulation", "Program", "Simulate a robotic toolpath.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param target = new Axis.Params.TargetParam();
            pManager.AddParameter(target, "Targets", "Targets", "T", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset", "Reset","",GH_ParamAccess.item );
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param target = new Axis.Params.TargetParam();
            pManager.AddParameter(target, "Target", "Target", "", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Target> targets = new List<Target>();
            bool run = false;
            bool rest = false;

            DateTime now = DateTime.Now;

            if (!DA.GetDataList("Targets", targets)) return;
            if (!DA.GetData("Run", ref run)) return;
            if (!DA.GetData("Reset", ref rest)) return;

            if ( toolpath== null) toolpath = new Toolpath(targets);

            if (rest)
            {
                strat = DateTime.Now;
                toolpath = new Toolpath(targets);
            }

            if (run) 
            {
                Target nTarget = toolpath.GetTarget(now - strat);
                if (cTarget != nTarget) 
                {
                    cTarget = nTarget;
                }
                
                DA.SetData("Target", cTarget);
                ExpireSolution(true);
            }
            else DA.SetData("Target", targets[0]);

        }

        #region UI
        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            //Subscribe to all event handelers
            this.Params.ParameterSourcesChanged += OnParameterSourcesChanged; ;
        }

        /// <summary>
        /// Replace a number slider with one that has the proper values set.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnParameterSourcesChanged(Object sender, GH_ParamServerEventArgs e)
        {
            int index = e.ParameterIndex;
            IGH_Param param = e.Parameter;

            //Only add value list to the first input
            if (param.Name !=  "Timeline") return;

            //Only change value lists
            var extractedItems = param.Sources.Where(p => p.Name == "Number Slider");

            //Set up the number slider
            Grasshopper.Kernel.Special.GH_NumberSlider gH_NumberSlider = Canvas.Component.CreateNumbersilder("Timeline",0, 1m, 4, 400);

            //The magic
            Canvas.Component.ChangeObjects(extractedItems, gH_NumberSlider);
        }

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[1]
        {
        new Param_Number() { Name = "Timeline", NickName = "Timeline", Description = "A timeline slider describing the position in the robot program to simulate. (0 = Beginning, 1 = End)" },
        };

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[4]
        {
        new Param_Number() { Name = "Speed", NickName = "Speed", Description = "The current speed of the robot in mm/s." },
        new Param_Number() { Name = "Angles", NickName = "Angles", Description = "The current angle values of the robot." },
        new Param_Number() { Name = "Motion", NickName = "Motion", Description = "The current motion type of the robot." },
        new Param_Number() { Name = "External", NickName = "External", Description = "The current external axis values as a list." },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem timelineOption = Menu_AppendItem(menu, "Use Timeline", timeline_Click, true, timeline);
            timelineOption.ToolTipText = "Use a timeline slider to specify the program position for simulation.";

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem speedCheck = Menu_AppendItem(menu, "Show Speed", speed_Click, true, showSpeed);
            speedCheck.ToolTipText = "Preview the current speed of the robot at each point in the simulation.";
            ToolStripMenuItem anglesCheck = Menu_AppendItem(menu, "Show Angles", angles_Click, true, showAngles);
            anglesCheck.ToolTipText = "Preview the current angle values of the robot at each point in the simulation.";
            ToolStripMenuItem motionCheck = Menu_AppendItem(menu, "Show Motion", motion_Click, true, showMotion);
            motionCheck.ToolTipText = "Preview the current motion type of the robot at each point in the simulation.";
            ToolStripMenuItem externalCheck = Menu_AppendItem(menu, "Show External", external_Click, true, showExternal);
            externalCheck.ToolTipText = "Preview the current position of each external axis as a list.";
        }

        private void timeline_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("TimelineClick");
            timeline = !timeline;

            if (timeline) { AddInput(0); }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Timeline"), true);
            }
            ExpireSolution(true);
        }

        private void speed_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("SpeedClick");
            showSpeed = !showSpeed;

            if (showSpeed) { AddOutput(0); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Speed"), true);
            }
            ExpireSolution(true);
        }

        private void angles_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("AnglesClick");
            showAngles = !showAngles;

            if (showAngles) { AddOutput(1); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Angles"), true);
            }
            ExpireSolution(true);
        }

        private void motion_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("MotionClick");
            showMotion = !showMotion;

            if (showMotion) { AddOutput(2); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Motion"), true);
            }
            ExpireSolution(true);
        }

        private void external_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ExternalClick");
            showExternal = !showExternal;

            if (showExternal) { AddOutput(3); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "External"), true);
            }
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
            writer.SetBoolean("UseTimeline", this.timeline);
            writer.SetBoolean("ShowSpeed", this.showSpeed);
            writer.SetBoolean("ShowAngles", this.showAngles);
            writer.SetBoolean("ShowMotion", this.showMotion);
            writer.SetBoolean("ShowExternal", this.showExternal);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.timeline = reader.GetBoolean("UseTimeline");
            this.showSpeed = reader.GetBoolean("ShowSpeed");
            this.showAngles = reader.GetBoolean("ShowAngles");
            this.showMotion = reader.GetBoolean("ShowMotion");
            this.showExternal = reader.GetBoolean("ShowExternal");
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        /// <summary>
        /// Component settings.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            { return Properties.Resources.Play; }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("7baedf8e-5efe-4549-b8d5-93a4b9e4a1fd"); }
        }
        #endregion
    }
}