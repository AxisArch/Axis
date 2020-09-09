using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Geometry
{
    /// <summary>
    /// Reparameterize a surface and return the central frame.
    /// </summary>
    public class SurfaceFrame_Obsolete : GH_Component
    {
        public SurfaceFrame_Obsolete() : base("Surface Frame", "Frame", "Reparamaterize a surface and return the frame at U:0.5, V:0.5.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface to return frame on.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Frame", "F", "Surface frame at U:0.5, V:0.5.", GH_ParamAccess.list);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Surface> surfs = new List<Surface>();

            if (!DA.GetDataList(0, surfs)) return;

            List<Plane> frames = new List<Plane>();
            Plane frame = new Plane();

            int index = 0;
            foreach (Surface srf in surfs)
            {
                srf.SetDomain(0, new Interval(0, 1));
                srf.SetDomain(1, new Interval(0, 1));

                bool success = srf.FrameAt(0.5, 0.5, out frame);
                if (!success)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, String.Format("Could not get valid frame for surface {0}.", index));
                    continue;
                }
                frames.Add(frame);

                index++;
            }

            DA.SetDataList(0, frames);
        }

        #region Component Settings
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.SurfaceFrame;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("ab5216a3-81b1-4c80-a6bb-da54b9a8169a"); }
        }
        #endregion
    }
}