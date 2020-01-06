using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Axis.Targets;

namespace Axis.Core
{
    

    public class TestSimulation : GH_Component
    {

        DateTime strat = new DateTime();
        Toolpath toolpath;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TestSimulation()
          : base("Test Simulation", "TestSim",
              "Description",
              "Axis", "Test")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "Targets", "T", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset", "Reset","",GH_ParamAccess.item );
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Target", "Target", "", GH_ParamAccess.item);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Target> targets = new List<Target>();
            bool run = false;
            bool rest = false;

            DateTime now = DateTime.Now;

            if (!DA.GetDataList("Targets", targets)) return;
            if (!DA.GetData("Run", ref run)) return;
            if (!DA.GetData("Reset", ref rest)) return;

            if ( toolpath== null) toolpath = new Toolpath(targets);

            if (rest)
            {
                strat = DateTime.Now;
                toolpath = new Toolpath(targets);
            }

            if (run) 
            {
                DA.SetData("Target", toolpath.GetTarget(now - strat));
                ExpireSolution(true);
            }
            else DA.SetData("Target", targets[0]);

        }

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
            get { return new Guid("7baedf8e-5efe-4549-b8d5-93a4b9e4a1fd"); }
        }
    }
}