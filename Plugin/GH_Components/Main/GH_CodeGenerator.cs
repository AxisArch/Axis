using Axis.GH_Params;
using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Generate the output code for a robot program.
    /// </summary>
    public class GH_CodeGenerator : AxisLogin_Component, IGH_VariableParameterComponent
    {
        public GH_CodeGenerator() : base("Code Generator", "Code", "Generate manufacturer-specific robot code from a toolpath.", AxisInfo.Plugin, AxisInfo.TabMain)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            IsMutiManufacture = true;

            this.UI_Elements = new Kernal.IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentButton("Export"){ LeftClickAction = Export },
            };

            attr.Update(UI_Elements);

            moduleName = new ToolStripMenuItem("Custom Module Name", null, modName_Click) 
            {
                ToolTipText = "Provide a custom name for the module and overwrite the default."
            };
            declarationsCheck = new ToolStripMenuItem("Custom Declarations", null, declarations_Click) 
            {
                ToolTipText = "Add custom declarations to the headers of the code [zone, tool, speed data etc]."
            };
            overrideCheck = new ToolStripMenuItem("Custom Overrides", null, overrides_Click) 
            {
                ToolTipText = "Provide custom overrides at the beginning of the main program."
            };
            ignore = new ToolStripMenuItem("Ignore Program Length", null, ignoreLen_Click) 
            {
                ToolTipText = "Ignore the length of the program and avoid spliting the main program in subprocedures."
            };

            RegularToolStripItems = new ToolStripItem[]
            {
                moduleName,
                declarationsCheck,
                overrideCheck,
                ignore,
            };
        }

        protected override void SolveInternal(IGH_DataAccess DA)
        {
            program = null;
            filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string strModName = "MainModule";
            string filename = "RobotProgram";

            List<Instruction> instructions = new List<Instruction>();

            List<string> strHeaders = new List<string>();
            List<string> strDeclarations = new List<string>();
            List<string> strProgram = new List<string>();
            List<Module.Procedure> procedures = new List<Module.Procedure>(); //<-- might need to become a more general type
            List<Instruction> overrides = new List<Instruction>();

            string smFileName = "Submodule";
            string dirHome = "RemovableDisk1:"; //  HOME: or RemovableDisk1:

            // Initialize lists to store the robot export code.
            this.Message = this.Manufacturer.ToString();

            if (!DA.GetDataList("Instructions", instructions)) return;
            if (!DA.GetDataList("Procedures", procedures)) ;
            if (!DA.GetData("Path", ref filePath)) ;
            if (!DA.GetData("Filename", ref filename)) ;

            // Get the optional inputs.
            if (moduleName.Checked)
                if (!DA.GetData("Name", ref strModName)) return;
            if (overrideCheck.Checked) DA.GetDataList("Overrides", overrides);
            if (declarationsCheck.Checked) DA.GetDataList("Declarations", strDeclarations);

            switch (Manufacturer)
            {
                case Manufacturer.ABB:
                    var RAPIDprogram = new Module(name: strModName, filename: filename);

                    if (procedures != null) RAPIDprogram.AddRotines(procedures);
                    if (overrideCheck.Checked && overrides != null) RAPIDprogram.AddOverrides(overrides);
                    program = RAPIDprogram;
                    break;

                case Manufacturer.Kuka:
                    program = new KPL();
                    break;

                default:
                    throw new NotImplementedException($"Code generation for {Manufacturer} has not yet been implemented");
            }
            program.SetInstructions(instructions);

            DA.SetDataList(0, program.GetInstructions());
        }

        #region Methods

        private void Export(object sender, object e)
        {
            if (program != null) { if (program.Export(filePath)) Util.AutoClosingMessageBox.Show("Export Successful!", "Export", 1300); }
            else { Util.AutoClosingMessageBox.Show("No program has been created", "Export", 1300); }
        }

        #endregion Methods

        #region Variables

        private Program program;
        private string filePath = Environment.SpecialFolder.Desktop.ToString(); // /Axis/LongCode/ or /

        ToolStripMenuItem moduleName;
        ToolStripMenuItem declarationsCheck;
        ToolStripMenuItem overrideCheck;
        ToolStripMenuItem ignore;

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param instruction = new InstructionParam();
            pManager.AddParameter(instruction, "Instructions", "Instructions", "Robot program as list of instructions.", GH_ParamAccess.list);

            IGH_Param procedures = new ProcedureParam();
            pManager.AddParameter(procedures, "Procedures", "Procedures", "Custom procedures / functions / routines to be appended to program.", GH_ParamAccess.list);

            IGH_Param path = new Param_FilePath();
            pManager.AddParameter(path, "Path", "Path", "File path for code generation.", GH_ParamAccess.item);

            pManager.AddTextParameter("Filename", "Filename", "File name for code generation.", GH_ParamAccess.item, "RobotProgram");
            for (int i = 0; i < 4; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Robot code.", GH_ParamAccess.list);
        }

        #endregion IO

        #region UI

        // Build a list of optional input and output parameters
        private IGH_Param[] inputParams = new IGH_Param[3]
        {
        new Param_String() { Name = "Name", NickName = "Name", Description = "A custom name for the code module." },
        new InstructionParam() { Name = "Overrides", NickName = "Overrides", Description = "Custom override code for insertion into the main program.", Access = GH_ParamAccess.list },
        new Param_String() { Name = "Declarations", NickName = "Declarations", Description = "Add custom declarations to the headers of the code [zone, tool, speed data etc].", Access = GH_ParamAccess.list },
        };


        private void modName_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("ModName");
            button.Checked = !button.Checked;

            // If the option to define the weight of the tool is enabled, add the input.
            if (button.Checked)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Name"), true);
            }
            ExpireSolution(true);
        }

        private void declarations_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("Declarations");
            button.Checked = !button.Checked;

            // If the option to define the weight of the tool is enabled, add the input.
            if (button.Checked)
            {
                this.AddInput(2, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Declarations"), true);
            }
            ExpireSolution(true);
        }

        private void overrides_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("Overrides");
            button.Checked = !button.Checked;

            // If the option to define the weight of the tool is enabled, add the input.
            if (button.Checked)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Overrides"), true);
            }
            ExpireSolution(true);
        }


        private void ignoreLen_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("Ignore Program Length");
            button.Checked = !button.Checked;
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("ModName", this.moduleName.Checked);
            writer.SetBoolean("Declarations", this.declarationsCheck.Checked);
            writer.SetBoolean("Overrides", this.overrideCheck.Checked);
            writer.SetInt32("Manufacturer", (int)this.Manufacturer);;
            writer.SetBoolean("IgnoreLen", this.ignore.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("ModName")) this.moduleName.Checked = reader.GetBoolean("ModName");
            if(reader.ItemExists("Declarations")) this.declarationsCheck.Checked = reader.GetBoolean("Declarations");
            if(reader.ItemExists("Overrides")) this.overrideCheck.Checked = reader.GetBoolean("Overrides");
            if(reader.ItemExists("Manufacturer")) this.Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            if(reader.ItemExists("IgnoreLen")) this.ignore.Checked = reader.GetBoolean("IgnoreLen");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

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
                return Properties.Icons.CodeGen;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{6dccd5dc-520b-482d-bbb2-93607ba5166f}"); }
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        #endregion Component Settings
    }
}