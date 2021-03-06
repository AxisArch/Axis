﻿using Axis.Kernal;
using Axis.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Params
{
    public class MotionTypeParam : GH_PersistentParam<GH_Integer>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public MotionTypeParam()
          : base("MotionType", "MotionType", "Axis target motion type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("6635130E-710A-41A4-BF6B-25F98D9C7917");

        protected override GH_Integer InstantiateT()
        {
            return new GH_Integer((int)MotionType.Linear);
        }

        protected override GH_GetterResult Prompt_Singular(ref GH_Integer value)
        {
            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default motion type.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = new GH_Integer((int)MotionType.Linear); }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_Integer> values)
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
            PersistentData.Append(new GH_Integer((int)MotionType.Linear), new GH_Path(0));
            ExpireSolution(true);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Icons.MovmentParam;
    }
}