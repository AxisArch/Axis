using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Axis.Params;
using Axis.Core;
using GH_IO.Serialization;
using Rhino.Geometry;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Axis.Params
{
    public class RobotParam : GH_PersistentParam<Manipulator>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.
        public RobotParam()
          : base("Robot", "Robot", "Axis robot type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("17C49BD4-7A54-4471-961A-B5E0E971F7F4");

        protected override Manipulator InstantiateT()
        {
            return Manipulator.Default;
        }
        protected override GH_GetterResult Prompt_Singular(ref Manipulator value)
        {
            var bPlane = Plane.WorldXY;

        MainMenu:
            var go = new Rhino.Input.Custom.GetString();
            go.SetCommandPrompt("Set default robot.");
            go.AcceptNothing(true);
            go.AddOption("Default");
            go.AddOption("IRB_120");
            go.AddOption("IRB_6620");
            go.AddOption("SetBasePlane", $"O({bPlane.OriginX.ToString("0.00")},{bPlane.OriginY.ToString("0.00")},{bPlane.OriginZ.ToString("0.00")}) " +
                $"Z({bPlane.ZAxis.X.ToString("0.00")},{bPlane.ZAxis.Y.ToString("0.00")}, {bPlane.ZAxis.Z.ToString("0.00")})");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "Default") { var rob = Manipulator.Default; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "IRB_120") { var rob = Manipulator.IRB120; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "IRB_6620") { var rob = Manipulator.IRB6620; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "SetBasePlane") { GetBPlane(out bPlane); goto MainMenu; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.cancel;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<Manipulator> values)
        {
            return GH_GetterResult.cancel;
        }
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Set the default value", SetDefaultHandler, SourceCount == 0);
            base.AppendAdditionalMenuItems(menu);
        }
        private void SetDefaultHandler(object sender, EventArgs e)
        {
            PersistentData.Clear();
            PersistentData.Append(Manipulator.Default, new GH_Path(0));
            ExpireSolution(true);
        }






        public static Rhino.Commands.Result GetBPlane(out Plane plane)
        {

            var cPlaneOrig = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetConstructionPlane();

            Rhino.Commands.Result reset(Rhino.Commands.Result result)
            {
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetConstructionPlane(cPlaneOrig.Plane);
                return result;
            }

            plane = Plane.WorldXY;

            MoveCPlanePoint go = new MoveCPlanePoint(plane, State.PickOrigin);
            go.SetCommandPrompt("Set Origin");
            go.Get();
            if (go.CommandResult() != Rhino.Commands.Result.Success) return reset(go.CommandResult());
            plane.Origin = go.Point();


            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetConstructionPlane(plane);
            MoveCPlanePoint gx = new MoveCPlanePoint(plane, State.PickX);
            gx.SetCommandPrompt("Set Point in X Direction");
            gx.SetBasePoint(plane.Origin, true);
            gx.DrawLineFromPoint(plane.Origin, true);
            gx.Get();
            if (gx.CommandResult() != Rhino.Commands.Result.Success) return reset(gx.CommandResult());
            plane = PlaneFromXZ(plane, gx.Point());


            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetConstructionPlane(new Plane(plane.Origin, plane.YAxis, plane.ZAxis));
            MoveCPlanePoint gy = new MoveCPlanePoint(plane, State.PickY);
            gy.SetCommandPrompt("Set Point in Y Direction");
            gy.SetBasePoint(plane.Origin, true);
            gy.DrawLineFromPoint(plane.Origin, true);
            gy.Get();
            if (gy.CommandResult() != Rhino.Commands.Result.Success) return reset(gy.CommandResult());
            plane = PlanexRotation(plane, gy.Point()) ;

            return reset(Rhino.Commands.Result.Success);
        }



        class MoveCPlanePoint : Rhino.Input.Custom.GetPoint
        {
            readonly Rhino.DocObjects.ConstructionPlane cPlane;
            Plane bPlane;

            State m_state;
            public MoveCPlanePoint(Plane plane, State state)
            {
                cPlane = new Rhino.DocObjects.ConstructionPlane();
                cPlane.Plane = plane;
                bPlane = plane;
                m_state = state;
            }

            protected override void OnMouseMove(Rhino.Input.Custom.GetPointMouseEventArgs e)
            {
                switch (m_state)
                {
                    case State.PickOrigin:
                        bPlane.Origin = e.Point;
                        cPlane.Plane = bPlane;
                        break;

                    case State.PickX:
                        cPlane.Plane = PlaneFromXZ(bPlane, e.Point);
                        break;

                    case State.PickY:
                        cPlane.Plane = PlanexRotation(bPlane, e.Point);
                        break;

                }
            }

            protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
            {
                e.Display.DrawConstructionPlane(cPlane);
            }
        }

        static Plane PlaneFromXZ(Plane plane, Point3d point)
        {
            Plane rot1 = plane.Clone();
            var vec = new Vector3d(point - plane.Origin);
            double x1; double y1;
            double x2; double y2;


            plane.ClosestParameter(point, out x1, out y1);
            //var angle1 = signedVectorAngle(Vector3d.XAxis, new Vector3d(x1, y1, 0));
            var angle1 = Vector3d.VectorAngle(rot1.XAxis, vec, rot1);
            var xform1 = Transform.Rotation(angle1, plane.ZAxis, plane.Origin);
            rot1.Transform(xform1);

            Plane rot2 = rot1.Clone();
            if (point.Z != plane.OriginZ)
            {
                var angle2 = Vector3d.VectorAngle(vec, rot2.XAxis, rot2.YAxis);
                var xform2 = Transform.Rotation(angle2, rot2.YAxis, rot2.Origin);
                rot2.Transform(xform2);
            }
            return rot2;
        }
        static Plane PlanexRotation(Plane plane, Point3d point)
        {
            var yV = new Vector3d(plane.YAxis); yV.Unitize();
            var zV = new Vector3d(plane.ZAxis); zV.Unitize();


            var planeYZ = new Plane(plane.Origin, yV, zV);

            double x1; double y1;
            planeYZ.ClosestParameter(point, out x1, out y1);

            var p1 = new Point3d(point);
            var xChangeBase = Transform.ChangeBasis(Plane.WorldXY, planeYZ);
            p1.Transform(xChangeBase);

            var angle1 = signedVectorAngle(Vector3d.XAxis, new Vector3d(p1.X, p1.Y, p1.Z));

            plane.Rotate(angle1, plane.XAxis, plane.Origin);
            return plane;
        }


        /// <summary>
        /// Calculates the signed vector angle between two
        /// vectors v1 and v2.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        static double signedVectorAngle(Vector3d v1, Vector3d v2) 
        {
            v1 = new Vector3d(v1);
            v2 = new Vector3d(v2);


            //unitize both input vectors
            v1.Unitize();
            v2.Unitize();

            //create reference plane
            var vXAxis = Vector3d.CrossProduct(v1, v2);
            var plane = new Plane(Point3d.Origin, v1, v2);

            //change base of vectors to reference plane
            var xChangeBase = Transform.ChangeBasis(Plane.WorldXY, plane);
            v1.Transform(xChangeBase);
            v2.Transform(xChangeBase);

            //signed angle calculation, see:
            //https://stackoverflow.com/a/33920320
            return Rhino.RhinoMath.ToDegrees(Math.Atan2(Vector3d.CrossProduct(v1, v2) * plane.ZAxis, v1 * v2)).ToRadians();
        }

        enum State
        {
            PickOrigin = 0,
            PickX = 1,
            PickY = 2,
        }
    }
}
