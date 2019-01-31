using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using static Axis.Util;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Axis.Workflow
{
    public class WindowsFormsComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public WindowsFormsComponent()
          : base("Console", "Console",
              "Open and write to a console before closing it.",
              "Axis", "7. Workflow")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("N", "N", "List of numbers to test console with.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run the console test.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            List<double> num = new List<double>();

            if (!DA.GetDataList(0, num)) return;
            DA.GetData(1, ref run);

            //NativeMethods.AllocConsole();

            if (run)
            {
                // Start debugging.
                MessageBoxButtons button = MessageBoxButtons.YesNoCancel;
                switch (MessageBox.Show("Message box test.", "My Message Box", button))
                {
                    case DialogResult.Yes: MessageBox.Show("Yes"); break;
                    case DialogResult.No: MessageBox.Show("No"); break;
                    case DialogResult.Cancel: MessageBox.Show("Cancel"); break;
                }
            }

            //NativeMethods.FreeConsole();
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
            get { return new Guid("e3262926-cf87-46aa-81e1-edcbb3460242"); }
        }
    }
}