using System;
using System.Collections.Generic;
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

    public class InverseKinematics : GH_Component, IGH_VariableParameterComponent
    {

        Manipulator m_Robot = null;
        Target m_Target = null;
        Tool m_Tool = null;


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

        public InverseKinematics() : base("Inverse Kinematics", "Kinematics", "Inverse and forward kinematics for a 6 degree of freedom robotic arm, based on Lobster Reloaded by Daniel Piker.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param Robot = new Axis.Params.Param_Manipulator();
            pManager.AddParameter(Robot, "Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Target", "Target", "Robotic target for inverse kinematics. Use the simulation component to select a specific target from a toolpath for preview of the kinematic solution.", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Flange", "Flange", "Robot flange position.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angles", "Angles", "Axis angles for forward kinematics.", GH_ParamAccess.list);
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Mesh", "Mesh", "Mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Manipulator manipulator = null;
            if (!DA.GetData(0, ref manipulator))
            {
                manipulator = Manipulator.Default;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No robot system defined, using default");
            }  //Set Robot 
            if (!DA.GetData(1, ref m_Target)) return; //Get the target
            if (m_Target.Tool != null) m_Tool = m_Target.Tool; //Get the tool from the target

            //Only Update Robot if it changes
            m_Robot = (Manipulator)manipulator.Duplicate();

            Manipulator.ManipulatorPose pose = new Manipulator.ManipulatorPose(m_Robot, m_Target);

            // Handle errors
            List<string> log = new List<string>();
            if (pose.OverHeadSig) log.Add("Close to overhead singularity.");
            if (pose.WristSing) log.Add("Close to wrist singularity.");
            if (pose.OutOfReach) log.Add("Target out of range.");
            if (pose.OutOfRoation) log.Add("Joint out of range.");

            m_Robot.SetPose(pose, validetyCheck: true);

            Plane flange = m_Robot.CurrentPose.Flange;
            double[] selectedAngles = m_Robot.CurrentPose.Angles;

            // Set output
            DA.SetData("Flange", flange);
            DA.SetDataList("Angles", selectedAngles);
            DA.SetDataList("Log", log);
            DA.SetData("Mesh", m_Robot);

        }



        //Component UI

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[3]
        {
            new Param_Mesh(){  Name = "Meshes", NickName = "Meshes", Description = "Transformed robot geometry as list.", Access = GH_ParamAccess.list},
            new Param_Colour() { Name = "Colour", NickName = "Colour", Description = "Preview indication colours.", Access = GH_ParamAccess.list },
            new Param_Mesh() { Name = "Tool", NickName = "Tool", Description = "Tool mesh as list.", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            //ToolStripMenuItem outputRobot = Menu_AppendItem(menu, "Output the robot geometry", robOut_Click, true, robOut);
            //outputRobot.ToolTipText = "This will provide the robot as geometry";
        }

        private void robOut_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Output Robot");
            //robOut = !robOut;

            ToggleOutput(0);
            ToggleOutput(1);
            ToggleOutput(2);

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

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            //writer.SetBoolean("OutputRobot", this.robOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            //this.robOut = reader.GetBoolean("OutputRobot");
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
