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
    public class Param_Target : GH_PersistentParam<Target>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public Param_Target()
          : base("Axis Target", "Axis Target", "This parampeter will store Axis Targets and their data.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabCore)
        { }

        public override Guid ComponentGuid => new Guid("03DE08A2-D283-4E6D-98D4-07BF9606F34A");

        protected override Target InstantiateT()
        {
            return Target.Default;
        }

        protected override GH_GetterResult Prompt_Singular(ref Target value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default Robot center point");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default Robot");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = Target.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Target> values)
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
            PersistentData.Append(Target.Default, new GH_Path(0));
            ExpireSolution(true);
        }
    }
}