using System;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using Grasshopper;

using Rhino.Geometry;
using Axis.Targets;
using Axis.Core;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;

namespace Axis.Core
{
    // Define a custom robot.
    public class Robot : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky context menu toggles
        bool m_Manufacturer = false;
        bool m_Pose = false;

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Robot;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{26289bd0-15cc-408f-af2d-5a87ea81cb18}"); }
        }

        public Robot() : base("Robot", "Robot", "Create a kinematic model of a custom robot.", "Axis", "2. Robot")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points", "Axis intersection points for kinematics. (Upper base, shoulder, elbow and wrist.)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimums", "Minimums", "Joint minimum angles, as a list of doubles.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Maximums", "Maximums", "Joint maximum angles, as a list of doubles.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "Indices", "Inverse kinematic solution indices.", GH_ParamAccess.list, new List<int>() { 2, 2, 2, 2, 2, 2 });
            pManager.AddMeshParameter("Mesh", "Mesh", "List of robot mesh geometry. [Base + 6 joint meshes]", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base", "Base", "Optional custom robot base plane. [Default = World XY]", GH_ParamAccess.item, Plane.WorldXY);
            pManager[3].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> inPoints = new List<Point3d>();
            List<double> inMin = new List<double>();
            List<double> inMax = new List<double>();
            List<int> indices = new List<int>();
            List<Mesh> inMeshes = new List<Mesh>();
            Plane inBase = Plane.WorldXY;

            if (!DA.GetDataList(0, inPoints)) return;
            if (!DA.GetDataList(1, inMin)) return;
            if (!DA.GetDataList(2, inMax)) return;
            if (!DA.GetDataList(3, indices)) indices = new List<int>() { 2, 2, 2, 2, 2, 2 };
            if (!DA.GetDataList(4, inMeshes)) return;
            if (!DA.GetData(5, ref inBase)) return;


            //Poor mans temporary fix
            var rType = Manufacturer.ABB;
            if (m_Manufacturer) 
            {
                rType = Manufacturer.Kuka;
            }

            Manipulator robot = new Manipulator(rType, inPoints, inMin, inMax, inMeshes, inBase, indices);
            List<Mesh> startPose = robot.StartPose();

            DA.SetData(0, robot);

            if (m_Pose)
            {
                DA.SetDataList("Pose", startPose);
            }
        }

        // Build a list of optional input and output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_Mesh() { Name = "Pose", NickName = "Pose", Description = "A list of robot mesh geometry, as defined in the robot object.", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem KukaRobot = Menu_AppendItem(menu, "Create KUKA Robot", kuka_Click, true, m_Manufacturer);
            KukaRobot.ToolTipText = "Create a KUKA robot. Used to choose the robot language in the post processor.";

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem PoseOut = Menu_AppendItem(menu, "Output Start Pose", pose_Click, true, m_Pose);
            PoseOut.ToolTipText = "Output the starting pose meshes of the robot as defined in the robot object.";
        }

        private void kuka_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("KukaRobot");
            m_Manufacturer = !m_Manufacturer;
            ExpireSolution(true);
        }

        private void pose_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Pose");
            m_Pose = !m_Pose;

            if (m_Pose)
            {
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Pose"), true);
            }
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
            writer.SetBoolean("KukaRobot", this.m_Manufacturer);
            writer.SetBoolean("StartPose", this.m_Pose);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_Manufacturer = reader.GetBoolean("KukaRobot");
            this.m_Pose = reader.GetBoolean("StartPose");
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