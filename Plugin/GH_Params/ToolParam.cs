using Axis.Kernal;
using Axis.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Params
{
    public class ToolParam : GH_PersistentParam<IGH_Goo>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public ToolParam()
          : base("Tool", "Tool", "Axis tool type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("E55644AF-9D59-486D-A698-637062C7734D");

        protected override IGH_Goo InstantiateT()
        {
            return ABBTool.Default;
        }

        protected override GH_GetterResult Prompt_Singular(ref IGH_Goo value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default tool center point.");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default tool.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = ABBTool.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }
        }

        protected override GH_GetterResult Prompt_Plural(ref List<IGH_Goo> values)
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
            PersistentData.Append(ABBTool.Default, new GH_Path(0));
            ExpireSolution(true);
        }

        protected override void OnVolatileDataCollected()
        {
            for (int p = 0; p < m_data.PathCount; p++)
            {
                List<IGH_Goo> branch = m_data.Branches[p];
                for (int i = 0; i < branch.Count; i++)
                {
                    IGH_Goo goo = branch[i];

                    //We accept existing nulls.
                    if (goo == null) continue;

                    //We accept colours.
                    if (goo is Tool) continue;


                    //Tough luck, the data is beyond repair. We'll set a runtime error and insert a null.
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                      string.Format("Data of type {0} could not be converted into a Robot", goo.TypeName));
                    branch[i] = null;

                    //As a side-note, we are not using the CastTo methods here on goo. If goo is of some unknown 3rd party type
                    //which knows how to convert itself into a curve then this parameter will not work with that. 
                    //If you want to know how to do this, ask.
                }
            }
        }
        protected override System.Drawing.Bitmap Icon => Properties.Icons.ToolParam;
    }
}