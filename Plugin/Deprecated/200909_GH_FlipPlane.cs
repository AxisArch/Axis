using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Targets
{
    /// <summary>
    /// Flip a plane to comply with robot programming conventions.
    /// </summary>
    public class GH_FlipPlane_Obsolete : GH_Component
    {
        public GH_FlipPlane_Obsolete() : base("Flip Plane", "Flip", "Flip a plane around it's Y axis.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Input plane to flip around the Y axis.", GH_ParamAccess.list);
        }
        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Flipped plane.", GH_ParamAccess.list);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> targIn = new List<Plane>();
            List<Plane> targOut = new List<Plane>();

            if (!DA.GetDataList<Plane>(0, targIn)) return;

            for (int i = 0; i < targIn.Count; i++)
            {
                Plane flipTarg = new Plane(targIn[i].Origin, -targIn[i].XAxis, targIn[i].YAxis);
                targOut.Add(flipTarg);
            }            

            DA.SetDataList(0, targOut);         
        }

        /// <summary>
        /// Flip the target using the utility method.
        /// </summary>
        /// <param name="targ"></param>
        /// <returns></returns>
        private Plane FlipTarg(Plane targ)
        {
            targ.Flip();
            return targ;
        }

        #region Component Settings
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Flip;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{d290273f-dad9-44af-9bc9-d2e7154abc90}"); }
        }
        #endregion
    }
}