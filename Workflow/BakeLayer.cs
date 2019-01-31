using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Axis.Workflow
{
    public class BakeLayer : GH_Component, IGH_BakeAwareData
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("86a489f8-3651-4e06-b369-16f338993d78"); }
        }

        // Default behaviour of the component is to overwrite existing objects.
        bool overwrite = true;

        public BakeLayer() : base("Bake Layer", "Bake Layer", "Bake geometry to a specific layer.", "Labrat", "Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Objects", "Objects", "Objects to bake.", GH_ParamAccess.list);
            pManager.AddTextParameter("Layer", "Layer", "Layer to bake objects onto.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Bake", "Bake", "Bake the objects to the specified layer.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ID", "ID", "Unique object identifier.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Debug", "Debug", "Debugging output.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Object> objects = new List<Object>();
            List<string> layers = new List<string>();
            bool bake = false;

            if (!DA.GetDataList(0, objects)) return;
            if (!DA.GetDataList(1, layers)) return;
            if (!DA.GetData(2, ref bake)) return;

            // Specify the active Rhino document.
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            List<Guid> ids = new List<Guid>();

            // Debugging list
            List<int> debug = new List<int>();

            if (bake)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    int layerIndex = -1;
                    if (layers.Count != objects.Count) { i = 0; }

                    Layer layer = doc.Layers.FindName(layers[i]);
                    if (layer == null)
                    {
                        this.Message = "Creating Layer";

                        Layer newLayer = new Layer();
                        newLayer.Name = layers[i];

                        // Add a new layer to the document
                        layerIndex = doc.Layers.Add(newLayer);
                    }
                    else
                    {
                        layerIndex = layer.Index;
                    }

                    GH_ObjectWrapper obj = new GH_ObjectWrapper(objects[i]);
                    ObjectAttributes oAtt = new ObjectAttributes();
                    oAtt.LayerIndex = layerIndex;

                    int tag = obj.GetHashCode();
                    debug.Add(tag);

                    Guid id = new Guid();
                    
                    if (overwrite)
                    {
                        /*
                        if (debug.
                        {

                        }
                        */
                    }
                    else
                    {
                        obj.BakeGeometry(doc, oAtt, ref id);
                    }
                    
                    ids.Add(id);
                }
            }
            this.Message = "Completed";
            DA.SetDataList(0, ids);
            DA.SetDataList(1, debug);
        }

        public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            throw new NotImplementedException();
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Overwrite", Menu_OverwriteClick, true, overwrite);
        }

        private void Menu_OverwriteClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Overwrite");
            overwrite = !overwrite;
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("overwrite", this.overwrite);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.overwrite = reader.GetBoolean("overwrite");
            return base.Read(reader);
        }
    }
}