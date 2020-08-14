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
    /// Robot target used to instruct a movement
    /// </summary>
    public class Target : GH_Goo<Target>
    {

        public Plane Plane { get; set; } // Position in World Coordinates


        public Plane TargetPlane; // Position in local coordinates
        public Quaternion Quaternion { get; set; }
        public List<double> JointAngles { get; set; }


        public Speed Speed { get; }
        public Zone Zone { get; }

        public Tool Tool { get; }
        public CSystem CSystem { get; set; }

        public ExtVal ExtRot { get; set; }
        public ExtVal ExtLin { get; set; }


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
                    Transform xForm = Transform.PlaneToPlane(this.CSystem.CSPlane, Plane.WorldXY);

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

                            #region Linear movement
                            case MotionType.Linear:


                                if (this.Tool.relTool != Vector3d.Zero)
                                {
                                    //MoveL RelTool ([[416.249, -110.455, 0],[0, 0, 1, 0], cData, eAxis], 0, 0,-120), v50, z1, tool0 \Wobj:=wobj0;
                                    string offset = $"[{this.Tool.relTool.X.ToString()}, {this.Tool.relTool.Y.ToString()}, {this.Tool.relTool.Z.ToString()}]";
                                    return $"MoveL RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(this.Manufacturer)},{this.Zone.CodeStrFor(this.Manufacturer)},{this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                                else
                                {
                                    return $"MoveL {robtarget}, {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }

                            #endregion
                            #region Joint movment
                            case MotionType.Joint:

                                if (this.Tool.relTool != Vector3d.Zero)
                                {
                                    string offset = $"[{this.Tool.relTool.X.ToString()}, {this.Tool.relTool.Y.ToString()}, {this.Tool.relTool.Z.ToString()}]";
                                    return $"MoveJ RelTool ({robtarget},{offset}),{this.Speed.CodeStrFor(this.Manufacturer)},{this.Zone.CodeStrFor(this.Manufacturer)},{this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                                else
                                {
                                    return $"MoveJ {robtarget}, {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)} {this.CSystem.CodeStrFor(this.Manufacturer)};";
                                }
                            #endregion
                            #region Absolute movment
                            case MotionType.AbsoluteJoint:
                                string jTarg = $"[{this.JointAngles[0].ToString()},{this.JointAngles[1].ToString()},{this.JointAngles[2].ToString()},{this.JointAngles[3].ToString()},{this.JointAngles[4].ToString()},{this.JointAngles[5].ToString()}]";
                                return $"MoveAbsJ [{jTarg},[{this.ExtRot.CodeStrFor(this.Manufacturer)},{this.ExtLin.CodeStrFor(this.Manufacturer)},9E9,9E9,9E9,9E9]], {this.Speed.CodeStrFor(this.Manufacturer)}, {this.Zone.CodeStrFor(this.Manufacturer)}, {this.Tool.CodeStrFor(this.Manufacturer)};";
                            #endregion

                            case MotionType.NoMovement:
                                return "! No Movement";
                        }
                        throw new Exception($"{this.Method.ToString()} not implemented for ABB");
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

                            #region Linear movement
                            case MotionType.Linear:
                                return $"LIN  {{E6POS:  {KUKAposition}, {strEuler}, {strExtAxis}}} C_VEL";

                            #endregion
                            case MotionType.Joint:
                                return $"PTP  {{E6POS:  {KUKAposition}, {strEuler}, {strExtAxis}}} C_PTP";

                            case MotionType.NoMovement:
                                return "! No Movement";
                        }
                        throw new Exception($"{this.Method.ToString()} not implemented for Kuka");
                        #endregion
                }
                throw new Exception($"The target string repersentation for {this.Manufacturer.ToString()} is not implemented");

            }
        }
        public Point3d Position { get =>  this.TargetPlane.Origin; }


        public MotionType Method { get; }

        public Manufacturer Manufacturer { get; }

        public static Target Default { get; }

        public Target(Plane target, MotionType method, Speed speed, Zone zone, Tool tool, CSystem wobj, double extRot, double extLin, Manufacturer robot)
        {
            // Adjust plane to comply with robot programming convetions.
            Quaternion realQuat = Util.QuaternionFromPlane(target);

            // Set publicly accessible property values based on the manufacturer data.
            this.TargetPlane = target;
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


            //Old Code
            #region
            /*
            // Copy target in case we are using a dynamic CS
            Plane dynamicTarget = new Plane(target);
            if (wobj.Dynamic)
            {
                Transform rot = Transform.Rotation(extRot.ToRadians(), wobj.ExternalAxis.Normal, wobj.ExternalAxis.Origin);
                if (dynamicTarget.Transform(rot))
                    this.Plane = dynamicTarget;
            }
            else
                this.Plane = target;

            // Offset with the CSystem to get the right program code.
            Transform xForm = Transform.PlaneToPlane(wobj.CSPlane, Plane.WorldXY);

            Plane plane = new Plane(target);
            plane.Transform(xForm);

            Quaternion quat = Util.QuaternionFromPlane(plane);

            Point3d position = plane.Origin;
            double posX = Math.Round(position.X, 3);
            double posY = Math.Round(position.Y, 3);
            double posZ = Math.Round(position.Z, 3);

            this.Position = position;

            // Declare some strings to store the target information in a manufacturer specific manner.
            string movement = null;
            string strRob = null;
            string strZone = zone.Name;
            string strSpeed = null;
            string exLin = "9E9";
            string exRot = "9E9";

            // Tool
            string toolName = String.Empty;
            if (tool.Name != "DefaultTool")
                toolName = tool.Name;

            // Work object
            string workObject = @"\Wobj:=" + wobj.Name;
            this.CSystem = wobj;

            //this.StrRob = strRob;
            */

            #endregion
        }

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


        public override string ToString() => (Method != null) ? $"Target ({Method.ToString()})" : $"Target ({Position})";
        public override string TypeName => "Target";
        public override string TypeDescription => "Robot target";
        public override bool IsValid => true;
        public override int GetHashCode()
        {
            var val = Plane.GetHashCode() + Speed.GetHashCode() + Zone.GetHashCode() + Tool.GetHashCode() + CSystem.GetHashCode();
            return val.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            //This should technically do a deep copy not a shalow one, like in this case
            return this;
        }
        public override bool CastTo<Q>(ref Q target)
        {

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

            return false;
        }


        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }
    }

    /// <summary>
    /// Class handlining the conversion for different types to the spesific manufacturere string representation
    /// </summary>
    static class StrringConvertiosn
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

            throw new Exception($"String representation of speeds not implemented for {manufacturer.ToString()}");
        }
        public static string CodeStrFor(this Zone zone, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    return zone.Name;
            }
            throw new Exception($"String representation of zones not implemented for {manufacturer.ToString()}");
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
            throw new Exception($"String representation of tools not implemented for {manufacturer.ToString()}");
        }
        public static string CodeStrFor(this CSystem cSystem, Manufacturer manufacturer)
        {
            switch (manufacturer)
            {
                case Manufacturer.ABB:
                    return $"\\WObj:={cSystem.Name}";
            }
            throw new Exception($"Linear external axis string not implemented for {manufacturer.ToString()}");
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
            throw new Exception($"External axis string not implemented for {manufacturer.ToString()}");
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
            throw new Exception($"Quaternion string not implemented for {manufacturer.ToString()}");
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
            throw new Exception($"Point3d string not implemented for {manufacturer.ToString()}");
        }

    }


    /// <summary>
    /// Type wrapper for a double representing the external axis value
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
    public class Speed : GH_Goo<double>
{
        public string Name { get; set; }
        public double TranslationSpeed { get; set; }
        public double RotationSpeed { get; set; }
        public double Time { get; set; } = 0;

        public static Speed Default { get; }

        static Speed()
        {
            Default = new Speed(100, 30, "DefaultSpeed");
        }

        public Speed(double tcpSpeed = 100, double rotSpeed = 30, string name = null, double time = 0.0)
        {
            this.Name = name;
            this.TranslationSpeed = tcpSpeed;
            this.RotationSpeed = rotSpeed;
            this.Time = time;
        }

        public override string ToString() => (Name != null) ? $"Speed ({Name})" : $"Speed ({TranslationSpeed:0.0} mm/s)";
        public override string TypeName => "Speed";
        public override string TypeDescription => "Movement speed in mm/s";
        public override bool IsValid => true;
        public override double Value { get => this.TranslationSpeed; set => this.TranslationSpeed = value; }
        public override int GetHashCode()
        {
            var val = Name.GetHashCode()+ TranslationSpeed.GetHashCode()+ RotationSpeed.GetHashCode()+ Time.GetHashCode();
            return val.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            //This should technically do a deep copy not a shalow one, like in this case
            return this;
        }
        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.TranslationSpeed != null))
            {
                object _number = new GH_Number(this.TranslationSpeed);
                target = (Q)_number;
                return true;
            }

            return false;
        }
    }
    /// <summary>
    /// Robot movement zone type
    /// </summary>
    public class Zone : GH_Goo<double>
    {
        public string Name { get; set; }
        public double PathRadius { get; set; }
        public double PathOrient { get; set; }
        public double PathExternal { get; set; }
        public double Orientation { get; set; }
        public double LinearExternal { get; set; }
        public double RotaryExternal { get; set; }
        public bool StopPoint { get; set; }

        public static Zone Default { get; }
        public override string TypeName => "Zone";
        public override string TypeDescription => "Precision zone in mm";
        public override double Value { get => this.PathRadius; set => this.PathRadius = value; }

        static Zone()
        {
            Default = new Zone(false, 5, 25, 25, 15, 35, 5, "DefaultZone");
        }

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

        public override string ToString() => (Name != null) ? $"Zone ({Name})" : $"Zone ({PathRadius:0.0} mm)";
        public override bool IsValid => true;
        public override int GetHashCode()
        {
            var val = Name.GetHashCode()+PathRadius.GetHashCode()+ PathOrient.GetHashCode()+ PathExternal.GetHashCode()+ Orientation.GetHashCode()+ LinearExternal.GetHashCode()+RotaryExternal.GetHashCode()+StopPoint.GetHashCode();
            return val.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            //This should technically do a deep copy not a shalow one, like in this case
            return this;
        }
        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Number)) && (this.PathRadius != null))
            {
                object _number = new GH_Number(this.PathRadius);
                target = (Q)_number;
                return true;
            }

            return false;
        }

    }
    /// <summary>
    /// Robot working coordinat system
    /// </summary>
    public class CSystem : GH_Goo<Plane>
    {
        public string Name { get; set; }
        public Plane CSPlane { get; set; }
        public bool Dynamic { get; set; }
        public Plane ExternalAxis { get; set; }

        public static CSystem Default { get; set; }

        public CSystem(string name, Plane csPlane, bool dynamicCS, Plane eAxisPlane)
        {
            this.Name = name;
            this.CSPlane = csPlane;
            this.Dynamic = dynamicCS;
            this.ExternalAxis = eAxisPlane;
        }
        public override string ToString()
        {
            return $"CSystem at: {CSPlane.ToString()}";
        }

        static CSystem()
        {
            Default = new CSystem("Default", Plane.WorldXY, false, Plane.WorldXY);
        }
        public override string TypeName => "CSystem";
        public override string TypeDescription => "Local coordinate system";
        public override bool IsValid {
            get {
                if (this.CSPlane != null) return true;
                else return false;
            }
        }
        public override Plane Value { get => this.CSPlane; set => this.CSPlane = value; }
        public override int GetHashCode()
        {
            var val = Name.GetHashCode() + CSPlane.GetHashCode() + Dynamic.GetHashCode() + ExternalAxis.GetHashCode();
            return val.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            //This should technically do a deep copy not a shalow one, like in this case
            return this;
        }
        public override bool CastTo<Q>(ref Q target)
        {

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

            return false;
        }
    }
    public class ExternalTarget
    {
        // MoveExtJ [\Conc] ToJointPos [\ID] [\UseEOffs] Speed [\T] Zone [\Inpos]

        public bool Concurrent { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }
        public Zone Zone { get; set; }
    }
    /// <summary>
    /// Tool path class allowing for time aproximate simulation
    /// </summary>
    public class Toolpath : GH_Goo<Toolpath>
    {
        public TimeSpan duration { get; private set; }

        double totalSec;
        List<Target> targets;
        List<double> targetProgress;

        /// <summary>
        /// Class initilaisation
        /// </summary>
        /// <param name="targets">List of targets the tool path consists of</param>
        public Toolpath(List<Target> targets)
        {
            Times(targets);
            this.targets = targets;
        }
        /// <summary>
        /// Internal Class initialisation
        /// </summary>
        /// <param name="targets">List of targets ta make up this tool path</param>
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

        /// <summary>
        /// Get the target interger for the current progress
        /// </summary>
        /// <param name="timePassed">Time passed since the start of the simulation</param>
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
        /// Get the target for the current progress
        /// </summary>
        /// <param name="timePassed">Time since the start of the simulation</param>
        /// <returns></returns>
        public Target GetTarget(TimeSpan timePassed)
        {
            return this.targets[this.GetProgress(timePassed)];
        }

        public override string TypeName => "Tool path";
        public override string TypeDescription => "A collection of targets as a tool path";
        public override bool IsValid
        {
            get
            {
                if (this.totalSec != null) return true;
                else return false;
            }
        }
        public override string ToString()
        {
            return $"Toolpath of length: {this.targets.Count}";
        }
        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }
        public override bool CastTo<Q>(ref Q target)
        {

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

            return false;
        }

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