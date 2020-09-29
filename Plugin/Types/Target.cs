using Axis.Kernal;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Axis.Types
{
    /// <summary>
    /// Construct a robot target position.
    /// </summary>
    public class ABBTarget : Target
    {
        public override string RobStr(Manufacturer manufacturer) 
        {
            string robtarget = string.Empty;
            Plane plane = Plane.WorldXY;
            Point3d position = Point3d.Origin;
            Quaternion quat = Quaternion.I;


            if (this.Method == MotionType.Linear | this.Method == MotionType.Joint)
            {
                // Offset with the CSystem to get the right program code.
                Transform xForm = Rhino.Geometry.Transform.PlaneToPlane(this.CSystem.CSPlane, Plane.WorldXY);

                plane = new Plane(this.TargetPlane);
                plane.Transform(xForm);

                position = this.TargetPlane.Origin;
                quat = Util.QuaternionFromPlane(this.TargetPlane);

                robtarget = $"[[{position.CodeStrFor(manufacturer)}],[{quat.CodeStrFor(manufacturer)}],cData,[{this.ExtRot.CodeStrFor(manufacturer)},{this.ExtLin.CodeStrFor(manufacturer)},9E9,9E9,9E9,9E9]]";
            }

            switch (this.Method)
            {
                #region Linear

                case MotionType.Linear:

                    if (this.tool.RelTool != Vector3d.Zero)
                    {
                        //MoveL RelTool ([[416.249, -110.455, 0],[0, 0, 1, 0], cData, eAxis], 0, 0,-120), v50, z1, tool0 \Wobj:=wobj0;
                        string offset = $"[{this.tool.RelTool.X.ToString()}, {this.tool.RelTool.Y.ToString()}, {this.tool.RelTool.Z.ToString()}]";
                        return $"MoveL RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(manufacturer)},{this.Zone.CodeStrFor(manufacturer)},{this.Tool.CodeStrFor(manufacturer)} {this.CSystem.CodeStrFor(manufacturer)};";
                    }
                    else
                    {
                        return $"MoveL {robtarget}, {this.Speed.CodeStrFor(manufacturer)}, {this.Zone.CodeStrFor(manufacturer)}, {this.Tool.CodeStrFor(manufacturer)} {this.CSystem.CodeStrFor(manufacturer)};";
                    }

                #endregion Linear

                #region Joint

                case MotionType.Joint:

                    if (this.tool.RelTool != Vector3d.Zero)
                    {
                        string offset = $"[{this.tool.RelTool.X.ToString()}, {this.tool.RelTool.Y.ToString()}, {this.tool.RelTool.Z.ToString()}]";
                        return $"MoveJ RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(manufacturer)},{this.Zone.CodeStrFor(manufacturer)},{this.Tool.CodeStrFor(manufacturer)} {this.CSystem.CodeStrFor(manufacturer)};";
                    }
                    else
                    {
                        return $"MoveJ {robtarget}, {this.Speed.CodeStrFor(manufacturer)}, {this.Zone.CodeStrFor(manufacturer)}, {this.Tool.CodeStrFor(manufacturer)} {this.CSystem.CodeStrFor(manufacturer)};";
                    }

                #endregion Joint

                #region Absolute

                case MotionType.AbsoluteJoint:
                    string jTarg = $"[{this.JointAngles[0].ToString()},{this.JointAngles[1].ToString()},{this.JointAngles[2].ToString()},{this.JointAngles[3].ToString()},{this.JointAngles[4].ToString()},{this.JointAngles[5].ToString()}]";
                    return $"MoveAbsJ [{jTarg},[{this.ExtRot.CodeStrFor(manufacturer)},{this.ExtLin.CodeStrFor(manufacturer)},9E9,9E9,9E9,9E9]], {this.Speed.CodeStrFor(manufacturer)}, {this.Zone.CodeStrFor(manufacturer)}, {this.Tool.CodeStrFor(manufacturer)};";

                #endregion Absolute

                case MotionType.NoMovement:
                    return "! No Movement";
            }
            throw new NotImplementedException($"{this.Method.ToString()} not implemented for ABB");

        }

        #region Variables
        private ABBTool tool;
        private Zone zone;
        private Speed speed;
        private MotionType method;

        #endregion Variables

        #region Class Fields

        public override Quaternion Quaternion { get; set; }
        public override List<double> JointAngles { get; set; }

        public override Speed Speed { get => speed; }
        public override Zone Zone { get => zone; }

        public override Tool Tool {
            get => tool as Tool; 
            set 
            {
                if (value is ABBTool) tool = value as ABBTool;
            }
        }
        public override CSystem CSystem { get; set; }

        public override ExtVal ExtRot { get; set; }
        public override ExtVal ExtLin { get; set; }
        public override  MotionType Method { get => method; }

        #endregion Class Fields

        #region Constructors

        /// <summary>
        /// Default target object.
        /// </summary>
        public static Target Default { get => new ABBTarget(new List<double> { 0, 0, 0, 0, 0, 0 }, Speed.Default, Zone.Default, ABBTool.Default, 0, 0); }

        /// <summary>
        /// Empty target Constructor;
        /// </summary>
        public ABBTarget() { }

        /// <summary>
        /// Default target constructor from a plane.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="speed"></param>
        /// <param name="zone"></param>
        /// <param name="tool"></param>
        /// <param name="wobj"></param>
        /// <param name="extRot"></param>
        /// <param name="extLin"></param>
        /// <param name="robot"></param>
        public ABBTarget(Plane target, MotionType method, Speed speed, Zone zone, Tool tool, CSystem wobj, double extRot, double extLin)
        {
            // Adjust plane to comply with robot programming convetions.
            Quaternion realQuat = Util.QuaternionFromPlane(target);

            // Set publicly accessible property values based on the manufacturer data.
            this.Plane = target;

            //Transform Plane To Location in CSystem
            var tP = target.Clone();
            Rhino.Geometry.Transform xform = Rhino.Geometry.Transform.PlaneToPlane(wobj.CSPlane, Plane.WorldXY);
            tP.Transform(xform);

            this.TargetPlane = tP;
            this.Quaternion = realQuat;
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.method = method;
            this.Tool = tool;
            this.speed = speed;
            this.zone = zone;
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.CSystem = wobj;
        }

        /// <summary>
        /// Default target constructor from joint values.
        /// </summary>
        /// <param name="axisVals"></param>
        /// <param name="speed"></param>
        /// <param name="zone"></param>
        /// <param name="tool"></param>
        /// <param name="extRot"></param>
        /// <param name="extLin"></param>
        /// <param name="robot"></param>
        public ABBTarget(List<double> axisVals, Speed speed, Zone zone, Tool tool, double extRot, double extLin)
        {
            this.JointAngles = axisVals;
            this.Tool = tool;
            this.speed = speed;
            this.zone = zone;
            this.ExtRot = extRot;
            this.ExtLin = extLin;

            this.method = MotionType.AbsoluteJoint;
        }

        #endregion Constructors

        #region Interfaces
        public override void ClearCaches()
        {
            this.speed = null;
            this.zone = null;
            this.method = 0;

            base.ClearCaches();
        }
        public override IGH_Goo Duplicate()
        {
            var target = default(Target);
            switch (this.Method)
            {
                case MotionType.Joint:
                case MotionType.Linear:
                    target = new ABBTarget(this.Plane.Clone(), this.Method, (Speed)this.Speed.Duplicate(), (Zone)this.Zone.Duplicate(), (Tool)this.Tool.Duplicate(), (CSystem)this.CSystem.Duplicate(), this.ExtRot, this.ExtLin);
                    break;

                case MotionType.AbsoluteJoint:
                    target = new ABBTarget(this.JointAngles, (Speed)this.Speed.Duplicate(), (Zone)this.Zone.Duplicate(), (Tool)this.Tool.Duplicate(), this.ExtRot, this.ExtLin);
                    break;
            }
            return target;
        }

        public override bool Read(GH_IReader reader)
        {
            if (reader.ChunkExists("Speed"))
            {
                this.speed = new Speed();
                var speedChunk = reader.FindChunk("Speed");
                this.Speed.Read(speedChunk);
            }
            if (reader.ChunkExists("Zone"))
            {
                this.zone = new Zone();
                var zoneChunk = reader.FindChunk("Zone");
                this.Zone.Read(zoneChunk);
            }
            if (reader.ItemExists("MovementType")) this.method = (MotionType)reader.GetInt32("MovementType");
            if (reader.ChunkExists("Tool"))
            {
                this.tool = new ABBTool();
                var toolChunk = reader.FindChunk("Tool");
                this.Tool.Read(toolChunk);
            }
            return base.Read(reader);
        }
        public override bool Write(GH_IWriter writer)
        {
            if (this.Speed != null)
            {
                var speedChunk = writer.CreateChunk("Speed");
                this.speed.Write(speedChunk);
            }
            if (this.Zone != null)
            {
                var zoneChunk = writer.CreateChunk("Zone");
                this.zone.Write(zoneChunk);
            }
            writer.SetInt32("MovementType", (int)this.method);
            if (this.Tool != null)
            {
                var toolChunk = writer.CreateChunk("Tool");
                this.Tool.Write(toolChunk);
            }
            return base.Write(writer);
        }
        #endregion Interfaces

    }

    public class KUKATarget : Target
    {
        public override string RobStr(Manufacturer manufacturer)
        {
            string robtarget = string.Empty;
            Plane plane = Plane.WorldXY;
            Point3d position = Point3d.Origin;
            Quaternion quat = Quaternion.I;


            if (this.Method == MotionType.Linear | this.Method == MotionType.Joint)
            {
                // Offset with the CSystem to get the right program code.
                Transform xForm = Rhino.Geometry.Transform.PlaneToPlane(this.CSystem.CSPlane, Plane.WorldXY);

                plane = new Plane(this.TargetPlane);
                plane.Transform(xForm);

                position = this.TargetPlane.Origin;
                quat = Util.QuaternionFromPlane(this.TargetPlane);

                robtarget = $"[[{position.CodeStrFor(manufacturer)}],[{quat.CodeStrFor(manufacturer)}],cData,[{this.ExtRot.CodeStrFor(manufacturer)},{this.ExtLin.CodeStrFor(manufacturer)},9E9,9E9,9E9,9E9]]";
            }

            string KUKAposition = position.CodeStrFor(manufacturer);

            List<double> eulers = Util.QuaternionToEuler(quat);

            double E1 = eulers[0] * 180 / Math.PI;
            E1 = Math.Round(E1, 3);
            double E2 = eulers[1] * 180 / Math.PI;
            E2 = Math.Round(E2, 3);
            double E3 = eulers[2] * 180 / Math.PI;
            E3 = Math.Round(E3, 3);

            string strEuler = "A " + E3.ToString() + ", B " + E2.ToString() + ", C " + E1.ToString();
            string strExtAxis = "E1 0, E2 0, E3 0, E4 0"; // Default values for the external axis.

            switch (this.Method)
            {
                #region Linear

                case MotionType.Linear:
                    return $"LIN  {{E6POS:  {KUKAposition}, {strEuler}, {strExtAxis}}} C_VEL";

                #endregion Linear

                case MotionType.Joint:
                    return $"PTP  {{E6POS:  {KUKAposition}, {strEuler}, {strExtAxis}}} C_PTP";

                case MotionType.NoMovement:
                    return "! No Movement";
            }
            throw new NotImplementedException($"{this.Method.ToString()} not implemented for Kuka");
        }

        #region Variables
        private ABBTool tool;
        private Zone zone;
        private Speed speed;
        private MotionType method;

        #endregion Variables


        public override Quaternion Quaternion { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override List<double> JointAngles { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override Speed Speed => throw new NotImplementedException();

        public override Zone Zone => throw new NotImplementedException();

        public override Tool Tool { get => tool as Tool; set => throw new NotImplementedException(); }
        public override CSystem CSystem { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ExtVal ExtRot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ExtVal ExtLin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override MotionType Method => throw new NotImplementedException();

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }


        #region Constructors

        /// <summary>
        /// Default target object.
        /// </summary>
        public static Target Default { get => new KUKATarget(new List<double> { 0, 0, 0, 0, 0, 0 }, Speed.Default, Zone.Default, ABBTool.Default, 0, 0); }

        /// <summary>
        /// Empty target Constructor;
        /// </summary>
        public KUKATarget() { }

        /// <summary>
        /// Default target constructor from a plane.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="speed"></param>
        /// <param name="zone"></param>
        /// <param name="tool"></param>
        /// <param name="wobj"></param>
        /// <param name="extRot"></param>
        /// <param name="extLin"></param>
        /// <param name="robot"></param>
        public KUKATarget(Plane target, MotionType method, Speed speed, Zone zone, Tool tool, CSystem wobj, double extRot, double extLin)
        {
            // Adjust plane to comply with robot programming convetions.
            Quaternion realQuat = Util.QuaternionFromPlane(target);

            // Set publicly accessible property values based on the manufacturer data.
            this.Plane = target;

            //Transform Plane To Location in CSystem
            var tP = target.Clone();
            Rhino.Geometry.Transform xform = Rhino.Geometry.Transform.PlaneToPlane(wobj.CSPlane, Plane.WorldXY);
            tP.Transform(xform);

            this.TargetPlane = tP;
            this.Quaternion = realQuat;
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.method = method;
            this.Tool = tool;
            this.speed = speed;
            this.zone = zone;
            this.ExtRot = extRot;
            this.ExtLin = extLin;
            this.CSystem = wobj;
        }

        /// <summary>
        /// Default target constructor from joint values.
        /// </summary>
        /// <param name="axisVals"></param>
        /// <param name="speed"></param>
        /// <param name="zone"></param>
        /// <param name="tool"></param>
        /// <param name="extRot"></param>
        /// <param name="extLin"></param>
        /// <param name="robot"></param>
        public KUKATarget(List<double> axisVals, Speed speed, Zone zone, Tool tool, double extRot, double extLin)
        {
            this.JointAngles = axisVals;
            this.Tool = tool;
            this.speed = speed;
            this.zone = zone;
            this.ExtRot = extRot;
            this.ExtLin = extLin;

            this.method = MotionType.AbsoluteJoint;
        }

        #endregion Constructors

    }

    public class Command : Instruction
    {
        string command;

        public Command() {}
        public Command(string command, Manufacturer manufacturer) { this.manufacturer = manufacturer; this.command = command; }

        public override string RobStr(Manufacturer manufacturer)
        {
            if (manufacturer == this.manufacturer) return command;
            else throw new NotImplementedException($"This is the wrong type if Instuction.\nThe instruction is for {this.manufacturer}");
        }

        #region Interfaces
        public override bool IsValid => (command != null);
        public override string IsValidWhyNot => "Command can not be empty";

        public override bool CastFrom(object source)
        {
            if (source.GetType().IsAssignableFrom(typeof(string))) 
            {
                command = source as string;
                return true;
            }
            return false;
        }

        public override bool CastTo<T>(out T target)
        {
            target = default;

            return false;
        }

        public override IGH_Goo Duplicate() => new Command() { command = this.command, manufacturer = this.manufacturer };


        public override object ScriptVariable() => null;

        public override string ToString() => $"{manufacturer} Instruction: {command}";

        // Serialisation
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("Command", command);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            command = reader.GetString("Command");
            return base.Read(reader);
        }
        #endregion
    }

    /// <summary>
    /// Type wrapper for a double representing the external axis value.
    /// </summary>
    public class ExtVal : IGH_Goo
    {
        private double val;
        private bool isNull = true;

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public static ExtVal Default { get => new ExtVal(); }

        public ExtVal()
        {
        }

        /// <summary>
        /// Standard speed constructor.
        /// </summary>
        /// <param name="tcpSpeed"></param>
        /// <param name="rotSpeed"></param>
        /// <param name="name"></param>
        /// <param name="time"></param>
        public ExtVal(double value)
        {
            this.val = value;
            this.isNull = false;
        }

        #endregion Constructors

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.isNull = true;
            this.val = double.NaN;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        // IGH_Goo
        public bool IsValid => !this.isNull;

        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "External Axis Value";
        public string TypeDescription => "The value describing the external axis movment";

        public bool CastFrom(object source) => throw new NotImplementedException();

        public bool CastTo<Q>(out Q target) => throw new NotImplementedException();

        public IGH_Goo Duplicate()
        {
            return new ExtVal(this.val);
        }

        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

        public object ScriptVariable() => throw new NotImplementedException();

        public override string ToString() => $"Extenal axis value ({val.ToString("0.00")})";

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            this.isNull = reader.GetBoolean("isValis");
            this.val = reader.GetDouble("Value");
            return true;
        }

        public bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("isValis", this.isNull);
            writer.SetDouble("Value", this.val);
            return true;
        }

        #endregion Interfaces

        public static implicit operator double(ExtVal eV) => eV.val;

        public static implicit operator ExtVal(double d) => new ExtVal(d);
    }

    /// <summary>
    /// Robot Speed type
    /// </summary>
    public class Speed : IGH_Goo
    {
        public string Name { get; set; }
        public double TranslationSpeed { get; set; }
        public double RotationSpeed { get; set; }
        public double Time { get; set; } = 0;

        #region Constructors

        /// <summary>
        /// Default speed object.
        /// </summary>
        public static Speed Default { get => new Speed(100, 30, "v100"); }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Speed() { }

        /// <summary>
        /// Standard speed constructor.
        /// </summary>
        /// <param name="tcpSpeed"></param>
        /// <param name="rotSpeed"></param>
        /// <param name="name"></param>
        /// <param name="time"></param>
        public Speed(double tcpSpeed = 100, double rotSpeed = 30, string name = null, double time = 0.0)
        {
            this.Name = name;
            this.TranslationSpeed = tcpSpeed;
            this.RotationSpeed = rotSpeed;
            this.Time = time;
        }

        #endregion Constructors

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.Name = string.Empty;
            this.TranslationSpeed = double.NaN;
            this.RotationSpeed = double.NaN;
            this.Time = double.NaN;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        // IGH_Goo
        public bool IsValid => throw new NotImplementedException();

        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "Speed";
        public string TypeDescription => "Movement speed in mm/s";

        public bool CastFrom(object source)
        {
            if (source.GetType().IsAssignableFrom(typeof(GH_Number)))
            {
                this.Name = $"v{((GH_Number)source).ToString()}";
                this.TranslationSpeed = (double)(((GH_Number)source).Value);
                this.RotationSpeed = 30;
                this.Time = 0;
                return true;
            }
            if (source.GetType().IsAssignableFrom(typeof(GH_Integer)))
            {
                this.Name = $"v{((GH_Integer)source).ToString()}";
                this.TranslationSpeed = (double)(((GH_Integer)source).Value);
                this.RotationSpeed = 30;
                this.Time = 0;
                return true;
            }
            return false;
        }

        public bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(Q).Name;
                object value = new GH_ObjectWrapper(this);
                target = (Q)value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.TranslationSpeed != double.NaN))
            {
                object _number = new GH_Number(this.TranslationSpeed);
                target = (Q)_number;
                return true;
            }
            target = (Q)default;
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return new Speed(this.TranslationSpeed, this.RotationSpeed, this.Name, this.Time);
        }

        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

        public object ScriptVariable() => throw new NotImplementedException();

        public override string ToString() => (Name != null) ? $"Speed ({Name})" : $"Speed ({TranslationSpeed:0.0} mm/s)";

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            this.Name = reader.GetString("Name");
            this.TranslationSpeed = reader.GetDouble("TranslationSpeed");
            this.RotationSpeed = reader.GetDouble("RotationSpeed");
            this.Time = reader.GetDouble("Time");
            return true;
        }

        public bool Write(GH_IWriter writer)
        {
            if (this.Name != string.Empty) writer.SetString("Name", this.Name);
            writer.SetDouble("TranslationSpeed", this.TranslationSpeed);
            writer.SetDouble("RotationSpeed", this.RotationSpeed);
            writer.SetDouble("Time", this.Time);
            return true;
        }

        #endregion Interfaces
    }

    /// <summary>
    /// Robot movement zone type
    /// </summary>
    public class Zone : IGH_Goo
    {
        public string Name { get; set; }
        public double PathRadius { get; set; }
        public double PathOrient { get; set; }
        public double PathExternal { get; set; }
        public double Orientation { get; set; }
        public double LinearExternal { get; set; }
        public double RotaryExternal { get; set; }
        public bool StopPoint { get; set; }

        #region Constructors

        /// <summary>
        /// Default zone object.
        /// </summary>
        public static Zone Default => new Zone(false, 5, 25, 25, 15, 35, 5, "z5");

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Zone() { }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="pathRadius"></param>
        /// <param name="pathOrient"></param>
        /// <param name="pathExternal"></param>
        /// <param name="orientation"></param>
        /// <param name="linExternal"></param>
        /// <param name="rotExternal"></param>
        /// <param name="name"></param>
        public Zone(bool stop = false, double pathRadius = 5, double pathOrient = 8, double pathExternal = 8, double orientation = 0.8, double linExternal = 8, double rotExternal = 0.8, string name = null)
        {
            this.Name = name;
            this.PathRadius = pathRadius;
            this.PathOrient = pathOrient;
            this.PathExternal = pathExternal;
            this.Orientation = orientation;
            this.LinearExternal = linExternal;
            this.RotaryExternal = rotExternal;
            this.StopPoint = stop;
        }

        #endregion Constructors

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.Name = string.Empty;
            this.PathRadius = double.NaN;
            this.PathOrient = double.NaN;
            this.PathExternal = double.NaN;
            this.Orientation = double.NaN;
            this.LinearExternal = double.NaN;
            this.RotaryExternal = double.NaN;
            this.StopPoint = false;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        // IGH_Goo
        public bool IsValid => throw new NotImplementedException();

        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "Zone";
        public string TypeDescription => "Precision zone in mm";

        public bool CastFrom(object source)
        {
            if (source.GetType().IsAssignableFrom(typeof(GH_Number)))
            {
                this.Name = $"z{((double)((GH_Number)source).Value).ToString("0.00")}";
                this.PathRadius = (double)((GH_Number)source).Value;
                this.PathOrient = 25;
                this.PathExternal = 25;
                this.Orientation = 15;
                this.LinearExternal = 35;
                this.RotaryExternal = 5;
                this.StopPoint = false;
                return true;
            }
            if (source.GetType().IsAssignableFrom(typeof(GH_Integer)))
            {
                this.Name = $"z{((double)((GH_Integer)source).Value).ToString("0.00")}";
                this.PathRadius = (double)((GH_Integer)source).Value;
                this.PathOrient = 25;
                this.PathExternal = 25;
                this.Orientation = 15;
                this.LinearExternal = 35;
                this.RotaryExternal = 5;
                this.StopPoint = false;
                return true;
            }
            return false;
        }

        public bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(Q).Name;
                object value = new GH_ObjectWrapper(this);
                target = (Q)value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.PathRadius != double.NaN))
            {
                object _number = new GH_Number(this.PathRadius);
                target = (Q)_number;
                return true;
            }
            target = default(Q);
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return new Zone(this.StopPoint, this.PathRadius, this.PathOrient, this.PathExternal, this.Orientation, this.LinearExternal, this.RotaryExternal, this.Name);
        }

        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

        public object ScriptVariable() => throw new NotImplementedException();

        public override string ToString() => (Name != null) ? $"Zone ({Name})" : $"Zone ({PathRadius:0.0} mm)";

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            this.Name = reader.GetString("Name");
            this.PathRadius = reader.GetDouble("PathRadius");
            this.PathOrient = reader.GetDouble("PathOrient");
            this.PathExternal = reader.GetDouble("PathExternal");
            this.Orientation = reader.GetDouble("Orientation");
            this.LinearExternal = reader.GetDouble("LinearExternal");
            this.RotaryExternal = reader.GetDouble("RotaryExternal");
            this.StopPoint = reader.GetBoolean("StopPoint");
            return true;
        }

        public bool Write(GH_IWriter writer)
        {
            if (this.Name != string.Empty) writer.SetString("Name", this.Name);
            writer.SetDouble("PathRadius", this.PathRadius);
            writer.SetDouble("PathOrient", this.PathOrient);
            writer.SetDouble("PathExternal", this.PathExternal);
            writer.SetDouble("Orientation", this.Orientation);
            writer.SetDouble("LinearExternal", this.LinearExternal);
            writer.SetDouble("RotaryExternal", this.RotaryExternal);
            writer.SetBoolean("StopPoint", this.StopPoint);
            return true;
        }

        #endregion Interfaces
    }

    /// <summary>
    /// Coordinate system.
    /// </summary>
    public class CSystem : IGH_Goo, Axis_IDisplayable
    {
        public string Name { get; set; }
        public Plane CSPlane { get; set; }
        public bool Dynamic { get; set; }
        public Plane ExternalAxis { get; set; }

        #region Constructors

        /// <summary>
        /// Default coordinate system object.
        /// </summary>
        public static CSystem Default => new CSystem("Default", Plane.WorldXY, false, Plane.WorldXY);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CSystem() { }

        /// <summary>
        /// Standard coordinate system constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="csPlane"></param>
        /// <param name="dynamicCS"></param>
        /// <param name="eAxisPlane"></param>
        public CSystem(string name, Plane csPlane, bool dynamicCS, Plane eAxisPlane)
        {
            this.Name = name;
            this.CSPlane = csPlane;
            this.Dynamic = dynamicCS;
            this.ExternalAxis = eAxisPlane;
        }

        #endregion Constructors

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.Name = string.Empty;
            this.CSPlane = Plane.Unset;
            this.Dynamic = false;
            this.ExternalAxis = Plane.Unset;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        // IGH_Goo
        public bool IsValid
        {
            get
            {
                if (this.CSPlane != null) return true;
                else return false;
            }
        }

        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "CSystem";
        public string TypeDescription => "Local coordinate system";

        public bool CastFrom(object source) => throw new NotImplementedException();

        public bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(Q).Name;
                object value = new GH_ObjectWrapper(this);
                target = (Q)value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)) && (this.CSPlane != null))
            {
                object _Plane = new GH_Plane(this.CSPlane);
                target = (Q)_Plane;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Point)) && (this.CSPlane != null))
            {
                object _Point = new GH_Point(this.CSPlane.Origin);
                target = (Q)_Point;
                return true;
            }
            target = default(Q);
            return false;
        }

        public IGH_Goo Duplicate()
        {
            return new CSystem(this.Name, this.CSPlane.Clone(), this.Dynamic, this.ExternalAxis.Clone());
        }

        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

        public object ScriptVariable() => throw new NotImplementedException();

        public override string ToString() => $"CSystem at: {CSPlane.ToString()}";

        //GH_ISerializable
        public bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("Name")) this.Name = reader.GetString("Name");
            if (reader.ChunkExists("CSPlane"))
            {
                var gh_CSPlane = new GH_Plane();
                Plane csPlane = Plane.Unset;
                gh_CSPlane.Read(reader.FindChunk("CSPlane"));
                GH_Convert.ToPlane(gh_CSPlane, ref csPlane, GH_Conversion.Both);
                this.CSPlane = csPlane;
            }
            if (reader.ItemExists("Dynamic")) this.Dynamic = reader.GetBoolean("Dynamic");
            if (reader.ItemExists("ExternalAxis"))
            {
                var gh_ExternalAxis = new GH_Plane();
                Plane eaPlane = Plane.Unset;
                gh_ExternalAxis.Read(reader.FindChunk("ExternalAxis"));
                GH_Convert.ToPlane(gh_ExternalAxis, ref eaPlane, GH_Conversion.Both);
                this.CSPlane = eaPlane;
            }

            return true;
        }

        public bool Write(GH_IWriter writer)
        {
            if (this.Name != string.Empty) writer.SetString("Name", this.Name);
            if (this.CSPlane != null)
            {
                var gh_CSPlane = new GH_Plane(this.CSPlane);
                gh_CSPlane.Write(writer.CreateChunk("CSPlane"));
            }
            writer.SetBoolean("Dynamic", this.Dynamic);
            if (this.ExternalAxis != null)
            {
                var gh_ExternalAxis = new GH_Plane(this.ExternalAxis);
                gh_ExternalAxis.Write(writer.CreateChunk("ExternalAxis"));
            }
            return true;
        }



        // Display

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            double sizeLine = 70; double sizeArrow = 30; int thickness = 3;

            args.Display.DrawLineArrow(
                new Line(this.CSPlane.Origin, this.CSPlane.XAxis, sizeLine),
                Axis.Styles.Pink,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(this.CSPlane.Origin, this.CSPlane.YAxis, sizeLine),
                Axis.Styles.LightBlue,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(this.CSPlane.Origin, this.CSPlane.ZAxis, sizeLine),
                Axis.Styles.LightGrey,
                thickness,
                sizeArrow);
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            throw new NotImplementedException();
        }

        #endregion Interfaces


    }

    /// <summary>
    /// External targets.
    /// </summary>
    public class ExternalTarget
    {
        // MoveExtJ [\Conc] ToJointPos [\ID] [\UseEOffs] Speed [\T] Zone [\Inpos]

        public bool Concurrent { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }
        public Zone Zone { get; set; }
    }

    /// <summary>
    /// Tool path class allowing for approximated time-based simulation.
    /// </summary>
    public class Toolpath : IGH_Goo, Axis_IDisplayable
    {
        #region Variables
        public TimeSpan duration { get; private set; }
        private double totalSec;
        private Instruction[] instructions;
        private Robot.Pose[] poses;
        private List<double> targetProgress;
        private Tuple< Command, Point3d>[] commands;
        private Robot robot;
        #endregion Variables

        #region Propperties

        public Robot.Pose StartPose => poses[0];

        public List<string> ErrorLog
        {
            get
            {
                var list = new List<string>();
                for (int i = 0; i < poses.Length; ++i)
                {
                    if (!poses[i].IsValid)
                    {
                        var msg = $"{(i + 1).ToString()}. Target";
                        if (poses[i].OverHeadSig) msg += ": Singularity";
                        if (poses[i].OutOfReach) msg += ": Unreachable";
                        if (poses[i].WristSing) msg += ": Wrist Singularity";
                        if (poses[i].OutOfRoation) msg += ": Joint Error";
                        if (poses[i].WristSing) msg += ": Wrist Singularity";
                        list.Add(msg);
                    }
                }
                return list;
            }
        }

        public List<Point3d> ErrorPositions
        {
            get
            {
                var points = new List<Point3d>();
                foreach (Robot.Pose pose in poses) if (!pose.IsValid) points.Add(pose.TargetPlane.Origin);
                return points;
            }
        }

        #endregion Propperties

        #region Constructor

        public Toolpath()
        {
        }

        /// <summary>
        /// Toolpath constructor.
        /// </summary>
        /// <param name="targets"></param>
        public Toolpath(List<Instruction> instructions, Robot robot)
        {
            this.instructions = instructions.ToArray();
            List<Target> targets = new List<Target>();
            List<Tuple<Command, Point3d>> commands = new List<Tuple<Command, Point3d>>();
            Point3d lastPoint = robot.GetPose(ABBTarget.Default).TargetPlane.Origin;

            foreach (Instruction inst in instructions) 
            {
                /*
                 * @ todo Make switch statement
                 * @ body With the upgrade of C# new type based switch statements will be possible.
                switch (inst) 
                {
                    case Target:
                        break;
                    case Command:
                        break;
                }*/
                if (inst is Target) 
                {
                    var targ = inst as Target;
                    targets.Add(targ);
                    lastPoint = targ.TargetPlane.Origin;
                }
                else if (inst is Command) 
                {
                    commands.Add(new Tuple<Command, Point3d>( inst as Command, new Point3d(lastPoint)));
                }
            }


            this.commands = commands.ToArray();
            this.robot = robot;
            var poses = robot.GetPoses(targets);

            Times(poses);
            this.poses = poses;
        }

        #endregion Constructor

        #region Methods

        //public
        /// <summary>
        /// Get the target index for the specified time.
        /// </summary>
        /// <param name="timePassed"></param>
        /// <returns></returns>
        public int GetProgress(TimeSpan timePassed)
        {
            double passedSec = timePassed.TotalSeconds;
            if (passedSec < this.totalSec)
            {
                var val = -this.targetProgress.BinarySearch(passedSec);
                if (val >= poses.Length) val = poses.Length - 1;
                return val;
            }
            else return poses.Length - 1;
        }

        /// <summary>
        /// Get the target object for the specified time.
        /// </summary>
        /// <param name="timePassed"></param>
        /// <returns></returns>
        public Robot.Pose GetPose(TimeSpan timePassed)
        {
            return this.poses[this.GetProgress(timePassed)];
        }

        public Robot.Pose GetPose(double value)
        {
            // Ensure value is between 0 and 1
            value = (value > 0) ? value : 0;
            value = (value < 1) ? value : 1;

            double total = poses.Length - 1;

            int position = (int)Math.Round((total / 100) * value * 100);

            return poses[position];
        }

        //private
        private void Times(IReadOnlyList<Robot.Pose> poses)
        {
            double timeTotal = 0;

            List<double> tProgress = new List<double>();
            tProgress.Add(0);


            for (int i = 0; i < poses.Count - 1; ++i)
            {
                var targets = poses[i].Target;
                double distance = new Line(poses[i].TargetPlane.Origin, poses[i + 1].TargetPlane.Origin).Length;
                double speed = poses[i + 1].Speed.TranslationSpeed;
                timeTotal += distance / speed;
                tProgress.Add(timeTotal);
            }

            this.totalSec = timeTotal;
            this.duration = new TimeSpan(0, 0, (int)timeTotal);
            this.targetProgress = tProgress;
        }

        #endregion Methods

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches()
        {
            this.duration = TimeSpan.Zero;
            this.totalSec = double.NaN;
            this.poses = null;
            this.targetProgress = null;
        }

        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();

        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();

        public bool LoadGeometry() => throw new NotImplementedException();

        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();

        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();

        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();


        // IGH_Goo
        public bool IsValid
        {
            get
            {
                bool valid = true;

                // Check if all poses are valid
                foreach (Robot.Pose p in poses) { valid = valid & p.IsValid; }

                //Make sure all poses are not in place
                if (this.totalSec == double.NaN) valid = false;
                if (poses.Length == 0) valid = false;

                return valid;
            }
        }

        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "Tool path";
        public string TypeDescription => "A collection of targets as a tool path";

        public bool CastFrom(object source) => throw new NotImplementedException();

        public bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_ObjectWrapper)))
            {
                string name = typeof(Q).Name;
                object value = new GH_ObjectWrapper(this);
                target = (Q)value;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                List<Point3d> points = new List<Point3d>();

                foreach (Robot.Pose p in this.poses)
                {
                    if (p.TargetPlane.Origin != null)
                    {
                        points.Add(p.TargetPlane.Origin);
                    }
                }

                PolylineCurve pLine = new PolylineCurve(points);
                object _pLine = new GH_Curve(pLine);
                target = (Q)_pLine;
                return true;
            }
            target = default(Q);
            return false;
        }

        public IGH_Goo Duplicate() => throw new NotImplementedException();

        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();

        public object ScriptVariable() => throw new NotImplementedException();

        public override string ToString() => $"Toolpath of length: {this.poses.Length}";


        //GH_ISerializable
        public bool Read(GH_IReader reader) => throw new NotImplementedException();

        public bool Write(GH_IWriter writer) => throw new NotImplementedException();


        //Display
        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            int thickness = 3;

            Line[] lines = new Polyline(poses.Select(p => p.TargetPlane.Origin).ToList()).GetSegments();
            Grasshopper.GUI.Gradient.GH_Gradient colors = new Grasshopper.GUI.Gradient.GH_Gradient(
                new List<double>() { 5, 150, 1000 }, 
                new List<Color>() { Color.Green, Color.Yellow, Color.Red}
                );

            if (lines != null) for (int i = 0; i < lines.Length; ++i) args.Display.DrawLine(lines[i], colors.ColourAt(poses[i + 1].Target.Speed.TranslationSpeed), thickness);
            for (int i = 0; i < commands.Length; ++i)
            {
                var raisedPoint = new Point3d() { 
                    X = commands[i].Item2.X, 
                    Y = commands[i].Item2.Y, 
                    Z = commands[i].Item2.Z + 20 };
                args.Display.DrawArrow(new Line( raisedPoint, commands[i].Item2), Styles.Blue);

                //Get the viewport plane
                var textPLane = new Plane();
                args.Viewport.GetFrustumFarPlane(out textPLane);
                textPLane.Origin = raisedPoint;
                
                args.Display.Draw3dText(commands[i].Item1.RobStr(robot.Manufacturer), Styles.Blue, textPLane, 10, "Arial") ;
            }
            

        }
        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            throw new NotImplementedException();
        }

        #endregion Interfaces
    }

    /*
     * @todo Move StringConvrsion
     * @body Move StringConvrsion methods in to the respective classes
     */
    /// <summary>
    /// Class handlining the conversion for different types to the spesific manufacturere string representation
    /// </summary>
    internal static class StringConversion
    {
        public static string CodeStrFor(this Speed speed, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:

                    string strSpeed = null;

                    if (speed.Time > 0)
                    {
                        if (speed != null)
                            strSpeed = speed.Name + @"\T:=" + speed.Time.ToString();
                        else
                            strSpeed = @"v200\T:=" + speed.Time.ToString();
                    }
                    else
                    {
                        if (speed.Name != null)
                            strSpeed = speed.Name;
                    }

                    return strSpeed;
            }
            throw new NotImplementedException($"String representation of speeds not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this Zone zone, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    return zone.Name;
            }
            throw new NotImplementedException($"String representation of zones not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this Tool tool, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    // ******** CTool() instead of Tool0?
                    string toolName = "tool0";
                    if (tool.Name != "DefaultTool")
                        toolName = tool.Name;
                    return toolName;
            }
            throw new NotImplementedException($"String representation of tools not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this CSystem cSystem, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    return $"\\WObj:={cSystem.Name}";
            }
            throw new NotImplementedException($"Linear external axis string not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this ExtVal eVal, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    string str = "9E9";
                    if (eVal.IsValid) // If the external axis value is present... (otherwise 0.00001 is passed as a default value).
                        str = Math.Round(eVal, 4).ToString(); // Get the external axis value per target and round it to two decimal places.
                    return str;
            }
            throw new NotImplementedException($"External axis string not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this Quaternion quat, Manufacturer manufacturer)
        {
            switch (Manufacturer.ABB)
            {
                case Manufacturer.ABB:
                    double A = quat.A, B = quat.B, C = quat.C, D = quat.D;

                    double w = Math.Round(A, 6);
                    double x = Math.Round(B, 6);
                    double y = Math.Round(C, 6);
                    double z = Math.Round(D, 6);

                    return $"{w.ToString()},{x.ToString()},{y.ToString()},{z.ToString()}";
            }
            throw new NotImplementedException($"Quaternion string not implemented for {manufacturer.ToString()}");
        }

        public static string CodeStrFor(this Point3d position, Manufacturer manufacturer)
        {
            double posX = Math.Round(position.X, 3);
            double posY = Math.Round(position.Y, 3);
            double posZ = Math.Round(position.Z, 3);

            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    return $"{posX.ToString()},{posY.ToString()},{posZ.ToString()}";

                case Manufacturer.Kuka:
                    return $"X {posX.ToString()}, Y {posY.ToString()}, Z {posZ.ToString()}";
            }
            throw new NotImplementedException($"Point3d string not implemented for {manufacturer.ToString()}");
        }
    }
}