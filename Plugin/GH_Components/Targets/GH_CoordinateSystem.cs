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
    /// Define a coordinate system / work object.
    /// </summary>
    public class GH_CoordinateSystem : Axis_Component, IGH_VariableParameterComponent
    {

        public GH_CoordinateSystem() : base("Work Object", "WObj", "Create a new work object or robot base from geometry or controller calibration values.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
            dynamicCS = new ToolStripMenuItem("Dynamic Coordinate System", null, dynamic_Click) 
            {
                ToolTipText = "Specify a dynamic coordinate system (rotary or linear axis holds the work object).",
            };
            outputDec = new ToolStripMenuItem("Output Declarations", null, output_Click) 
            {
                ToolTipText = "Output the declarations in RAPID format.",
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                dynamicCS,
                outputDec,
            };
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> names = new List<string>();
            List<Plane> planes = new List<Plane>();

            if (!DA.GetDataList(0, names)) names.Add("WObj0");
            if (!DA.GetDataList(1, planes)) planes.Add(Plane.WorldXY);

            // Dynamic coordinate systems move dependent on external axis values.
            if (dynamicCS.Checked) { if (!DA.GetData("External Axis", ref eAxis)) return; }

            // Declare an empty string to hold our outputs.
            List<string> declarations = new List<string>();
            List<CSystem> cSystems = new List<CSystem>();

            for (int i = 0; i < planes.Count; i++)
            {
                string name = "NoName";
                if (names.Count > 0)
                {
                    if (i < names.Count) { name = names[i]; }
                    else { name = names[names.Count - 1]; }
                }
                else { name = "NoName"; }

                Quaternion quat = Util.QuaternionFromPlane(planes[i]);

                Point3d position = planes[i].Origin;
                double posX = Math.Round(position.X, 3);
                double posY = Math.Round(position.Y, 3);
                double posZ = Math.Round(position.Z, 3);

                // Compose ABB workobject data type declaration.
                string dec = "PERS wobjdata " + name + @" := [FALSE, TRUE, "", [[" + posX.ToString() + ", " + posY.ToString() + ", " + posZ.ToString() + "],[" + Math.Round(quat.A, 6).ToString() + ", " + Math.Round(quat.B, 6).ToString() + ", " + Math.Round(quat.C, 6).ToString() + ", " + Math.Round(quat.D, 6).ToString() + "]],[0, 0, 0],[1, 0, 0, 0]]];";
                declarations.Add(dec);

                CSystem cSys = new CSystem(name, planes[i], dynamicCS.Checked, eAxis);
                cSystems.Add(cSys);
            }

            DA.SetDataList(0, cSystems);

            m_cSystems = cSystems;
            List<Point3d> points = new List<Point3d>();
            foreach (CSystem c in m_cSystems) points.Add(c.CSPlane.Origin);
            m_bBox = new BoundingBox(points);

            if (outputDec.Checked)
            {
                DA.SetDataList("Dec", declarations);
            }
        }
        public override void ClearData()
        {
            base.ClearData();
            m_cSystems.Clear();
            m_bBox = BoundingBox.Unset;
        }

        #region Variables
        private Plane eAxis = Plane.WorldXY;
        private List<CSystem> m_cSystems = new List<CSystem>();
        private BoundingBox m_bBox = new BoundingBox();

        // Optional context menu toggles
        ToolStripMenuItem dynamicCS;
        ToolStripMenuItem outputDec;

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of the coordinate system.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "Plane", "Plane to prescribe coordinate system.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param csystem = new Axis.GH_Params.CSystemParam();
            pManager.AddParameter(csystem, "Wobj", "Wobj", "Work object coordinate system.", GH_ParamAccess.list);
        }

        #endregion IO

        #region Display

        // Custom preview options for the component.
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);
            foreach (CSystem c in m_cSystems) c.DrawViewportWires(args);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
        }

        #endregion Display

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[1]
        {
        new Param_Plane() { Name = "External Axis", NickName = "eAxis", Description = "A plane that discribes the location of the external axis.", Access = GH_ParamAccess.item},
        };

        // Build a list of optional output parameters
        private IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Dec", NickName = "Dec", Description = "A list of declarations in RAPID." }
        };


        private void dynamic_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("CSDynClick");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "External Axis"), true);
            }
            ExpireSolution(true);
        }

        private void output_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("CSDecClick");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Dec"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("CSDyn", this.dynamicCS.Checked);
            writer.SetBoolean("CSDec", this.outputDec.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if (reader.ItemExists("CSDyn")) this.dynamicCS.Checked = reader.GetBoolean("CSDyn");
            if (reader.ItemExists("CSDec")) this.outputDec.Checked = reader.GetBoolean("CSDec");
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

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.CSystem;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{b11f15a4-c0dd-43d6-aecd-d87d2fb05664}"); }
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        #endregion Component Settings
    }
}