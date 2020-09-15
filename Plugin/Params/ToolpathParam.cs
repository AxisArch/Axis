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
    public class ToolpathParam : GH_PersistentParam<Toolpath>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public ToolpathParam()
          : base("Toolpath", "Toolpath", "Axis toolpath type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("59EAD78C-C880-4B65-A26E-8454C05397C3");

        protected override Toolpath InstantiateT()
        {
            return new Toolpath();
        }

        protected override GH_GetterResult Prompt_Singular(ref Toolpath value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default toolpath center point.");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default toolpath.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = new Toolpath(); }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Toolpath> values)
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
            //PersistentData.Append(Toolpath.Default, new GH_Path(0));
            ExpireSolution(true);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Icons.ToolPathParam;

    }
}