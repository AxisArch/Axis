using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Targets
{
    public class FlipPlane : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Flip;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{d290273f-dad9-44af-9bc9-d2e7154abc90}"); }
        }

        public FlipPlane() : base("Flip Plane", "Flip", "Flip a plane around it's Y axis.", "Axis", "3. Targets")
        {
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Input plane to flip around the Y axis.", GH_ParamAccess.list);
        }
        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Flipped plane.", GH_ParamAccess.list);
        }
        
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

        private Plane FlipTarg(Plane targ)
        {
            targ.Flip();
            return targ;
        }       
    }
}