using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using OfficeOpenXml;

namespace Axis.Workflow
{
    public class ExcelCreate : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.ExcelCreate;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("f379dbee-6c7d-4c80-9cda-ceaf8b5c886d"); }

        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected string exPath = String.Empty;
        bool overwrite = false;
        List<string> log = new List<string>();


        public ExcelCreate() : base("Excel Create", "Create", "Create a new isntance of an Excel file.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "Directory to create the Excel file in.", GH_ParamAccess.item, Environment.SpecialFolder.MyDocuments.ToString() + @"\");
            pManager.AddTextParameter("Name", "Name", "Name of the Excel file.", GH_ParamAccess.item, "GH_Data");
            pManager.AddTextParameter("Sheet", "Sheet", "Name of the Excel worksheet.", GH_ParamAccess.item, "Grasshopper");
            pManager.AddGenericParameter("Headers", "Headers", "Headers for column data.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Values", "Values", "Values to store in each row. Data structure must match header branches.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Run", "Run", "Create or overwrite the Excel file at the specified location.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Program log.", GH_ParamAccess.list);
            pManager.AddTextParameter("Path", "Path", "Path to the Excel file.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = Environment.SpecialFolder.MyDocuments.ToString() + @"\";
            string name = "GH_Data";
            string sheet = "Grasshopper";

            // Uses the GH_Structure instead of DataTree<T> inside a component.
            GH_Structure<IGH_Goo> headers = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> values = new GH_Structure<IGH_Goo>();

            bool run = false;

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref name)) return;
            if (!DA.GetData(2, ref sheet)) return;
            if (!DA.GetDataTree(3, out headers)) return;
            if (!DA.GetDataTree(4, out values)) return;
            if (!DA.GetData(5, ref run)) return;

            DataTable dt = new DataTable();
            exPath = path + name + @".xlsx";

            // Check that our data matches up.
            if (headers.Branches.Count != values.Branches.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Amount of input row values do not match the amount of columns in the data table.");
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Each value branch should contain as many items as there are header branches.");
            }

            // Warn if data will be overwritten.
            if (overwrite) { this.Message = "Overwrite"; } else { this.Message = String.Empty; }

            if (run)
            {
                for (int i = 0; i < headers.Branches.Count; i++)
                {
                    // Add the column header.
                    dt.Columns.Add(headers.Branches[i][0].ToString());
                }

                int maxLen = 0;
                // Find the longest column of data.
                for (int i = 0; i < values.Branches.Count; i++)
                {
                    int len = values.Branches[i].Count;
                    if (len > maxLen) { maxLen = len; }
                }

                // Add the contents of the corresponding values branch.
                for (int i = 0; i < maxLen; i++)
                {
                    string[] vals = new string[headers.Branches.Count];
                    for (int j = 0; j < values.Branches.Count; j++)
                    {
                        if (values.Branches[j].Count > i) { vals[j] = values.Branches[j][i].ToString(); }
                        else { vals[j] = null; }
                    }
                    dt.Rows.Add(vals);
                }

                try
                {
                    System.IO.FileInfo file = new System.IO.FileInfo(exPath);
                    if (File.Exists(exPath) && overwrite)
                    {
                        File.Delete(exPath); log.Add("File Exists."
                        + Environment.NewLine + "Overwrite Enabled." + Environment.NewLine + "Deleting File.");
                    }
                    using (ExcelPackage pck = new ExcelPackage(file))
                    {
                        ExcelWorksheet ws = pck.Workbook.Worksheets.Add(sheet);
                        ws.Cells["A1"].LoadFromDataTable(dt, true);
                        pck.Save();
                        log.Add("File updated.");
                    }
                }
                catch (Exception ex)
                {
                    log.Add("Export to Excel failed: " + ex.Message);
                }
            }

            DA.SetDataList(0, log);
            DA.SetData(1, exPath);
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Overwrite File", Overwrite_Click, true, overwrite);
        }

        private void Overwrite_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ExcelCOW");
            overwrite = !overwrite;
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("ExcelCOW", this.overwrite);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.overwrite = reader.GetBoolean("ExcelCOW");
            return base.Read(reader);
        }
    }
}