using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Canvas;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Axis.Geometry
{
    public class PlaneOrientations : GH_Component
    {
        Opperation currentState = Opperation.QuaternionToPlane;
        Opperation previouseState = Opperation.PlaneToEuler;


        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PlaneOrientations()
          : base("Plane Orientations", "Plane Orientations",
              "This porvides accsess to different conversion methods for plain oriemtations, such as Quaternions and Euler angles",
              AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        #region IO
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        #endregion 

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            Point3d point = Point3d.Unset;
            List<double> list = new List<double>();
            Surface surf = null;

            if(UpdateIO()) return;

            switch (currentState) 
            {
                case Opperation.PlaneToQuatertion:
                    if (Params.Input.Count == 0 | Params.Output.Count == 0) return;
                    if (!DA.GetData("Plane", ref plane)) return;
                    var q = Util.QuaternionFromPlane(plane);
                    DA.SetDataList("List", new List<string>() { q.A.ToString("0.000000"), q.B.ToString("0.000000"), q.C.ToString("0.000000"), q.D.ToString("0.000000") });
                    break;
                case Opperation.QuaternionToPlane:
                    if (Params.Input.Count == 0 | Params.Output.Count == 0) return;
                    if (!DA.GetData("Point", ref point)) return;
                    if (!DA.GetDataList("List", list)) return;
                    if (list.Count != 4) return;
                    DA.SetData("Plane", Util.QuaternionToPlane(point, new Quaternion(list[0], list[1], list[2], list[3])));
                    break;
                case Opperation.PlaneToEuler:
                    if (Params.Input.Count == 0 | Params.Output.Count == 0) return;
                    if (!DA.GetData("Plane", ref plane)) return;
                    DA.SetDataList("List", Util.QuaternionToEuler(Util.QuaternionFromPlane(plane)));
                    break;
                case Opperation.EulerToPlane:
                    if (Params.Input.Count == 0 | Params.Output.Count == 0) return;
                    if (!DA.GetData("Point", ref point)) return;
                    if (!DA.GetDataList("List", list)) return;
                    if (list.Count != 3) return;
                    var eulerPlane = Plane.WorldXY;
                    eulerPlane.Transform(Transform.RotationZYX(list[0].ToRadians(), list[1].ToRadians(), list[2].ToRadians()));
                    eulerPlane.Transform(Transform.Translation((Vector3d)point));
                    DA.SetData("Plane", eulerPlane);
                    break;
                case Opperation.SurfaceFrame:
                    if (Params.Input.Count == 0 | Params.Output.Count == 0) return;
                    if (!DA.GetData("Surface", ref surf)) return;
                    Plane frame = new Plane();

                    surf.SetDomain(0, new Interval(0, 1));
                    surf.SetDomain(1, new Interval(0, 1));

                    bool success = surf.FrameAt(0.5, 0.5, out frame);
                    if (!success)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not get valid frame for surface.");
                        return;
                    }

                    DA.SetData("Plane", frame);
                    break;
            }
        }

        #region Display Pipeline
        #endregion

        #region UI

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[4]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_Point() { Name = "Point", NickName = "Point", Description = "", Access = GH_ParamAccess.item },
            new Param_Number() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
            new Param_Surface() { Name = "Surface", NickName = "Surface", Description = "", Access = GH_ParamAccess.item},
        };

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[2]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_String() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem selectState = Menu_AppendItem(menu, "Select the function");
            selectState.ToolTipText = "Select the function to component should perform";

            foreach (string name in typeof(Opperation).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, state_Click);

                if (name == this.currentState.ToString()) item.Checked = true;
                selectState.DropDownItems.Add(item);
            }

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);
        }
        private void state_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Set Function");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.currentState = (Opperation)currentItem.Owner.Items.IndexOf(currentItem);

            UpdateIO();
        }
        private bool UpdateIO() 
        {
            bool update = (previouseState != currentState);

            if (!update) return false;

            // Set the input and Out put parameters for the different states
            switch (currentState)
            {
                case Opperation.PlaneToQuatertion:
                    int pInput = Params.Input.Count;
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInput(0, inputParams);
                    this.AddOutput(1, outputParams);
                    break;
                case Opperation.QuaternionToPlane:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 1, 2 }, inputParams);
                    this.AddOutput(0, outputParams);
                    break;
                case Opperation.PlaneToEuler:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInput(0, inputParams);
                    this.AddOutput(1, outputParams);
                    break;
                case Opperation.EulerToPlane:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInputs(new[] { 1, 2 }, inputParams);
                    this.AddOutput(0, outputParams);
                    break;
                case Opperation.SurfaceFrame:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInput(3, inputParams);
                    this.AddOutput(0, outputParams);
                    break;
            }

            previouseState = currentState;
            ExpireSolution(true);

            return true;
        }

        #endregion

        #region Serialization
        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ComponentState", (int)this.currentState);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            this.currentState = (Opperation)reader.GetInt32("ComponentState");
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                switch (currentState) 
                {
                    case Opperation.EulerToPlane: return null;
                    case Opperation.PlaneToEuler: return null;
                    case Opperation.PlaneToQuatertion: return null;
                    case Opperation.QuaternionToPlane: return null;
                    case Opperation.SurfaceFrame: return null;
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8128e6cf-4b8d-4395-b941-1cfa734cab1a"); }
        }
        #endregion

        enum Opperation
        {
            PlaneToQuatertion = 0,
            QuaternionToPlane = 1,
            EulerToPlane = 2,
            PlaneToEuler = 3,
            SurfaceFrame = 4,
        }
    }
}