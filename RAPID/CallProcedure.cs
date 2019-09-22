using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.RAPID
{
    public class CallProcedure : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.RAPID;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{9f7e521f-ea12-4699-b73e-a6201c32eff6}"); }
        }

        public CallProcedure() : base("Call Procedure", "Call", "Call a custom RAPID procedure.", "Axis", "7. RAPID")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of procedure to call.", GH_ParamAccess.item, "Hello");
            pManager.AddTextParameter("Arguments", "Args", "Optional procedure arguments.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Resultant procedure call.", GH_ParamAccess.list);
        }

        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strName = null;
            List<string> arg = new List<string>();
            List<string> strProc = new List<string>();
            bool args = true;
            
            if (!DA.GetData(0, ref strName)) return;
            if (!DA.GetDataList(1, arg)) args = false;

            if (args)
            {
                for (int i = 0; i < arg.Count; i++)
                {
                    string proc = strName + "(" + arg[i] + ")" + ";";
                    strProc.Add(proc);
                }
            }
            else
            {
                string proc = strName + ";";
                strProc.Add(proc);
            }

            DA.SetDataList(0, strProc);
        }
    }
}
 