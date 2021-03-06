﻿using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Define a custom robot object.
    /// </summary>
    public class CreateRobot : Axis_Component, IGH_VariableParameterComponent
    {
        public CreateRobot() : base("Robot", "Robot", "Create a kinematic model of a custom robot.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
            IsMutiManufacture = true;

            PoseOut = new ToolStripMenuItem("Output Start Pose", null, pose_Click) 
            {
                ToolTipText = "Output the starting pose meshes of the robot as defined in the robot object."
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                PoseOut,
            };
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

            this.Message = this.Manufacturer.ToString();


            Robot robot = new Abb6DOFRobot(inPlanes.ToArray(), inMin, inMax, inMeshes, inBase, indices);
            //robot.SetPose();

            DA.SetData(0, robot);

            if (PoseOut.Checked)
            {
                DA.SetDataList("Pose", robot.GetPose().Geometries);
            }
        }

        #region Variables
        ToolStripMenuItem PoseOut;
        #endregion Variables

        #region IO

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
            IGH_Param robot = new Axis.GH_Params.RobotParam();
            pManager.AddParameter(robot, "Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
        }

        #endregion IO

        #region UI

        // Build a list of optional input and output parameters
        private IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_Mesh() { Name = "Pose", NickName = "Pose", Description = "A list of robot mesh geometry, as defined in the robot object.", Access = GH_ParamAccess.list },
        };


        private void pose_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("Pose");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Pose"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Manufacturer", (int)this.Manufacturer);
            writer.SetBoolean("Pose", this.PoseOut.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("Manufacturer")) this.Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            if(reader.ItemExists("StartPose")) this.PoseOut.Checked = reader.GetBoolean("Pose");
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

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Robot_3;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{26289bd0-15cc-408f-af2d-5a87ea81cb18}"); }
        }

        #endregion Component Settings
    }
}