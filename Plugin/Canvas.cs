using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

/// <summary>
/// This namespace provides functions for canvas manipulation in Grasshopper
/// </summary
namespace Canvas
{
    /// <summary>
    /// This class provides functions for components
    /// </summary>
    internal class Component
    {
        static public void SetValueList(GH_Document doc, GH_Component comp, int InputIndex, List<KeyValuePair<string, string>> valuePairs, string name)
        {
            if (valuePairs.Count == 0) return;

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

        public static void ChangeObjects(IEnumerable<IGH_Param> items, IGH_Param newObject)
        {
            foreach (IGH_Param item in items)
            {
                //get the input it is connected to
                if (item.Recipients.Count == 0) return;
                var parrent = item.Recipients[0];

                GH_DocumentIO docIO = new GH_DocumentIO();
                docIO.Document = new GH_Document();

                //get active GH doc
                GH_Document doc = item.OnPingDocument();
                if (doc == null) return;
                if (docIO.Document == null) return;

                Component.AddObject(docIO, newObject, parrent, item.Attributes.Pivot);
                Component.MergeDocuments(docIO, doc, $"Create {newObject.Name}");

                doc.RemoveObject(item, false);
                parrent.AddSource(newObject);
            }
        }

        public static GH_ValueList CreateValueList(string name, Dictionary<string, string> valuePairs)
        {
            //initialize object
            Grasshopper.Kernel.Special.GH_ValueList vl = new Grasshopper.Kernel.Special.GH_ValueList();
            //clear default contents
            vl.ListItems.Clear();

            //set component nickname
            vl.NickName = name;
            vl.Name = name;

            foreach (KeyValuePair<string, string> entety in valuePairs)
            {
                GH_ValueListItem vi = new GH_ValueListItem(entety.Key, entety.Value);
                vl.ListItems.Add(vi);
            }

            return vl;
        }

        public static GH_NumberSlider CreateNumbersilder(string name, decimal min, decimal max, int precision = 0, int length = 174)
        {
            var nS = new GH_NumberSlider();
            nS.ClearData();

            //Naming
            nS.Name = name;
            nS.NickName = name;

            nS.Slider.Minimum = min;
            nS.Slider.Maximum = max;

            nS.Slider.DecimalPlaces = Axis.Util.LimitToRange(precision, 0, 12);

            if (precision == 0)
                nS.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Integer;
            else
                nS.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Float;

            nS.CreateAttributes();
            var bounds = nS.Attributes.Bounds;
            bounds.Width = length;
            nS.Attributes.Bounds = bounds;

            nS.SetSliderValue(min);
            return nS;
        }

        // private methods to magee the placement of ne objects
        private static void AddObject(GH_DocumentIO docIO, IGH_Param Object, IGH_Param param, PointF location = new PointF())
        {
            // place the object
            docIO.Document.AddObject(Object, false, 1);

            //get the pivot of the "accent" param
            System.Drawing.PointF currPivot = param.Attributes.Pivot;

            if (location == new PointF()) Object.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 120, currPivot.Y - 11);
            //set the pivot of the new object
            else Object.Attributes.Pivot = location;
        }

        private static void MergeDocuments(GH_DocumentIO docIO, GH_Document doc, string name = "Merge")
        {
            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent(name, objs);
            doc.MergeDocument(docIO.Document);
            //doc.ScheduleSolution(10);
        }

        #region Display Methods

        static public void DisplayPlane(Plane plane, IGH_PreviewArgs args, double sizeLine = 70, double sizeArrow = 30, int thickness = 3)
        {
            args.Display.DrawLineArrow(
                new Line(plane.Origin, plane.XAxis, sizeLine),
                Axis.Styles.Pink,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(plane.Origin, plane.YAxis, sizeLine),
                Axis.Styles.LightBlue,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(plane.Origin, plane.ZAxis, sizeLine),
                Axis.Styles.LightGrey,
                thickness,
                sizeArrow);
        }

        static public void DisplayRobotLines(Axis.Types.Abb6DOFRobot robot, IGH_PreviewArgs args, int thickness = 3)
        {
            //List<Point3d> points = new List<Point3d>();
            //foreach (Plane p in robot.ikPlanes) { points.Add(p.Origin); }
            //
            //Polyline pLine = new Polyline(points);
            //
            //Line[] lines = pLine.GetSegments();
            //
            //// Draw lines
            //for (int i = 0; i < lines.Length; ++i)
            //{
            //    int cID = i;
            //    if (i >= lines.Length) cID = robot.ikColors.Count - 1;
            //    args.Display.DrawLine(lines[i], robot.ikColors[cID], thickness);
            //}
            //
            ////Draw Sphers
            //
            ////Draw Plane
            //DisplayPlane(robot.ikPlanes[0], args);
        }

        static public void DisplayTool(Axis.Types.Tool tool, IGH_PreviewArgs args)
        {
            //int cC = tool.ikColors.Count;
            //int tC = tool.ikGeometry.Count;
            //
            //for (int i = 0; i < tC; ++i)
            //{
            //    int cID = i;
            //
            //    if (i >= cC) cID = cC - 1;
            //    args.Display.DrawMeshShaded(tool.ikGeometry[i], new DisplayMaterial(tool.ikColors[cID]));
            //}
        }

        static public void DisplayToolLines(Axis.Types.Tool tool, IGH_PreviewArgs args, int thickness = 3)
        {
            //Line line = new Line(tool.ikBase.Origin, tool.ikTCP.Origin);
            //args.Display.DrawLine(line, tool.ikColors[0], thickness);
            //
            ////Draw Plane
            //DisplayPlane(tool.ikTCP, args);
        }

        static public void DisplayToolPath(Axis.Types.Toolpath toolpath, IGH_PreviewArgs args, int thickness = 3, double radius = 2)
        {
            List<Line> lines = new List<Line>();
            List<Color> colors = new List<Color>();

            Dictionary<Line, Color> pairs = lines.Zip(colors, (k, v) => new { k, v })
              .ToDictionary(x => x.k, x => x.v);

            //Draw line segments
            foreach (KeyValuePair<Line, Color> pair in pairs)
            {
                args.Display.DrawLine(pair.Key, pair.Value, thickness);
            }

            List<Point3d> points = new List<Point3d>();
            List<Sphere> spheres = points.Select(p => new Sphere(p, radius)).ToList();

            //Draw Commants
            foreach (Sphere sphere in spheres)
            {
                //IF bitmats are to be used
                //args.Display.DrawSprites();
                args.Display.DrawSphere(sphere, Axis.Styles.LightOrange);
            }
        }

        #endregion Display Methods
    }

    internal static class Menu
    {
        /// <summary>
        /// Uncheck other dropdown menu items
        /// </summary>
        /// <param name="selectedMenuItem"></param>
        public static void UncheckOtherMenuItems(ToolStripMenuItem selectedMenuItem)
        {
            selectedMenuItem.Checked = true;

            // Select the other MenuItens from the ParentMenu(OwnerItens) and unchecked this,
            // The current Linq Expression verify if the item is a real ToolStripMenuItem
            // and if the item is a another ToolStripMenuItem to uncheck this.
            foreach (var ltoolStripMenuItem in (from object
                                                    item in selectedMenuItem.Owner.Items
                                                let ltoolStripMenuItem = item as ToolStripMenuItem
                                                where ltoolStripMenuItem != null
                                                where !item.Equals(selectedMenuItem)
                                                select ltoolStripMenuItem))
                (ltoolStripMenuItem).Checked = false;

            // This line is optional, for show the mainMenu after click
            //selectedMenuItem.Owner.Show();
        }

        // Register the new input parameters to a component.
        public static void AddInput(this IGH_Component gH_Component, int index, IGH_Param[] inputParams)
        {
            IGH_Param parameter = inputParams[index];

            if (gH_Component.Params.Input.Any(x => x.Name == parameter.Name))
                gH_Component.Params.UnregisterInputParameter(gH_Component.Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = gH_Component.Params.Input.Count;
                for (int i = 0; i < gH_Component.Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(inputParams, x => x.Name == gH_Component.Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                gH_Component.Params.RegisterInputParam(parameter, insertIndex);
            }
            gH_Component.Params.OnParametersChanged();
            gH_Component.ExpireSolution(true);
        }

        public static void AddInputs(this IGH_Component gH_Component, int[] indexes, IGH_Param[] inputParams)
        {
            foreach (int index in indexes)
            {
                IGH_Param parameter = inputParams[index];

                if (gH_Component.Params.Input.Any(x => x.Name == parameter.Name))
                    gH_Component.Params.UnregisterInputParameter(gH_Component.Params.Input.First(x => x.Name == parameter.Name), true);
                else
                {
                    int insertIndex = gH_Component.Params.Input.Count;
                    for (int i = 0; i < gH_Component.Params.Input.Count; i++)
                    {
                        int otherIndex = Array.FindIndex(inputParams, x => x.Name == gH_Component.Params.Input[i].Name);
                        if (otherIndex > index)
                        {
                            insertIndex = i;
                            break;
                        }
                    }

                    gH_Component.Params.RegisterInputParam(parameter, insertIndex);
                }
                gH_Component.Params.OnParametersChanged();
            }
            gH_Component.ExpireSolution(true);
        }

        // Register the new output parameters to a component.
        public static void AddOutput(this IGH_Component gH_Component, int index, IGH_Param[] outputParams)
        {
            IGH_Param parameter = outputParams[index];

            if (gH_Component.Params.Output.Any(x => x.Name == parameter.Name))
                gH_Component.Params.UnregisterOutputParameter(gH_Component.Params.Output.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = gH_Component.Params.Output.Count;
                for (int i = 0; i < gH_Component.Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(outputParams, x => x.Name == gH_Component.Params.Output[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                gH_Component.Params.RegisterOutputParam(parameter, insertIndex);
            }
            gH_Component.Params.OnParametersChanged();
            gH_Component.ExpireSolution(true);
        }

        public static void AddOutputs(this IGH_Component gH_Component, int[] indexes, IGH_Param[] outputParams)
        {
            foreach (int index in indexes)
            {
                IGH_Param parameter = outputParams[index];

                if (gH_Component.Params.Output.Any(x => x.Name == parameter.Name))
                    gH_Component.Params.UnregisterOutputParameter(gH_Component.Params.Output.First(x => x.Name == parameter.Name), true);
                else
                {
                    int insertIndex = gH_Component.Params.Output.Count;
                    for (int i = 0; i < gH_Component.Params.Output.Count; i++)
                    {
                        int otherIndex = Array.FindIndex(outputParams, x => x.Name == gH_Component.Params.Output[i].Name);
                        if (otherIndex > index)
                        {
                            insertIndex = i;
                            break;
                        }
                    }

                    gH_Component.Params.RegisterOutputParam(parameter, insertIndex);
                }
                gH_Component.Params.OnParametersChanged();
            }
            gH_Component.ExpireSolution(true);
        }

        public static void RemoveAllInputs(this GH_ComponentParamServer Params)
        {
            int count = Params.Input.Count;
            for (int i = 0; i < count; ++i) Params.UnregisterInputParameter(Params.Input[0]);
        }

        public static void RemoveAllOutputs(this GH_ComponentParamServer Params)
        {
            int count = Params.Output.Count;
            for (int i = 0; i < count; ++i) Params.UnregisterOutputParameter(Params.Output[0]);
        }
    }
}