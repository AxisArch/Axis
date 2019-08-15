using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;

namespace Axis.Targets
{
    public class CoordinateSystem : GH_Component, IGH_VariableParameterComponent
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.CSystem;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{b11f15a4-c0dd-43d6-aecd-d87d2fb05664}"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        
        // Optional context menu toggles
        bool m_dynamicCS = false;
        bool m_outputDeclarations = false;

        public CoordinateSystem() : base("Work Object", "WObj", "Create a new work object or robot base from geometry or controller calibration values.", "Axis", "2. Robot")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of the coordinate system.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "Plane", "Plane to prescribe coordinate system.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Wobj", "Wobj", "Work object coordinate system.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> names = new List<string>();
            List<Plane> planes = new List<Plane>();

            if (!DA.GetDataList(0, names)) names.Add("WObj0");
            if (!DA.GetDataList(1, planes)) planes.Add(Plane.WorldXY);

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

                CSystem cSys = new CSystem(name, planes[i]);
                cSystems.Add(cSys);
            }

            DA.SetDataList(0, cSystems);

            if (m_outputDeclarations)
            {
                DA.SetDataList("Dec", declarations);
            }
        }

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Dec", NickName = "Dec", Description = "A list of declarations in RAPID." }
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            ToolStripMenuItem dynamicCS = Menu_AppendItem(menu, "Dynamic Coordinate System", dynamic_Click, true, m_dynamicCS);
            dynamicCS.ToolTipText = "Specify a dynamic coordinate system (rotary or linear axis holds the work object).";
            ToolStripMenuItem outputDec = Menu_AppendItem(menu, "Output Declarations", output_Click, true, m_outputDeclarations);
            outputDec.ToolTipText = "Output the declarations in RAPID format.";
        }

        private void dynamic_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("CSDynClick");
            m_dynamicCS = !m_dynamicCS;
        }

        private void output_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("CSDecClick");
            m_outputDeclarations = !m_outputDeclarations;

            if (m_outputDeclarations)
            {
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Declarations"), true);
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
            writer.SetBoolean("CSDyn", this.m_dynamicCS);
            writer.SetBoolean("CSDec", this.m_outputDeclarations);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_dynamicCS = reader.GetBoolean("CSDyn");
            this.m_outputDeclarations = reader.GetBoolean("CSDec");
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