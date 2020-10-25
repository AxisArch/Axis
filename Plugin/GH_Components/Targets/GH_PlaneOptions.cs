using Axis.Kernal;
using Canvas;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    public class GH_PlaneOptions : Axis_Component, IGH_VariableParameterComponent
    {

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GH_PlaneOptions()
          : base("Plane Orientations", "Plane Orientations",
              "This porvides accsess to different conversion methods for plain oriemtations, such as Quaternions and Euler angles",
              AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
            ToolStripMenuItem selectState = new ToolStripMenuItem("Select the function") 
            {
                ToolTipText = "Select the function to component should perform"
            };
            foreach (string name in typeof(Opperation).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, state_Click);

                if (name == this.currentState.ToString()) item.Checked = true;
                selectState.DropDownItems.Add(item);
            }

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                selectState,
            };
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.Unset;
            Point3d point = Point3d.Unset;
            List<double> list = new List<double>();
            Surface surf = null;

            if (UpdateIO()) return;

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

                case Opperation.FlipPlane:
                    Plane targIn = Plane.Unset; if (!DA.GetData<Plane>(0, ref targIn)) return;
                    DA.SetData(0, new Plane(targIn.Origin, -targIn.XAxis, targIn.YAxis));
                    break;
            }
        }

        #region Variales
        private Opperation currentState = Opperation.FlipPlane;
        private Opperation previouseState = Opperation.PlaneToQuatertion; // Should not be the same as currentState
        #endregion Variales

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

        #endregion IO

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[4]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_Point() { Name = "Point", NickName = "Point", Description = "", Access = GH_ParamAccess.item },
            new Param_Number() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
            new Param_Surface() { Name = "Surface", NickName = "Surface", Description = "", Access = GH_ParamAccess.item},
        };

        // Build a list of optional output parameters
        private IGH_Param[] outputParams = new IGH_Param[2]
        {
            new Param_Plane() { Name = "Plane", NickName = "Plane", Description = "" , Access = GH_ParamAccess.item},
            new Param_String() { Name = "List", NickName = "List", Description = "", Access = GH_ParamAccess.list},
        };

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

                case Opperation.FlipPlane:
                    Params.RemoveAllInputs();
                    Params.RemoveAllOutputs();
                    this.AddInput(0, inputParams);
                    this.AddOutput(0, outputParams);
                    break;
            }

            previouseState = currentState;
            DestroyIconCache();
            ExpireSolution(true);

            return true;
        }

        #endregion UI

        #region Serialization

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ComponentState", (int)this.currentState);
            writer.SetInt32("PreviouseComponentState", (int)this.previouseState);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            this.currentState = (Opperation)reader.GetInt32("ComponentState");
            this.previouseState = (Opperation)reader.GetInt32("PreviouseComponentState");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                switch (currentState)
                {
                    case Opperation.EulerToPlane:
                    case Opperation.PlaneToEuler:
                    case Opperation.PlaneToQuatertion:
                    case Opperation.QuaternionToPlane:
                        return Properties.Icons.Core;

                    case Opperation.SurfaceFrame: return Properties.Icons.SurfaceFrame;
                    case Opperation.FlipPlane: return Properties.Icons.Flip;
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

        #endregion Component Settings

        private enum Opperation
        {
            PlaneToQuatertion = 0,
            QuaternionToPlane = 1,
            EulerToPlane = 2,
            PlaneToEuler = 3,
            SurfaceFrame = 4,
            FlipPlane = 5,
        }
    }
}