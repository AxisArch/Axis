using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;
using Axis.Tools;
using Axis.Core;


namespace Axis.Robot
{
    /// <summary>
    /// Define a custom robot tool.
    /// </summary>
    public class CreateTool : GH_Component, IGH_VariableParameterComponent
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Tool;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{ae134b08-ee29-444e-b689-a218ff73379d}"); }
        }

        // Sticky context menu toggles
        bool manufacturer = false;
        bool toolWeight = false;
        bool declaration = false;
        bool relTool = false;

        public CreateTool() : base("Tool", "Tool", "Define a custom robot tool object.", "Axis", "2. Robot")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Tool name.", GH_ParamAccess.item, "AxisTool");
            pManager.AddPlaneParameter("TCP", "TCP", "Tool Centre Point plane, at end of tool.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddMeshParameter("Mesh", "Mesh", "Tool mesh geometry for kinematic preview.", GH_ParamAccess.list);
            //for (int i = 1; i < 2; ++i) { pManager[i].Optional = true; }
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "Tool", "Axis tool definition.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "AxisTool";
            Plane TCP = Plane.WorldXY;
            double weight = 2.5;
            List<Mesh> mesh = new List<Mesh>();
            Vector3d reltoolOffset = new Vector3d(0,0,0);

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref TCP)) return;
            if (!DA.GetDataList(2, mesh) && mesh == null) return;



            if (manufacturer)
            {
                this.Message = "KUKA";
            }
            else
            {
                this.Message = "ABB";
            }

            if (toolWeight)
            {
                if (!DA.GetData("Weight", ref weight)) return;
            }
            if (relTool)
            {
                if (!DA.GetData("RelTool", ref reltoolOffset)) return;
            }

            Tool tool = new Tool(name, TCP, weight, mesh, manufacturer, reltoolOffset);

            DA.SetData(0, tool);

            if (declaration)
            {
                DA.SetData("Declaration", tool.Declaration);
            }            
        }

        // Build a list of optional input and output parameters
        IGH_Param[] inputParams = new IGH_Param[2]
        {
            new Param_Number() { Name = "Weight", NickName = "Weight", Description = "The weight of the tool in kilograms. Necessary for accurate motion planning." },
            new Param_Vector() { Name = "RelTool", NickName = "RelTool", Description = "Relative tool offset." },
        };

        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Declaration of the tool in the native manufacturer language." },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem kukaOption = Menu_AppendItem(menu, "KUKA Tool", KUKA_Click, true, manufacturer);
            kukaOption.ToolTipText = "Create a KUKA-formatted tool declaration.";
            ToolStripMenuItem weightOption = Menu_AppendItem(menu, "Define Tool Weight", Weight_Click, true, toolWeight);
            weightOption.ToolTipText = "Add an parameter to define the weight of the tool.";
            ToolStripMenuItem declOption = Menu_AppendItem(menu, "Create Declaration", Declaration_Click, true, declaration);
            declOption.ToolTipText = "If checked, the component will also output the manufacturer-specific tool declaration.";
            ToolStripMenuItem reltoolOption = Menu_AppendItem(menu, "Relative Tool Offset", RelTool_Click, true, relTool);
            reltoolOption.ToolTipText = "If checked, the component will allow a tool offset value.";
        }

        private void KUKA_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("KukaTool");
            manufacturer = !manufacturer;
        }

        private void Weight_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Weight");
            toolWeight = !toolWeight;

            // If the option to define the weight of the tool is enabled, add the input.
            if (toolWeight)
            {
                AddInput(0);
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
                AddOutput(0);
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
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "RelTool"), true);
            }
            ExpireSolution(true);
        }

        // Register the new input parameters to our component.
        private void AddInput(int index)
        {
            IGH_Param parameter = inputParams[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(inputParams, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
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
            writer.SetBoolean("KukaTool", this.manufacturer);
            writer.SetBoolean("Weight", this.toolWeight);
            writer.SetBoolean("Declaration", this.declaration);
            writer.SetBoolean("Relative Tool Offset", this.relTool);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.manufacturer = reader.GetBoolean("KukaTool");
            this.toolWeight = reader.GetBoolean("Weight");
            this.declaration = reader.GetBoolean("Declaration");
            this.relTool = reader.GetBoolean("Relative Tool Offset");
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
