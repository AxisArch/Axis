using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.DocObjects.Custom;
using Rhino.Render;

namespace Axis.Workflow
{
    public class CreateLayers : GH_Component
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
            get { return new Guid("07f7b24f-a777-49ad-836e-f35879beb8d5"); }
        }

        public CreateLayers() : base("Create Layers", "Layers", "Create a series of layers in the current Rhino document.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Parent", "Parent", "List of parent layer names to create.", GH_ParamAccess.list, "Axis");
            pManager.AddTextParameter("Children", "Children", "List of child layer names to create.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Check the layer table and add the relevant objects.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Log", "Log", "Information about the operating status of the component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> parentsIn = new List<string>();
            List<string> childrenIn = new List<string>();
            bool run = false;

            if (!DA.GetDataList(0, parentsIn)) return;
            if (!DA.GetDataList(1, childrenIn)) return;
            if (!DA.GetData(2, ref run)) return;

            //List<string> log = new List<string>();
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;

            // Create a list of strings to hold the formatted layer names as well as the material for each layer.
            List<string> parentNames = parentsIn;
            List<string> childNames = new List<string>();
            List<Color> childColors = new List<Color>();
            List<double> childTrans = new List<double>();

            // Create a character array to split our strings and arrays to hold the chunks
            char[] seperators = { ':', ',' };
            string[] chunks = null;

            for (int i = 0; i < childrenIn.Count; i++)
            {
                chunks = childrenIn[i].Split(seperators);

                childNames.Add(chunks[0]);

                List<int> colParts = new List<int>();
                if (chunks.Length < 4 && chunks.Length > 1)
                {
                    Color col = Color.FromName(chunks[1].ToUpper());
                    childColors.Add(col);

                    // If the layer name also includes information about the transparency,
                    // add it to the layer information list, otherwise add a default value of 1.0 (opaque).
                    if (chunks.Length == 3)
                    {
                        double trans = Convert.ToDouble(chunks[2]);
                        childTrans.Add(trans);
                    }
                    else
                    {
                        childTrans.Add(0.0);
                    }
                }
                else if (chunks.Length >= 4)
                {
                    for (int j = 1; j < 4; j++)
                    {
                        int colPart = Convert.ToInt32(chunks[j]);
                        colParts.Add(colPart);
                    }

                    Color col = Color.FromArgb(colParts[0], colParts[1], colParts[2]);
                    childColors.Add(col);
                    colParts.Clear();

                    // If the layer name also includes information about the transparency,
                    // add it to the layer information list, otherwise add a default value of 1.0 (opaque).
                    if (chunks.Length == 5)
                    {
                        double trans = Convert.ToDouble(chunks[4]);
                        childTrans.Add(trans);
                    }
                    else
                    {
                        childTrans.Add(0.0);
                    }
                }
                else
                {

                    Color defaultCol = Color.FromName("Black");
                    childColors.Add(defaultCol);

                    childTrans.Add(1.0);
                }

                this.Message = "Ready";
            }

            // Create and append the child materials to a list, as well as storing the document material index
            List<Material> parentMats = new List<Material>();
            List<Material> childMats = new List<Material>();
            List<int> childMatIndices = new List<int>();

            /*
            for (int i = 0; i < childColors.Count; i++)
            {
                Material mat = Material.DefaultMaterial;
                mat.Name = childNames[i] + "_Material";
                mat.DiffuseColor = childColors[i];
                mat.Transparency = childTrans[i];

                childMats.Add(mat);
                int existingMat = doc.Materials.Find(mat.Name, true);

                // If no material with the same name currently exists in the document material table, add it.
                // Otherwise, add the index of the material that already exists.
                if (existingMat == -1)
                {
                    int matIndex = doc.Materials.Add(mat);
                    childMatIndices.Add(matIndex);
                }
                else
                {
                    childMatIndices.Add(existingMat);
                }

            }
            */

            if (run)
            {
                this.Message = "Running";

                for (int i = 0; i < parentNames.Count; i++)
                {
                    Layer currentParent;

                    // Get the parent name and ensure it's validity.
                    string parentName = parentNames[i];
                    if (!Layer.IsValidName(parentName)) { parentName = "NewParent"; }

                    // Does the parent layer already exist?
                    Layer existingParent = doc.Layers.FindName(parentName);
                    currentParent = existingParent;

                    if (existingParent == null)
                    {
                        // Add the new parent layer to the document
                        int parentIndex = doc.Layers.Add(parentName, Color.Black);
                        Layer newParent = doc.Layers.FindIndex(parentIndex);
                        currentParent = newParent;
                    }

                    for (int j = 0; j < childNames.Count; j++)
                    {
                        string childName = childNames[j];
                        //RenderMaterial renderMat = RenderMaterial.CreateBasicMaterial(doc.Materials.FindIndex(childMatIndices[j]));

                        // Is the child layer name valid?
                        if (!Layer.IsValidName(childName)) { childName = "NewChild"; }

                        // Create a child layer
                        Layer childLayer = new Layer();
                        childLayer.ParentLayerId = currentParent.Id;
                        childLayer.Name = childName;
                        childLayer.Color = childColors[j];
                        //childLayer.RenderMaterial = renderMat;
                        //childLayer.SetUserString("transparency", childTrans.ToString());

                        int layerIndex = -1;
                        // Does a layer with the same name already exist?
                        Layer existingChild = doc.Layers.FindName(childName);
                        if (existingChild == null)
                        {
                            this.Message = "Creating " + (j + 1).ToString() + "/" + ((i + 1) * (childNames.Count)).ToString();
                            Grasshopper.Instances.RedrawCanvas();

                            // Add a new layer to the document
                            layerIndex = doc.Layers.Add(childLayer);
                        }

                        this.Message = "Completed";

                        /*
                        Layer lay = doc.Layers.FindIndex(layerIndex);
                        UserDataList data = lay.UserData;
                        string logStr = data.ToString();
                        */
                    }
                }
            }
        }
    }
}