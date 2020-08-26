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
    /// <summary>
    /// Define a custom robot object.
    /// </summary>
    public class Robot : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky context menu toggles
        Manufacturer m_Manufacturer = Manufacturer.ABB;
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

        public Robot() : base("Robot", "Robot", "Create a kinematic model of a custom robot.", AxisInfo.Plugin, AxisInfo.TabRobot)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "Planes", "Axis rotation planes for kinematics.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimums", "Minimums", "Joint minimum angles, as a list of doubles.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Maximums", "Maximums", "Joint maximum angles, as a list of doubles.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "Indices", "Inverse kinematic solution indices.", GH_ParamAccess.list, new List<int>() { 0, 0, 0, 0, 0, 0 });
            pManager.AddMeshParameter("Mesh", "Mesh", "List of robot mesh geometry. [Base + 6 joint meshes]", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base", "Base", "Optional custom robot base plane. [Default = World XY]", GH_ParamAccess.item, Plane.WorldXY);
            pManager[3].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param robot = new Axis.Params.RobotParam();
            pManager.AddParameter(robot, "Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> inPlanes = new List<Plane>();
            List<double> inMin = new List<double>();
            List<double> inMax = new List<double>();
            List<int> indices = new List<int>();
            List<Mesh> inMeshes = new List<Mesh>();
            Plane inBase = Plane.WorldXY;
            

            if (!DA.GetDataList(0, inPlanes)) return;
            if (!DA.GetDataList(1, inMin)) return;
            if (!DA.GetDataList(2, inMax)) return;
            if (!DA.GetDataList(3, indices)) return;
            if (!DA.GetDataList(4, inMeshes)) return;
            if (!DA.GetData(5, ref inBase)) return;

            this.Message = this.m_Manufacturer.ToString();

            // Create axis planes in relation to robot joint points.
            //List<Plane> axisPlanes = new List<Plane>();
            //List<Plane> tAxisPlanes = new List<Plane>();



            Manipulator robot = new Manipulator(m_Manufacturer, inPlanes.ToArray(), inMin, inMax, inMeshes, inBase, indices);
            //robot.SetPose();

            DA.SetData(0, robot);

            //if (m_Pose)
            //{
            //    DA.SetDataList("Pose", startPose);
            //}
        }

        // Build a list of optional input and output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_Mesh() { Name = "Pose", NickName = "Pose", Description = "A list of robot mesh geometry, as defined in the robot object.", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem robotManufacturers = Menu_AppendItem(menu, "Manufacturer");
            robotManufacturers.ToolTipText = "Select the robot manufacturer";

            foreach (string name in typeof(Manufacturer).GetEnumNames()) 
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, manufacturer_Click);

                if (name == this.m_Manufacturer.ToString()) item.Checked = true;
                robotManufacturers.DropDownItems.Add(item);
            }

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem PoseOut = Menu_AppendItem(menu, "Output Start Pose", pose_Click, true, m_Pose);
            PoseOut.ToolTipText = "Output the starting pose meshes of the robot as defined in the robot object.";
        }
        private void manufacturer_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.m_Manufacturer = (Manufacturer)currentItem.Owner.Items.IndexOf(currentItem);
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

        /// <summary>
        /// Register the new output parameters to our component.
        /// </summary>
        /// <param name="index"></param>
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
        /// <summary>
        /// Uncheck other dropdown menu items
        /// </summary>
        /// <param name="selectedMenuItem"></param>
        

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Manufacturer", (int)this.m_Manufacturer);
            writer.SetBoolean("StartPose", this.m_Pose);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
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