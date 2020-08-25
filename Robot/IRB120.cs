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
    public class IRB120 : GH_Component, IGH_VariableParameterComponent
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
            get { return new Guid("{913F60CD-85E9-4582-B017-A2ED7D6BD529}"); }
        }

        public IRB120() : base("IRB120", "IRB120", "Create a kinematic model of an ABB IRB120 robot.", AxisInfo.Plugin, AxisInfo.TabRobot)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "List of robot mesh geometry. [Base + 6 joint meshes]", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> inPoints = new List<Point3d> { new Point3d(0, 0, 290), new Point3d(0, 0, 560), new Point3d(302, 0, 630), new Point3d(374, 0, 630) };
            List<double> inMin = new List<double> { -165, -110, -110, -160, -120, -400 };
            List<double> inMax = new List<double> { 165, 110, 70, 160, 120, 400 };
            List<Mesh> inMeshes = new List<Mesh>();
            Plane inBase = Plane.WorldXY;
            List<int> indices = new List<int> { 2, 2, 2, 2, 2, 2 };

            if (!DA.GetDataList(0, inMeshes)) return;

            this.Message = this.m_Manufacturer.ToString();


            Manipulator robot = new Manipulator(m_Manufacturer, inPoints, inMin, inMax, inMeshes, inBase, indices);
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