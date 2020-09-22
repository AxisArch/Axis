using Axis.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Params
{
    public class SpeedParam : GH_PersistentParam<Speed>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public SpeedParam()
          : base("Speed", "Target", "Axis target speed type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("F11127E6-F5AE-4E43-BB14-C17B98C931E6");

        protected override Speed InstantiateT()
        {
            return Speed.Default;
        }

        protected override GH_GetterResult Prompt_Singular(ref Speed value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default speed.");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default speed.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = Speed.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Speed> values)
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
            PersistentData.Append(Speed.Default, new GH_Path(0));
            ExpireSolution(true);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Icons.SpeedParam;
    }
}