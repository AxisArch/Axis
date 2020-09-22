using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Rhino;
using Rhino.Geometry;
using Rhino.Display;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using Axis.Robot;
using Axis.Targets;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;

namespace Axis.Core
{
    /// <summary>
    /// Core inverse kinematics class.
    /// </summary>
    public class InverseKinematics_Obsolete : GH_Component, IGH_VariableParameterComponent
    {
        // Global variables.
        Manipulator c_Robot = null;
        Target c_Target = null;
        Tool c_Tool = null;
        Manipulator.ManipulatorPose c_Pose = null;
        bool c_PoseOut = false;

        public InverseKinematics_Obsolete() : base("Inverse Kinematics", "Kinematics", "Inverse and forward kinematics for a 6 degree of freedom robotic arm, based on Lobster Reloaded by Daniel Piker.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param Robot = new Axis.Params.RobotParam();
            pManager.AddParameter(Robot, "Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            IGH_Param Target = new Axis.Params.TargetParam();
            pManager.AddParameter(Target, "Target", "Target", "Robotic target for inverse kinematics. Use the simulation component to select a specific target from a toolpath for preview of the kinematic solution.", GH_ParamAccess.item);
            
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Flange", "Flange", "Robot flange position.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angles", "Angles", "Axis angles for forward kinematics.", GH_ParamAccess.list);
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Manipulator manipulator = null;
            if (!DA.GetData(0, ref manipulator))
            {
                manipulator = Manipulator.Default;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No robot system defined, using default");
            }   // Set Robot.
            if (!DA.GetData(1, ref c_Target)) return; // Get the target.
            c_Robot = (Manipulator)manipulator.Duplicate();
            c_Target = (Target)c_Target.Duplicate();
            if (c_Target.Tool != null) c_Tool = c_Target.Tool; // Get the tool from the target.


            Manipulator.ManipulatorPose pose = new Manipulator.ManipulatorPose(c_Robot, c_Target);

            // Handle errors
            List<string> log = new List<string>();
            if (pose.OverHeadSig) log.Add("Close to overhead singularity.");
            if (pose.WristSing) log.Add("Close to wrist singularity.");
            if (pose.OutOfReach) log.Add("Target out of range.");
            if (pose.OutOfRoation) log.Add("Joint out of range.");

            if (c_Pose != null) c_Robot.SetPose(c_Pose, checkValidity: true);
            c_Robot.SetPose(pose, checkValidity: true);
            if (c_Robot.CurrentPose.IsValid) c_Pose = c_Robot.CurrentPose;

            Plane flange = (c_Robot.CurrentPose.IsValid)? c_Robot.CurrentPose.Flange: Plane.Unset;
            double[] selectedAngles = c_Robot.CurrentPose.Angles;

            // Set output
            DA.SetData("Flange", flange);
            DA.SetDataList("Angles", selectedAngles);
            DA.SetDataList("Log", log);
            if(c_PoseOut)DA.SetData("Robot Pose", c_Robot.CurrentPose);


            // Update and display data
            c_Robot.UpdatePose();
            c_Robot.GetBoundingBox(Transform.Identity);

            c_Tool.UpdatePose(c_Robot);
            c_Tool.GetBoundingBox(Transform.Identity);
        }

        public override void ClearData()
        {
            base.ClearData();
            if (c_Robot != null) c_Robot.ClearCaches();
            if (c_Tool != null) c_Tool.ClearCaches();
            if (c_Target != null) c_Target.ClearCaches();
        }

        #region Display Pipeline
        // Custom display pipeline 
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (c_Robot == null) return;
            if (c_Robot.CurrentPose == null) return;
            if (c_Robot.CurrentPose.Colors == null) return;

            var meshColorPair = c_Robot.RobMeshes.Zip(c_Robot.CurrentPose.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPair) args.Display.DrawMeshShaded(pair.Mesh, new DisplayMaterial(pair.Color));
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            if (c_Robot == null) return;
            if (c_Robot.CurrentPose == null) return;
            if (c_Robot.CurrentPose.Colors == null) return;

            var meshColorPairRobot = c_Robot.Geometries.Zip(c_Robot.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPairRobot) args.Display.DrawMeshShaded(pair.Mesh, new DisplayMaterial(pair.Color));

            if (c_Tool == null) return;
            var meshColorPairTool = c_Tool.Geometries.Zip(c_Tool.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPairTool) args.Display.DrawMeshShaded(pair.Mesh, new DisplayMaterial(pair.Color));
        }
        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, obj_ids);
            for (int i = 0; i < c_Robot.CurrentPose.Geometries.Count(); i++)
            {
                var attributes = doc.CreateDefaultAttributes();
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = c_Robot.CurrentPose.Colors[i];
                obj_ids.Add(doc.Objects.AddMesh(c_Robot.CurrentPose.Geometries[i], attributes));
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, att, obj_ids);
            for (int i = 0; i < c_Robot.CurrentPose.Geometries.Count(); i++)
            {
                var attributes = doc.CreateDefaultAttributes();
                if (att != null) attributes = att;
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = c_Robot.CurrentPose.Colors[i];
                obj_ids.Add(doc.Objects.AddMesh(c_Robot.CurrentPose.Geometries[i], attributes));
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                if (c_Robot != null) box.Union(c_Robot.Boundingbox);
                //if (c_Tool != null) box.Union(c_Tool.Boundingbox);
                //if (c_Target != null) box.Union(c_Target.Boundingbox);
                return box;
            }
        }
        #endregion

        #region UI
        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
            new Param_GenericObject(){ Name = "Robot Pose", NickName = "Pose", Description = "The current robot pose", Access = GH_ParamAccess.item },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem outputRobot = Menu_AppendItem(menu, "Output the curretn robot pose", poseOut_Click, true, c_PoseOut);
            outputRobot.ToolTipText = "This will provide the robot as a position class";
        }

        private void poseOut_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Output Robot");
            c_PoseOut = !c_PoseOut;

            ToggleOutput(0);
            //ToggleOutput(1);
            //ToggleOutput(2);

            ExpireSolution(true);
        }

        // Register the new output parameters to our component.
        private void ToggleOutput(int index)
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
            //ExpireSolution(true);
        }
        #endregion

        #region Serialization
        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("PoseOut", this.c_PoseOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.c_PoseOut = reader.GetBoolean("PoseOut");
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

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.IK;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{9079ef8a-31c0-4c5a-8f04-775b9aa21047}"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        #endregion
    }
}
