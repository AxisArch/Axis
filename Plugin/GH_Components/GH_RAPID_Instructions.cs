using Axis.Kernal;
using Canvas;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    public class GH_RAPID_Instructions : Axis_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GH_RAPID_Instructions()
          : base("RAPID Instruction", "RAPID Instruction",
              "Creates different Rapid instructions",
              AxisInfo.Plugin, AxisInfo.TabMain)
        {

            ToolStripMenuItem selectState = new ToolStripMenuItem("Select the function") 
            {
                ToolTipText = "Select the function to component should perform",
            };
            foreach (string name in typeof(Opperation).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, state_Click);

                if (name == this.currentState.ToString()) item.Checked = true;
                selectState.DropDownItems.Add(item);
            }

            RegularToolStripItems = new ToolStripMenuItem[] { selectState };
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> instructions = new List<object>();

            if (UpdateIO()) return;

            switch (currentState)
            {
                case Opperation.Acceleration:
                    double accVal = 35; if (!DA.GetData(0, ref accVal)) return;
                    double decVal = 60; if (!DA.GetData(1, ref decVal)) return;
                    instructions.Add(new Axis.Types.Command($"AccSet {accVal.ToString()}, {decVal.ToString()};", Kernal.Manufacturer.ABB));
                    break;

                case Opperation.CallProcedure:
                    string strName = null; if (!DA.GetData(0, ref strName)) return;
                    bool args = true;
                    List<string> arg = new List<string>(); if (!DA.GetDataList(1, arg)) args = false;

                    if (args) foreach (string a in arg) instructions.Add(new Axis.Types.Command($"{strName}({a});", Kernal.Manufacturer.ABB));
                    else instructions.Add(new Axis.Types.Command($"{strName};", Kernal.Manufacturer.ABB));
                    break;

                case Opperation.Comment:
                    string strComm = null;
                    if (!DA.GetData(0, ref strComm)) return;

                    instructions.Add(new Axis.Types.Command($"! {strComm}", Kernal.Manufacturer.ABB));
                    break;

                case Opperation.DefineProcedure:
                    string procName = null; if (!DA.GetData(0, ref procName)) return;
                    string procVariable = null; DA.GetData(1, ref procVariable);
                    List<string> strCommands = new List<string>(); if (!DA.GetDataList(2, strCommands)) return;

                    // Open procedure and build up list of commands.
                    //strProc.Add("!");
                    //strProc.Add("PROC" + " " + procName + "(" + procVariable + ")\n");

                    //strProc.AddRange(strCommands);

                    // Close procedure.
                    //strProc.Add("ENDPROC");
                    //strProc.Add("!");

                    List<Axis.Kernal.Instruction> instr = strCommands.Select(ins => new Axis.Types.Command(ins, Kernal.Manufacturer.ABB) as Kernal.Instruction).ToList();

                    /*
                     * @todo Add procVariable once implemented
                     * @body Pass the procVariable to the procedure once this implements the support therefore
                     */
                    instructions.Add(new Types.Module.Procedure(code: instr, progName: procName));
                    break;

                case Opperation.SetDO:
                    string name = "DO10_1";
                    int status = 0;
                    bool sync = false;

                    if (!DA.GetData(0, ref name)) return;
                    if (!DA.GetData(1, ref status)) return;
                    DA.GetData(2, ref sync);

                    if (sync) instructions.Add(new Axis.Types.Command($"SetDO \\Sync, {name}, {status.ToString()};", Kernal.Manufacturer.ABB));
                    else instructions.Add(new Axis.Types.Command($"SetDO {name}, {status.ToString()};", Kernal.Manufacturer.ABB));
                    break;

                case Opperation.SetVelocity:
                    double velPct = 100; if (!DA.GetData(0, ref velPct)) return;
                    double maxSpeed = 900; if (!DA.GetData(1, ref maxSpeed)) return;

                    instructions.Add(new Axis.Types.Command($"VelSet {velPct.ToString()}, {maxSpeed.ToString()};", Kernal.Manufacturer.ABB));
                    break;

                    //case Opperation.SoftMove:
                    //    // string cssPlane = inPlane
                    //    // string cssAct = "CSSAct ";
                    //    break;
            }
            DA.SetDataList(0, instructions);
        }

        #region Variables
        private Opperation currentState = Opperation.Comment;
        private Opperation previouseState = Opperation.Acceleration;
        #endregion Variables

        #region IO

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        #endregion IO

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[]
        {
            new Param_Number(){ Name = "Acceleration", NickName = "Acc", Description = "Desired robot acceleration value. [As % of default values]" , Access = GH_ParamAccess.item},
            new Param_Number(){ Name = "Deceleration", NickName = "Dec", Description = "Desired robot deceleration value. [As % of default values]" , Access = GH_ParamAccess.item},
            new Param_String(){ Name = "Name", NickName = "Name", Description = "Name of procedure to call." , Access = GH_ParamAccess.item},
            new Param_String(){ Name = "Arguments", NickName = "Args", Description = "Optional procedure arguments." , Access = GH_ParamAccess.list},
            new Param_String(){ Name = "Text", NickName = "Text", Description = "Comment as string." , Access = GH_ParamAccess.item},
            new Param_String(){ Name = "Name", NickName = "Name", Description = "Name of the RAPID procedure." , Access = GH_ParamAccess.item},
            new Param_String(){ Name = "Variable", NickName = "Variable", Description = "(Optional) Name of variable." , Access = GH_ParamAccess.item},
            new Param_String(){ Name = "Commands", NickName = "Commands", Description = "A list of RAPID commands as strings." , Access = GH_ParamAccess.list},
            new Param_String(){ Name = "Output", NickName = "Output", Description = "Name of the digital output to set." , Access = GH_ParamAccess.item},
            new Param_Integer(){ Name = "Status", NickName = "Status", Description = "Status of the signal to set. (1 = On, 0 = Off)" , Access = GH_ParamAccess.item},
            new Param_Boolean(){ Name = "Sync", NickName = "Sync", Description = "Toggel Sync" , Access = GH_ParamAccess.item},
            new Param_Number(){ Name = "Override %", NickName = "Override %", Description = "Desired robot speed as a percentage of programmed speed." , Access = GH_ParamAccess.item},
            new Param_Number(){ Name = "Speed Limit", NickName = "Speed Limit", Description = "Desired robot deceleration value. [As % of default values]" , Access = GH_ParamAccess.item},
            new Param_Integer(){ Name = "Control", NickName = "Control", Description = "Turn the Cartesian Soft Servo option on or off." , Access = GH_ParamAccess.item},
            new Param_Integer(){ Name = "Stiffness", NickName = "ConStiffnesstrol", Description = "Turn the Cartesian Soft Servo option on or off." , Access = GH_ParamAccess.item},
        };

        private IGH_Param[] outputParams = new IGH_Param[]
        {
            new GH_Params.InstructionParam(){ Name = "Instruction", NickName = "Instructionc", Description = "RAPID-formatted robot Instruction." , Access = GH_ParamAccess.list},
            new GH_Params.ProcedureParam(){ Name = "Procedure", NickName = "Procedure", Description = "ABB RAPID procedure" , Access = GH_ParamAccess.list},
        };


        private void state_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Set Function");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.currentState = (Opperation)currentItem.Owner.Items.IndexOf(currentItem);

            UpdateIO();
        }

        private bool UpdateIO()
        {
            bool update = (previouseState != currentState);

            if (!update) return false;

            // Set the input and Out put parameters for the different states
            switch (currentState)
            {
                case Opperation.Acceleration:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 0, 1 }, inputParams);
                    this.AddOutput(0, outputParams);
                    Params.Input[0].Optional = true;
                    Params.Input[1].Optional = true;
                    break;

                case Opperation.CallProcedure:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 2, 3 }, inputParams);
                    this.AddOutput(0, outputParams);
                    Params.Input[1].Optional = true;
                    break;

                case Opperation.Comment:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInput(4, inputParams);
                    this.AddOutput(0, outputParams);
                    break;

                case Opperation.DefineProcedure:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 5, 6, 7 }, inputParams);
                    this.AddOutput(1, outputParams);
                    Params.Input[1].Optional = true;
                    break;

                case Opperation.SetDO:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 8, 9, 10 }, inputParams);
                    this.AddOutput(0, outputParams);
                    Params.Input[0].Optional = true;
                    Params.Input[1].Optional = true;
                    Params.Input[2].Optional = true;
                    break;

                case Opperation.SetVelocity:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 11, 12 }, inputParams);
                    this.AddOutput(0, outputParams);
                    Params.Input[0].Optional = true;
                    Params.Input[1].Optional = true;
                    break;
                    //case Opperation.SoftMove:
                    //    Params.RemoveAllInputs();
                    //    this.AddInputs(new[] { 13,14 }, inputParams);
                    //    Params.Input[0].Optional = true;
                    //    Params.Input[1].Optional = true;
                    //    break;
            }

            previouseState = currentState;
            DestroyIconCache();
            ExpireSolution(true);

            return true;
        }

        #endregion UI

        #region Serialization

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ComponentState", (int)this.currentState);
            writer.SetInt32("PreviouseComponentState", (int)this.previouseState);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            this.currentState = (Opperation)reader.GetInt32("ComponentState");
            this.previouseState = (Opperation)reader.GetInt32("PreviouseComponentState");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        public override GH_Exposure Exposure => GH_Exposure.secondary;

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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                switch (currentState)
                {
                    case Opperation.Acceleration: return Properties.Icons.RAPID;
                    case Opperation.CallProcedure: return Properties.Icons.RAPID;
                    case Opperation.Comment: return Properties.Icons.RAPID;
                    case Opperation.DefineProcedure: return Properties.Icons.RAPID;
                    case Opperation.SetDO: return Properties.Icons.RAPID;
                    case Opperation.SetVelocity: return Properties.Icons.RAPID;
                    //case Opperation.SoftMove: return Properties.Icons.RAPID;
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b189aeb0-da71-457a-a5c9-976fb7d2afce"); }
        }

        #endregion Component Settings

        private enum Opperation
        {
            Acceleration = 0,
            CallProcedure = 1,
            Comment = 2,
            DefineProcedure = 3,
            SetDO = 4,
            SetVelocity = 5,
            //SoftMove = 6,
        }
    }
}