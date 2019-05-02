using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Tools
{
    public class StandardDeviation : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.SigmaS;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("843d18ab-ffc4-48e2-b77e-b9bd6dc5a3cb"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public StandardDeviation() : base("Standard Deviation", "SD", "Calculate the standard deviation for a given list of values.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "Values", "A list of numbers to calculate the SD from.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Result", "Result", "Standard deviation of the data.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> data = new List<double>();

            if (!DA.GetDataList(0, data)) return;

            double result = Util.StandardDev(data);

            DA.SetData(0, result);
        }
    }
}