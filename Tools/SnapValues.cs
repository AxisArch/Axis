using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Tools
{
    public class SnapValues : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Snap;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("12bc81df-b1ae-4788-a571-d3717ea996be"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public SnapValues() : base("Snap Values To List", "Snap", "Snap values to the closest value from a given list.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "Values", "List of values to act on.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Snaps", "Snaps", "List of values to snap to.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "Values", "List of snapped values.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> vals = new List<double>();
            List<double> snaps = new List<double>();

            if (!DA.GetDataList("Values", vals)) return;
            if (!DA.GetDataList("Snaps", snaps)) return;

            List<double> snappedList = Util.SnapValues(vals, snaps);

            DA.SetDataList(0, snappedList);
        }
    }
}
