using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Tools
{
    public class QuatToPlane : GH_Component
    {
        public QuatToPlane() : base("QuatToPlane", "QuatToPlane", "Convert a point and a quaternion back to a Rhino plane.", "Axis", "Utilities")
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{c834c84f-e010-4a51-a4a6-06b16257538c}"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Point", "Plane location / TCP", GH_ParamAccess.item);
            pManager.AddNumberParameter("Quaternion", "Quaternion", "Rotation as four-component quaternion string.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Output plane.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d inPoint = new Point3d();
            List<double> inQuat = new List<double>();

            if (!DA.GetData(0, ref inPoint)) return;
            if (!DA.GetDataList(1, inQuat)) return;

            Quaternion quat = new Quaternion(inQuat[0], inQuat[1], inQuat[2], inQuat[3]);
            Plane outPlane = Util.QuaternionToPlane(inPoint, quat);

            DA.SetData(0, outPlane);
        }
    }
}