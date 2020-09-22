using Axis.Types;
using Axis.Kernal;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Params
{
    public class ContollerParam : GH_PersistentParam<Controller>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public ContollerParam()
          : base("Controller", "Controller", "Axis robot controller type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("2E7E0CBA-4F94-4EB2-9B73-998B4732EC0F");

        protected override Controller InstantiateT()
        {
            return null; // new AbbIRC5Contoller();
        }

        protected override GH_GetterResult Prompt_Singular(ref Controller value)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Controller> values)
        {
            return GH_GetterResult.cancel;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Set the default value", SetDefaultHandler, SourceCount == 0);
            base.AppendAdditionalMenuItems(menu);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Icons.Connect;

    }
}