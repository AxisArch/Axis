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
using System.Windows.Forms;

namespace Axis.Workflow
{
    public class ExcelWrite : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.ExcelWrite;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("88cc0019-a849-400b-87d9-54cc830d5b9c"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected string exPath = String.Empty;
        bool numbers = false;
        bool clear = false;
        bool hideHeaders = false;
        List<string> log = new List<string>();

        public ExcelWrite() : base("Excel Write", "Write", "Write to an Excel file.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "Path to the existing file.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Sheet", "Sheet", "Sheet name to write to.", GH_ParamAccess.item);
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
            string path = String.Empty;
            string sheet = String.Empty;

            // Uses the GH_Structure instead of DataTree<T> inside a component.
            GH_Structure<IGH_Goo> headers = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> values = new GH_Structure<IGH_Goo>();

            bool run = false;

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref sheet)) return;
            if (!DA.GetDataTree(2, out headers)) return;
            if (!DA.GetDataTree(3, out values)) return;
            if (!DA.GetData(4, ref run)) return;

            DataTable dt = new DataTable();

            // Check that our data matches up.
            if (headers.Branches.Count != values.Branches.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Amount of input row values do not match the amount of columns in the data table.");
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Each value branch should contain as many items as there are header branches.");
            }

            // Warn if the data will be forced to number formatting.
            if (numbers) { this.Message = "Force Numbers"; } else { this.Message = String.Empty; }

            if (run)
            {
                try
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
                        object[] vals = new object[headers.Branches.Count];
                        for (int j = 0; j < values.Branches.Count; j++)
                        {
                            if (values.Branches[j].Count > i) { values.Branches[j][i].CastTo<object>(out vals[j]); }
                            else { vals[j] = null; }
                        }
                        dt.Rows.Add(vals);
                    }

                    System.IO.FileInfo file = new System.IO.FileInfo(path);
                    using (ExcelPackage pck = new ExcelPackage(file))
                    {
                        ExcelWorksheet ws = pck.Workbook.Worksheets[sheet];
                        if (clear && pck.Workbook.Worksheets[sheet] != null)
                        {
                            pck.Workbook.Worksheets.Delete(sheet);
                            ws = pck.Workbook.Worksheets.Add(sheet);
                        }

                        //ws.Cells["A1"].LoadFromDataTable(dt, !hideHeaders);
                        ws.Cells["A1"].LoadFromDataTable(dt, !hideHeaders);

                        int startPos = 2; if (hideHeaders) { startPos = 1; }

                        string maxRange = "A" + startPos.ToString() + ":AA" + maxLen.ToString();
                        if (numbers)
                        {
                            foreach (var cell in ws.Cells[maxRange])
                            {
                                if (cell.Value != null) { cell.Value = Convert.ToDecimal(cell.Value); }
                            }
                        }
                        pck.Save();
                        log.Add("File updated at " + DateTime.Now.ToString());
                    }
                }
                catch (Exception ex)
                {
                    log.Add("Write to Excel failed: " + ex.Message);
                }
            }

            DA.SetDataList(0, log);
            DA.SetData(1, exPath);
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem mClear = Menu_AppendItem(menu, "Clear Sheet", Clear_Click, true, clear);
            mClear.ToolTipText = "Delete the worksheet and create a new one.";
            ToolStripMenuItem mForce = Menu_AppendItem(menu, "Force Numbers", Num_Click, true, numbers);
            mForce.ToolTipText = "Force the formatting of all data cells to general numbers.";
            ToolStripMenuItem mHide = Menu_AppendItem(menu, "Hide Headers", Head_Click, true, hideHeaders);
            mHide.ToolTipText = "Hide the headers of the data to enable easier processing.";
        }

        private void Num_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ForceNum");
            numbers = !numbers;
            ExpireSolution(true);
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ClearSheet");
            clear = !clear;
            ExpireSolution(true);
        }

        private void Head_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Headers");
            hideHeaders = !hideHeaders;
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("ForceNum", this.numbers);
            writer.SetBoolean("ClearSheet", this.clear);
            writer.SetBoolean("Headers", this.hideHeaders);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.numbers = reader.GetBoolean("ForceNum");
            this.clear = reader.GetBoolean("ClearSheet");
            this.hideHeaders = reader.GetBoolean("Headers");
            return base.Read(reader);
        }
    }
}