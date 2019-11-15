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
    //merge this class witth Util and update refferences
    class Toolbox
    {
        static public void SetValueList(GH_Document doc,GH_Component comp, int InputIndex, List<KeyValuePair<string, string>> valuePairs, string name)
        {
            if (valuePairs.Count == 0) return;
            doc = doc;
            comp = comp;
            GH_DocumentIO docIO = new GH_DocumentIO();
            docIO.Document = new GH_Document();

            if (docIO.Document == null) return;
            doc.MergeDocument(docIO.Document);

            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent("Create Accent List", objs);
            doc.MergeDocument(docIO.Document);

            doc.ScheduleSolution(10, chanegValuelist);


            void chanegValuelist(GH_Document document)
            {
                
                IList<IGH_Param> sources = comp.Params.Input[InputIndex].Sources;
                int inputs = sources.Count;


                // If nothing has been conected create a new component
                if (inputs == 0)
                {
                    //instantiate  new value list and clear it
                    GH_ValueList vl = new GH_ValueList();
                    vl.ListItems.Clear();
                    vl.NickName = name;
                    vl.Name = name;

                    //Create values for list and populate it
                    for (int i = 0; i < valuePairs.Count; ++i)
                    {
                        var item = new GH_ValueListItem(valuePairs[i].Key, valuePairs[i].Value);
                        vl.ListItems.Add(item);
                    }

                    //Add value list to the document
                    document.AddObject(vl, false, 1);

                    //get the pivot of the "accent" param
                    System.Drawing.PointF currPivot = comp.Params.Input[InputIndex].Attributes.Pivot;
                    //set the pivot of the new object
                    vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 210, currPivot.Y - 11);

                    // Connect to input
                    comp.Params.Input[InputIndex].AddSource(vl);
                }

                // If inputs exist replace the existing ones
                else
                {
                    for (int i = 0; i < inputs; ++i)
                    {
                        if (sources[i].Name == "Value List" | sources[i].Name == name)
                        {
                            //instantiate  new value list and clear it
                            GH_ValueList vl = new GH_ValueList();
                            vl.ListItems.Clear();
                            vl.NickName = name;
                            vl.Name = name;

                            //Create values for list and populate it
                            for (int j = 0; j < valuePairs.Count; ++j)
                            {
                                var item = new GH_ValueListItem(valuePairs[j].Key, valuePairs[j].Value);
                                vl.ListItems.Add(item);
                            }

                            document.AddObject(vl, false, 1);
                            //set the pivot of the new object
                            vl.Attributes.Pivot = sources[i].Attributes.Pivot;
                            
                            var currentSource = sources[i];
                            comp.Params.Input[InputIndex].RemoveSource(sources[i]);

                            currentSource.IsolateObject();
                            document.RemoveObject(currentSource, false);

                            //Connect new vl
                            comp.Params.Input[InputIndex].AddSource(vl);
                        }
                        else
                        {
                            //Do nothing if it dosent mach any of the above
                        }
                    }
                }
            }
        }
    }
    /*
    class DoubelClick : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
    {
        override RespondToMouseDoubleClick()
        {

        }
    }*/
}