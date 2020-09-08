using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Axis.Geometry
{
    public class PlaneOrientations : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PlaneOrientations()
          : base("Plane Orientations", "Plane Orientations",
              "This porvides accsess to different conversion methods for plain oriemtations, such as Quaternions and Euler angles",
              AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        #region IO
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        #endregion 

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        #region Display Pipeline
        #endregion

        #region UI

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[3]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_String() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
            new Param_Point() { Name = "Point", NickName = "Point", Description = "", Access = GH_ParamAccess.item },
        };

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[2]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_String() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
        };

        #endregion

        #region Serialization
        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8128e6cf-4b8d-4395-b941-1cfa734cab1a"); }
        }
        #endregion
    }
}