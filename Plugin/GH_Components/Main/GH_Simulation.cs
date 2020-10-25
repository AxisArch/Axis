using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Stepwise simulation of a robotic program.
    /// </summary>
    public class GH_Simulation : Axis_Component, IGH_VariableParameterComponent
    {

        public GH_Simulation() : base("Simulation", "Program", "Simulate a robotic toolpath.", AxisInfo.Plugin, AxisInfo.TabMain)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            this.UI_Elements = new IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentToggle("Start"){ LeftClickAction = StartStop , Toggle = new Tuple<string, string>("Start","Stop")},
            };

            attr.Update(UI_Elements);

            timelineOption = new ToolStripMenuItem("Use Timeline", null, timeline_Click)
            {
                ToolTipText = "Use a timeline slider to specify the program position for simulation."
            };
            flangeCheck = new ToolStripMenuItem("Show Flange", null, flange_Click) 
            {
                ToolTipText = "Preview the current flange of the robot at each point in the simulation.",
            };
            speedCheck = new ToolStripMenuItem("Show Speed", null, speed_Click) 
            {
                ToolTipText = "Preview the current speed of the robot at each point in the simulation.",
            };
            anglesCheck = new ToolStripMenuItem("Show Angles", null, angles_Click)
            {
                ToolTipText = "Preview the current angle values of the robot at each point in the simulation.",
            };
            motionCheck = new ToolStripMenuItem("Show Motion", null, motion_Click) 
            {
                ToolTipText = "Preview the current motion type of the robot at each point in the simulation.",
            };
            externalCheck = new ToolStripMenuItem("Show External", null, external_Click) 
            {
                ToolTipText = "Preview the current position of each external axis as a list.",
            };
            fullprogramCheck = new ToolStripMenuItem( "Show full error log", null, fullCheck_Click) 
            {
                ToolTipText = "Out put a list of all the targets that are unreachable and a cresponding log.",
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                timelineOption,
                flangeCheck,
                speedCheck,
                anglesCheck,
                motionCheck,
                externalCheck,
                fullprogramCheck,
            };
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Set up solution variables
            List<Kernal.Instruction> instructions = new List<Kernal.Instruction>();
            DateTime now = DateTime.Now;
            Robot c_Robot = null;

            // Load Inputs
            if (!DA.GetData(0, ref c_Robot)) return;
            if (!DA.GetDataList("Instructions", instructions)) return;

            if (!run | newData)
            {
                //Set Up Tool path
                toolpath = new Toolpath(instructions, c_Robot);
                this.Message = (toolpath.IsValid) ? "No Errors detected" : "Errors";
                strat = DateTime.Now;

                if (!toolpath.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Toolpath");
                    return;
                }

                c_Pose = toolpath.StartPose;
                newData = false;
            }

            // Out put a full error log
            if (toolpath != null && fullprogramCheck.Checked && !run)
            {
                if (!DA.SetDataList("Full Error Log", toolpath.ErrorLog)) return;
                if (!DA.SetDataList("Error Positions", toolpath.ErrorPositions)) return;
            }

            if (toolpath != null && run)
            {
                if (c_Pose == null) c_Pose = toolpath.GetPose(DateTime.Now - TimeSpan.FromSeconds(0.1) - strat);

                var pose = toolpath.GetPose(DateTime.Now - strat);

                if (!pose.IsValid) c_Pose.Colors = pose.Colors;
                else c_Pose = pose;

                if ((DateTime.Now - strat - TimeSpan.FromSeconds(1)) > toolpath.duration)
                {
                    run = false;
                    ExpireSolution(true);
                }
            }
            else if (timelineOption.Checked && !run)
            {
                double tValue = 0;
                if (!DA.GetData("*Timeline", ref tValue)) return;
                c_Pose = toolpath.GetPose(tValue);
            }
            // DA.SetData("Target", targets[0]);

            if (c_Pose != null)
            {
                // Handle errors
                void SetLogMessages(Robot.Pose Poses, List<string> Log)
                {
                    if (Poses.OverHeadSig) Log.Add("Close to overhead singularity.");
                    if (Poses.WristSing) Log.Add("Close to wrist singularity.");
                    if (Poses.OutOfReach) Log.Add("Target out of range.");
                    if (Poses.OutOfRoation) Log.Add("Joint out of range.");
                }
                List<string> log = new List<string>();
                SetLogMessages(c_Pose, log);

                // Set output
                if (flangeCheck.Checked) DA.SetData("Flange", c_Pose.Flange);
                if (anglesCheck.Checked) DA.SetDataList("Angles", c_Pose.Angles);
                if (speedCheck.Checked) DA.SetData("Speed", c_Pose.Speed.TranslationSpeed);
                if (motionCheck.Checked) DA.SetData("Motion", c_Pose.Target.Method.ToString());
                if (externalCheck.Checked) DA.SetData("External", (double)c_Pose.Target.ExtRot);
                //if (showExternal) DA.SetData("External", (double)c_Pose.Target.ExtLin);

                DA.SetDataList("Log", log);

                // Update and display data
                c_Pose.GetBoundingBox(Transform.Identity);

                if (run) ExpireSolution(true);
            }
        }

        #region Variables
        public override bool IsPreviewCapable => true;

        // Global variables.
        private DateTime strat = new DateTime();

        private Robot.Pose c_Pose = null;
        private Toolpath toolpath;

        private bool run = false;
        private bool newData = false;


        ToolStripMenuItem timelineOption;
        ToolStripMenuItem flangeCheck;
        ToolStripMenuItem speedCheck;
        ToolStripMenuItem anglesCheck;
        ToolStripMenuItem motionCheck;
        ToolStripMenuItem externalCheck;
        ToolStripMenuItem fullprogramCheck;

        //private string startStopButton = "Start";

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param robot = new Axis.GH_Params.RobotParam();
            pManager.AddParameter(robot, "Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            IGH_Param instruction = new Axis.GH_Params.InstructionParam();
            pManager.AddParameter(instruction, "Instructions", "Instructions", "Robot program instructions", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //IGH_Param target = new Axis.Params.TargetParam();
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
        }

        #endregion IO


        #region Display Pipeline

        // Custom display pipeline
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);
            if (toolpath != null) if( toolpath.IsValid) toolpath.DrawViewportWires(args);

            // Only display line if Mesh is hidden
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded)
            {
                if (c_Pose != null) c_Pose.DrawViewportWires(args);
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            if (c_Pose != null) c_Pose.DrawViewportMeshes(args);
        }

        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, obj_ids);
            for (int i = 0; i < c_Pose.Geometries.Count(); i++)
            {
                var attributes = doc.CreateDefaultAttributes();
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = c_Pose.Colors[i];
                obj_ids.Add(doc.Objects.AddMesh(c_Pose.Geometries[i], attributes));
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, att, obj_ids);
            for (int i = 0; i < c_Pose.Geometries.Count(); i++)
            {
                var attributes = doc.CreateDefaultAttributes();
                if (att != null) attributes = att;
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = c_Pose.Colors[i];
                obj_ids.Add(doc.Objects.AddMesh(c_Pose.Geometries[i], attributes));
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                if (c_Pose != null) box.Union(c_Pose.Boundingbox);
                //if (c_Tool != null) box.Union(c_Tool.Boundingbox);
                //if (c_Target != null) box.Union(c_Target.Boundingbox);
                return box;
            }
        }

        #endregion Display Pipeline

        #region UI

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            //Subscribe to all event handelers
            this.Params.ParameterSourcesChanged += OnParameterSourcesChanged;
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

            //Trigger data collection
            if (param.Name == "Robot" | param.Name == "Instructions") newData = true;

            //Only add value list to the first input
            if (param.Name != "*Timeline") return;

            //Only change value lists
            var extractedItems = param.Sources.Where(p => p.Name == "Number Slider");

            //Set up the number slider
            Grasshopper.Kernel.Special.GH_NumberSlider gH_NumberSlider = Canvas.Component.CreateNumbersilder("Timeline", 0, 1m, 4, 400);

            //The magic
            Canvas.Component.ChangeObjects(extractedItems, gH_NumberSlider);
        }

        private void StartStop(object sender, object e)
        {
            var toggle = (IToggle)sender;
            toggle.State = !toggle.State;
            if (toggle.State)
            {
                run = false;            }
            else
            {
                run = true;
                strat = DateTime.Now;
            }
        }

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[1]
        {
        new Param_Number() { Name = "*Timeline", NickName = "*Timeline", Description = "A timeline slider describing the position in the robot program to simulate. (0 = Beginning, 1 = End)" },
        };

        // Build a list of optional output parameters
        private IGH_Param[] outputParams = new IGH_Param[]
        {
        new Param_Number() { Name = "Speed", NickName = "Speed", Description = "The current speed of the robot in mm/s." },
        new Param_Number() { Name = "Angles", NickName = "Angles", Description = "The current angle values of the robot." },
        new Param_String() { Name = "Motion", NickName = "Motion", Description = "The current motion type of the robot." },
        new Param_Number() { Name = "External", NickName = "External", Description = "The current external axis values as a list." },
        new Param_String() { Name = "Full Error Log", NickName = "Full Error Log", Description = "The full log of all erroro positions" },
        new Param_Point(){ Name = "Error Positions", NickName = "Error Positions", Description = "A list of all the positions where errors are couring" },
        new Param_Plane(){ Name = "Flange", NickName = "Flange", Description = "Output the robot flange" },
        };


        private void timeline_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("TimelineClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddInput(0, inputParams); }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "*Timeline"), true);
            }
            ExpireSolution(true);
        }

        private void speed_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("SpeedClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutput(0, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Speed"), true);
            }
            ExpireSolution(true);
        }

        private void angles_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("AnglesClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutput(1, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Angles"), true);
            }
            ExpireSolution(true);
        }

        private void motion_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("MotionClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutput(2, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Motion"), true);
            }
            ExpireSolution(true);
        }

        private void external_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("ExternalClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutput(3, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "External"), true);
            }
            ExpireSolution(true);
        }

        private void flange_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("FlangeClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutput(6, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Flange"), true);
            }
            ExpireSolution(true);
        }

        private void fullCheck_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("FullCheckClick");
            button.Checked = !button.Checked;

            if (button.Checked) { this.AddOutputs(new[] { 4, 5 }, outputParams); }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Full Error Log"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Error Positions"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("UseTimeline", this.timelineOption.Checked);
            writer.SetBoolean("ShowSpeed", this.speedCheck.Checked);
            writer.SetBoolean("ShowAngles", this.anglesCheck.Checked);
            writer.SetBoolean("ShowMotion", this.motionCheck.Checked);
            writer.SetBoolean("ShowExternal", this.externalCheck.Checked);
            writer.SetBoolean("ShowFlange", this.flangeCheck.Checked);
            writer.SetBoolean("FullProgramCheck", this.fullprogramCheck.Checked);


            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("UseTimeline")) this.timelineOption.Checked = reader.GetBoolean("UseTimeline");
            if(reader.ItemExists("ShowSpeed")) this.speedCheck.Checked = reader.GetBoolean("ShowSpeed");
            if(reader.ItemExists("ShowAngles")) this.anglesCheck.Checked = reader.GetBoolean("ShowAngles");
            if(reader.ItemExists("ShowMotion")) this.motionCheck.Checked = reader.GetBoolean("ShowMotion");
            if(reader.ItemExists("ShowExternal")) this.externalCheck.Checked = reader.GetBoolean("ShowExternal");
            if(reader.ItemExists("ShowFlange")) this.flangeCheck.Checked = reader.GetBoolean("ShowFlange");
            if(reader.ItemExists("FullProgramCheck")) this.fullprogramCheck.Checked = reader.GetBoolean("FullProgramCheck");

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

        /// <summary>
        /// Component settings.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            { return Properties.Icons.Play; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7baedf8e-5efe-4549-b8d5-93a4b9e4a1fd"); }
        }

        #endregion Component Settings
    }
}