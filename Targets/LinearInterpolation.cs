using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Targets
{
    public class LinearInterpolation : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Lerp;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("2c5e04ac-7638-46ac-9871-718eeaeff764"); }
        }

        public LinearInterpolation() : base("Lerp", "Lerp", "Quaternion linear interpolation between two robot targets, based on the Robots plugin (https://github.com/visose/Robots) and Lobster Reloaded by Daniel Piker.", "Axis", "3. Targets")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Start", "Start", "First target for linear interpolation.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("End", "End", "Second target for linear interpolation.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Steps", "Steps", "Number of steps between 0.0 and 1.0 for target evaluation.", GH_ParamAccess.item, 10);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Targets", "Targets", "Interpolated planes.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> targsA = new List<Plane>();
            List<Plane> targsB = new List<Plane>();
            int count = 10;

            List<Plane> targets = new List<Plane>();
            List<double> intervals = new List<double>();

            if (!DA.GetDataList(0, targsA)) return;
            if (!DA.GetDataList(1, targsB)) return;
            if (!DA.GetData(2, ref count)) return;
            
            double max = 1.000;
            double step = max / count;
            double increment = 0.000;

            for (int i = count; i > 0; i--)
            {
                intervals.Add(0 + increment);
                increment = increment + step;
            }

            for (int i = 0; i < targsA.Count; i++)
            {
                foreach (double t in intervals)
                {
                    Plane targ = Util.Lerp(targsA[i], targsB[i], t, 0, 1);
                    targets.Add(targ);
                }
            }

            Plane endTarg = targsB[targsB.Count - 1];
            targets.Add(endTarg);

            DA.SetDataList(0, targets);
        }

    }
}
 