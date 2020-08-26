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
    public class InverseKinematics : GH_Component, IGH_VariableParameterComponent
    {
        // Global variables.
        Manipulator m_Robot = null;
        Target m_Target = null;
        Tool m_Tool = null;
        Manipulator.ManipulatorPose m_Pose = null;
        bool m_PoseOut = false;

        public InverseKinematics() : base("Inverse Kinematics", "Kinematics", "Inverse and forward kinematics for a 6 degree of freedom robotic arm, based on Lobster Reloaded by Daniel Piker.", AxisInfo.Plugin, AxisInfo.TabCore)
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
            if (!DA.GetData(1, ref m_Target)) return; // Get the target.
            if (m_Target.Tool != null) m_Tool = m_Target.Tool; // Get the tool from the target.
            m_Robot = m_Robot = (Manipulator)manipulator.Duplicate();

            Manipulator.ManipulatorPose pose = new Manipulator.ManipulatorPose(m_Robot, m_Target);

            // Handle errors
            List<string> log = new List<string>();
            if (pose.OverHeadSig) log.Add("Close to overhead singularity.");
            if (pose.WristSing) log.Add("Close to wrist singularity.");
            if (pose.OutOfReach) log.Add("Target out of range.");
            if (pose.OutOfRoation) log.Add("Joint out of range.");

            if (m_Pose != null) m_Robot.SetPose(m_Pose, checkValidity: true);
            m_Robot.SetPose(pose, checkValidity: true);
            if (m_Robot.CurrentPose.IsValid) m_Pose = m_Robot.CurrentPose;

            Plane flange = (m_Robot.CurrentPose.IsValid)? m_Robot.CurrentPose.Flange: Plane.Unset;
            double[] selectedAngles = m_Robot.CurrentPose.Angles;

            // Set output
            DA.SetData("Flange", flange);
            DA.SetDataList("Angles", selectedAngles);
            DA.SetDataList("Log", log);
            if(m_PoseOut)DA.SetData("Robot Pose", m_Robot.CurrentPose);

            // Update and display data
            m_Robot.UpdatePose();
            m_Robot.GetBoundingBox(Transform.Identity);
        }

        public override void ClearData()
        {
            base.ClearData();
            if (m_Robot != null) m_Robot.ClearCaches();
            //if (m_Tool != null) m_Tool.ClearCaches();
            //if (m_Target != null) m_Target.ClearCaches();
        }

        #region Display Pipeline
        // Custom display pipeline 
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (m_Robot == null) return;
            if (m_Robot.CurrentPose == null) return;
            if (m_Robot.CurrentPose.Colors == null) return;

            var meshColorPair = m_Robot.RobMeshes.Zip(m_Robot.CurrentPose.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPair) args.Display.DrawMeshShaded(pair.Mesh, new DisplayMaterial(pair.Color));
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            if (m_Robot == null) return;
            if (m_Robot.CurrentPose == null) return;
            if (m_Robot.CurrentPose.Colors == null) return;

            var meshColorPair = m_Robot.Geometries.Zip(m_Robot.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPair) args.Display.DrawMeshShaded(pair.Mesh, new DisplayMaterial(pair.Color));
        }
        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, obj_ids);
            for (int i = 0; i < m_Robot.CurrentPose.Geometry.Count(); i++)
            {
                int cID = i;
                if (i >= m_Robot.CurrentPose.Colors.Count()) cID = m_Robot.CurrentPose.Colors.Count() - 1;
                var attributes = doc.CreateDefaultAttributes();
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = m_Robot.CurrentPose.Colors[cID];
                obj_ids.Add(doc.Objects.AddMesh(m_Robot.CurrentPose.Geometry[i], attributes));
            }
        }

        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            base.BakeGeometry(doc, att, obj_ids);
            for (int i = 0; i < m_Robot.CurrentPose.Geometry.Count(); i++)
            {
                int cID = i;
                if (i >= m_Robot.CurrentPose.Colors.Count()) cID = m_Robot.CurrentPose.Colors.Count() - 1;
                var attributes = doc.CreateDefaultAttributes();
                if (att != null) attributes = att;
                attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = m_Robot.CurrentPose.Colors[cID];
                obj_ids.Add(doc.Objects.AddMesh(m_Robot.CurrentPose.Geometry[i], attributes));
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                if (m_Robot != null) box.Union(m_Robot.Boundingbox);
                //if (m_Tool != null) box.Union(m_Tool.Boundingbox);
                //if (m_Target != null) box.Union(m_Target.Boundingbox);
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
            ToolStripMenuItem outputRobot = Menu_AppendItem(menu, "Output the curretn robot pose", poseOut_Click, true, m_PoseOut);
            outputRobot.ToolTipText = "This will provide the robot as a position class";
        }

        private void poseOut_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Output Robot");
            m_PoseOut = !m_PoseOut;

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
            writer.SetBoolean("PoseOut", this.m_PoseOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_PoseOut = reader.GetBoolean("PoseOut");
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
        public override GH_Exposure Exposure => GH_Exposure.primary;
        #endregion
    }
}
