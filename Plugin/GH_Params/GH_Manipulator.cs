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
using Axis.Core;
using Microsoft.Office.Interop.Excel;
using System.Linq;
using GH_IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using Windows.ApplicationModel.Appointments.DataProvider;

namespace Axis.Params
{
    public class GH_Manipulator : IGH_GeometricGoo
    {
        Manipulator value = null;

        #region Constructors and defaults
        public static GH_Manipulator Default { get; }
        public static GH_Manipulator IRB120{ get => Manipulator.IRB120; }
        public static GH_Manipulator IRB6620 { get => Manipulator.IRB6620; }
        static GH_Manipulator()
        {
            Default = IRB120;
        }
        public GH_Manipulator(Manufacturer manufacturer, Plane[] axisPlanes, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
        {
            this.value = new Manipulator( manufacturer,  axisPlanes, minAngles,  maxAngles,  robMeshes,  basePlane, indices);
        }
        #endregion

        #region Interface variables 

        //IGH_GeometricGoo
        public BoundingBox Boundingbox => throw new NotImplementedException();
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => false; }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() { }// => throw new NotImplementedException();
        public IGH_GeometricGoo DuplicateGeometry()
        {
            var robot = new Manipulator(value.Manufacturer, value.AxisPlanes.ToArray(), value.MinAngles, value.MaxAngles, value.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), value.RobBasePlane.Clone(), value.Indices);
            if (value.Name == string.Empty) robot.Name = value.Name;
            return robot;
        }
        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();
        public bool LoadGeometry() => throw new NotImplementedException();
        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();
        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();
        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();


        //IGH_Goo
        public bool IsValid
        {
            get
            {
                if (value.CurrentPose != null) return value.CurrentPose.IsValid;
                else return false;
            }
        }
        public string IsValidWhyNot => "Since no or no valid poes has been set the is no representation possible for this robot";
        public string TypeName => "Manipulator";
        public string TypeDescription => "Robot movment system";

        public bool CastFrom(object source) => throw new NotImplementedException();
        public bool CastTo<T>(out T target) => throw new NotImplementedException();
        public IGH_Goo Duplicate()
        {
            Manipulator robot = new Manipulator(value.Manufacturer, value.AxisPlanes.ToArray(), value.MinAngles, value.MaxAngles, value.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), value.RobBasePlane.Clone(), value.Indices);
            if (value.Name == string.Empty) robot.Name = value.Name;
            return robot;
        }
        public IGH_GooProxy EmitProxy() => null;
        public object ScriptVariable() => this;
        public override string ToString()
        {
            if (value.Name != string.Empty) return $"Robot {value.Manufacturer.ToString()}  \'{value.Name}\'";
            else return $"Robot {value.Manufacturer.ToString()}";
        }

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            value.Name = reader.GetString("Name");
            value.Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");

            Plane bPlane = new Plane();
            GH_Convert.ToPlane(reader.GetPlane("RobBasePlane"), ref bPlane, GH_Conversion.Both);
            value.RobBasePlane = bPlane;

            for (int i = 0; i < 6; ++i)
            {
                Plane plane = new Plane();
                GH_Convert.ToPlane(reader.GetPlane("AxisPlanes", i), ref plane, GH_Conversion.Both);
                value.AxisPlanes.Add(plane);
            }
            for (int i = 0; i < 6; ++i)
            {
                value.MinAngles.Add(reader.GetDouble("MinAngles", i));
            }
            for (int i = 0; i < 6; ++i)
            {
                value.MaxAngles.Add(reader.GetDouble("MaxAngles", i));
            }
            for (int i = 0; i < 6; ++i)
            {
                value.Indices.Add(reader.GetInt32("Indices", i));
            }
            for (int i = 0; i < 6; ++i)
            {
                Mesh mesh = new Mesh();
                GH_Convert.ToMesh(reader.GetByteArray("RobMeshes", i), ref mesh, GH_Conversion.Both);
                value.RobMeshes.Add(mesh);
            }

            return true;

        }
        public bool Write(GH_IWriter writer)
        {
            GH_Plane gH_RobBasePlane = new GH_Plane(value.RobBasePlane);

            writer.SetString("Name", value.Name);
            writer.SetInt32("Manufacturer", (int)value.Manufacturer);
            gH_RobBasePlane.Write(writer.CreateChunk("RobBasePlane"));

            for (int i = 0; i < value.AxisPlanes.Count; ++i)
            {
                GH_IO.Types.GH_Plane plane = new GH_IO.Types.GH_Plane(
                    value.AxisPlanes[i].OriginX,
                    value.AxisPlanes[i].OriginY,
                    value.AxisPlanes[i].OriginZ,
                    value.AxisPlanes[i].XAxis.X,
                    value.AxisPlanes[i].XAxis.Y,
                    value.AxisPlanes[i].XAxis.Z,
                    value.AxisPlanes[i].YAxis.X,
                    value.AxisPlanes[i].YAxis.Y,
                    value.AxisPlanes[i].YAxis.Z
                    );
                writer.SetPlane("AxisPlanes", i, plane);
            }
            for (int i = 0; i < value.MinAngles.Count; ++i)
            {
                writer.SetDouble("MinAngles", i, value.MinAngles[i]);
            }
            for (int i = 0; i < value.MinAngles.Count; ++i)
            {
                writer.SetDouble("MaxAngles", i, value.MaxAngles[i]);
            }
            for (int i = 0; i < value.Indices.Count; ++i)
            {
                writer.SetInt32("Indices", i, value.Indices[i]);
            }
            for (int i = 0; i < value.RobMeshes.Count; ++i)
            {
                GH_Mesh mesh = new GH_Mesh(value.RobMeshes[i]);
                mesh.Write(writer.CreateChunk("RobMeshes", i));
            }

            writer.AddComment("This should be the manipulator");
            return true;

        }
        #endregion

    }
}
