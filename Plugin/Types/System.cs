using Axis;
using Axis.Kernal;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Axis.Types
{
    /// <summary>
    /// Create custom industrial robot configurations.
    /// </summary>
    public sealed class Abb6DOFRobot : Robot, Axis_Displayable
    {
        #region Constructors

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
        public Abb6DOFRobot(Manufacturer manufacturer, Plane[] axisPlanes, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
        {
            this.Manufacturer = manufacturer;

            this.AxisPlanes = axisPlanes.ToList();

            this.MinAngles = minAngles;
            this.MaxAngles = maxAngles;

            this.RobMeshes = robMeshes;
            this.RobBasePlane = AxisPlanes[0];

            this.Indices = indices;

            ChangeBasePlane(basePlane);

            this.SetPose();
        }

        /// <summary>
        /// Built-in default robot - ABB IRB 120.
        /// </summary>
        public static Robot Default { get => IRB120; }

        /// <summary>
        /// Built-in ABB IRB 120.
        /// </summary>
        public static Robot IRB120
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB120mesh))
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

                var robot = new Abb6DOFRobot(manufacturer, axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 120";
                return robot;

                //Robot manipulator;
                //
                //using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB120))
                //{
                //    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //    manipulator = (Robot)br.Deserialize(ms);
                //}
                //
                //return manipulator;
            }
        }

        /// <summary>
        /// Built-in ABB IRB 6620.
        /// </summary>
        public static Robot IRB6620
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6620mesh))
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

                var robot = new Abb6DOFRobot(manufacturer, axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6620";
                return robot;

                //Robot manipulator;
                //
                //using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Robot.RobotSystems.IRB6620))
                //{
                //    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //    manipulator = (Robot)br.Deserialize(ms);
                //}
                //
                //return manipulator;
            }
        }

        #endregion Constructors

        #region Display

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (this.CurrentPose == null) return;
            if (this.CurrentPose.Colors == null) return;

            var meshColorPair = this.RobMeshes.Zip(this.CurrentPose.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPair) args.Display.DrawMeshShaded(pair.Mesh, new Rhino.Display.DisplayMaterial(pair.Color));
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (this.CurrentPose == null) return;
            if (this.CurrentPose.Colors == null) return;

            var meshColorPair = this.RobMeshes.Zip(this.CurrentPose.Colors, (mesh, color) => new { Mesh = mesh, Color = color });
            foreach (var pair in meshColorPair) args.Display.DrawMeshShaded(pair.Mesh, new Rhino.Display.DisplayMaterial(pair.Color));
        }

        #endregion Display

        #region Interfaces

        public override IGH_Goo Duplicate()
        {
            Robot robot = new Abb6DOFRobot(this.Manufacturer, this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            robot.ReferenceID = this.ReferenceID;
            return robot;
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            var robot = new Abb6DOFRobot(this.Manufacturer, this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            return robot;
        }

        #endregion Interfaces
    }

    /// <summary>
    /// Class represeing an endefector for a robotic manipulator
    /// </summary>
    public sealed class Tool : IGH_GeometricGoo, Axis_Displayable<Mesh>
    {
        public Color[] Colors
        {
            get
            {
                Color[] list = new Color[this.Geometries.Length];
                for (int i = 0; i < this.Geometries.Length; ++i) list[i] = Axis.Styles.MediumGrey;
                return list;
            }
        }

        public Mesh[] Geometries { get; private set; }
        public Manufacturer Manufacturer { get; private set; }
        public string Name { get; private set; }
        public Vector3d RelTool { get; private set; }
        public Plane TCP { get; private set; }
        public double Weight { get; private set; }
        private Guid ID { get; set; }

        #region Propperties

        public string Declaration
        {
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

        public Transform FlangeOffset
        {
            get
            {
                return Rhino.Geometry.Transform.PlaneToPlane(TCP, Plane.WorldXY);
            }
        }

        public Transform ResetTransform { get; set; } = Rhino.Geometry.Transform.Identity;

        #endregion Propperties

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Tool() { }

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
            this.Geometries = (mesh != null) ? mesh.ToArray() : new Mesh[0];
            this.Manufacturer = type;
            this.RelTool = relToolOffset;
        }

        /// <summary>
        /// Default tool.
        /// </summary>
        public static Tool Default { get => new Tool("DefaultTool", Plane.WorldXY, 1.5, null, Manufacturer.ABB, Vector3d.Zero); }

        #endregion Constructors

        #region Methods

        public void UpdatePose(Robot robot)
        {
            var forward = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, robot.CurrentPose.Flange);
            Rhino.Geometry.Transform reverse;
            forward.TryGetInverse(out reverse);

            foreach (Mesh m in this.Geometries) m.Transform(this.ResetTransform * forward);
            this.ResetTransform = reverse;
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

        public void ClearCaches()
        {
            this.Name = string.Empty;
            this.ID = Guid.Empty;
            this.TCP = Plane.Unset;
            this.Weight = double.NaN;
            this.Geometries = null;
            this.Manufacturer = 0;
            this.RelTool = Vector3d.Unset;
        }

        public IGH_Goo Duplicate()
        {
            return new Tool(this.Name, this.TCP, this.Weight, this.Geometries.Select(m => (Mesh)m.Duplicate()).ToList(), this.Manufacturer, this.RelTool);
        }

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

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

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

            if (reader.ChunkExists("FlangeOffset"))
            {
                var chunk3 = reader.FindChunk("FlangeOffset");
                if (chunk3 != null)
                {
                    var data = new GH_Vector();
                    var vec = new Vector3d();
                    data.Read(chunk3);
                    GH_Convert.ToVector3d(data, ref vec, GH_Conversion.Both);
                    this.RelTool = vec;
                }
            }
            return true;
        }

        public object ScriptVariable() => this;

        public override string ToString() => $"Tool: {this.Name}";

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        public bool Write(GH_IWriter writer)
        {
            writer.SetString("Name", this.Name);
            writer.SetGuid("GUID", ID);
            writer.SetDouble("Weight", this.Weight);
            writer.SetInt32("Manufacturer", (int)this.Manufacturer);

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

            GH_Vector gH_relTool = new GH_Vector(this.RelTool);
            gH_relTool.Write(writer.CreateChunk("RelTool"));

            return true;
        }

        #endregion Interfaces
    }
}