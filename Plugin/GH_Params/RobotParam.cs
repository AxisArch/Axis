using Axis.Kernal;
using Axis.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Axis.GH_Params
{
    public class RobotParam : GH_PersistentParam<IGH_Goo>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public RobotParam()
          : base("Robot", "Robot", "Axis robot type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("17C49BD4-7A54-4471-961A-B5E0E971F7F4");

        protected override IGH_Goo InstantiateT()
        {
            return new Abb6DOFRobot();
        }

        protected override GH_GetterResult Prompt_Singular(ref IGH_Goo value)
        {
            var bPlane = Plane.WorldXY;

        MainMenu:
            var go = new Rhino.Input.Custom.GetString();
            go.SetCommandPrompt("Set default robot.");
            go.AcceptNothing(true);
            go.AddOption("Default");
            go.AddOption("IRB_120");
            go.AddOption("IRB_6620");
            go.AddOption("IRB_6700_150");
            go.AddOption("SetBasePlane", $"O({bPlane.OriginX.ToString("0.00")},{bPlane.OriginY.ToString("0.00")},{bPlane.OriginZ.ToString("0.00")}) " +
                $"Z({bPlane.ZAxis.X.ToString("0.00")},{bPlane.ZAxis.Y.ToString("0.00")}, {bPlane.ZAxis.Z.ToString("0.00")})");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "Default") { var rob = Abb6DOFRobot.Default; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "IRB_120") { var rob = Abb6DOFRobot.IRB120; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "IRB_6620") { var rob = Abb6DOFRobot.IRB6620; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "IRB_6700_150") { var rob = Abb6DOFRobot.IRB6700_150kg_320m; rob.ChangeBasePlane(bPlane); value = rob; }
                    if (go.Option().EnglishName == "SetBasePlane") { GetBPlane(out bPlane); goto MainMenu; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.cancel;

                default:
                    return GH_GetterResult.cancel;
            }
        }

        protected override GH_GetterResult Prompt_Plural(ref List<IGH_Goo> values)
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
            PersistentData.Append(Abb6DOFRobot.Default, new GH_Path(0));
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
            plane = PlaneOrientXAxis(plane, gx.Point());

            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetConstructionPlane(new Plane(plane.Origin, plane.YAxis, plane.ZAxis));
            MoveCPlanePoint gy = new MoveCPlanePoint(plane, State.PickY);
            gy.SetCommandPrompt("Set Point in Y Direction");
            gy.SetBasePoint(plane.Origin, true);
            gy.DrawLineFromPoint(plane.Origin, true);
            gy.Get();
            if (gy.CommandResult() != Rhino.Commands.Result.Success) return reset(gy.CommandResult());
            plane = PlaneXAxisRotation(plane, gy.Point());

            return reset(Rhino.Commands.Result.Success);
        }

        private class MoveCPlanePoint : Rhino.Input.Custom.GetPoint
        {
            private readonly Rhino.DocObjects.ConstructionPlane cPlane;
            private Plane bPlane;

            private State m_state;

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
                        cPlane.Plane = PlaneOrientXAxis(bPlane, e.Point);
                        break;

                    case State.PickY:
                        cPlane.Plane = PlaneXAxisRotation(bPlane, e.Point);
                        break;
                }
            }

            protected override void OnDynamicDraw(Rhino.Input.Custom.GetPointDrawEventArgs e)
            {
                e.Display.DrawConstructionPlane(cPlane);
            }
        }

        private static Plane PlaneOrientXAxis(Plane plane, Point3d point)
        {
            Plane rot1 = plane.Clone();
            var vec = new Vector3d(point - plane.Origin);

            var angle1 = Vector3d.VectorAngle(rot1.XAxis, vec, rot1);
            rot1.Rotate(angle1, rot1.ZAxis, rot1.Origin);

            Plane rot2 = rot1.Clone();
            if (point.Z != plane.OriginZ)
            {
                var angle2 = Vector3d.VectorAngle(rot2.XAxis, vec);
                rot2.Rotate(angle2, rot2.YAxis, rot2.Origin);
            }
            return rot2;
        }

        private static Plane PlaneXAxisRotation(Plane plane, Point3d point)
        {
            var vec = new Vector3d(point - plane.Origin);
            var angle1 = Vector3d.VectorAngle(plane.YAxis, vec, new Plane(plane.Origin, plane.YAxis, plane.ZAxis));
            plane.Rotate(angle1, plane.XAxis, plane.Origin);
            return plane;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Icons.RobotParam;

        protected override void OnVolatileDataCollected()
        {
            for (int p = 0; p < m_data.PathCount; p++)
            {
                List<IGH_Goo> branch = m_data.Branches[p];
                for (int i = 0; i < branch.Count; i++)
                {
                    IGH_Goo goo = branch[i];

                    //We accept existing nulls.
                    if (goo == null) continue;

                    //We accept colours.
                    if (goo is Robot) continue;

                    //Tough luck, the data is beyond repair. We'll set a runtime error and insert a null.
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                      string.Format("Data of type {0} could not be converted into a Robot", goo.TypeName));
                    branch[i] = null;

                    //As a side-note, we are not using the CastTo methods here on goo. If goo is of some unknown 3rd party type
                    //which knows how to convert itself into a curve then this parameter will not work with that.
                    //If you want to know how to do this, ask.
                }
            }
        }

        private enum State
        {
            PickOrigin = 0,
            PickX = 1,
            PickY = 2,
        }
    }
}