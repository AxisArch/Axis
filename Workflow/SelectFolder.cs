using System;
using System.Windows.Forms;

using Grasshopper.Kernel;

namespace Axis.Workflow
{
    public class SelectFolder : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.SelectFolder;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("0b72dc36-531b-4ec9-a568-9ee9bd0b559f"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // Create a global variable to contain the path when the trigger is released.
        string directoryPath = "";

        public SelectFolder() : base("Select Folder", "Select", "Select a folder using the Windows Folder Browser Dialog.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Launch an instance of the Windows Folder Browser Dialog.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "Path to the directory.", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;

            if (!DA.GetData(0, ref run)) return;

            if (run)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    // Return the path if it makes sense.
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        directoryPath = fbd.SelectedPath + @"\";
                    }
                }
            }

            DA.SetData(0, directoryPath);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetString("strPath", this.directoryPath);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.directoryPath = reader.GetString("strPath");
            return base.Read(reader);
        }
    }
}