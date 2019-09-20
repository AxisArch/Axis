using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Axis.Online
{
    class Toolbox
    {
        public void SetValueList(int InputIndex, List<KeyValuePair<string, string>> valuePairs, string name)
        {
            GH_DocumentIO docIO = new GH_DocumentIO();
            docIO.Document = new GH_Document();

            List<IGH_Param> inputsToRemove = new List<IGH_Param>();

            //instantiate  new value list and clear it
            GH_ValueList vl = new GH_ValueList();
            vl.ListItems.Clear();
            vl.NickName = name;
            vl.Name = name;

            GH_Document doc = OnPingDocument();
            if (docIO.Document == null) return;
            doc.MergeDocument(docIO.Document);

            //Create values for list and populate it
            for (int i = 0; i < valuePairs.Count; ++i)
            {
                var item = new GH_ValueListItem(valuePairs[i].Key, valuePairs[i].Value);
                vl.ListItems.Add(item);
            }

            // Find out what this is doing and why
            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent("Create Accent List", objs);
            doc.MergeDocument(docIO.Document);

            doc.ScheduleSolution(10, chanegValuelist);

            /// <sample>
            /*
                    //Create or replace input
                    if (Params.Input[3].Sources.Count == 0)
                    {
                        // place the object
                        docIO.Document.AddObject(vl, false, 1);

                        //get the pivot of the "accent" param
                        System.Drawing.PointF currPivot = Params.Input[3].Attributes.Pivot;
                        //set the pivot of the new object
                        vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 210, currPivot.Y - 11);
                        Params.Input[3].AddSource(vl);
                    }
                    else
                    {
                        IList<IGH_Param> sources = this.Params.Input[3].Sources;
                        for (int i = 0; i < sources.Count; ++i)
                        {
                            if (sources[i].Name == "Value List" | sources[i].Name == "Controller")
                            {
                                //get the pivot of the "source" value list
                                System.Drawing.PointF currPivot = sources[i].Attributes.Pivot;
                                //set the pivot of the new object
                                vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X, currPivot.Y);
                                delInputs.Add(sources[i]);
                            }
                        }

                    }
            */

            void chanegValuelist(GH_Document doc)
            {
                /// <sample>
                /*
                if (delInputs != null && delInputs.Count > 0)
                {
                    doc.AddObject(vl, false, 1);

                    for (int i = 0; i < delInputs.Count; ++i)
                    {
                        Params.Input[3].RemoveSource(delInputs[i]);
                        delInputs[i].IsolateObject();
                        doc.RemoveObject(delInputs[i], false);
                        doc.AddObject(vl, false, 1);
                    }
                    Params.Input[3].AddSource(vl);
                }

                if (false)
                {

                }


                delInputs.Clear();
                */
            }
        }
    }
}