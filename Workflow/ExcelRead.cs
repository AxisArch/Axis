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
    public class ExcelRead : GH_Component
    {

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.ExcelRead;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("f4213172-d1d5-43bc-b59f-e23e7bdeaa81"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        List<string> log = new List<string>();

        public ExcelRead() : base("Excel Read", "Read", "Read an Excel file from a path.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "Directory to create the Excel file in.", GH_ParamAccess.item, Environment.SpecialFolder.MyDocuments.ToString() + @"\");
            pManager.AddTextParameter("Sheet", "Sheet", "Sheet name to read.", GH_ParamAccess.item, "Sheet1");
            pManager.AddBooleanParameter("Run", "Run", "Read the Excel file from the specified location.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Program log.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Headers", "Headers", "Excel headers by column.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Values", "Values", "Excel values by row.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = Environment.SpecialFolder.MyDocuments.ToString() + @"\";
            string sheet = "Sheet1";
            bool run = false;

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref sheet)) return;
            if (!DA.GetData(2, ref run)) return;

            // Uses the GH_Structure instead of DataTree<T> inside a component.
            GH_Structure<IGH_Goo> headers = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> values = new GH_Structure<IGH_Goo>();

            if (run)
            {
                try
                {
                    System.IO.FileInfo file = new System.IO.FileInfo(path);
                    var package = new ExcelPackage(file);

                    ExcelWorksheet workSheet = package.Workbook.Worksheets[sheet];

                    for (int j = workSheet.Dimension.Start.Column; j <= workSheet.Dimension.End.Column; j++)
                    {
                        GH_Path ghp = new GH_Path(j);
                        IGH_Goo obj = GH_Convert.ToGoo(workSheet.Cells[1, j].Value);
                        headers.EnsurePath(ghp);
                        headers.Append(obj, ghp);
                    }

                    for (int i = workSheet.Dimension.Start.Row + 1; i <= workSheet.Dimension.End.Row; i++)
                    {
                        for (int j = workSheet.Dimension.Start.Column; j <= workSheet.Dimension.End.Column; j++)
                        {
                            GH_Path ghp = new GH_Path(j);
                            GH_String cellValue = new GH_String();
                            // Excel does not use zero-base indexing, also, we have skipped the headers.
                            IGH_Goo obj = GH_Convert.ToGoo(workSheet.Cells[i, j].Value);
                            values.EnsurePath(ghp);
                            values.Append(obj, ghp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Add("Read from Excel failed: " + ex.Message);
                }
            }

            DA.SetDataList(0, log);
            DA.SetDataTree(1, headers);
            DA.SetDataTree(2, values);
        }
    }
}