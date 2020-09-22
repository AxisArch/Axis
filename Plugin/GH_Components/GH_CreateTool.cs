using Axis.Types;
using Axis.Kernal;
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
    /// Create custom robotic end effectors.
    /// </summary>
    public class GH_CreateTool : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky context menu toggles
        private Manufacturer manufacturer = Manufacturer.ABB;

        private bool toolWeight = false;
        private bool declaration = false;
        private bool relTool = false;

        public GH_CreateTool() : base("Tool", "Tool", "Define a custom robot tool object.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Tool name.", GH_ParamAccess.item, "AxisTool");
            pManager.AddPlaneParameter("TCP", "TCP", "Tool Centre Point plane, at end of tool.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddMeshParameter("Mesh", "Mesh", "Tool mesh geometry for kinematic preview.", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param tool = new Axis.GH_Params.ToolParam();
            pManager.AddParameter(tool, "Tool", "Tool", "Axis tool definition.", GH_ParamAccess.item);
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "AxisTool";
            Plane TCP = Plane.WorldXY;
            double weight = 2.5;
            List<Mesh> mesh = new List<Mesh>();
            Vector3d reltoolOffset = new Vector3d(0, 0, 0);

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref TCP)) return;
            if (!DA.GetDataList(2, mesh) && mesh == null) return;

            this.Message = manufacturer.ToString();

            if (toolWeight)
                if (!DA.GetData("Weight", ref weight)) return;
            if (relTool)
                if (!DA.GetData("Offset", ref reltoolOffset)) return;

            // Move TCP for simulation if reltool is used
            if (relTool)
            {
                var moveVector = reltoolOffset;
                moveVector.Transform(Transform.PlaneToPlane(Plane.WorldXY, TCP));
                moveVector.Reverse();
                TCP.Transform(Transform.Translation(moveVector));
            }

            Tool tool = new Tool(name, TCP, weight, mesh, manufacturer, reltoolOffset);

            DA.SetData(0, tool);

            if (declaration)
                DA.SetData("Declaration", tool.Declaration);
        }

        #region UI

        // Build a list of optional input and output parameters
        private IGH_Param[] inputParams = new IGH_Param[2]
        {
            new Param_Number() { Name = "Weight", NickName = "Weight", Description = "The weight of the tool in kilograms. Necessary for accurate motion planning." },
            new Param_Vector() { Name = "Offset", NickName = "Offset", Description = "Relative tool offset in mm." },
        };

        private IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Declaration of the tool in the native manufacturer language." },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem robotManufacturers = Menu_AppendItem(menu, "Manufacturer");
            robotManufacturers.ToolTipText = "Select the robot manufacturer";
            foreach (string name in typeof(Manufacturer).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, manufacturer_Click);

                if (name == this.manufacturer.ToString()) item.Checked = true;
                robotManufacturers.DropDownItems.Add(item);
            }
            ToolStripSeparator seperator = Menu_AppendSeparator(menu);
            ToolStripMenuItem weightOption = Menu_AppendItem(menu, "Define Tool Weight", Weight_Click, true, toolWeight);
            weightOption.ToolTipText = "Add an parameter to define the weight of the tool.";
            ToolStripMenuItem declOption = Menu_AppendItem(menu, "Create Declaration", Declaration_Click, true, declaration);
            declOption.ToolTipText = "If checked, the component will also output the manufacturer-specific tool declaration.";
            ToolStripMenuItem reltoolOption = Menu_AppendItem(menu, "Relative Tool Offset", RelTool_Click, true, relTool);
            reltoolOption.ToolTipText = "If checked, the component will allow a tool offset value.";
        }

        private void manufacturer_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.manufacturer = (Manufacturer)currentItem.Owner.Items.IndexOf(currentItem);
            ExpireSolution(true);
        }

        private void Weight_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Weight");
            toolWeight = !toolWeight;

            // If the option to define the weight of the tool is enabled, add the input.
            if (toolWeight)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Weight"), true);
            }
            ExpireSolution(true);
        }

        private void Declaration_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Declaration");
            declaration = !declaration;

            // If the option to output the declaration is active, add the output param.
            if (declaration)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Declaration"), true);
            }
            ExpireSolution(true);
        }

        private void RelTool_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Relative Tool Offset");
            relTool = !relTool;

            // If the option to define the weight of the tool is enabled, add the input.
            if (relTool)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Offset"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Manufacturer", (int)this.manufacturer);
            writer.SetBoolean("Weight", this.toolWeight);
            writer.SetBoolean("Declaration", this.declaration);
            writer.SetBoolean("Relative Tool Offset", this.relTool);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            this.toolWeight = reader.GetBoolean("Weight");
            this.declaration = reader.GetBoolean("Declaration");
            this.relTool = reader.GetBoolean("Relative Tool Offset");
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
                return Axis.Properties.Icons.Tool;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{ae134b08-ee29-444e-b689-a218ff73379d}"); }
        }

        #endregion Component Settings
    }
}