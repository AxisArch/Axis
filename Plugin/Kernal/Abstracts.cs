using Axis.Types;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Axis.Kernal
{
    /// <summary>
    /// Base class for components that require login
    /// </summary>
    public abstract class AxisLogin_Component : Axis_Component
    {
        private bool IsTokenValid { get; set; }
        private string WarningMessage = "Please log in to Axis.";
        
        

        public AxisLogin_Component(string name, string nickname, string discription, string plugin, string tab) : base(name, nickname, discription, plugin, tab)
        {
            //IsTokenValid = Auth.AuthCheck();
        }

        protected void UpdateToken(object sender, Axis.Auth.LoginEnventArgs e)
        {
            var component = this;

            //component.IsTokenValid = e.LogedIn;

            var doc = component.OnPingDocument();
            if (doc != null) doc.ScheduleSolution(10, ExpireComponent);

            void ExpireComponent(GH_Document document)
            {
                component.ExpireSolution(false);
            }
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            Auth.OnLoginStateChange += UpdateToken;

            if (!Properties.Settings.Default.LoggedIn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, WarningMessage);
            }
            else
            {
                ClearRuntimeMessages();
            }
        }

        protected override sealed void SolveInstance(IGH_DataAccess da)
        {
            if (!Properties.Settings.Default.LoggedIn)
                return;

            SolveInternal(da);
        }

        protected abstract void SolveInternal(IGH_DataAccess da);

        public override void ClearData()
        {
            Auth.OnLoginStateChange -= UpdateToken;
            base.ClearData();
        }
    }

    public abstract class Axis_Component : GH_Component
    {
        public bool IsMutiManufacture { get; set; } = false;
        protected Manufacturer Manufacturer { get; set; } = Manufacturer.ABB;
        public Axis_Component(string name, string nickname, string discription, string plugin, string tab) : base(name, nickname, discription, plugin, tab)
        {
            var attr = this.Attributes as AxisComponentAttributes;
            attr.Update(UI_Elements);
        }

        protected ToolStripItem[] RegularToolStripItems;
        protected ToolStripItem[] ABBToolStripItems;
        protected ToolStripItem[] KukaToolStripItems;
        protected ToolStripItem[] UniversalToolStripItems;
        protected IComponentUiElement[] UI_Elements;


        protected override sealed void AppendAdditionalComponentMenuItems(ToolStripDropDown menu) //sealed
        {
            // Add menue if this component supposts different manufactures
            Menu_AppendSeparator(menu);
            if (IsMutiManufacture)
            {

                ToolStripMenuItem robotManufacturers = Menu_AppendItem(menu, "Manufacturer");
                robotManufacturers.ToolTipText = "Select the robot manufacturer";
                foreach (string name in typeof(Manufacturer).GetEnumNames())
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(name, null, manufacturer_Click);

                    if (name == this.Manufacturer.ToString()) item.Checked = true;
                    robotManufacturers.DropDownItems.Add(item);
                }

                Menu_AppendSeparator(menu);
            }

            // Add menue Items that are not manufacture spesific
            if (RegularToolStripItems != null)
            {
                menu.Items.AddRange(RegularToolStripItems);
                Menu_AppendSeparator(menu);
            }

            // Add manufacturer spesific items
            if (IsMutiManufacture)
            {
                switch (Manufacturer) 
                {
                    case Manufacturer.ABB:
                        if (ABBToolStripItems != null) menu.Items.AddRange(ABBToolStripItems);
                        break;
                    case Manufacturer.Kuka:
                        if (KukaToolStripItems != null) menu.Items.AddRange(KukaToolStripItems);
                        break;
                    case Manufacturer.Universal:
                        if (UniversalToolStripItems != null) menu.Items.AddRange(UniversalToolStripItems);
                        break;
                    default:
                        break;
                }
            }
        }

        public override void CreateAttributes()
        {
            base.CreateAttributes();
            m_attributes = MakeAtributes();
        }
        private GH_ComponentAttributes MakeAtributes() 
        {
            IGH_Component component = this as IGH_Component;
            GH_ComponentAttributes attributes = new AxisComponentAttributes(component, UI_Elements);
            return attributes;
        }

        internal class AxisComponentAttributes : GH_ComponentAttributes
        {
            public AxisComponentAttributes(IGH_Component comp, IComponentUiElement[] elements) : base(comp) 
            {
                this.comp = comp;
                this.elements = elements;
            }

            private IComponentUiElement[] elements { get; set; }
            private IGH_Component comp { get; set; }

            public void Update(IComponentUiElement[] elements) 
            {
                this.elements = elements;
                Layout();
            }

            protected override void Layout()
            {
                base.Layout();

                // Add main Component Button
                if (elements != null) foreach (IComponentUiElement element in elements.Where(e => e.Type == UIElementType.ComponentButton) ) 
                {
                    element.Bounds = new System.Drawing.RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
                    // We'll extend the basic layout by adding two regions to the bottom of this component,
                    Bounds = new System.Drawing.RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 20);
                }      
            }

            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
            {

                if (elements != null)  switch (e.Button) 
                {
                    case MouseButtons.Left:
                        foreach (IComponentUiElement element in elements)
                        {
                            if (element.Bounds.Contains(e.CanvasLocation))
                            {
                                comp.RecordUndoEvent(element.Name);
                                element.LeftClickAction.Invoke(element, e);
                                comp.ExpireSolution(true);
                                return Grasshopper.GUI.Canvas.GH_ObjectResponse.Handled;
                            }
                        }
                        break;
                    default:
                        break;
                }

                return base.RespondToMouseDown(sender, e);
            }

            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                switch (channel)
                {
                    case GH_CanvasChannel.Objects:

                        // We need to draw everything outselves.
                        base.RenderComponentCapsule(canvas, graphics, true, true, false, true, true, true);

                        if (elements != null) foreach (IComponentUiElement element in elements.Where(e => e.Type == UIElementType.ComponentButton && e is IButton))
                        {

                            GH_Capsule button = GH_Capsule.CreateCapsule(element.Bounds, GH_Palette.White);
                            button.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                            button.Dispose();

                            graphics.DrawString(element.Name, GH_FontServer.Small, Brushes.Black, element.Bounds, Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                        }
                        if (elements != null) foreach (IComponentUiElement element in elements.Where(e => e.Type == UIElementType.ComponentButton && e is IToggle))
                            {

                                var toggel = element as IToggle;
                                GH_Capsule button = GH_Capsule.CreateCapsule(element.Bounds, toggel.State? GH_Palette.Blue: GH_Palette.White);
                                button.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                                button.Dispose();

                                if (toggel.Toggle != null) graphics.DrawString(
                                    toggel.State ? toggel.Toggle.Item2 : toggel.Toggle.Item1, 
                                    GH_FontServer.Small,
                                    (toggel.State) ? Brushes.Black: Brushes.Black,
                                    element.Bounds, 
                                    Grasshopper.GUI.GH_TextRenderingConstants.CenterCenter);
                            }

                        break;

                    default:
                        base.Render(canvas, graphics, channel);
                        break;
                }
            }
        }



        private void manufacturer_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.Manufacturer = (Manufacturer)currentItem.Owner.Items.IndexOf(currentItem);
            this.ExpireSolution(true);
        }
    }

    /// <summary>
    /// Base calss for all robot systems
    /// </summary>
    public abstract class Robot : IGH_GeometricGoo, Axis_IDisplayable
    {
        #region Variables
        private Guid id = Guid.Empty;
        #endregion Variables

        #region Propperties

        public abstract Manufacturer Manufacturer { get; }
        public string Name { get; set; } = "Wall-E";


        public Plane RobBasePlane { get; protected set; }
        public List<Plane> AxisPlanes { get; protected set; }
        public List<double> MinAngles { get; protected set; }
        public List<double> MaxAngles { get; protected set; }
        public List<int> Indices { get; protected set; }
        public List<Mesh> RobMeshes { get; protected set; }


        public abstract Plane Flange { get; }


        private Transform[] resetTransform = null;
        private Transform[] ResetTransform
        {
            get
            {
                // Check if transform already has been applied
                if (this.resetTransform != null) return resetTransform;

                var rT = new Transform[this.RobMeshes.Count - 1]; // <--- Probably change this to the number of meshes
                for (int i = 0; i < this.RobMeshes.Count - 1; ++i) rT[i] = Rhino.Geometry.Transform.Identity;
                resetTransform = rT;
                return rT;
            }
            set => resetTransform = value;
        }


        //Used for Axis display pipeline
        public Mesh[] Geometries { get => RobMeshes.ToArray(); }
        public class SimpleModle { public Line[] Lines; public Brep[] Joints; public Curve[] Arrows; public Line Tool; }
        public SimpleModle LineModle { get; set; }

        #endregion Propperties

        #region Methods

        public abstract Pose GetPose();
        public abstract Pose GetPose(Target target);
        public Pose[] GetPoses(IEnumerable<Target> targets)
        {
            return targets.Select(t => this.GetPose(t)).ToArray();
        }


        /// <summary>
        /// Update the locattion of the robot
        /// </summary>
        /// <param name="plane"></param>
        public void ChangeBasePlane(Plane plane)
        {
            // Transformation
            Rhino.Geometry.Transform xform = Rhino.Geometry.Transform.PlaneToPlane(this.RobBasePlane, plane);

            // Move Planes to new locating
            Plane[] tempPlanes = this.AxisPlanes.Select(p => p.Clone()).ToArray();
            for (int i = 0; i < tempPlanes.Length; ++i) tempPlanes[i].Transform(xform);
            this.AxisPlanes = tempPlanes.ToList();

            // Then transform all of these meshes based on the input base plane.
            List<Mesh> tempMesh = this.RobMeshes.Select(m => m.DuplicateMesh()).ToList();
            tempMesh.ForEach(m => m.Transform(xform));
            this.RobMeshes = tempMesh;

            this.RobBasePlane = plane;
        }

        protected SimpleModle LineDrawing()
        {
            double offsetDistanc = 100;
            double arrowDistance = 20;
            double circleoffsetDistanc = 20;

            double bPlaneSize = 360;
            double minLength = 5;


            Line[] lines = new Polyline(AxisPlanes.Select(plane => plane.Origin).ToList()).GetSegments();
            Circle[] circles = AxisPlanes.Select(p => new Circle(p, 1)).ToArray();


            double[] offsetDistancArray = lines.Select(l => (l.Length > offsetDistanc + minLength) ? offsetDistanc : l.Length).ToArray();
            double[] arrowDistanceArray = offsetDistancArray.Select(v => v - arrowDistance).Append(offsetDistanc - arrowDistance).ToArray();
            double[] circleoffsetDistancArray = arrowDistanceArray.Select(v => v - circleoffsetDistanc).ToArray();


            double[] t1 = new double[lines.Length];
            double[] t2 = new double[lines.Length];
            Point3d[] p1 = new Point3d[lines.Length];
            Point3d[] p2 = new Point3d[lines.Length];

            lines.Select((line, i) => Rhino.Geometry.Intersect.Intersection.LineCircle(line, circles[i], out t1[i], out p1[i], out t2[i], out p2[i]));
            bool[] startIntersect = t1.Select((t, i) => t != double.NaN | t2[i] != double.NaN).ToArray();

            lines.Select((line, i) => Rhino.Geometry.Intersect.Intersection.LineCircle(line, circles[i + 1], out t1[i + 1], out p1[i + 1], out t2[i + 1], out p2[i + 1]));
            bool[] endIntersect = t1.Select((t, i) => t != double.NaN | t2[i] != double.NaN).ToArray();


            Brep bplane = Brep.CreateEdgeSurface(new List<NurbsCurve>() { new Circle(this.RobBasePlane, bPlaneSize).ToNurbsCurve() });

            lines.Select((l, i) => l.Extend((startIntersect[i]) ? offsetDistancArray[i] : 0, (startIntersect[i]) ? offsetDistancArray[i] : 0));

            Brep[] jointPlanes = AxisPlanes.Select((p, index) => 
            Brep.CreatePlanarBreps(
                new Circle(p, circleoffsetDistancArray[index]).ToNurbsCurve(), 0.10)[0]
                ).ToArray();

            double Unwind(double value) 
            {
                while (value > 360) 
                {
                    value -= 360;
                }
                while (value < -360)
                {
                    value += 360;
                }
                return value;
            }

            Curve[] arrorsCurves = AxisPlanes.Select((p, i) => 
            new Arc(
                new Circle(p, arrowDistanceArray[i]), 
                new Interval(
                    Unwind(MinAngles[i]).ToRadians(),
                    Unwind(MaxAngles[i]).ToRadians()
                    )
                ).ToNurbsCurve()
            ).ToArray();

            return new SimpleModle() { Lines = lines, Joints = jointPlanes, Arrows = arrorsCurves };
        }

        #endregion Methods

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; }

        public Guid ReferenceID
        {
            get
            {
                if (id != Guid.Empty) return id;
                id = System.Guid.NewGuid();
                return id;
            }
            set
            {
                if (typeof(Guid) == value.GetType()) id = value;
            }
        }

        public bool IsReferencedGeometry { get => false; }
        public bool IsGeometryLoaded { get => false; }

        public void ClearCaches()
        {
            this.Name = null;
            this.RobBasePlane = Plane.Unset;
            this.AxisPlanes = null;
            this.MinAngles = null;
            this.MaxAngles = null;
            this.Indices = null;
            this.RobMeshes = null;
        }

        public abstract IGH_GeometricGoo DuplicateGeometry();

        public BoundingBox GetBoundingBox(Transform xform)
        {
            BoundingBox box = BoundingBox.Empty;
            this.RobMeshes.ForEach(m => m.Transform(xform));
            this.RobMeshes.ForEach(m => box.Union(m.GetBoundingBox(false)));
            this.Boundingbox = box;
            return box;
        }

        public bool LoadGeometry() => false;

        public bool LoadGeometry(Rhino.RhinoDoc doc) => false;

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        //IGH_Goo
        public bool IsValid { get => true; }

        public string IsValidWhyNot => "Since no or no valid poes has been set there is no representation possible for this robot";
        public string TypeName => "Manipulator";
        public string TypeDescription => "Robot movment system";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Robot))
            {
                Robot manipulator = source as Robot;
                this.Name = manipulator.Name;
                this.AxisPlanes = manipulator.AxisPlanes;
                this.MinAngles = manipulator.MinAngles;
                this.MaxAngles = manipulator.MaxAngles;
                this.RobMeshes = manipulator.RobMeshes;
                this.RobBasePlane = manipulator.RobBasePlane;
                this.Indices = manipulator.Indices;
            }

            return false;
        }

        public bool CastTo<T>(out T target)
        {
            target = default(T);

            if (typeof(T).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(T).Name;
                object value = new GH_ObjectWrapper(this);
                target = (T)value;
                return true;
            }

            return false;
        }

        public abstract IGH_Goo Duplicate();

        public IGH_GooProxy EmitProxy() => null;

        public object ScriptVariable() => this;

        public override string ToString()
        {
            if (this.Name != string.Empty) return $"Robot {this.Manufacturer.ToString()}  \'{this.Name}\'";
            else return $"Robot {this.Manufacturer.ToString()}";
        }

        //GH_ISerializable
        public virtual bool Read(GH_IReader reader)
        {
            this.Name = reader.GetString("RobotName");
            this.id = reader.GetGuid("GUID");

            Plane bPlane = new Plane();
            if (reader.ItemExists("RobBasePlane"))
            {
                GH_IO.Types.GH_Plane ghPlane = reader.GetPlane("RobBasePlane");
                bPlane = new Plane(
                    new Point3d(
                        ghPlane.Origin.x,
                        ghPlane.Origin.y,
                        ghPlane.Origin.z),
                    new Vector3d(
                        ghPlane.XAxis.x,
                        ghPlane.XAxis.y,
                        ghPlane.XAxis.z),
                    new Vector3d(
                        ghPlane.YAxis.x,
                        ghPlane.YAxis.y,
                        ghPlane.YAxis.z)
                    );
            }
            this.RobBasePlane = bPlane;

            List<Plane> axisPlanes = new List<Plane>();
            for (int i = 0; i < 6; ++i)
            {
                GH_IO.Types.GH_Plane ghPlane = reader.GetPlane("AxisPlanes", i);
                Plane plane = new Plane(
                    new Point3d(
                        ghPlane.Origin.x,
                        ghPlane.Origin.y,
                        ghPlane.Origin.z),
                    new Vector3d(
                        ghPlane.XAxis.x,
                        ghPlane.XAxis.y,
                        ghPlane.XAxis.z),
                    new Vector3d(
                        ghPlane.YAxis.x,
                        ghPlane.YAxis.y,
                        ghPlane.YAxis.z)
                    );
                axisPlanes.Add(plane);
            }
            this.AxisPlanes = axisPlanes;

            List<double> minAngles = new List<double>();
            for (int i = 0; i < 6; ++i)
            {
                minAngles.Add(reader.GetDouble("MinAngles", i));
            }
            this.MinAngles = minAngles;

            List<double> maxAngles = new List<double>();
            for (int i = 0; i < 6; ++i)
            {
                maxAngles.Add(reader.GetDouble("MaxAngles", i));
            }
            this.MaxAngles = maxAngles;

            List<int> indices = new List<int>();
            for (int i = 0; i < 6; ++i)
            {
                indices.Add(reader.GetInt32("Indices", i));
            }
            this.Indices = indices;

            List<Mesh> robMeshes = new List<Mesh>();
            for (int i = 0; i < 7; ++i)
            {
                if (reader.ChunkExists("RobMeshes", i))
                {
                    Mesh mesh = new Mesh();
                    GH_Mesh gh_mesh = new GH_Mesh();
                    var cunckMesh = reader.FindChunk("RobMeshes", i);
                    gh_mesh.Read(cunckMesh);
                    bool sucsess = GH_Convert.ToMesh(gh_mesh, ref mesh, GH_Conversion.Both);
                    robMeshes.Add(mesh);
                }
            }
            this.RobMeshes = robMeshes;

            //this.SetPose();
            return true;
        }

        public virtual bool Write(GH_IWriter writer)
        {
            GH_IO.Types.GH_Plane gH_RobBasePlane = new GH_IO.Types.GH_Plane(
                this.RobBasePlane.Origin.X,
                this.RobBasePlane.Origin.Y,
                this.RobBasePlane.Origin.Z,
                this.RobBasePlane.XAxis.X,
                this.RobBasePlane.XAxis.Y,
                this.RobBasePlane.XAxis.Z,
                this.RobBasePlane.YAxis.X,
                this.RobBasePlane.YAxis.Y,
                this.RobBasePlane.YAxis.Z
                );

            writer.SetString("RobotName", this.Name);
            writer.SetPlane("RobBasePlane", gH_RobBasePlane);
            writer.SetGuid("GUID", id);

            for (int i = 0; i < this.AxisPlanes.Count; ++i)
            {
                GH_IO.Types.GH_Plane plane = new GH_IO.Types.GH_Plane(
                    this.AxisPlanes[i].OriginX,
                    this.AxisPlanes[i].OriginY,
                    this.AxisPlanes[i].OriginZ,
                    this.AxisPlanes[i].XAxis.X,
                    this.AxisPlanes[i].XAxis.Y,
                    this.AxisPlanes[i].XAxis.Z,
                    this.AxisPlanes[i].YAxis.X,
                    this.AxisPlanes[i].YAxis.Y,
                    this.AxisPlanes[i].YAxis.Z
                    );
                writer.SetPlane("AxisPlanes", i, plane);
            }
            for (int i = 0; i < this.MinAngles.Count; ++i)
            {
                writer.SetDouble("MinAngles", i, this.MinAngles[i]);
            }
            for (int i = 0; i < this.MinAngles.Count; ++i)
            {
                writer.SetDouble("MaxAngles", i, this.MaxAngles[i]);
            }
            for (int i = 0; i < this.Indices.Count; ++i)
            {
                writer.SetInt32("Indices", i, this.Indices[i]);
            }
            for (int i = 0; i < this.RobMeshes.Count; ++i)
            {
                GH_Mesh mesh = new GH_Mesh(this.RobMeshes[i]);
                mesh.Write(writer.CreateChunk("RobMeshes", i));
            }

            return true;
        }


        #endregion Interfaces

        #region Display

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            foreach (var mesh in this.Geometries) args.Display.DrawMeshShaded(mesh, new Rhino.Display.DisplayMaterial(Axis.Styles.DarkGrey));
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            var linecolor = Styles.DarkGrey;
            var surfacecolor = new Rhino.Display.DisplayMaterial(Styles.LightGrey);
            int lineThickness = 17;
            int arrowThickness = 3;
            int arrowHeadSize = 20;

            // Lines
            for (int i = 0; i < LineModle.Lines.Length; ++i) args.Display.DrawCurve(LineModle.Lines[i].ToNurbsCurve(), linecolor, lineThickness - (i * 2));
            for (int i = 0; i < LineModle.Joints.Length; ++i) args.Display.DrawBrepShaded(LineModle.Joints[i], surfacecolor);
            for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawCurve(LineModle.Arrows[i], linecolor, arrowThickness);

            //int j = 5;
            //args.Display.DrawBrepShaded(LineModle.Joints[j], surfacecolor[j]);

            // Arrowheads
            for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawArrowHead(LineModle.Arrows[i].PointAtStart,
                LineModle.Arrows[i].TangentAt(LineModle.Arrows[i].Domain.T0) * -1, linecolor, 0, arrowHeadSize);

            for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawArrowHead(LineModle.Arrows[i].PointAtEnd,
                LineModle.Arrows[i].TangentAt(LineModle.Arrows[i].Domain.T1), linecolor, 0, arrowHeadSize);
        }

        #endregion Display

        /// <summary>
        /// Class to hold the values describing the tansformation of a tool
        /// </summary>
        public abstract class Pose : IGH_GeometricGoo, Axis_IDisplayable
        {
            #region Variables
            private Guid id = Guid.Empty;
            protected Axis.Kernal.Target target;
            protected double[] radAngles;
            protected JointState[] jointStates;
            protected Color[] jointColors;
            private SimpleModle lineModle;

            #endregion Variables

            #region Prooperties
            // Public
            public abstract Robot Robot { get; }
            public double[] Angles { get => this.radAngles.Select(d => d.ToDegrees()).ToArray(); }
            public Target Target { get => target; }
            public JointState[] JointStates { get => jointStates; }
            public Transform[] Forward { get; protected set; }

            #region Signals
            public bool OutOfReach { get => jointStates.Any(js => js == JointState.OutOfReach); }
            public bool OutOfRoation { get => jointStates.Any(js => js == JointState.OutOfRotation); }
            public bool OverHeadSig { get => jointStates.Any(js => js == JointState.OverHeadSing); }
            public bool WristSing { get => jointStates.Any(js => js == JointState.WristSing); }
            #endregion Signal 

            public abstract Plane Flange {get; }
            public Plane[] Planes
            {
                get
                {
                    Plane[] planes = this.Robot.AxisPlanes.Select(plane => plane.Clone()).ToArray();
                    for (int i = 0; i < planes.Length; ++i) planes[i].Transform(this.Robot.ResetTransform[i] * this.Forward[i]);
                    return planes;
                }
            }
            public Plane TargetPlane
            {
                get
                {
                    switch (this.target.Method)
                    {
                        case MotionType.Linear:
                        case MotionType.Joint:
                            return this.target.Plane;

                        case MotionType.AbsoluteJoint:
                            var plane = this.Flange.Clone();
                            var xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.target.Tool.TCP);
                            plane.Transform(xform);
                            return plane;
                    }
                    return Plane.Unset;
                }
            }

            /*
             * @ todo Transform the tool
             * @ body Ensure the tool is moved the the propper location
             */
            public Tool Tool => this.target.Tool;
            public Speed Speed => this.target.Speed;


            public Mesh[] Geometries
            {
                get
                {
                    Mesh[] meshes = new Mesh[this.Robot.RobMeshes.Count + this.target.Tool.Geometries.Length];

                    var rob = this.Robot.RobMeshes.Select(mesh => mesh.DuplicateMesh()).ToArray();
                    for (int i = 0; i < rob.Length - 1; ++i) rob[i + 1].Transform(this.Forward[i]);

                    var tool = this.target.Tool.Geometries.Select(mesh => mesh.DuplicateMesh()).ToArray();
                    foreach (Mesh m in tool) m.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.Flange));

                    for (int i = 0; i < rob.Length; ++i) meshes[i] = rob[i];
                    for (int i = rob.Length; i < this.Robot.RobMeshes.Count + tool.Length; ++i) meshes[i] = tool[i - rob.Length];

                    //Maybe a list based implementation is easier.
                    // But this way I'm sure that the out put of "Geometies" has the identical length of "Colors"
                    return meshes;
                }
            }  // Combined 
            public SimpleModle LineModle { 
                get 
                {
                    if (lineModle != null) return lineModle;

                    var planes = Robot.AxisPlanes.Select(p => p.Clone()).ToArray();
                    for (int i = 0; i < planes.Length; ++i) planes[i].Transform(this.Forward[i]);


                    Line[] lines = (Line[])Robot.LineModle.Lines.Clone();
                    for(int i = 0; i < lines.Length; ++i) lines[i].Transform(this.Forward[i]);

                    Brep[] joints = Robot.LineModle.Joints.Select(b => b.DuplicateBrep()).ToArray();
                    for (int i = 0; i < joints.Length; ++i) joints[i].Transform(this.Forward[i]);

                    Curve[] arrows = Robot.LineModle.Arrows.Select(c => c.DuplicateCurve()).ToArray();
                    for (int i = 0; i < arrows.Length; ++i) arrows[i].Transform(this.Forward[i]);

                    for (int i = 0; i < arrows.Length; ++i) 
                        arrows[i].Transform(Rhino.Geometry.Transform.Rotation(-radAngles[i], planes[i].ZAxis, planes[i].Origin));

                    Line tool = this.Tool.SimpleTool.Line;
                    tool.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.Flange));

                    lineModle = new SimpleModle() { Lines = lines, Joints = joints, Arrows = arrows, Tool = tool };
                    return lineModle;
                } 
            }
            public Color[] Colors
            {
                get
                {
                    if (this.jointColors != null) return this.jointColors;

                    Color[] rob = this.jointStates.Select(state => GetColour(state)).Prepend(Axis.Styles.DarkGrey).ToArray();
                    Color[] tool = Enumerable.Range(0, this.Tool.Geometries.Length).Select(_ => Styles.DarkGrey).ToArray();

                    this.jointColors = rob.Concat(tool).ToArray();
                    return jointColors;
                }
                set => this.jointColors = value;
            }



            #endregion Propperties

            #region Methods
            //Public
            public abstract void SetPose(Kernal.Target target);
            public Transform[] GetPose()
            {
                if (this.Forward == null) throw new Exception("Unable to transfromation for empty pose please first call SetPost");

                // Set Transform in relation to current position
                this.Forward = this.Forward.Zip(this.Robot.ResetTransform, (forward, reverse) => reverse * forward).ToArray();

                // Update inverse of current position

                Rhino.Geometry.Transform[] rXform = new Transform[this.Forward.Length];
                for (int i = 0; i < this.Forward.Length; ++i)
                {
                    rXform[i] = Rhino.Geometry.Transform.Identity;
                    var sucsess = this.Forward[i].TryGetInverse(out rXform[i]);
                }
                //var sucsess = this.Forward.Select((xform, idx) => xform.TryGetInverse(out rXform[idx]));
                //this.Reverse = rXform;

                return this.Forward;
            }

            // Display
            public virtual void DrawViewportMeshes(IGH_PreviewArgs args)
            {
                if (this.Colors == null) return;

                var meshColorPairRobot = this.Geometries.Zip(this.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
                foreach (var pair in meshColorPairRobot) args.Display.DrawMeshShaded(pair.Mesh, new Rhino.Display.DisplayMaterial(pair.Color));

                if (this.Tool.Geometries.Length == 0) 
                {
                    var toolcolor = Styles.Orange;
                    args.Display.DrawArrow(LineModle.Tool, toolcolor, 0, 0.2);
                }
            }
            public virtual void DrawViewportWires(IGH_PreviewArgs args)
            {
                Rhino.Display.DisplayMaterial[] Gradient(double[] min, double[] max, double[] value) 
                {
                    var gripPositions = new List<double> { 0, 0.25,0.5,0.75, 1 };
                    var gripColors = new List<Color> { Color.Red, Color.Yellow, Color.Green, Color.Yellow, Color.Red };
                    Grasshopper.GUI.Gradient.GH_Gradient gH_Gradient = new Grasshopper.GUI.Gradient.GH_Gradient(gripPositions, gripColors);
                    var position = value.Select((v, i) => Util.Remap(v, min[i], max[i], 0, 1)).ToArray();
                    return position.Select(d => new Rhino.Display.DisplayMaterial(gH_Gradient.ColourAt(d))).ToArray();
                    
                }
                var linecolor = Styles.DarkGrey;
                var toolcolor = Styles.Orange;
                var surfacecolor = Gradient(Robot.MinAngles.ToArray(), Robot.MaxAngles.ToArray(), Angles);
                int lineThickness = 17;
                int arrowThickness = 3;
                int arrowHeadSize = 20;

                // Lines
                for (int i = 0; i < LineModle.Lines.Length; ++i) args.Display.DrawCurve(LineModle.Lines[i].ToNurbsCurve(), linecolor, lineThickness - (i*2));
                for (int i = 0; i < LineModle.Joints.Length; ++i) args.Display.DrawBrepShaded(LineModle.Joints[i], surfacecolor[i]);
                for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawCurve(LineModle.Arrows[i], linecolor, arrowThickness);
                
                //Tool
                args.Display.DrawArrow(LineModle.Tool, toolcolor, 0, 0.2);

                //int j = 5;
                //args.Display.DrawBrepShaded(LineModle.Joints[j], surfacecolor[j]);

                // Arrowheads
                for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawArrowHead(LineModle.Arrows[i].PointAtStart,
                    LineModle.Arrows[i].TangentAt(LineModle.Arrows[i].Domain.T0)*-1, linecolor, 0, arrowHeadSize);

                for (int i = 0; i < LineModle.Arrows.Length; ++i) args.Display.DrawArrowHead(LineModle.Arrows[i].PointAtEnd,
                    LineModle.Arrows[i].TangentAt(LineModle.Arrows[i].Domain.T1), linecolor, 0, arrowHeadSize);
            }



            //Private
            /// <summary>
            /// Closed form inverse kinematics for a 6 DOF industrial robot. Returns flags for validity and error types.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="overheadSing"></param>
            /// <param name="outOfReach"></param>
            /// <returns></returns>
            protected abstract List<List<double>> TargetInverseKinematics(Plane target, out bool overheadSing, out bool outOfReach, double singularityTol = 5);


            protected static Rhino.Geometry.Transform[] ForwardKinematics(double[] radAngles, Robot robot)
            {
                // Create an array of identety matrices
                Transform[] transforms = new Transform[radAngles.Length];
                transforms = transforms.Select(value => value = Rhino.Geometry.Transform.Identity).ToArray();

                void UpdateRotat(double angle, int level)
                {
                    var range = Enumerable.Range(level, transforms.Length - level);
                    if (range.Count() == 0) return;

                    foreach (int i in range) transforms[i] = (transforms[i] * Rhino.Geometry.Transform.Rotation(angle, robot.AxisPlanes[level].ZAxis, robot.AxisPlanes[level].Origin));
                }
                for (int i = 0; i < radAngles.Length; ++i) UpdateRotat(radAngles[i], i);

                return transforms;
            }

            /// <summary>
            /// Check joint angle values against the robot model data.
            /// </summary>
            /// <param name="angeles"></param>
            /// <param name="robot"></param>
            /// <param name="checkSingularity"></param>
            /// <param name="singularityTol"></param>
            /// <returns>Returns a list of JointStates</returns>
            protected static JointState[] CheckJointAngles(double[] angeles, Robot robot, bool checkSingularity = false, double singularityTol = 5)
            {
                // Check for out of rotation
                JointState[] states = new JointState[angeles.Length];
                for (int i = 0; i < angeles.Length; ++i)
                {
                    if (angeles[i] < robot.MaxAngles[i] && angeles[i] > robot.MinAngles[i]) states[i] = JointState.Normal;
                    else states[i] = JointState.OutOfRotation;
                }

                // Check for Wrist Singularity
                if (checkSingularity && angeles[4] > -singularityTol && angeles[4] < singularityTol)
                {
                    //states[5] = JointStatesValue.Singularity;
                }

                return states;
            }

            /// <summary>
            /// Updates the coresponding signals in the class
            /// </summary>
            /// <param name="states"></param>
            /// <param name="OverHeadSig"></param>
            /// <param name="OutOfReach"></param>
            /// <param name="WristSing"></param>
            /// <param name="OutOfRoation"></param>
            protected static void SetSignals(JointState[] states, out bool OverHeadSig, out bool OutOfReach, out bool WristSing, out bool OutOfRoation)
            {
                bool overHeadSig = false;
                bool outOfReach = false;
                bool wristSing = false;
                bool outOfRoation = false;

                void Update(JointState state)
                {
                    switch (state)
                    {
                        case JointState.Normal:
                            break;

                        case JointState.OutOfRotation:
                            outOfRoation = true;
                            break;

                        case JointState.Singularity:
                            wristSing = true;
                            break;
                    }
                }
                states.ToList().ForEach(state => Update(state));

                OverHeadSig = overHeadSig;
                OutOfReach = outOfReach;
                WristSing = wristSing;
                OutOfRoation = outOfRoation;
            }

            /// <summary>
            /// Retives the coresponding colour for a given JointState
            /// </summary>
            /// <param name="state"></param>
            /// <returns>Colour</returns>
            protected static Color GetColour(JointState state)
            {
                Color color = new Color();
                JointColours.TryGetValue(state, out color);
                return color;
            }

            #endregion Methods

            #region Interfaces

            public bool IsValid => jointStates.All(js => js == JointState.Normal);
            public string IsValidWhyNot => "Pose does not have a valid solution";
            public string TypeName => "RobotPose";
            public string TypeDescription => "This describes a robots position at a given moment";

            public Guid ReferenceID
            {
                get
                {
                    if (id != Guid.Empty) return id;
                    id = System.Guid.NewGuid();
                    return id;
                }
                set
                {
                    if (typeof(Guid) == value.GetType()) id = value;
                }
            }

            public BoundingBox Boundingbox { get; private set; }

            public bool IsReferencedGeometry => false;

            public bool IsGeometryLoaded => false;

            public bool CastFrom(object source) => throw new NotImplementedException();

            public bool CastTo<T>(out T target) => throw new NotImplementedException();

            public abstract IGH_Goo Duplicate();

            public IGH_GooProxy EmitProxy() => null;

            public object ScriptVariable() => this;

            public override string ToString() => $"Position for {this.Robot.Name} [{string.Join(";", this.radAngles.Select(a => a.ToDegrees().ToString("0.00")).ToArray())}]";




            public BoundingBox GetBoundingBox(Transform xform)
            {
                BoundingBox box = BoundingBox.Empty;
                this.Geometries.ToList().ForEach(m => m.Transform(xform));
                this.Geometries.ToList().ForEach(m => box.Union(m.GetBoundingBox(false)));
                this.Boundingbox = box;
                return box;
            }

            public IGH_GeometricGoo DuplicateGeometry()
            {
                GH_Structure<GH_Mesh> geo = new GH_Structure<GH_Mesh>();
                foreach (Mesh m in this.Geometries) 
                {
                    GH_Mesh mesh = new GH_Mesh();
                    GH_Convert.ToGHMesh(m, GH_Conversion.Both, ref mesh);
                    geo.Append(mesh);    
                }

                return (IGH_GeometricGoo)geo;
            }

            public IGH_GeometricGoo Transform(Transform xform)
            {
                GH_Structure<GH_Mesh> geo = new GH_Structure<GH_Mesh>();
                foreach (Mesh m in this.Geometries)
                {
                    m.Transform(xform);
                    GH_Mesh mesh = new GH_Mesh();
                    GH_Convert.ToGHMesh(m, GH_Conversion.Both, ref mesh);
                    geo.Append(mesh);
                }
                return (IGH_GeometricGoo)geo;
            }

            public IGH_GeometricGoo Morph(SpaceMorph xmorph)
            {
                GH_Structure<GH_Mesh> geo = new GH_Structure<GH_Mesh>();
                foreach (Mesh m in this.Geometries)
                {
                    xmorph.Morph(m);
                    GH_Mesh mesh = new GH_Mesh();
                    GH_Convert.ToGHMesh(m, GH_Conversion.Both, ref mesh);
                    geo.Append(mesh);
                }
                return (IGH_GeometricGoo)geo;
            }

            public bool LoadGeometry() => false;

            public bool LoadGeometry(RhinoDoc doc) => false;

            public void ClearCaches()
            {
                throw new NotImplementedException();
            }


            // Serialisation
            public virtual bool Read(GH_IReader reader)
            {
                return true;
            }
            public virtual bool Write(GH_IWriter writer)
            {
                return true;
            }
            #endregion Interfaces

            /// <summary>
            /// Dictionary for the ´colors repersenting the joint states
            /// </summary>
            private static Dictionary<JointState, Color> JointColours = new Dictionary<JointState, Color>()
            {
                { JointState.Normal, Axis.Styles.DarkGrey },
                { JointState.OutOfReach, Axis.Styles.Pink },
                { JointState.OutOfRotation, Axis.Styles.Pink },
                { JointState.WristSing, Axis.Styles.Blue },
                { JointState.OverHeadSing, Axis.Styles.Blue },
            };

            /// <summary>
            /// A list of different joint states.
            /// </summary>
            public enum JointState
            {
                Normal = 0,
                OutOfReach = 1,
                OutOfRotation = 2,
                Singularity = 3,
                WristSing = 4,
                OverHeadSing = 5,
            }
        }
    }

    /// <summary>
    /// Base calss for all robot contollers 
    /// </summary>
    public abstract class Controller : IGH_Goo
    {
        private Guid id;
        public abstract string Name { get; }
        protected  string Type;
        protected  Manufacturer Manufacturer;

        protected List<string> log = new List<string>();
        private EventHandler logUpdate;

        public Controller() { }

        public EventHandler LogEvent 
        {
            get => logUpdate;
            set => logUpdate = value;
        }
        public void Log(string messag) { log.Add(messag); }
        public List<string> LogGet => this.log;
        public void LogClear() => log.Clear();

        public abstract bool Connect();
        public abstract bool LogOff();
        public abstract bool Start();
        public abstract bool Stop();
        public abstract bool Reset();
        public abstract bool SetProgram(List<string> program);
        //public abstract bool GetProgram();
        public abstract bool Stream(Target target);
        public abstract Plane GetTCP();
        public abstract void GetIO();




        public abstract ControllerState State { get; }

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; }

        public Guid ReferenceID
        {
            get
            {
                if (id != Guid.Empty) return id;
                id = System.Guid.NewGuid();
                return id;
            }
            set
            {
                if (typeof(Guid) == value.GetType()) id = value;
            }
        }

        public bool IsReferencedGeometry { get => false; }
        public bool IsGeometryLoaded { get => false; }

        public virtual void ClearCaches() 
        {
            id = Guid.Empty;
            Type = string.Empty;
            Manufacturer = 0;
        }

        public IGH_GeometricGoo DuplicateGeometry() => null;

        protected BoundingBox GetBoundingBox(Transform xform) => BoundingBox.Empty;

        public bool LoadGeometry() => false;

        public bool LoadGeometry(Rhino.RhinoDoc doc) => false;

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => null;

        public IGH_GeometricGoo Transform(Transform xform) => null;

        //IGH_Goo
        public abstract bool IsValid { get; }
        public abstract string IsValidWhyNot { get; }


        public string TypeName => "RobotContoller";
        public string TypeDescription => $"{Manufacturer} robot contoller";

        public bool CastFrom(object source) => false;

        public bool CastTo<T>(out T target)
        {
            target = (T)default;
            return false;
        }

        public IGH_Goo Duplicate() => null;

        public IGH_GooProxy EmitProxy() => null;

        public object ScriptVariable() => this;

        public override string ToString() => $"{Manufacturer} Robot Controller ({Name} - {State})";


        //GH_ISerializable
        public bool Read(GH_IReader reader) 
        {
            id = reader.GetGuid("Guid");
            Type = reader.GetString("ControllerType");
            Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            return true;
        }

        public bool Write(GH_IWriter writer) 
        {
            writer.SetGuid("Guid", id);
            writer.SetString("ControllerType", Type);
            writer.SetInt32("Manufacturer", (int)Manufacturer);
            return true;
        }

        #endregion Interfaces
    }

    /// <summary>
    /// Represents a robot ptogramm
    /// </summary>
    public abstract class Program : IGH_Goo
    {
        protected string name;
        protected string type;
        protected Manufacturer manufacturer;


        public abstract bool SetInstructions(List<Instruction> targets);
        public abstract List<string> GetInstructions();
        public abstract bool Export(string filepath);

        #region Interfaces
        public override string ToString() => $"{manufacturer} {type}: {name}";
        public abstract bool IsValid { get; }
        public abstract string IsValidWhyNot { get; }
        public string TypeName => $"{manufacturer}RobotProgram";
        public string TypeDescription => $"";


        public virtual bool CastFrom(object source) => false;
        public virtual bool CastTo<T>(out T target) 
        {
            target = default(T);
            return false;
        }


        public abstract IGH_Goo Duplicate();
        public object ScriptVariable() => null;
        public IGH_GooProxy EmitProxy() => null;

        public virtual bool Read(GH_IReader reader)
        {
            name = reader.GetString("Name");
            type = reader.GetString("ProgramType");
            manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");


            return true;
        }
        public virtual bool Write(GH_IWriter writer)
        {
            writer.SetString("Name",name);
            writer.SetString("ProgramType",type);
            writer.SetInt32("Manufacturer", (int)manufacturer);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// A single instruction in a Program
    /// </summary>
    public abstract class Procedure  : IGH_Goo
    {
        protected string name;
        protected string type;
        protected Manufacturer manufacturer;


        #region Interfaces
        public override string ToString() => $"{manufacturer} {type} Prcecdure: {name}";
        public abstract bool IsValid { get; }
        public abstract string IsValidWhyNot { get; }
        public string TypeName => $"{manufacturer}RobotProcedure";
        public string TypeDescription => $"";


        public virtual bool CastFrom(object source) => false;
        public virtual bool CastTo<T>(out T target)
        {
            target = default(T);
            return false;
        }


        public abstract IGH_Goo Duplicate();
        public object ScriptVariable() => null;
        public IGH_GooProxy EmitProxy() => null;

        public virtual bool Read(GH_IReader reader)
        {
            name = reader.GetString("Name");
            type = reader.GetString("ProcedureType");
            manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");


            return true;
        }
        public virtual bool Write(GH_IWriter writer)
        {
            writer.SetString("Name", name);
            writer.SetString("ProcedureType", type);
            writer.SetInt32("Manufacturer", (int)manufacturer);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Single robot instruction
    /// </summary>
    public abstract class Instruction : IGH_Goo
    {
        protected Manufacturer manufacturer;

        public abstract string RobStr(Manufacturer manufacturer);

        #region Interfaces
        public abstract override string ToString();
        public abstract bool IsValid { get; }
        public abstract string IsValidWhyNot { get; }
        public string TypeName => $"{manufacturer}Instruction";
        public string TypeDescription => $"Single {manufacturer} robot instruction";

        public abstract bool CastFrom(object source);
        public abstract bool CastTo<T>(out T target);


        public abstract IGH_Goo Duplicate();
        public IGH_GooProxy EmitProxy() => null;
        public abstract object ScriptVariable();


        // Serialisation
        public virtual bool Read(GH_IReader reader)
        {
            return true;
        }

        public virtual bool Write(GH_IWriter writer)
        {
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Robot movement instruction
    /// </summary>
    public abstract class Target : Instruction, IGH_GeometricGoo, Axis_IDisplayable
    {
        #region Class Fields

        private Guid Guid;
        public Plane Plane { get; set; } // Position in World Coordinates

        public Plane TargetPlane; // Position in local coordinates
        public abstract Quaternion Quaternion { get; set; }
        public abstract List<double> JointAngles { get; set; }

        public abstract Speed Speed { get; }
        public abstract Zone Zone { get;  }

        public abstract Tool Tool { get; set; }
        public abstract CSystem CSystem { get; set; }

        public abstract ExtVal ExtRot { get; set; }
        public abstract ExtVal ExtLin { get; set; }
        public abstract MotionType Method { get; }

        #endregion Class Fields

        public Point3d Position { get => this.TargetPlane.Origin; }

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; } //Cached boundingbox

        public Guid ReferenceID
        {
            get
            {
                if (this.Guid != Guid.Empty) return this.Guid;
                this.Guid = Guid.NewGuid();
                return this.Guid;
            }
            set
            {
                if (value.GetType() == typeof(Guid)) this.Guid = value;
            }
        }

        public bool IsReferencedGeometry { get => false; }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public virtual void ClearCaches()
        {
            this.Guid = Guid.Empty;
            this.Plane = Plane.Unset;
            this.TargetPlane = Plane.Unset;
            this.Quaternion = Quaternion.Zero;
            this.JointAngles = null;
            this.Tool = null;
            this.ExtRot = null;
            this.ExtLin = null;

        }

        public IGH_GeometricGoo DuplicateGeometry()
        {
            throw new NotImplementedException();
        }


        public bool LoadGeometry() => false;

        public bool LoadGeometry(Rhino.RhinoDoc doc) => false;

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();



        // IGH_Goo
        public override bool IsValid => true;

        public override string IsValidWhyNot => throw new NotImplementedException();

        public override bool CastFrom(object source) => throw new NotImplementedException();

        public override bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(Q).Name;
                object value = new GH_ObjectWrapper(this);
                target = (Q)value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)) && (this.Plane != null))
            {
                object _Plane = new GH_Plane(this.Plane);
                target = (Q)_Plane;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Point)) && (this.Plane != null))
            {
                object _Point = new GH_Point(this.Plane.Origin);
                target = (Q)_Point;
                return true;
            }
            target = default(Q);
            return false;
        }

        public abstract override IGH_Goo Duplicate();

        public override object ScriptVariable() => this;

        public override string ToString() => $"Target ({Method.ToString()})";

        //GH_ISerializable
        public override bool Read(GH_IReader reader)
        {
            base.Read(reader);
            if (reader.ItemExists("Guid")) this.Guid = reader.GetGuid("Guid");

            if (reader.ChunkExists("Plane"))
            {
                var ghPlane = new GH_Plane();
                var plane = Plane.Unset;
                ghPlane.Read(reader.FindChunk("Plane"));
                if (GH_Convert.ToPlane(ghPlane, ref plane, GH_Conversion.Both))
                    this.Plane = plane;
            }
            if (reader.ChunkExists("PlaneTarget"))
            {
                var ghPlaneTarget = new GH_Plane();
                var targetPlane = Plane.Unset;
                ghPlaneTarget.Read(reader.FindChunk("Plane"));
                GH_Convert.ToPlane(ghPlaneTarget, ref targetPlane, GH_Conversion.Both);
                this.TargetPlane = targetPlane;
            }

            if (reader.ItemExists("Quaternion"))
            {
                int[] Q = new int[4];
                for (int i = 0; i < 4; ++i) Q[i] = reader.GetInt32("Quaternion", i);
                this.Quaternion = new Quaternion(Q[0], Q[1], Q[2], Q[3]);
            }
            if (reader.ItemExists("JointAnglesCount"))
            {
                var JointAnglesCount = reader.GetInt32("JointAnglesCount");
                if (reader.ItemExists("JointAngles"))
                {
                    this.JointAngles = new double[JointAnglesCount].ToList();
                    for (int i = 0; i < JointAnglesCount; ++i)
                        this.JointAngles[i] = reader.GetDouble("JointAngles", i);
                }
            }


            if (reader.ChunkExists("CSystem"))
            {
                this.CSystem = new CSystem();
                var cystemChunk = reader.FindChunk("CSystem");
                this.CSystem.Read(cystemChunk);
            }

            if (reader.ItemExists("ExtRot")) this.ExtRot = reader.GetDouble("ExtRot");
            if (reader.ItemExists("ExtLin")) this.ExtLin = reader.GetDouble("ExtLin");

            return true;
        }

        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);

            if (this.Guid != Guid.Empty)
                writer.SetGuid("Guid", this.Guid);

            if (this.Plane != null)
            {
                var ghPlane = new GH_Plane(this.Plane);
                ghPlane.Write(writer.CreateChunk("Plane"));
            }
            if (this.TargetPlane != null)
            {
                var ghPlaneTarget = new GH_Plane(this.TargetPlane);
                ghPlaneTarget.Write(writer.CreateChunk("PlaneTarget"));
            }

            if (this.Quaternion != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    switch (i)
                    {
                        case 0:
                            writer.SetInt32("Quaternion", i, (int)this.Quaternion.A); break;
                        case 1:
                            writer.SetInt32("Quaternion", i, (int)this.Quaternion.B); break;
                        case 2:
                            writer.SetInt32("Quaternion", i, (int)this.Quaternion.C); break;
                        case 3:
                            writer.SetInt32("Quaternion", i, (int)this.Quaternion.D); break;
                    }
                }
            }
            if (this.JointAngles != null)
            {
                writer.SetInt32("JointAnglesCount", this.JointAngles.Count);
                for (int i = 0; i < this.JointAngles.Count; ++i)
                {
                    writer.SetDouble("JointAngles", i, this.JointAngles[i]);
                }
            }


            if (this.CSystem != null)
            {
                var cystemChunk = writer.CreateChunk("CSystem");
                this.CSystem.Write(cystemChunk);
            }

            if (this.ExtRot != null)
                writer.SetDouble("ExtRot", this.ExtRot);
            if (this.ExtLin != null)
                writer.SetDouble("ExtLin", this.ExtLin);

            return true;
        }


        //Display
        public void DrawViewportWires(IGH_PreviewArgs args)
        {

            double sizeLine = 70; double sizeArrow = 30; int thickness = 3;

            args.Display.DrawLineArrow(
                new Line(this.Plane.Origin, this.Plane.XAxis, sizeLine),
                Axis.Styles.Pink,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(this.Plane.Origin, this.Plane.YAxis, sizeLine),
                Axis.Styles.LightBlue,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(this.Plane.Origin, this.Plane.ZAxis, sizeLine),
                Axis.Styles.LightGrey,
                thickness,
                sizeArrow);
        }
        public void DrawZone(IGH_PreviewArgs args) 
        {
            args.Display.DrawSphere(new Sphere(this.Plane, this.Zone.PathRadius), Styles.MediumWhite);
        }
        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            throw new NotImplementedException();
        }

        #endregion Interfaces

    }

    /// <summary>
    /// Robot code instruction
    /// </summary>
    public abstract class Tool: IGH_GeometricGoo, Axis_IDisplayable
    {
        #region Variables
        private SimpleGeo simpleTool;
        #endregion Variables

        #region Propperties 
        private Guid ID { get; set; }
        public string Name { get; set; }
        public Plane TCP { get; set; }
        public abstract Manufacturer Manufacturer { get; }
        public abstract string Declaration { get; }
        

        /* @ todo Change Geometries to ToolMesh
         * @ body Geometries will be reserved for the robot class where it discribes a copy of both rob and tool mesh.
         */
        public Mesh[] Geometries { get; set; }
        public class SimpleGeo { public Line Line; }
        public SimpleGeo SimpleTool { 
            get 
            {
                if (simpleTool != null) return simpleTool;
                simpleTool = DrawSimpleTool();
                return simpleTool;   
            } 
            private set 
            {
                simpleTool = value;
            } 
        }


        public Transform FlangeOffset
        {
            get
            {
                return Rhino.Geometry.Transform.PlaneToPlane(TCP, Plane.WorldXY);
            }
        }


        #endregion Propperties 

        #region Methods
        //Display
        private SimpleGeo DrawSimpleTool()
        {
            return new SimpleGeo() { Line = new Line(Plane.WorldXY.Origin, this.TCP.Origin) };
        }
        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Color color = Styles.DarkGrey;
            args.Display.DrawLineArrow(SimpleTool.Line, color, 10, 10);
        }
        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            var color = new Rhino.Display.DisplayMaterial(Axis.Styles.MediumGrey);
            foreach (var geo in Geometries) args.Display.DrawMeshShaded(geo, color);
        }
        #endregion Methods

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; }

        public bool IsGeometryLoaded { get; }
        public bool IsReferencedGeometry { get; }

        //IGH_Goo
        public bool IsValid => true;

        public string IsValidWhyNot { get { return ""; } }
        public Guid ReferenceID { get; set; }
        public string TypeDescription => "Robot end effector";

        public string TypeName => "Tool";
        public override string ToString() => $"Tool: {this.Name}";


        public bool CastFrom(object o) => false;

        public bool CastTo<T>(out T target)
        {
            target = default(T);

            if (typeof(T).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(T).Name;
                object value = new GH_ObjectWrapper(this);
                target = (T)value;
                return true;
            }

            return false;
        }

        public virtual void ClearCaches()
        {
            this.Name = string.Empty;
            this.ID = Guid.Empty;
            this.TCP = Plane.Unset;
            this.Geometries = null;
        }

        public abstract IGH_Goo Duplicate();

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public IGH_GooProxy EmitProxy() => null;

        public BoundingBox GetBoundingBox(Transform xform)
        {
            BoundingBox box = BoundingBox.Empty;
            foreach (Mesh m in this.Geometries) m.Transform(xform);
            foreach (Mesh m in this.Geometries) box.Union(m.GetBoundingBox(false));
            this.Boundingbox = box;
            return box;
        }

        public bool LoadGeometry() => false;
        public object ScriptVariable() => this;

        public bool LoadGeometry(Rhino.RhinoDoc doc) => false;

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();
        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();


        //GH_ISerializable
        public virtual bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("Name")) this.Name = reader.GetString("Name");
            if (reader.ItemExists("GUID")) this.ID = reader.GetGuid("GUID");
            if (reader.ChunkExists("TCP"))
            {
                var chunk1 = reader.FindChunk("TCP");
                if (chunk1 != null)
                {
                    var data = new GH_Plane();
                    var plane = new Plane();
                    data.Read(chunk1);
                    GH_Convert.ToPlane(data, ref plane, GH_Conversion.Both);
                    this.TCP = plane;
                }
            }

            List<Mesh> meshes = new List<Mesh>();
            if (reader.ItemExists("GeometryCout"))
            {
                int geometriesCount = reader.GetInt32("GeometryCout");
                for (int i = 0; i < geometriesCount; ++i)
                {
                    if (reader.ChunkExists("Geometry", i))
                    {
                        Mesh mesh = new Mesh();
                        GH_Mesh gh_mesh = new GH_Mesh();
                        var cunckMesh = reader.FindChunk("Geometry", i);
                        gh_mesh.Read(cunckMesh);
                        bool sucsess = GH_Convert.ToMesh(gh_mesh, ref mesh, GH_Conversion.Both);
                        meshes.Add(mesh);
                    }
                }
            }
            this.Geometries = meshes.ToArray();
            return true;
        }

        public virtual bool Write(GH_IWriter writer)
        {
            writer.SetString("Name", this.Name);
            writer.SetGuid("GUID", ID);

            GH_Plane gH_TCP = new GH_Plane(this.TCP);
            gH_TCP.Write(writer.CreateChunk("TCP"));

            if (this.Geometries.Length != 0)
            {
                writer.SetInt32("GeometryCout", this.Geometries.Length);
                for (int i = 0; i < this.Geometries.Length; ++i)
                {
                    GH_Mesh mesh = new GH_Mesh(this.Geometries[i]);
                    mesh.Write(writer.CreateChunk("Geometry", i));
                }
            }

            return true;
        }

        #endregion Interfaces
    }

}