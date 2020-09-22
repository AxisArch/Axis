using Axis.Types;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Axis.Kernal
{
    /// <summary>
    /// Base class for components that require login
    /// </summary>
    public abstract class AxisLogin_Component : GH_Component
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

    /// <summary>
    /// Base calss for all robot systems
    /// </summary>
    public abstract class Robot : IGH_GeometricGoo
    {
        public string Name = "Wall-E"; // This variable can hold the model number
        private Guid id = Guid.Empty;
        public Manufacturer Manufacturer { get; protected set; }
        public Plane RobBasePlane { get; protected set; }
        public List<Plane> AxisPlanes { get; protected set; }
        public List<double> MinAngles { get; protected set; }
        public List<double> MaxAngles { get; protected set; }
        public List<int> Indices { get; protected set; }
        public List<Mesh> RobMeshes { get; protected set; }

        public ManipulatorPose CurrentPose { get; private set; }
        public double WristOffsetLength { get => this.AxisPlanes[5].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double LowerArmLength { get => this.AxisPlanes[1].Origin.DistanceTo(this.AxisPlanes[2].Origin); }
        public double UpperArmLength { get => this.AxisPlanes[2].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double AxisFourOffsetAngle { get => Math.Atan2(this.AxisPlanes[4].Origin.Z - this.AxisPlanes[2].Origin.Z, this.AxisPlanes[4].Origin.X - this.AxisPlanes[2].Origin.X); }  // =>0.22177 for IRB 6620 //This currently limitting the robot to be in a XZ configuration

        public Plane Flange { get => this.AxisPlanes[5].Clone(); }
        private Transform[] resetTransform = null;

        public Transform[] ResetTransform
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
        public Color[] Colors { get => CurrentPose.Colors; }

        public Mesh[] Geometries { get => RobMeshes.ToArray(); }

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Robot() { }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Set the default robot pose.
        /// </summary>
        public void SetPose()
        {
            this.SetPose(Types.Target.Default);
        }

        /// <summary>
        /// Set the pose based on a target TCP/flange position.
        /// </summary>
        /// <param name="target"></param>
        public void SetPose(Types.Target target)
        {
            this.CurrentPose = new Robot.ManipulatorPose(this, target);
        }

        /// <summary>
        /// Set the pose for a given robot based on a target TCP/flange position.
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="checkValidity"></param>
        public void SetPose(ManipulatorPose pose, bool checkValidity = false)
        {
            if (checkValidity)
            {
                if (pose.IsValid)
                {
                    pose.UpdateRobot(this); // <-- this oen should not have been nessesary if the class had been propperly persistant.
                    this.CurrentPose = pose;
                }
                else if (pose.JointStates != null) this.CurrentPose.JointStates = pose.JointStates;
            }
            else this.CurrentPose = pose; ;
        }

        /// <summary>
        /// Update the current pose.
        /// </summary>
        public void UpdatePose()
        {
            var xform = this.CurrentPose.GetPose().ToList();
            for (int i = 0; i < this.RobMeshes.Count - 1; ++i) this.RobMeshes[i + 1].Transform(xform[i]);
            this.ResetTransform = this.CurrentPose.Reverse;
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
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.Name = null;
            this.Manufacturer = 0;
            this.RobBasePlane = Plane.Unset;
            this.AxisPlanes = null;
            this.MinAngles = null;
            this.MaxAngles = null;
            this.Indices = null;
            this.RobMeshes = null;
            this.CurrentPose = null;
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

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        //IGH_Goo
        public bool IsValid { get => (this.CurrentPose != null) ? this.CurrentPose.IsValid : false; }

        public string IsValidWhyNot => "Since no or no valid poes has been set there is no representation possible for this robot";
        public string TypeName => "Manipulator";
        public string TypeDescription => "Robot movment system";

        public bool CastFrom(object source)
        {
            if (source.GetType() == typeof(Robot))
            {
                Robot manipulator = source as Robot;
                this.Name = manipulator.Name;
                this.Manufacturer = manipulator.Manufacturer;
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
        public bool Read(GH_IReader reader)
        {
            this.Name = reader.GetString("RobotName");
            this.Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
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

            this.SetPose();
            return true;
        }

        public bool Write(GH_IWriter writer)
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
            writer.SetInt32("Manufacturer", (int)this.Manufacturer);
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

        /// <summary>
        /// Class to hold the values describing the tansformation of a tool
        /// </summary>
        public class ManipulatorPose : IGH_Goo
        {
            private Robot robot;
            private Axis.Types.Target target;
            private double[] radAngles;
            private JointState[] jointStates;

            private bool outOfReach = false;
            private bool outOfRoation = false;
            private bool overHeadSig = false;
            private bool wristSing = false;

            public double[] Angles { get => this.radAngles.Select(d => d.ToDegrees()).ToArray(); }

            public JointState[] JointStates
            {
                get => jointStates;
                set { jointStates = value; }
            }

            public bool OutOfReach { get => outOfReach; }
            public bool OutOfRoation { get => outOfRoation; }
            public bool OverHeadSig { get => overHeadSig; }
            public bool WristSing { get => wristSing; }

            // Mesh transformations
            private Transform[] Forward;

            private Transform[] reverse = null;

            public Transform[] Reverse
            {
                get
                {
                    if (reverse != null) return reverse;
                    GetPose();
                    return reverse; //<-- Not sure if this code is ever gonna be called.
                }
                set => reverse = value;
            }

            // Mesh colours
            private static Dictionary<JointState, Color> JointColours = new Dictionary<JointState, Color>()
            {
                { JointState.Normal, Axis.Styles.DarkGrey },
                { JointState.OutOfReach, Axis.Styles.Pink },
                { JointState.OutOfRotation, Axis.Styles.Pink },
                { JointState.WristSing, Axis.Styles.Blue },
                { JointState.OverHeadSing, Axis.Styles.Blue },
            };

            public Plane[] Planes
            {
                get
                {
                    Plane[] planes = this.robot.AxisPlanes.Select(plane => plane.Clone()).ToArray();
                    for (int i = 0; i < planes.Length; ++i) planes[i].Transform(this.robot.ResetTransform[i] * this.Forward[i]);
                    return planes;
                }
            }

            public Plane Flange
            {
                get
                {
                    Plane flange = this.robot.AxisPlanes[5].Clone();
                    flange.Transform(Forward[5]);
                    return flange;
                }
            }

            public Plane Target
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

            public Types.Tool Tool => this.target.Tool;
            public Types.Speed Speed => this.target.Speed;

            public Mesh[] Geometries
            {
                get
                {
                    Mesh[] meshes = new Mesh[this.robot.RobMeshes.Count + this.target.Tool.Geometries.Length];

                    var rob = this.robot.RobMeshes.Select(mesh => mesh.DuplicateMesh()).ToArray();
                    for (int i = 0; i < rob.Length - 1; ++i) rob[i + 1].Transform(this.robot.ResetTransform[i] * this.Forward[i]);

                    var tool = this.target.Tool.Geometries.Select(mesh => mesh.DuplicateMesh()).ToArray();
                    foreach (Mesh m in tool) m.Transform(this.target.Tool.ResetTransform * Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, this.Flange));

                    for (int i = 0; i < rob.Length; ++i) meshes[i] = rob[i];
                    for (int i = rob.Length; i < this.robot.RobMeshes.Count + tool.Length; ++i) meshes[i] = tool[i - rob.Length];

                    //Maybe a list based implementation is easier.
                    // But this way I'm sure that the out put of "Geometies" has the identical length of "Colors"
                    return meshes;
                }
            }

            public Color[] Colors
            {
                get
                {
                    if (this.jointStates == null) return null;

                    var jointColors = this.jointStates.Select(state => GetColour(state)).ToArray();
                    var tool = this.target.Tool.Colors;

                    Color[] colors = new Color[this.robot.Geometries.Length + this.target.Tool.Geometries.Length];

                    colors[0] = Axis.Styles.DarkGrey; //Base Color
                    for (int i = 1; i < jointColors.Length + 1; ++i) colors[i] = jointColors[i - 1]; // Joint Colours
                    for (int i = 1 + jointColors.Length; i < this.robot.Geometries.Length; ++i) colors[i] = Axis.Styles.DarkGrey; //Left over Colors

                    for (int i = this.robot.Geometries.Length; i < this.robot.Geometries.Length + this.target.Tool.Geometries.Length; ++i) colors[i] = tool[i - this.robot.Geometries.Length]; // Tool Color

                    //Maybe a list based implementation is easier.
                    // But this way I'm sure that the out put of "Geometies" has the identical length of "Colors"

                    return colors;
                }
            }

            #region Constructors

            public ManipulatorPose(Robot robot)
            {
                this.robot = robot;
            }

            public ManipulatorPose(Robot robot, Axis.Types.Target target)
            {
                this.robot = robot;
                this.SetPose(target);
            }

            #endregion Constructors

            #region Methods

            //Public
            public void SetPose(Types.Target target)
            {
                this.target = target;

                double[] radAngles = new double[6];
                double[] degAngles = new double[6];
                JointState[] jointStates = new JointState[6];

                //Invers and farward kinematics devided by manufacturar
                switch (this.robot.Manufacturer)
                {
                    case Manufacturer.ABB:
                        switch (target.Method)
                        {
                            case MotionType.Linear:
                            case MotionType.Joint:

                                // Compute the flane position by moving the TCP to the base of the tool
                                Plane flange = target.Plane.Clone();
                                Rhino.Geometry.Transform t1 = Rhino.Geometry.Transform.PlaneToPlane(flange, Plane.WorldXY);
                                Rhino.Geometry.Transform t2; t1.TryGetInverse(out t2);
                                flange.Transform(t2 * target.Tool.FlangeOffset * t1);

                                //double[,] anglesSet = newTargetInverseKinematics(target, out overHeadSig, out outOfReach);
                                List<List<double>> anglesSet = TargetInverseKinematics(flange, out overHeadSig, out outOfReach);

                                // Select Solution based on Indecies
                                //for (int i = 0; i < anglesSet.Length / anglesSet.GetLength(1); ++i) degAngles[i] = anglesSet[i, this.Robot.Indices[i]];
                                radAngles = anglesSet.Zip<List<double>, int, double>(this.robot.Indices, (solutions, selIdx) => solutions[selIdx]).ToArray();
                                degAngles = radAngles.Select(a => a.ToDegrees()).ToArray();

                                jointStates = CheckJointAngles(degAngles, this.robot, checkSingularity: true);

                                break;

                            case MotionType.AbsoluteJoint:
                                degAngles = target.JointAngles.ToArray();
                                radAngles = degAngles.Select(a => a.ToRadians()).ToArray();

                                jointStates = CheckJointAngles(degAngles, this.robot);

                                break;

                            default:
                                throw new NotImplementedException($"This movment: {target.Method.ToString()} has not jet been implemented for {this.robot.Manufacturer.ToString()}"); ;
                        }
                        break;

                    case Manufacturer.Kuka:
                        switch (target.Method)
                        {
                            case MotionType.AbsoluteJoint:

                                degAngles = target.JointAngles.ToArray();
                                jointStates = CheckJointAngles(degAngles, this.robot);

                                radAngles = degAngles.Select(value => value.ToRadians()).ToArray();
                                break;

                            default:
                                throw new NotImplementedException($"This movment: {target.Method.ToString()} has not jet been implemented for {this.robot.Manufacturer.ToString()}");
                        }
                        break;
                }

                SetSignals(jointStates, out this.outOfReach, out this.outOfRoation, out this.wristSing, out this.overHeadSig);
                this.JointStates = jointStates;

                this.radAngles = radAngles;

                this.Forward = ForwardKinematics(radAngles, this.robot);

                this.IsValid = (!outOfReach && !outOfRoation && !overHeadSig && !wristSing) ? true : false;
            }

            public void UpdateRobot(Robot robot) => this.robot = robot;

            public Transform[] GetPose()
            {
                if (this.Forward == null) throw new Exception("Unable to transfromation for empty pose please first call SetPost");

                // Set Transform in relation to current position
                this.Forward = this.Forward.Zip(this.robot.ResetTransform, (forward, reverse) => reverse * forward).ToArray();

                // Update inverse of current position

                Rhino.Geometry.Transform[] rXform = new Transform[this.Forward.Length];
                for (int i = 0; i < this.Forward.Length; ++i)
                {
                    rXform[i] = Rhino.Geometry.Transform.Identity;
                    var sucsess = this.Forward[i].TryGetInverse(out rXform[i]);
                }
                //var sucsess = this.Forward.Select((xform, idx) => xform.TryGetInverse(out rXform[idx]));
                this.Reverse = rXform;

                return this.Forward;
            }

            //Private
            /// <summary>
            /// Closed form inverse kinematics for a 6 DOF industrial robot. Returns flags for validity and error types.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="overheadSing"></param>
            /// <param name="outOfReach"></param>
            /// <returns></returns>
            private List<List<double>> TargetInverseKinematics(Plane target, out bool overheadSing, out bool outOfReach, double singularityTol = 5)
            {
                //////////////////////////////////////////////////////////////
                //// Setup of the vartiables needed for Iverse Kinematics ////
                //////////////////////////////////////////////////////////////

                //Setting up Planes
                Plane P0 = this.robot.AxisPlanes[0].Clone();
                Plane P1 = this.robot.AxisPlanes[1].Clone();
                Plane P2 = this.robot.AxisPlanes[2].Clone();
                Plane P3 = this.robot.AxisPlanes[3].Clone();
                Plane P4 = this.robot.AxisPlanes[4].Clone();
                Plane P5 = this.robot.AxisPlanes[5].Clone();
                Plane flange = target.Clone();

                // Setting Up Lengths
                double wristOffsetLength = this.robot.WristOffsetLength;
                double lowerArmLength = this.robot.LowerArmLength;
                double upperArmLength = this.robot.UpperArmLength;
                double axisFourOffsetAngle = this.robot.AxisFourOffsetAngle;

                //////////////////////////////////////////////////////////////
                //// Everything past this should be local to the Function ////
                //////////////////////////////////////////////////////////////

                double UnWrap(double value)
                {
                    while (value >= Math.PI) value -= 2 * Math.PI;
                    while (value < -Math.PI) value += 2 * Math.PI;

                    return value;
                }
                double AngleOnPlane(Plane plane, Point3d point)
                {
                    double outX, outY;
                    plane.ClosestParameter(point, out outX, out outY);

                    return Math.Atan2(outY, outX);
                }

                // Validity checks
                bool unreachable = true;
                bool singularity = false;

                // Lists of doubles to hold our axis values and our output log.
                List<double> a1list = new List<double>(),
                    a2list = new List<double>(),
                    a3list = new List<double>(),
                    a4list = new List<double>(),
                    a5list = new List<double>(),
                    a6list = new List<double>();

                // Find the wrist position by moving back along the robot flange the distance of the wrist link.
                Point3d WristLocation = new Point3d(flange.PointAt(0, 0, -wristOffsetLength));

                // Check for overhead singularity and add message to log if needed
                bool checkForOverheadSingularity(Point3d wristLocation, Point3d bPlane)
                {
                    if ((bPlane.Y - WristLocation.Y) < singularityTol && (bPlane.Y - WristLocation.Y) > -singularityTol &&
                         (bPlane.X - WristLocation.X) < singularityTol && (bPlane.X - WristLocation.X) > -singularityTol)
                        return true;
                    else return false;
                }
                singularity = checkForOverheadSingularity(WristLocation, P0.Origin);

                double angle1 = AngleOnPlane(P0, WristLocation);

                // Standard cases for axis one.
                if (angle1 > Math.PI) angle1 -= 2 * Math.PI;
                for (int j = 0; j < 4; j++)
                    a1list.Add(angle1);

                // Other cases for axis one.
                angle1 += Math.PI;
                if (angle1 > Math.PI) angle1 -= 2 * Math.PI;
                for (int j = 0; j < 4; j++)
                    a1list.Add(1 * angle1);

                // Generate four sets of values for each option of axis one
                for (int j = 0; j < 2; j++)
                {
                    angle1 = a1list[j * 4];

                    // Rotate all of our points based on axis one.
                    Transform Rotation1 = Rhino.Geometry.Transform.Rotation(angle1, P0.ZAxis, P0.Origin);

                    Plane P1A = new Plane(P1); P1A.Transform(Rotation1);
                    Plane P2A = new Plane(P2); P2A.Transform(Rotation1);
                    Plane P3A = new Plane(P3); P3A.Transform(Rotation1);
                    Plane P4A = new Plane(P4); P4A.Transform(Rotation1);
                    Plane P5A = new Plane(P5); P5A.Transform(Rotation1);

                    // Create our spheres for doing the intersections.
                    Sphere Sphere1 = new Sphere(P1A, lowerArmLength);
                    Sphere Sphere2 = new Sphere(WristLocation, upperArmLength);
                    Circle Circ = new Circle();

                    double Par1 = new double(), Par2 = new double();

                    // Do the intersections and store them as pars.
                    Rhino.Geometry.Intersect.Intersection.SphereSphere(Sphere1, Sphere2, out Circ);
                    Rhino.Geometry.Intersect.Intersection.PlaneCircle(P1A, Circ, out Par1, out Par2);

                    // Logic to check if the target is unreachable.
                    if (unreachable)
                        if (Par1 != double.NaN || Par2 != double.NaN)
                            unreachable = false;

                    // Get the points.
                    Point3d IntersectPt1 = Circ.PointAt(Par1), IntersectPt2 = Circ.PointAt(Par2);

                    // Solve IK for the remaining axes using these points.
                    for (int k = 0; k < 2; k++)
                    {
                        Point3d ElbowPt = new Point3d();

                        if (k == 0) ElbowPt = IntersectPt1;
                        else ElbowPt = IntersectPt2;

                        double angle2 = AngleOnPlane(P1A, ElbowPt);

                        Transform Rotation2 = Rhino.Geometry.Transform.Rotation(angle2, P1A.ZAxis, P1A.Origin);
                        Plane P2B = P2A.Clone(); P2B.Transform(Rotation2);
                        Plane P3B = P3A.Clone(); P3B.Transform(Rotation2);
                        Plane P4B = P4A.Clone(); P4B.Transform(Rotation2);
                        Plane P5B = P5A.Clone(); P5B.Transform(Rotation2);

                        double angle3 = AngleOnPlane(P2B, WristLocation) + axisFourOffsetAngle;

                        // Add Twice to list
                        for (int n = 0; n < 2; n++)
                        {
                            a2list.Add(angle2);
                            a3list.Add(UnWrap(angle3));
                        }

                        for (int n = 0; n < 2; n++)
                        {
                            Transform Rotation3 = Rhino.Geometry.Transform.Rotation(angle3, P2B.ZAxis, P2B.Origin);
                            Plane P3C = P3B.Clone(); P3C.Transform(Rotation3);
                            Plane P4C = P4B.Clone(); P4C.Transform(Rotation3);
                            Plane P5C = P5B.Clone(); P5C.Transform(Rotation3);

                            double angle4 = AngleOnPlane(P3C, flange.Origin);
                            if (n == 1) angle4 += Math.PI;
                            a4list.Add(UnWrap(angle4));

                            Transform Rotation4 = Rhino.Geometry.Transform.Rotation(angle4, P3C.ZAxis, P3C.Origin);
                            Plane P4D = P4C.Clone(); P4D.Transform(Rotation4);
                            Plane P5D = P5C.Clone(); P5D.Transform(Rotation4);

                            double angle5 = AngleOnPlane(P4D, flange.Origin);
                            a5list.Add(angle5);

                            Transform Rotation5 = Rhino.Geometry.Transform.Rotation(angle5, P4D.ZAxis, P4D.Origin);

                            Plane P5E = P5D.Clone(); P5E.Transform(Rotation5);

                            double angle6 = AngleOnPlane(P5E, flange.PointAt(1, 0));
                            a6list.Add(angle6);
                        }
                    }
                }

                //////////////////////////////////////////////////////////////////////
                ////// Cleaning up and preping returning the angles //////////////////
                //////////////////////////////////////////////////////////////////////

                // Compile our list of all axis angle value lists.
                List<List<double>> angles = new List<List<double>>();
                angles.Add(a1list); angles.Add(a2list); angles.Add(a3list); angles.Add(a4list); angles.Add(a5list); angles.Add(a6list);

                // Update validity based on flags
                outOfReach = unreachable;
                overheadSing = singularity;

                return angles; // Return the angles.
            }

            private static Rhino.Geometry.Transform[] ForwardKinematics(double[] radAngles, Robot robot)
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
            private static JointState[] CheckJointAngles(double[] angeles, Robot robot, bool checkSingularity = false, double singularityTol = 5)
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
            private static void SetSignals(JointState[] states, out bool OverHeadSig, out bool OutOfReach, out bool WristSing, out bool OutOfRoation)
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
            private static Color GetColour(JointState state)
            {
                Color color = new Color();
                JointColours.TryGetValue(state, out color);
                return color;
            }

            #endregion Methods

            #region Interfaces

            public bool IsValid { get; private set; } = false;
            public string IsValidWhyNot => "Pose does not have a valid solution";
            public string TypeName => "Robot Pose";
            public string TypeDescription => "This describes a robots position at a given moment";

            public bool CastFrom(object source) => throw new NotImplementedException();

            public bool CastTo<T>(out T target) => throw new NotImplementedException();

            public IGH_Goo Duplicate()
            {
                if (this.target == null) return new ManipulatorPose(this.robot);
                else return new ManipulatorPose(this.robot, this.target);
            }

            public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

            public object ScriptVariable() => this;

            public override string ToString() => $"Position for {this.robot.Name} [{string.Join(";", this.radAngles.Select(a => a.ToDegrees().ToString("0.00")).ToArray())}]";

            public bool Read(GH_IReader reader) => throw new NotImplementedException();

            public bool Write(GH_IWriter writer) => throw new NotImplementedException();

            #endregion Interfaces

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
}