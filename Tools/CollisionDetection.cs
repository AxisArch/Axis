using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Tools.GH
{
    public class CollisionDetection : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Collision;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("9e92d5d2-e9d7-4cee-8086-8ca340fdf701"); }
        }

        public CollisionDetection() : base("Collision", "!", "Perform collision detection on two sets of meshes.", "Axis", "4. Toolpath")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Set A", "A", "The set of geometry to test for collision against Set B.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Set B", "B", "The set of geometry to test for collision against Set A.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Result", "Result", "Boolean flag for collision detection result. (True if collision detected)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Create a simple boolean intersection between the two mesh sets.
        /// If a curve is returned, we know something is touching, and
        /// we flag a collision.
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> Set_A = new List<Mesh>();
            List<Mesh> Set_B = new List<Mesh>();
            bool flag = false;
            Mesh[] collisions = null;

            if (!DA.GetDataList(0, Set_A)) ;
            if (!DA.GetDataList(1, Set_B)) ;

            // Attempt to boolean intersect the mesh sets.
            collisions = Mesh.CreateBooleanIntersection(Set_A, Set_B);

            // If an intersection (collision) is found, update the flag.
            if (collisions.Length > 0)
            {
                flag = true;
            }
            else
            {
                flag = false;
            }

            DA.SetData(0, flag);
        }
    }
}