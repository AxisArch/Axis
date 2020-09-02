using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Axis.Core;
using GH_IO.Serialization;

namespace Axis.Targets
{
    /// <summary>
    /// Construct a robot target position.
    /// </summary>
    public class Target : IGH_GeometricGoo
    {
        #region Class Fields
        public Plane Plane { get; set; } // Position in World Coordinates

        public Plane TargetPlane; // Position in local coordinates
        public Quaternion Quaternion { get; set; }
        public List<double> JointAngles { get; set; }

        public Speed Speed { get; private set; }
        public Zone Zone { get; private set; }

        public Tool Tool { get; private set; }
        public CSystem CSystem { get; set; }

        public ExtVal ExtRot { get; set; }
        public ExtVal ExtLin { get; set; }
        #endregion
        public Manufacturer Manufacturer { get; private set; }

        public string StrRob
        { get
            {
                string robtarget = string.Empty;
                Plane plane =  Plane.WorldXY;
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

                    robtarget = $"[[{position.CodeStrFor(this.Manufacturer)}],[{quat.CodeStrFor(this.Manufacturer)}],cData,[{this.ExtRot.CodeStrFor(this.Manufacturer)},{this.ExtLin.CodeStrFor(this.Manufacturer)},9E9,9E9,9E9,9E9]]";

                }


                switch (this.Manufacturer)
                {
                    #region ABB
                    case Manufacturer.ABB:
                        switch (this.Method)
                        {

                            #region Linear
                            case MotionType.Linear:


                                if (this.Tool.RelTool != Vector3d.Zero)
                                {
                                    //MoveL RelTool ([[416.249, -110.455, 0],[0, 0, 1, 0], cData, eAxis], 0, 0,-120), v50, z1, tool0 \Wobj:=wobj0;
                                    string offset = $"[{this.Tool.RelTool.X.ToString()}, {this.Tool.RelTool.Y.ToString()}, {this.Tool.RelTool.Z.ToString()}]";
                                    return $"MoveL RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(this.Manufacturer)},{this.Zone.CodeStrFor(this.Manufacturer)},{this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                                else
                                {
                                    return $"MoveL {robtarget}, {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }

                            #endregion

                            #region Joint
                            case MotionType.Joint:

                                if (this.Tool.RelTool != Vector3d.Zero)
                                {
                                    string offset = $"[{this.Tool.RelTool.X.ToString()}, {this.Tool.RelTool.Y.ToString()}, {this.Tool.RelTool.Z.ToString()}]";
                                    return $"MoveJ RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(this.Manufacturer)},{this.Zone.CodeStrFor(this.Manufacturer)},{this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                                else
                                {
                                    return $"MoveJ {robtarget}, {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                            #endregion

                            #region Absolute
                            case MotionType.AbsoluteJoint:
                                string jTarg = $"[{this.JointAngles[0].ToString()},{this.JointAngles[1].ToString()},{this.JointAngles[2].ToString()},{this.JointAngles[3].ToString()},{this.JointAngles[4].ToString()},{this.JointAngles[5].ToString()}]";
                                return $"MoveAbsJ [{jTarg},[{this.ExtRot.CodeStrFor(this.Manufacturer)},{this.ExtLin.CodeStrFor(this.Manufacturer)},9E9,9E9,9E9,9E9]], {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)};";
                            #endregion

                            case MotionType.NoMovement:
                                return "! No Movement";
                        }
                        throw new NotImplementedException($"{this.Method.ToString()} not implemented for ABB");
                    #endregion

                    #region Kuka
                    case Manufacturer.Kuka:

                        string KUKAposition = position.CodeStrFor(this.Manufacturer);

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

                            #endregion

                            case MotionType.Joint:
                                return $"PTP  {{E6POS:  {KUKAposition}, {strEuler}, {strExtAxis}}} C_PTP";

                            case MotionType.NoMovement:
                                return "! No Movement";
                        }
                        throw new NotImplementedException($"{this.Method.ToString()} not implemented for Kuka");
                        #endregion
                }
                throw new NotImplementedException($"The target string repersentation for {this.Manufacturer.ToString()} is not implemented");

            }
        }
        public Point3d Position { get =>  this.TargetPlane.Origin; }
        public MotionType Method { get; private set; }

        #region Constructors

        /// <summary>
        /// Default target object.
        /// </summary>
        public static Target Default { get => new Target(new List<double> { 0, 0, 0, 0, 0, 0 }, Speed.Default, Zone.Default, Tool.Default, 0, 0, Manufacturer.ABB); }
        
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
        public Target(Plane target, MotionType method, Speed speed, Zone zone, Tool tool, CSystem wobj, double extRot, double extLin, Manufacturer robot)
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
            this.Method = method;
            this.Tool = tool;
            this.Speed = speed;
            this.Zone = zone;
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
        public Target(List<double> axisVals, Speed speed, Zone zone, Tool tool, double extRot, double extLin, Manufacturer robot)
        {
            this.JointAngles = axisVals;
            this.Tool = tool;
            this.Speed = speed;
            this.Zone = zone;
            this.ExtRot = extRot;
            this.ExtLin = extLin;

            this.Method = MotionType.AbsoluteJoint;
            this.Manufacturer = robot;
        }
        #endregion

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); } //Cached boundingbox
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => false; }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() 
        {
            this.Plane = Plane.Unset;
            this.TargetPlane = Plane.Unset;
            this.Quaternion = Quaternion.Zero;
            this.JointAngles = null;
            this.Tool = null;
            this.Speed = null;
            this.Zone = null;
            this.ExtRot = null;
            this.ExtLin = null;
            this.Method = 0;
            this.Manufacturer = 0;
        }
        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();
        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();
        public bool LoadGeometry() => throw new NotImplementedException();
        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();
        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();
        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();


        // IGH_Goo
        public bool IsValid => true;
        public string IsValidWhyNot => throw new NotImplementedException();
        public string TypeName => "Target";
        public string TypeDescription => "Robot target";

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
        public IGH_Goo Duplicate() 
        {
            var target = default(Target);
            switch (this.Method) 
            {
                case MotionType.Joint:
                case MotionType.Linear:
                    target = new  Target(this.Plane.Clone(), this.Method , (Speed)this.Speed.Duplicate(), (Zone)this.Zone.Duplicate(), (Tool)this.Tool.Duplicate(), (CSystem)this.CSystem.Duplicate(), this.ExtRot, this.ExtLin, this.Manufacturer);
                    break;
                case MotionType.AbsoluteJoint:
                    target = new Target(this.JointAngles, (Speed)this.Speed.Duplicate(), (Zone)this.Zone.Duplicate(), (Tool)this.Tool.Duplicate(), this.ExtRot, this.ExtLin, this.Manufacturer);
                    break;
            }
            return target;
        }
        public IGH_GooProxy EmitProxy() => throw new NotImplementedException();
        public object ScriptVariable() => throw new NotImplementedException();
        public override string ToString() => (Method != null) ? $"Target ({Method.ToString()})" : $"Target ({Position})";


        //GH_ISerializable
        public bool Read(GH_IReader reader) => throw new NotImplementedException();
        public bool Write(GH_IWriter writer) => throw new NotImplementedException();

        #endregion
    }

    /// <summary>
    /// Class handlining the conversion for different types to the spesific manufacturere string representation
    /// </summary>
    static class StringConversion
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
                    if (eVal != Util.ExAxisTol) // If the external axis value is present... (otherwise 0.00001 is passed as a default value).
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

    /// <summary>
    /// Type wrapper for a double representing the external axis value.
    /// </summary>
    public class ExtVal
    {
        private double val;

        ExtVal(double d) => this.val = d;

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
        public static Speed Default { get; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        static Speed()
        {
            Default = new Speed(100, 30, "DefaultSpeed");
        }

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
        #endregion

        #region Interfaces
        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() => throw new NotImplementedException();
        public IGH_GeometricGoo DuplicateGeometry() => throw new NotImplementedException();
        public BoundingBox GetBoundingBox(Transform xform) => throw new NotImplementedException();
        public bool LoadGeometry() => throw new NotImplementedException();
        public bool LoadGeometry(Rhino.RhinoDoc doc) => throw new NotImplementedException();
        public IGH_GeometricGoo Morph(SpaceMorph xmorph) => throw new NotImplementedException();
        public IGH_GeometricGoo Transform(Transform xform) => throw new NotImplementedException();

        // IGH_Goo
        public bool IsValid => throw new NotImplementedException();
        public string IsValidWhyNot => throw new NotImplementedException();
        public  string TypeName => "Speed";
        public  string TypeDescription => "Movement speed in mm/s";

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

            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.TranslationSpeed != null))
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
        public  override string ToString() => (Name != null) ? $"Speed ({Name})" : $"Speed ({TranslationSpeed:0.0} mm/s)";

        //GH_ISerializable
        public bool Read(GH_IReader reader) => throw new NotImplementedException();
        public bool Write(GH_IWriter writer) => throw new NotImplementedException();
        #endregion
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
        public static Zone Default => new Zone(false, 5, 25, 25, 15, 35, 5, "DefaultZone");

        /// <summary>
        /// Default constructor.
        /// </summary>
        static Zone(){}

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
        #endregion

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() => throw new NotImplementedException();
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

            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.PathRadius != null))
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
        public bool Read(GH_IReader reader) => throw new NotImplementedException();
        public bool Write(GH_IWriter writer) => throw new NotImplementedException();
        #endregion
    }

    /// <summary>
    /// Coordinate system.
    /// </summary>
    public class CSystem : IGH_Goo
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
        static CSystem(){}

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
        #endregion

        #region Interfaces

        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() => throw new NotImplementedException();
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
        public  override string ToString() => $"CSystem at: {CSPlane.ToString()}";

        //GH_ISerializable
        public bool Read(GH_IReader reader) => throw new NotImplementedException();
        public bool Write(GH_IWriter writer) => throw new NotImplementedException();
        #endregion
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
    public class Toolpath : IGH_Goo
    {
        public TimeSpan duration { get; private set; }
        double totalSec;
        List<Target> targets;
        List<double> targetProgress;

        #region Constructor
        public Toolpath() { }
        /// <summary>
        /// Toolpath constructor.
        /// </summary>
        /// <param name="targets"></param>
        public Toolpath(List<Target> targets)
        {
            Times(targets);
            this.targets = targets;
        }
        #endregion

        #region Methods
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
                if (val >= targets.Count) val = targets.Count - 1;
                return val;
            }
            else return targets.Count - 1;
        }

        /// <summary>
        /// Get the target object for the specified time.
        /// </summary>
        /// <param name="timePassed"></param>
        /// <returns></returns>
        public Target GetTarget(TimeSpan timePassed)
        {
            return this.targets[this.GetProgress(timePassed)];
        }

        void Times(List<Target> targets)
        {
            double timeTotal = 0;

            List<double> tProgress = new List<double>();
            tProgress.Add(0);

            for (int i = 0; i < targets.Count - 1; ++i)
            {
                double distance = new Line(targets[i].Position, targets[i + 1].Position).Length;
                double speed = targets[i + 1].Speed.TranslationSpeed;
                timeTotal += distance / speed;
                tProgress.Add(timeTotal);
            }

            this.totalSec = timeTotal;
            this.duration = new TimeSpan(0, 0, (int)timeTotal);
            this.targetProgress = tProgress;
        }
        #endregion

        #region Interfaces
        //IGH_GeometricGoo
        public BoundingBox Boundingbox { get => throw new NotImplementedException(); }
        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsReferencedGeometry { get => throw new NotImplementedException(); }
        public bool IsGeometryLoaded { get => throw new NotImplementedException(); }

        public void ClearCaches() => throw new NotImplementedException();
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
                if (this.totalSec != null) return true;
                else return false;
            }
        }
        public string IsValidWhyNot => throw new NotImplementedException();
        public  string TypeName => "Tool path";
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

                foreach (Target t in this.targets)
                {
                    if (t.Plane.Origin != null)
                    {
                        points.Add(t.Plane.Origin);
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
        public override string ToString() => $"Toolpath of length: {this.targets.Count}";

        //GH_ISerializable
        public bool Read(GH_IReader reader) => throw new NotImplementedException();
        public bool Write(GH_IWriter writer) => throw new NotImplementedException();
        #endregion
    }

    /// <summary>
    /// List of motion types
    /// </summary>
    public enum MotionType
    {
        Linear = 0,
        Joint = 1,
        AbsoluteJoint = 2,
        NoMovement = 3
    }

}