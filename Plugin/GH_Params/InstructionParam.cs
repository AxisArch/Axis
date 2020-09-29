using Axis.Types;
using Axis.Kernal;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel.Types;

namespace Axis.GH_Params
{
    public class InstructionParam : GH_PersistentParam<IGH_Goo>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public InstructionParam()
          : base("Robot Instruction", "Instruction", "Axis robot instruction type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("1214409E-41C7-46EA-9899-06327605BB5F");

        protected override IGH_Goo InstantiateT()
        {
            return null;
        }

        protected override GH_GetterResult Prompt_Singular(ref IGH_Goo value)
        {
            return GH_GetterResult.cancel;
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
                    if (goo is Instruction) continue;


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
        protected override System.Drawing.Bitmap Icon => Properties.Icons.RAPID;

    }
}