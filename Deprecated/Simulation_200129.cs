using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Axis.Targets;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Axis.Core
{
    public class Simulation_Obsolete : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// This Component has been depricated
        /// </summary>
        public override bool Obsolete => true;

        internal int n = 0;

        protected static List<double> cAngles = new List<double>();
        protected static Target cTarg = Target.Default;
        List<string> log = new List<string>();

        bool timeline = false;
        bool showSpeed = false;
        bool showAngles = false;
        bool showMotion = false;
        bool showExternal = false;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Play;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("0d998b74-4e74-4108-a4e8-c49103160f73"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public Simulation_Obsolete() : base("Simulation", "Program", "Simulate a robotic toolpath.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "Toolpath", "Robotic toolpath as a list of robot targets and strings.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Begin", "Begin", "Begin the simulation.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the simulation to the first command in the list.", GH_ParamAccess.item, false);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Target", "Target", "Current robot target.", GH_ParamAccess.item);
            pManager.AddTextParameter("Command", "Command", "Current command.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_ObjectWrapper> toolpath = new List<GH_ObjectWrapper>();
            bool begin = false;
            bool reset = false;
            double position = 0.000;
            bool hasToolpath = true;

            if (!DA.GetDataList(0, toolpath)) { hasToolpath = false; return; }
            if (!DA.GetData(1, ref begin)) return;
            if (!DA.GetData(2, ref reset)) return;

            if (timeline) { if (!DA.GetData("Timeline", ref position)) return; }

            int index = 0;
            double programLength = toolpath.Count - 1;
            string mode = "Auto | ";

            if (begin && hasToolpath) // Add condition !timeline to ensure that the program can only be simulated using the slider.
            {
                if (reset) { n = 0; } // Reset the simulation to the first target.
                else if (n < programLength)
                {
                    // Step through the program.
                    n++;
                    index = n;
                    ExpireSolution(true);
                }
                else
                {
                    if (toolpath.Count != 0)
                    {
                        index = toolpath.Count - 1;
                    }
                    else { index = 0; }
                }
            }
            else // Use the timeline slider to define the previewed program position.
            {
                mode = "Manual | ";
                double remappedIndex = Util.Remap(position, 0.0, 1.0, 0.0, programLength);
                int roundedIndex = Convert.ToInt32(remappedIndex);
                index = roundedIndex;
            }

            this.Message = mode + "Index: " + (index).ToString();

            // Retrieve the current target from the program.
            GH_ObjectWrapper currTarg = toolpath[index];

            Target targ = Target.Default;
            MotionType mType = MotionType.Linear;

            Type cType = currTarg.Value.GetType();

            string command = String.Empty;
            double speed = 0;

            List<double> externals = new List<double>();
            double linExt = 0;
            double rotExt = 0;

            if (cType.Name == "GH_String")
            {
                string cmd = currTarg.Value.ToString();
                log.Add("Command.");

                // If we don't have a target, keep the robot at the last known position.
                if (cTarg != Target.Default)
                {
                    DA.SetData(0, cTarg);
                }
                DA.SetData(1, cmd);
            }
            else
            {
                targ = currTarg.Value as Target; // Cast back to target type
                cTarg = targ; // Updated last target.
                DA.SetData(0, targ); // Output current target

                if (showSpeed)
                {
                    // Current translational speed
                    speed = targ.Speed.TranslationSpeed;
                    DA.SetData("Speed", speed);
                }
                if (showExternal)
                {
                    // External rotary axis
                    rotExt = targ.ExtRot;
                    if (rotExt != Util.ExAxisTol) { externals.Add(rotExt); }

                    // External linear axis
                    linExt = targ.ExtLin;
                    if (linExt != Util.ExAxisTol) { externals.Add(linExt); }

                    DA.SetDataList("External", externals);
                }
            }
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

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
}
