using Axis.Core;
using Axis.Targets;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Axis.Params
{
    public class MotionTypeParam : GH_PersistentParam<Enum>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public MotionTypeParam()
          : base("MotionType", "MotionType", "Axis target motion type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabCore)
        { }

        public override Guid ComponentGuid => new Guid("6635130E-710A-41A4-BF6B-25F98D9C7917");

        protected override Enum InstantiateT()
        {
            return MotionType.Linear;
        }

        protected override GH_GetterResult Prompt_Singular(ref Enum value)
        {
            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default motion type.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = MotionType.Linear; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Enum> values)
        {
            return GH_GetterResult.cancel;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Set the default value", SetDefaultHandler, SourceCount == 0);
            base.AppendAdditionalMenuItems(menu);
        }

        private void SetDefaultHandler(object sender, EventArgs e)
        {
            PersistentData.Clear();
            PersistentData.Append(MotionType.Linear, new GH_Path(0));
            ExpireSolution(true);
        }
    }
}