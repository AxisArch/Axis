using System;
using System.Drawing;
using System.Collections.Generic;
using static System.Math;

using Rhino.Geometry;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using Axis;
using Microsoft.Office.Interop.Excel;
using System.Linq;

namespace Axis.Core 
{
    /// <summary>
    /// Create custom industrial robot configurations.
    /// </summary>
    public class Manipulator : IGH_GeometricGoo, Axis_Displayable<Mesh>
    {
        public string Name = "Wall-E"; // This variable can hold the model number
        private Guid id = Guid.Empty;
        public Manufacturer Manufacturer { get; private set; }
        public Plane RobBasePlane { get; private set; }
        public List<Plane> AxisPlanes { get; private set; }
        public List<double> MinAngles { get; private set; }
        public List<double> MaxAngles { get; private set; }
        public List<int> Indices { get; private set; }
        public List<Mesh> RobMeshes { get; private set; }

        public ManipulatorPose CurrentPose { get; private set; }
        public double WristOffsetLength { get => this.AxisPlanes[5].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double LowerArmLength { get => this.AxisPlanes[1].Origin.DistanceTo(this.AxisPlanes[2].Origin); }
        public double UpperArmLength { get => this.AxisPlanes[2].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double AxisFourOffsetAngle { get => Math.Atan2(this.AxisPlanes[4].Origin.Z - this.AxisPlanes[2].Origin.Z, this.AxisPlanes[4].Origin.X - this.AxisPlanes[2].Origin.X); }  // =>0.22177 for IRB 6620 //This currently limitting the robot to be in a XZ configuration

        public Plane Flange { get => this.AxisPlanes[5].Clone(); }
        private Transform[] resetTransform = null;
        public Transform[] ResetTransform { get 
            {
                // Check if transform already has been applied
                if (this.resetTransform != null) return resetTransform;

                var rT = new Transform[this.RobMeshes.Count-1]; // <--- Probably change this to the number of meshes
                for (int i = 0; i < this.RobMeshes.Count -1; ++i) rT[i] = Rhino.Geometry.Transform.Identity;
                resetTransform = rT;
                return rT;

            } set => resetTransform = value; 
        }

        //Used for Axis display pipeline
        public Color[] Colors { get => CurrentPose.Colors; }
        public Mesh[] Geometries { get => RobMeshes.ToArray(); }

        #region Constructors
        /// <summary>
        /// Built-in default robot - ABB IRB 120.
        /// </summary>
        public static Manipulator Default { get => IRB120; }

        /// <summary>
        /// Built-in ABB IRB 120.
        /// </summary>
        public static Manipulator IRB120 { get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB120mesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Manufacturer manufacturer = Manufacturer.ABB;
                Plane[] axisPlanes = new Plane[6] {
                    new Plane(new Point3d(0, 0, 0), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
                    new Plane(new Point3d(0, 0, 290), new Vector3d(0,0,1), new Vector3d(1,0,0)),
                    new Plane(new Point3d(0, 0, 560), new Vector3d(1,0,0), new Vector3d(0,0,-1)),
                    new Plane(new Point3d(150, 0, 630), new Vector3d(0,0,-1), new Vector3d(0,1,0)),
                    new Plane(new Point3d(302, 0, 630), new Vector3d(1,0,0), new Vector3d(0,0,-1)),
                    new Plane(new Point3d(374, 0, 630), new Vector3d(0,0,-1), new Vector3d(0,1,0)),
                };
                List<double> minAngles = new List<double> { -165, -110, -110, -160, -120, -400 };
                List<double> maxAngles = new List<double> { 165, 110, 70, 160, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Manipulator(manufacturer, axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 120";
                return robot;

                //Manipulator manipulator;
                //
                //using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB120))
                //{
                //    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //    manipulator = (Manipulator)br.Deserialize(ms);
                //}
                //
                //return manipulator;
            }
        }

        /// <summary>
        /// Built-in ABB IRB 6620.
        /// </summary>
        public static Manipulator IRB6620
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB6620mesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Manufacturer manufacturer = Manufacturer.ABB;
                Plane[] axisPlanes = new Plane[6]
                    {
                        new Plane(new Point3d(0, 0, 0), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
                        new Plane(new Point3d(320, 0, 680), new Vector3d(0,0,1), new Vector3d(1,0,0)),
                        new Plane(new Point3d(320, 0, 1655), new Vector3d(1,0,0), new Vector3d(0,0,-1)),
                        new Plane(new Point3d(502, 0, 1855), new Vector3d(0,0,-1), new Vector3d(0,1,0)),
                        new Plane(new Point3d(1207, 0, 1855), new Vector3d(1,0,0), new Vector3d(0,0,-1)),
                        new Plane(new Point3d(1407, 0, 1855), new Vector3d(0,0,-1), new Vector3d(0,1,0)),
                        };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -300 };
                List<double> maxAngles = new List<double> { 170, 140, 70, 300, 130, 300 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Manipulator(manufacturer, axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6620";
                return robot;

                //Manipulator manipulator;
                //
                //using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB6620))
                //{
                //    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //    manipulator = (Manipulator)br.Deserialize(ms);
                //}
                //
                //return manipulator;

            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Manipulator() { }

        /// <summary>
        /// Stanard robot constructor method.
        /// </summary>
        /// <param name="manufacturer"></param>
        /// <param name="axisPlanes"></param>
        /// <param name="minAngles"></param>
        /// <param name="maxAngles"></param>
        /// <param name="robMeshes"></param>
        /// <param name="basePlane"></param>
        /// <param name="indices"></param>
        public Manipulator(Manufacturer manufacturer, Plane[] axisPlanes, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
        {
            this.Manufacturer = manufacturer;

            this.AxisPlanes = axisPlanes.ToList();

            this.MinAngles = minAngles;
            this.MaxAngles = maxAngles;

            this.RobMeshes = robMeshes;
            this.RobBasePlane = basePlane;

            this.Indices = indices;

            // Transformation
            Rhino.Geometry.Transform Remap = Rhino.Geometry.Transform.PlaneToPlane(AxisPlanes[0], this.RobBasePlane);

            Plane[] tempPlanes = this.AxisPlanes.Select(plane => plane.Clone()).ToArray();
            //tempPlanes.ForEach(p => p.Transform(Remap));
            for (int i = 0; i < tempPlanes.Length; ++i) tempPlanes[i].Transform(Remap);
            this.AxisPlanes = tempPlanes.ToList();

            // Then transform all of these meshes based on the input base plane.
            List<Mesh> tempMesh = this.RobMeshes.Select(m => m.DuplicateMesh()).ToList();
            tempMesh.ForEach(m => m.Transform(Remap));
            this.RobMeshes = tempMesh;

            this.SetPose();

        }
        #endregion

        #region Methods
        /// <summary>
        /// Set the default robot pose.
        /// </summary>
        public void SetPose()
        {
            this.SetPose(Targets.Target.Default);
        }

        /// <summary>
        /// Set the pose based on a target TCP/flange position.
        /// </summary>
        /// <param name="target"></param>
        public void SetPose(Targets.Target target)
        {
            this.CurrentPose = new Manipulator.ManipulatorPose(this, target);
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
        #endregion

        #region Interfaces
        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; }

        public Guid ReferenceID { 
            get
            {
                if (id != Guid.Empty) return id;
                id = System.Guid.NewGuid();
                return id;
            } set 
            {
                if (typeof(Guid) == value.GetType()) id = value;
            } }
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
        public IGH_GeometricGoo DuplicateGeometry()
        {
            var robot = new Manipulator(this.Manufacturer, this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            return robot;
        }
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
        public bool IsValid { get => (this.CurrentPose != null)? this.CurrentPose.IsValid : false; }
        public string IsValidWhyNot => "Since no or no valid poes has been set there is no representation possible for this robot";
        public string TypeName => "Manipulator";
        public string TypeDescription => "Robot movment system";

        public bool CastFrom(object source) 
        {
            if (source.GetType() == typeof(Manipulator))
            {
                Manipulator manipulator = source as Manipulator;
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
        public bool CastTo<T>(out T target) => throw new NotImplementedException();
        public IGH_Goo Duplicate()
        {
            Manipulator robot = new Manipulator(this.Manufacturer, this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            robot.ReferenceID = this.ReferenceID;
            return robot;
        }
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
                if (reader.ChunkExists("RobMeshes",i)) 
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
                byte[] meshes;
                GH_Mesh mesh = new GH_Mesh(this.RobMeshes[i]);
                mesh.Write(writer.CreateChunk("RobMeshes", i));
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Class to hold the values describing the tansformation of a tool
        /// </summary>
        public class ManipulatorPose : IGH_Goo
        {
            private Manipulator Robot;
            private Axis.Targets.Target Target;
            private double[] radAngles;
            private JointState[] jointStates;

            private bool outOfReach = false;
            private bool outOfRoation = false;
            private bool overHeadSig = false;
            private bool wristSing = false;

            public double[] Angles { get => this.radAngles.Select(d => d.ToDegrees()).ToArray(); }
            public JointState[] JointStates {
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
            public Transform[] Reverse {
                get
                {
                    if (reverse != null) return reverse;
                    GetPose();
                    return reverse; //<-- Not sure if this code is ever gonna be called.
                }
                set => reverse = value;
            }

            // Mesh colours
            public Color[] Colors { get => (this.jointStates != null) ? this.jointStates.Select(state => GetColour(state)).ToArray() : null; }
            private static Dictionary<JointState, Color> JointColours = new Dictionary<JointState, Color>()
            {
                { JointState.Normal, Axis.Styles.DarkGrey },
                { JointState.OutOfReach, Axis.Styles.Pink },
                { JointState.OutOfRotation, Axis.Styles.Pink },
                { JointState.WristSing, Axis.Styles.Blue },
                { JointState.OverHeadSing, Axis.Styles.Blue },

            };

            public Plane[] Planes { get
                {
                    Plane[] planes = this.Robot.AxisPlanes.Select(plane => plane.Clone()).ToArray();
                    for (int i = 0; i < planes.Length; ++i) planes[i].Transform(this.Robot.ResetTransform[i] * this.Forward[i]);
                    return planes;
                } }

            public Plane Flange { get
                {
                    Plane flange = this.Robot.AxisPlanes[5].Clone();
                    flange.Transform(Forward[5]);
                    return flange;
                } }

            public Mesh[] Geometry
            {
                get
                {
                    Mesh[] meshes = this.Robot.RobMeshes.Select(plane => plane.DuplicateMesh()).ToArray();
                    for (int i = 0; i < meshes.Length - 1; ++i) meshes[i + 1].Transform(this.Robot.ResetTransform[i] * this.Forward[i]);
                    return meshes;
                }
            }

            #region Constructors
            public ManipulatorPose(Manipulator robot)
            {
                this.Robot = robot;
            }
            public ManipulatorPose(Manipulator robot, Targets.Target target)
            {
                this.Robot = robot;
                this.SetPose(target);
            }
            #endregion

            #region  Methods
            //Public
            public void SetPose(Targets.Target target)
            {
                this.Target = target;

                double[] radAngles = new double[6];
                double[] degAngles = new double[6];
                JointState[] jointStates = new JointState[6];

                //Invers and farward kinematics devided by manufacturar
                switch (this.Robot.Manufacturer)
                {
                    case Manufacturer.ABB:
                        switch (target.Method)
                        {
                            case Targets.MotionType.Linear:
                            case Targets.MotionType.Joint:

                                ////Bring Target to Robot refference
                                //Rhino.Geometry.Transform reverse = Rhino.Geometry.Transform.PlaneToPlane(this.Robot.AxisPlanes[0], Plane.WorldXY);
                                //Plane ikPlane = target.Plane.Clone();
                                //ikPlane.Transform(reverse);

                                //double[,] anglesSet = newTargetInverseKinematics(target, out overHeadSig, out outOfReach);
                                List<List<double>> anglesSet = TargetInverseKinematics(target.Plane, out overHeadSig, out outOfReach);

                                // Select Solution based on Indecies
                                //for (int i = 0; i < anglesSet.Length / anglesSet.GetLength(1); ++i) degAngles[i] = anglesSet[i, this.Robot.Indices[i]];
                                radAngles = anglesSet.Zip<List<double>, int, double>(this.Robot.Indices, (solutions, selIdx) => solutions[selIdx]).ToArray();
                                degAngles = radAngles.Select(a => a.ToDegrees()).ToArray();

                                jointStates = CheckJointAngles(degAngles, this.Robot, checkSingularity: true);

                                break;
                            case Targets.MotionType.AbsoluteJoint:
                                degAngles = target.JointAngles.ToArray();
                                radAngles = degAngles.Select(a => a.ToRadians()).ToArray();

                                jointStates = CheckJointAngles(degAngles, this.Robot);

                                break;
                            default:
                                throw new Exception($"This movment: {target.Method.ToString()} has not jet been implemented for {this.Robot.Manufacturer.ToString()}"); ;
                        } break;
                    case Manufacturer.Kuka:
                        switch (target.Method)
                        {
                            case Targets.MotionType.AbsoluteJoint:

                                degAngles = target.JointAngles.ToArray();
                                jointStates = CheckJointAngles(degAngles, this.Robot);

                                radAngles = degAngles.Select(value => value.ToRadians()).ToArray();
                                break;
                            default:
                                throw new Exception($"This movment: {target.Method.ToString()} has not jet been implemented for {this.Robot.Manufacturer.ToString()}");
                        } break;
                }

                SetSignals(jointStates, out this.outOfReach, out this.outOfRoation, out this.wristSing, out this.overHeadSig);
                this.JointStates = jointStates;

                this.radAngles = radAngles;

                this.Forward = ForwardKinematics(radAngles, this.Robot);

                this.IsValid = (!outOfReach && !outOfRoation && !overHeadSig && !wristSing) ? true : false;

            }
            public void UpdateRobot(Manipulator robot) => this.Robot = robot;
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
                Plane P0 = this.Robot.AxisPlanes[0].Clone();
                Plane P1 = this.Robot.AxisPlanes[1].Clone();
                Plane P2 = this.Robot.AxisPlanes[2].Clone();
                Plane P3 = this.Robot.AxisPlanes[3].Clone();
                Plane P4 = this.Robot.AxisPlanes[4].Clone();
                Plane P5 = this.Robot.AxisPlanes[5].Clone();
                Plane flange = target.Clone();


                // Setting Up Lengths
                double wristOffsetLength = this.Robot.WristOffsetLength;
                double lowerArmLength = this.Robot.LowerArmLength;
                double upperArmLength = this.Robot.UpperArmLength;
                double axisFourOffsetAngle = this.Robot.AxisFourOffsetAngle;




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
            private static Rhino.Geometry.Transform[] ForwardKinematics(double[] radAngles, Manipulator robot)
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
            private double[,] AttemptTargetInverseKinematics(Axis.Targets.Target target, out bool overheadSing, out bool outOfReach, double singularityTol = 5)
            {
                // Validity checks
                bool unreachable = false;
                bool singularity = false;


                Plane FlangeFromTargetAndToo(Axis.Targets.Target t)
                {
                    Transform toolCompensation = Rhino.Geometry.Transform.PlaneToPlane(t.Tool.TCP, Plane.WorldXY);
                    Plane flange = t.Plane.Clone();
                    flange.Transform(toolCompensation);
                    return flange;
                }
                Plane Flange = FlangeFromTargetAndToo(target);


                /// Konstruct K points
                Plane K0 = this.Robot.RobBasePlane.Clone();
                Plane K1 = this.Robot.AxisPlanes[1].Clone();
                Plane K2 = this.Robot.AxisPlanes[2].Clone();
                Plane K3 = this.Robot.AxisPlanes[3].Clone();
                Plane K4 = this.Robot.AxisPlanes[4].Clone();

                //Move K4 in rlation to the flange
                //K4.Transform(Rhino.Geometry.Transform.Translation(this.Robot.WristOffset));

                void UpdateValuesIn2DMatrix(double[,] table, double[] value, int row)
                {
                    for (int i = 0; i < value.Length; ++i)
                    {
                        //Make sure always the smalles possible rotation is used
                        while (value[i] >= Math.PI) value[i] -= 2 * Math.PI;
                        while (value[i] < -Math.PI) value[i] += 2 * Math.PI;


                        var dim = table.GetLength(2); //Get the dimention of the matrix
                        foreach (int j in Enumerable.Range(
                            dim / value.Length * i,
                            dim / value.Length + (i * dim / value.Length))) table[row, j] = value[i]; // It might be nessesary to flip row/j
                    }
                }
                void FillOutMatrix(double[,] matrix, IList<double[]> lists)
                {
                    for (int i = 0; i < lists.Count(); ++i)
                    {
                        UpdateValuesIn2DMatrix(matrix, lists[i], i);
                    }
                }

                double[] AnglesOnPlane(Plane k0, Point3d k4)
                {
                    double U; double V;
                    k0.ClosestParameter(k4, out U, out V);
                    var value = Math.Atan2(U, V);
                    return new double[] { value, value + Math.PI };
                }
                List<Plane> Rotations(IList<double> ang, Plane planeOfRotation, Plane geo)
                {
                    List<Plane> list = new List<Plane>();
                    for (int i = 0; i < ang.Count(); ++i)
                    {
                        Transform rot = Rhino.Geometry.Transform.Rotation(ang[i], planeOfRotation.ZAxis, planeOfRotation.Origin);
                        Plane geoClone = geo.Clone();
                        geoClone.Transform(rot);
                        list.Add(geoClone);
                    }
                    return list;
                }
                Tuple<double[], double[], Vector3d[], Vector3d[]>
                    AnglesForASetOfPointsT(IList<Plane> startPlanes, IList<Plane> targetPlane, double armLength1, double armLength2)
                {
                    List<double> angleSet1 = new List<double>();
                    List<double> angleSet2 = new List<double>();
                    List<Vector3d> vectorSet1 = new List<Vector3d>();
                    List<Vector3d> vectorSet2 = new List<Vector3d>();

                    int k = 0;
                    for (int i = 0; i < startPlanes.Count(); ++i)
                    {
                        for (int j = 0; i < targetPlane.Count(); ++i)
                        {
                            // Spheres
                            Sphere Sphere1 = new Sphere(startPlanes[i], this.Robot.LowerArmLength);
                            Sphere Sphere2 = new Sphere(targetPlane[j], this.Robot.UpperArmLength);
                            Circle Circ = new Circle();

                            double Par1 = new double();
                            double Par2 = new double();

                            Rhino.Geometry.Intersect.Intersection.SphereSphere(Sphere1, Sphere2, out Circ);
                            Rhino.Geometry.Intersect.Intersection.PlaneCircle(startPlanes[i], Circ, out Par1, out Par2);

                            Point3d IntersectPt1 = Circ.PointAt(Par1);
                            Point3d IntersectPt2 = Circ.PointAt(Par2);

                            foreach (Point3d pt in new List<Point3d> { IntersectPt1, IntersectPt2 })
                            {
                                var val = AnglesOnPlane(startPlanes[i], pt);
                                foreach (double a in val) angleSet1.Add(a);
                                vectorSet1.Add(new Vector3d(pt - startPlanes[i].Origin));
                                vectorSet2.Add(new Vector3d(pt - targetPlane[j].Origin));

                                Plane K3A = startPlanes[i].Clone();

                                //K3A.Translate(this.Robot.AxisFourOffset);
                                val = AnglesOnPlane(startPlanes[i], pt);
                                foreach (double a in val) angleSet2.Add(a);
                                vectorSet2.Add(new Vector3d(pt - targetPlane[j].Origin));
                            }
                        }

                    }
                    return new Tuple<double[], double[], Vector3d[], Vector3d[]>(angleSet1.ToArray(), angleSet2.ToArray(), vectorSet1.ToArray(), vectorSet2.ToArray());
                }
                double[] Angeles(IList<Vector3d> vec1, IList<Vector3d> vec2)
                {
                    List<double> set = new List<double>();
                    for (int i = 0; i < vec1.Count(); ++i)
                    {
                        for (int j = 0; j < vec1.Count(); ++j)
                        {
                            var result = Vector3d.VectorAngle(vec1[i], vec2[j]);
                            set.Add(result);
                            result += result * Math.PI;
                            set.Add(result);
                        }
                    }
                    return set.ToArray();
                }



                // Initiate Values
                //double[] angles3 = new double[8];
                //double[] angles4 = new double[8]; // Positive negative value pairs
                //double[] angles5 = new double[8];

                // Axis 1 angele value pair
                //double[] angles0 = AnglesOnPlane(K0, K4.Origin);
                /*
                for (int i = 0; i < angles0.Count(); ++i)
                {
                    Transform rot = Rhino.Geometry.Transform.Rotation(angles0[i], K0.ZAxis, K0.Origin);
                    Plane K1A = K1.Clone();
                    K1A.Transform(rot);
                }
                */
                //Rotate K1 around K0
                //List<Plane> K1As = Rotations(angles0, K0, K1);

                //Rotation of K1 towards K4
                //var results = AnglesForASetOfPointsT(K1As, new Plane[1]{ K4}, this.Robot.LowerArm.Length, this.Robot.UpperArm.Length);

                //double[] angles1 = results.Item1;
                //double[] angles2 = results.Item2;
                //Vector3d[] vectors2 = results.Item4;

                //angles3 = Angeles(vectors2, new Vector3d[1] { K4.Normal });


                // Fill in all values in the Matrix 
                // It might be nessesary to flip the array
                double[,] angles = new double[6, 8];
                //List<double[]> setOfangles = new List<double[]> { angles0, angles1, angles2, angles3, angles4, angles5 };
                //FillOutMatrix(angles, setOfangles);

                outOfReach = unreachable;
                overheadSing = singularity;
                return angles;

            }
            /// <summary>
            /// Check joint angle values against the robot model data. 
            /// </summary>
            /// <param name="angeles"></param>
            /// <param name="robot"></param>
            /// <param name="checkSingularity"></param>
            /// <param name="singularityTol"></param>
            /// <returns>Returns a list of JointStates</returns>
            private static JointState[] CheckJointAngles(double[] angeles, Manipulator robot, bool checkSingularity = false, double singularityTol = 5)
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
            #endregion

            #region Interfaces
            public bool IsValid { get; private set; } = false;
            public string IsValidWhyNot => "Pose does not have a valid solution";
            public string TypeName => "Robot Pose";
            public string TypeDescription => "This describes a robots position at a given moment";
            
            public bool CastFrom(object source) => throw new NotImplementedException();
            public  bool CastTo<T>(out T target) => throw new NotImplementedException();
            public IGH_Goo Duplicate()
            {
                if (this.Target == null) return new ManipulatorPose(this.Robot);
                else return new ManipulatorPose(this.Robot, this.Target);
            }
            public IGH_GooProxy EmitProxy() => throw new NotImplementedException();
            public object ScriptVariable() => this;
            public override string ToString() => $"Position for {this.Robot.Name} [{string.Join(";", this.radAngles.Select(a => a.ToDegrees().ToString("0.00")).ToArray())}]";


            public bool Read(GH_IReader reader) => throw new NotImplementedException();
            public bool Write(GH_IWriter writer) => throw new NotImplementedException();
            #endregion

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
    /// Class represeing an endefector for a robotic manipulator
    /// </summary>
    public class Tool: IGH_GeometricGoo, Axis_Displayable<Mesh>
    {
        public string Name { get; private set; }
        private Guid ID { get; set; }
        public Plane TCP { get; private set; }
        public double Weight { get; private set; }
        public Mesh[] Geometries { get; private set; }
        public Color[] Colors
        { 
            get 
            {
                Color[] list = new Color[this.Geometries.Length];
                for (int i = 0; i < this.Geometries.Length; ++i) list[i] = Axis.Styles.MediumGrey;
                return list;
            } 
        }
        public Manufacturer Manufacturer { get; private set; }
        public Vector3d RelTool { get; private set; }

        #region Propperties
        public string Declaration { 
            get 
            {
                string declaration = string.Empty;
                // Compute the tool TCP offset from the flange.

                switch (this.Manufacturer) 
                {
                    case Manufacturer.ABB:
                        string COG = "[0.0,0.0,10.0]";
                        string userOffset = "[1,0,0,0],0,0,0]]";
                        string strPosX, strPosY, strPosZ;

                        // Round each position component to two decimal places.
                        string Round(double value)
                        {
                            string strvalue = string.Empty;
                            if (value < 0.005 && value > -0.005) { strPosX = "0.00"; }
                            else { strPosX = value.ToString("#.##"); }
                            return strvalue;
                        }  // Specific rounding method
                        strPosX = Round(TCP.Origin.X);
                        strPosY = Round(TCP.Origin.Y);
                        strPosZ = Round(TCP.Origin.Z);

                        // Recompose component parts.
                        string shortenedPosition = $"{strPosX},{strPosY}{strPosZ}";


                        // Quaternien String
                        Quaternion quat = Util.QuaternionFromPlane(TCP);
                        double A = quat.A, B = quat.B, C = quat.C, D = quat.D;
                        double w = Math.Round(A, 6); double x = Math.Round(B, 6); double y = Math.Round(C, 6); double z = Math.Round(D, 6);
                        string strQuat = $"{w.ToString()},{x.ToString()},{y.ToString()},{z.ToString()}";

                        declaration = $"PERS tooldata {this.Name} := [TRUE,[[{shortenedPosition}], [{strQuat}]], [{this.Weight.ToString()},{COG},{userOffset};";
                        break;
                    //case Manufacturer.Kuka:
                    //    /*
                    //    List<double> eulers = new List<double>();
                    //    eulers = Util.QuaternionToEuler(quat);
                    //
                    //    double eA = eulers[0];
                    //    double eB = eulers[1];
                    //    double eC = eulers[2];
                    //
                    //    // Compose KRL-formatted declaration.
                    //    declaration = "$TOOL = {X " + strPosX + ", Y " + strPosY + ", Z " + strPosZ + ", A " + eA.ToString() + ", B " + eB.ToString() + ", C " + eC.ToString() + "}";
                    //    */
                    //    declaration = "KUKA TOOL DECLARATION";
                    //    break;
                    default:
                        throw new NotImplementedException($"{this.Manufacturer} has not yet been implemented.");
                }
                return declaration;
            }
        }
        public Transform FlangeOffset { 
            get 
            {
                return Rhino.Geometry.Transform.PlaneToPlane(TCP, Plane.WorldXY);
            }  
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default tool.
        /// </summary>
        public static Tool Default { get => new Tool("DefaultTool", Plane.WorldXY, 1.5, null, Manufacturer.ABB, Vector3d.Zero); }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Tool(){}

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="TCP"></param>
        /// <param name="weight"></param>
        /// <param name="mesh"></param>
        /// <param name="type"></param>
        /// <param name="relToolOffset"></param>
        public Tool(string name, Plane TCP, double weight, List<Mesh> mesh, Manufacturer type, Vector3d relToolOffset)
        {
            this.Name = name;
            this.TCP = TCP;
            this.Weight = weight;
            if (mesh != null) this.Geometries = mesh.ToArray();
            this.Manufacturer = type;
            this.RelTool = relToolOffset;
        }
        #endregion

        #region Interfaces
        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get; private set; }
        public Guid ReferenceID { get; set; }
        public bool IsReferencedGeometry { get; }
        public bool IsGeometryLoaded { get; }

        public void ClearCaches() 
        {
            this.Name =string.Empty;
            this.ID = Guid.Empty;
            this.TCP = Plane.Unset;
            this.Weight = double.NaN;
            this.Geometries = null;
            this.Manufacturer = 0;
            this.RelTool = Vector3d.Unset;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) 
        {
            BoundingBox box = BoundingBox.Empty;
            foreach (Mesh m in this.Geometries) m.Transform(xform);
            foreach (Mesh m in this.Geometries) box.Union(m.GetBoundingBox(false));
            this.Boundingbox = box;
            return box;
        }
        public bool LoadGeometry() => throw new NotImplementedException();
        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();
        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();
        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        //IGH_Goo
        public  bool IsValid => true;
        public string IsValidWhyNot { get { return ""; } }
        public string TypeName => "Tool";
        public string TypeDescription => "Robot end effector";

        public bool CastFrom(object o) => false;
        public bool CastTo<T>(out T target) {target = default; return false; }
        public IGH_Goo Duplicate()
        {
            return new Tool(this.Name, this.TCP, this.Weight, this.Geometries.ToList(), this.Manufacturer, this.RelTool);
        }
        public IGH_GooProxy EmitProxy() => null;
        public object ScriptVariable() => null;
        public override string ToString() => $"Tool: {this.Name}";

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("Name")) this.Name = reader.GetString("Name");
            if (reader.ItemExists("GUID")) this.ID = reader.GetGuid("GUID");
            if (reader.ItemExists("Weight")) this.Weight = reader.GetDouble("Weight");
            if (reader.ItemExists("Manufacturer")) this.Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");

            if (reader.ChunkExists("TCP"))
            {
                var chunk1 = reader.FindChunk("TCP");
                if (chunk1 != null)
                {
                    var data = new GH_Plane();
                    var plane = new Plane();
                    data.Read(chunk1);
                    GH_Convert.ToPlane(data, ref plane, GH_Conversion.Both);
                    this.TCP = this.TCP;
                }
            }
            if (reader.ChunkExists("Geometry")) 
            {
               var chunk2 = reader.FindChunk("Geometry");
               if (chunk2 != null) 
                {
                    var data = new GH_Structure<GH_Mesh>();
                    data.Read(chunk2);
                    this.Geometries = data.ToList<Mesh, GH_Mesh>().ToArray();
                }
            }
            if (reader.ChunkExists("FlangeOffset")) 
            {
                var chunk3 = reader.FindChunk("FlangeOffset");
                if (chunk3 != null)
                {
                    var data = new GH_Vector();
                    var vec = new Vector3d();
                    data.Read(chunk3);
                    GH_Convert.ToVector3d(chunk3, ref vec, GH_Conversion.Both);
                    this.RelTool = vec;
                }
            }
            return true;
        }

        public bool Write(GH_IWriter writer)
        {
            writer.SetString("Name", this.Name);
            writer.SetGuid("GUID", ID);
            writer.SetDouble("Weight", this.Weight);
            writer.SetInt32("Manufacturer", (int)this.Manufacturer);

            GH_Plane gH_TCP = new GH_Plane(this.TCP);
            gH_TCP.Write(writer.CreateChunk("TCP"));
            
            GH_Structure<GH_Mesh> gh_Meshes = this.Geometries.ToList().ToGHStructure<GH_Mesh, Mesh>();
            gh_Meshes.Write(writer.CreateChunk("Geometry"));
            
            GH_Vector gH_relTool = new GH_Vector(this.RelTool);
            gH_relTool.Write(writer.CreateChunk("RelTool"));

            return true;
        }
        #endregion
    }

    /// <summary>
    /// Interface ensuring types have the right propperies to be displayed inside the other componets
    /// This should help enforce consistency throughout the plugin.
    /// </summary>
    interface Axis_Displayable<T>: IGH_GeometricGoo where T: Rhino.Runtime.CommonObject 
    {
        T[] Geometries { get; }
        Color[] Colors { get; }
    }

    /// <summary>
    /// List of manufacturers.
    /// </summary>
    public enum Manufacturer
    {
        ABB = 0,
        Kuka = 1,
        Universal = 2
    }
}