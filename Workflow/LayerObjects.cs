using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Axis.Workflow
{
    public class LayerObjects : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Layers;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("56760692-99cf-4a12-bc8d-70146638f218"); }
        }

        // Sticky context menu flags
        bool[] flags = { false, false, false, false, false, false, false };

        public LayerObjects() : base("Layer Objects", "Layer", "Get all objects on a given Rhino layer. A geometry filter can additionally be applied.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Layer", "Layer", "Name of the Rhino layer from which to get objects.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Objects", "Objects", "List of objects found on Rhino layer.", GH_ParamAccess.list);
            pManager.AddGenericParameter("IDs", "IDs", "Globally unique identifiers (GUIDs).", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string layerName = String.Empty;

            if (!DA.GetData(0, ref layerName)) return;

            // Set the filter to on by default, turn it off if any selection filters are active
            bool NoFilter = true; this.Message = "Filter Disabled";
            foreach (bool flag in flags)
            {
                if (flag)
                {
                    NoFilter = false;
                    this.Message = "Filter Active";
                }

            }

            // Get all of the Rhino objects using the active doc.
            Rhino.DocObjects.RhinoObject[] rhiObjects = Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(layerName);

            // Geometry container to hold the list of new objects.
            List<GeometryBase> objsOut = new List<GeometryBase>();
            List<string> GUIDs = new List<string>(); // Empty list to store GUIDs.

            if (rhiObjects != null)
            {
                for (int i = 0; i < rhiObjects.Length; i++)
                {
                    if (rhiObjects[i].IsNormal)
                    {
                        GeometryBase geoBase = rhiObjects[i].Geometry;

                        /*
                         * Check the object type:
                         * -1: No type found (type not implemented)
                         * 0: Brep
                         * 1: Extrusion
                         * 2: Surface
                         * 3: Mesh
                         * 4: Curve
                         * 5: Point
                         * */

                        int type = Util.TypeCheck(rhiObjects[i]);

                        if (type > -1 && flags[type] == true) // Check if that specific type filter is active.
                        {
                            objsOut.Add(geoBase);
                        }

                        // If there are no filters in place, just add all of the layer geometry to the output list.
                        else if (NoFilter)
                        {
                            objsOut.Add(geoBase);
                        }

                        string GUID = rhiObjects[i].Id.ToString();
                        GUIDs.Add(GUID);
                    }
                }
            }
            else
            {
                this.Message = "Invalid Layer";
            }


            // Output the list of filtered geometry.
            DA.SetDataList(0, objsOut);
            DA.SetDataList(1, GUIDs);
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem brep = Menu_AppendItem(menu, "Breps", Menu_BrepsClick, true, flags[0]); // Append the item to the menu.
            brep.ToolTipText = "When checked, the geometry is filtered to include breps."; // Specifically assign a tooltip text to the menu item.
            ToolStripMenuItem extrusion = Menu_AppendItem(menu, "Extrusions", Menu_ExtrusionsClick, true, flags[1]);
            extrusion.ToolTipText = "When checked, the geometry is filtered to include extrusions.";
            ToolStripMenuItem srf = Menu_AppendItem(menu, "Surfaces", Menu_SurfacesClick, true, flags[2]);
            srf.ToolTipText = "When checked, the geometry is filtered to include surfaces.";
            ToolStripMenuItem mesh = Menu_AppendItem(menu, "Meshes", Menu_MeshesClick, true, flags[3]);
            mesh.ToolTipText = "When checked, the geometry is filtered to include meshes.";
            ToolStripMenuItem curve = Menu_AppendItem(menu, "Curves", Menu_CurvesClick, true, flags[4]);
            curve.ToolTipText = "When checked, the geometry is filtered to include curves.";
            ToolStripMenuItem points = Menu_AppendItem(menu, "Points", Menu_PointsClick, true, flags[5]);
            points.ToolTipText = "When checked, the geometry is filtered to include points.";
        }

        private void Menu_BrepsClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Breps");
            flags[0] = !flags[0];
            ExpireSolution(true);
        }

        private void Menu_ExtrusionsClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Extrusions");
            flags[1] = !flags[1];
            ExpireSolution(true);
        }

        private void Menu_SurfacesClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Surfaces");
            flags[2] = !flags[2];
            ExpireSolution(true);
        }

        private void Menu_MeshesClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Meshes");
            flags[3] = !flags[3];
            ExpireSolution(true);
        }

        private void Menu_CurvesClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Curves");
            flags[4] = !flags[4];
            ExpireSolution(true);
        }

        private void Menu_PointsClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Points");
            flags[5] = !flags[5];
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("Breps", this.flags[0]);
            writer.SetBoolean("Extrusions", this.flags[1]);
            writer.SetBoolean("Surfaces", this.flags[2]);
            writer.SetBoolean("Meshes", this.flags[3]);
            writer.SetBoolean("Curves", this.flags[4]);
            writer.SetBoolean("Points", this.flags[5]);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.flags[0] = reader.GetBoolean("Breps");
            this.flags[1] = reader.GetBoolean("Extrusions");
            this.flags[2] = reader.GetBoolean("Surfaces");
            this.flags[3] = reader.GetBoolean("Meshes");
            this.flags[4] = reader.GetBoolean("Curves");
            this.flags[5] = reader.GetBoolean("Points");
            return base.Read(reader);
        }
    }
}