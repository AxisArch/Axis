using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Axis.Params;
using Axis.Core;
using Axis.Targets;
using GH_IO.Serialization;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Axis.Params
{
    public class ExtValParam : GH_PersistentParam<ExtVal>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.
        public ExtValParam()
          : base("Extenal axis value", "ExtVal", "Axis external axis value type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabCore)
        { }

        public override Guid ComponentGuid => new Guid("717ED30D-25A6-4D67-BA50-41B3746DA095");

        protected override ExtVal InstantiateT()
        {
            return ExtVal.Default;
        }
        protected override GH_GetterResult Prompt_Singular(ref ExtVal value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default robot center point.");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default robot.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = ExtVal.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<ExtVal> values)
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
            PersistentData.Append(ExtVal.Default, new GH_Path(0));
            ExpireSolution(true);
        }
    }
}
