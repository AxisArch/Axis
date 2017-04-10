using System;
using System.Collections.Generic;
using System.IO;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Tools;


namespace Axis.Waterjet
{
    public class WaterjetPost : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconWarning;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{20d3efe8-7f2f-4ae4-8b19-7e0a52ffff53}"); }
        }

        public WaterjetPost() : base("Waterjet", "Waterjet", "Postprocessor for P301 CNC waterjet controller.", "Axis", "Post Processors")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Targets", "Targets", "Target planes for cutting, as lists of targets per cut.", GH_ParamAccess.list);
            pManager.AddTextParameter("Material", "Material", "Material name, as defined in the TecnoCAM template file.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Thickness", "Thickness", "Material thickness [mm].", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "Path", "File path for code generation.", GH_ParamAccess.item);
            pManager.AddTextParameter("File", "File", "File name for code generation.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Export", "Export", "Export the file as .cnc to the path specified.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Output GCode for the waterjet.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Debug", "Debug", "Debug stuff.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> targets = new List<Plane>();
            string material = "Aluminium(6061)";
            int thickness = 100;
            string path = @"C:\";
            string filename = "WaterjetCode";
            bool export = false;

            if (!DA.GetDataList(0, targets)) return;
            if (!DA.GetData(1, ref material)) return;
            if (!DA.GetData(2, ref thickness)) return;
            if (!DA.GetData(3, ref path)) ;
            if (!DA.GetData(4, ref filename)) ;
            if (!DA.GetData(5, ref export)) ;

            List<string> gCode = new List<string>();
            List<object> debug = new List<object>();

            /*
            // Check Workpiece Dimensions
            BoundingBox pieceBbox = new Rhino.Geometry.BoundingBox(targPoints);

            double pieceMaxX = pieceBbox.Max.X;
            double pieceMinX = pieceBbox.Min.X;
            double pieceXDim = (pieceMaxX - pieceMinX);

            double pieceMaxY = pieceBbox.Max.Y;
            double pieceMinY = pieceBbox.Min.Y;
            double pieceYDim = (pieceMaxY - pieceMinY);
            */

            // Headers
            gCode.Add("[ Created using Axis machine control.");
            gCode.Add("[ Ryan Hughes ]");
            gCode.Add("[ Idroline 5-AX waterjet CNC code.");
            gCode.Add("[ Jet diameter (mm): 0.95");

            // Declarations
            gCode.Add("[ Declarations.");
            gCode.Add("[");
            gCode.Add("[*# Material: " + material);
            gCode.Add("P11=" + thickness);
            gCode.Add("M5001");
            gCode.Add("FP23");

            // Programmed Movement
            gCode.Add("[ Programmed Movement.");
            gCode.Add("[");
            gCode.Add("G41");

            /*
            gCode.Add("[ Workpiece X Dimensions. (mm) ]");
            Math.Round(pieceXDim, 3);
            string strWorkpieceXDim = "P2=" + workpieceXDim.ToString();
            gCode.Add(strWorkpieceXDim);
            gCode.Add("[ Workpiece Y Dimensions. (mm) ]");
            Math.Round(pieceYDim, 3);
            string strWorkpieceYDim = "P3=" + workpieceYDim.ToString();
            gCode.Add(strWorkpieceYDim);

            // Test input plate and workpiece sizes for collisions / conflicts.
            if (pieceXDim > plateXDim || pieceYDim > plateYDim)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workpiece is too large for plate. Please review nesting and placement.");
                return;
            }
            
            // Dimensions
            // Plate X
            gCode.Add("[ Plate X Dimensions. (mm) ]");
            Math.Round(plateXDim, 3);
            string strPlateXDim = "P0=" + plateXDim.ToString();
            gCode.Add(strPlateXDim);
            // Plate Y
            gCode.Add("[ Plate Y Dimensions. (mm) ]");
            Math.Round(plateYDim, 3);
            string strPlateYDim = "P1=" + plateYDim.ToString();
            gCode.Add(strPlateYDim);
            
            // Commuter Origins
            // Left
            gCode.Add("[ Origin Left. ]");
            int originSX = 3;
            string strOriginSX = "P4=" + originSX.ToString();
            gCode.Add(strOriginSX);
            // Right
            gCode.Add("[ Origin Right. ]");
            int originDX = 4;
            string strOriginDX = "P5=" + originDX.ToString();
            gCode.Add(strOriginDX);

            // Keyboard
            gCode.Add("[ Keyboard. 0 = No, 1 = Yes ]");
            int keyboard = 1;
            string strKeyboard = "P6=" + keyboard.ToString();
            gCode.Add(strKeyboard);
            // Active Heads
            gCode.Add("[ Active Heads. 1 = Left, 2 = Right, 12 = Both ]");
            int activeHeads = 1;
            string strActiveHeads = "P8=" + activeHeads.ToString();
            gCode.Add(strActiveHeads);
            // Run Mode
            gCode.Add("[ Run Mode. 0 = Single, 1 = Matrix, 2 = Pendular ]");
            int runMode = 0;
            string strRunMode = "P9=" + runMode.ToString();
            gCode.Add(strRunMode);

            gCode.Add("[ ]");

            // Upper Pressure Limit
            gCode.Add("[ Upper Pressure Limit. (MPa) ]");
            int pressureHigh = 370;
            string strPressureHigh = "P10=" + pressureHigh.ToString();
            gCode.Add(strPressureHigh);
            // Lower Pressure Limit
            gCode.Add("[ Lower Pressure Limit. (MPa) ]");
            int pressureLow = 80;
            string strPressureLow = "P20=" + pressureLow.ToString();
            gCode.Add(strPressureLow);

            // Abrasives
            gCode.Add("[ Abrasive Range. (g/min) ]");
            int abrasiveP19 = 400;
            string strAbrasiveP19 = "P19=" + abrasiveP19.ToString();
            gCode.Add(strAbrasiveP19);
            gCode.Add("[ Abrasive Range. (g/min) ]");
            int abrasiveP190 = 400;
            string strAbrasiveP190 = "P190=" + abrasiveP190.ToString();
            gCode.Add(strAbrasiveP190);

            // Plate Thickness
            gCode.Add("[ Plate Thickness. (mm) ]");
            Math.Round(plateThickness, 1);
            string strPlateThickness = "P11=" + plateThickness.ToString();
            gCode.Add(strPlateThickness);

            // Drilling Diameter
            gCode.Add("[ Drilling Diameter. (mm) ]");
            int drillingDiameter = 1;
            string strDrillingDiameter = "P15=" + drillingDiameter.ToString();
            gCode.Add(strDrillingDiameter);

            // Disengage Feeler
            gCode.Add("[ Disengage Z Without Feeler. (mm) ]");
            int disengageZ = 15;
            string strDisengageZ = "P7=" + disengageZ.ToString();
            gCode.Add(strDisengageZ);

            // Disengage for Discharge Pressure
            gCode.Add("[ Disengage for Discharge Pressure. (mm) ]");
            int disengagePressure = 100;
            string strDisengagePressure = "P18=" + disengagePressure.ToString();
            gCode.Add(strDisengagePressure);

            // Maximum Radius of Slowing Speed
            gCode.Add("[ Maximum Radius of Slowing Speed. (mm) ]");
            int slowingRadius = 15;
            string strSlowingRadius = "P94=" + slowingRadius.ToString();
            gCode.Add(strSlowingRadius);

            // Minimum Feed with Radius < Maximum Radius
            gCode.Add("[ Minimum Feed with Radius < Maximum Radius. 0 = Disabled, 1 = Enabled ]");
            int minFeedRadius = 1;
            string strMinFeedRadius = "P95=" + minFeedRadius.ToString();
            gCode.Add(strMinFeedRadius);

            // Sampling Interval
            gCode.Add("[ Sampling Interval. (s) ]");
            int samplingInterval = 0;
            string strSamplingInterval = "P16=" + samplingInterval.ToString();
            gCode.Add(strSamplingInterval);

            // Cycle Repetition Count
            gCode.Add("[ Cycle Repetition Count. ]");
            int cycles = 1;
            string strCycles = "P17=" + cycles.ToString();
            gCode.Add(strCycles);

            gCode.Add("[ ]");

            // Feed Speeds.
            gCode.Add("[ Feed 1. (mm/min) ]");
            int feedOne = 652;
            string strFeedOne = "P21=" + feedOne.ToString();
            gCode.Add(strFeedOne);

            gCode.Add("[ Feed 2. (mm/min) ]");
            int feedTwo = 543;
            string strFeedTwo = "P22=" + feedTwo.ToString();
            gCode.Add(strFeedTwo);

            gCode.Add("[ Feed 3. (mm/min) ]");
            int feedThree = 341;
            string strFeedThree = "P23=" + feedThree.ToString();
            gCode.Add(strFeedThree);

            gCode.Add("[ Feed 4. (mm/min) ]");
            int feedFour = 245;
            string strFeedFour = "P24=" + feedFour.ToString();
            gCode.Add(strFeedFour);

            gCode.Add("[ Feed 5. (mm/min) ]");
            int feedFive = 245;
            string strFeedFive = "P25=" + feedFive.ToString();
            gCode.Add(strFeedFive);

            gCode.Add("[ ]");

            // Drilling Times.
            gCode.Add("[ Drilling Time 1. (s) ]");
            int drillTimeOne = 30;
            string strDrillTimeOne = "P31=" + drillTimeOne.ToString();
            gCode.Add(strDrillTimeOne);

            gCode.Add("[ Drilling Time 2. (s) ]");
            int drillTimeTwo = 30;
            string strDrillTimeTwo = "P32=" + drillTimeTwo.ToString();
            gCode.Add(strDrillTimeTwo);

            gCode.Add("[ Drilling Time 3. (s) ]");
            int drillTimeThree = 1;
            string strDrillTimeThree = "P33=" + drillTimeThree.ToString();
            gCode.Add(strDrillTimeThree);

            gCode.Add("[ Drilling Time 4. (s) ]");
            int drillTimeFour = 30;
            string strDrillTimeFour = "P34=" + drillTimeFour.ToString();
            gCode.Add(strDrillTimeFour);

            gCode.Add("[ Drilling Time 5. (s) ]");
            int drillTimeFive = 30;
            string strDrillTimeFive = "P35=" + drillTimeFive.ToString();
            gCode.Add(strDrillTimeFive);

            gCode.Add("[ ]");

            // Slowing Distances.
            gCode.Add("[ Slowing Distance 1. (mm) ]");
            double slowDistOne = 6.0;
            string strSlowDistOne = "P41=" + slowDistOne.ToString();
            gCode.Add(strSlowDistOne);

            gCode.Add("[ Slowing Distance 2. (mm) ]");
            double slowDistTwo = 5.2;
            string strSlowDistTwo = "P42=" + slowDistTwo.ToString();
            gCode.Add(strSlowDistTwo);

            gCode.Add("[ Slowing Distance 3. (mm) ]");
            double slowDistThree = 3.4;
            string strSlowDistThree = "P43=" + slowDistThree.ToString();
            gCode.Add(strSlowDistThree);

            gCode.Add("[ Slowing Distance 4. (mm) ]");
            double slowDistFour = 3.0;
            string strSlowDistFour = "P44=" + slowDistFour.ToString();
            gCode.Add(strSlowDistFour);

            gCode.Add("[ Slowing Distance 5. (mm) ]");
            double slowDistFive = 3.0;
            string strSlowDistFive = "P45=" + slowDistFive.ToString();
            gCode.Add(strSlowDistFive);

            gCode.Add("[ ]");

            // Minimum Feeds.
            gCode.Add("[ Minimum Feed 1. (%) ]");
            int minFeedOne = 30;
            string strMinFeedOne = "P51=" + minFeedOne.ToString();
            gCode.Add(strMinFeedOne);

            gCode.Add("[ Minimum Feed 2. (%) ]");
            int minFeedTwo = 36;
            string strMinFeedTwo = "P52=" + minFeedTwo.ToString();
            gCode.Add(strMinFeedTwo);

            gCode.Add("[ Minimum Feed 3. (%) ]");
            int minFeedThree = 57;
            string strMinFeedThree = "P53=" + minFeedThree.ToString();
            gCode.Add(strMinFeedThree);

            gCode.Add("[ Minimum Feed 4. (%) ]");
            int minFeedFour = 80;
            string strMinFeedFour = "P54=" + minFeedFour.ToString();
            gCode.Add(strMinFeedFour);

            gCode.Add("[ Minimum Feed 5. (%) ]");
            int minFeedFive = 90;
            string strMinFeedFive = "P55=" + minFeedFive.ToString();
            gCode.Add(strMinFeedFive);

            gCode.Add("[ ]");

            // Jet Diameters.
            gCode.Add("[ Jet Diameter 1. (mm) ]");
            double jetDiameter = 0.95;
            string strJetOne = "P61=" + jetDiameter.ToString();
            gCode.Add(strJetOne);

            gCode.Add("[ Jet Diameter 2. (mm) ]");
            string strJetTwo = "P62=" + jetDiameter.ToString();
            gCode.Add(strJetTwo);

            gCode.Add("[ Jet Diameter 3. (mm) ]");
            string strJetThree = "P63=" + jetDiameter.ToString();
            gCode.Add(strJetThree);

            gCode.Add("[ Jet Diameter 4. (mm) ]");
            string strJetFour = "P64=" + jetDiameter.ToString();
            gCode.Add(strJetFour);

            gCode.Add("[ Jet Diameter 5. (mm) ]");
            string strJetFive = "P65=" + jetDiameter.ToString();
            gCode.Add(strJetFive);

            gCode.Add("[ ]");

            // Initialisation Routine.
            gCode.Add("L@INIZIO.ISS:");
            gCode.Add("[ Return From Cyclical Repitition. ]");
            gCode.Add("L=150");
            gCode.Add("[ Run Booster Matrix. ]");
            gCode.Add("(P9=1) L100");
            gCode.Add("[ Run Pendular Booster. ]");
            gCode.Add("(P9=2) L110");
            gCode.Add("[ Run Singlular Booster. ]");
            gCode.Add("L120");
            gCode.Add("[ Back Executions. ]");
            gCode.Add("L=140");
            gCode.Add("[ Decrement Counter Reps. ]");
            gCode.Add("P17=P17-1");
            gCode.Add("L@FINE.ISS:");
            gCode.Add("[ Skip If Counter Reps Not Exhausted. ]");
            gCode.Add("(P17>0) L150");
            gCode.Add("F5000");
            gCode.Add("&ENDF");
            gCode.Add("[ Fine Execution. ]");
            gCode.Add("M30");

            gCode.Add("[ ]");

            gCode.Add("[ Run Matrix. ]");
            gCode.Add("L=100");
            gCode.Add("P26=INT(P0/P2), P27=INT(P1/P3)");
            gCode.Add("L130");
            gCode.Add("[ Return. ]");
            gCode.Add("(P0=P0) L140");
            gCode.Add("G32");

            gCode.Add("[ ]");

            gCode.Add("[ Run Pendular. ]");
            gCode.Add("L=110");
            gCode.Add("[ Number Of Columns. ]");
            gCode.Add("P26=?");
            gCode.Add("[ Number Of Lines. ]");
            gCode.Add("P27=?");
            gCode.Add("[ Left Origin. ]");
            gCode.Add("TP4");
            gCode.Add("L130");
            gCode.Add("[ Number Of Columns. ]");
            gCode.Add("P26=?");
            gCode.Add("[ Number Of Lines. ]");
            gCode.Add("P27=?");
            gCode.Add("[ Right Origin. ]");
            gCode.Add("TP5");
            gCode.Add("L130");
            gCode.Add("[ Stop With <ESC>.");
            gCode.Add("L110 K999");
            gCode.Add("[ Return. ]");
            gCode.Add("(P0=P0)L140");
            gCode.Add("G32");

            gCode.Add("[ ]");

            gCode.Add("[ Matrix ]");
            gCode.Add("L=130");
            gCode.Add("P0=(P26*P2)+10, P1=(P27*P3)+10, P28=0, P29=0");
            gCode.Add("&X-10IP0Y-10JP1A0K0");
            gCode.Add("L=131");
            gCode.Add("G51 XP28 YP29");
            gCode.Add("L121");
            gCode.Add("P29=P29+P3");
            gCode.Add("L131KP27");
            gCode.Add("P28=P28+P2, P29=0");
            gCode.Add("L131KP26");
            gCode.Add("G32");

            gCode.Add("[ ]");

            gCode.Add("L=120");
            gCode.Add("&GRAPH-INIT");
            gCode.Add("&GRAPH-VIEW K0 A0");
            gCode.Add("&GRAPH-AREA H375.1 X15 Y15");
            gCode.Add("&GRAPH-SET");
            gCode.Add("L=121");
            gCode.Add("G16 XY");

            gCode.Add("[ ]");

            gCode.Add("[ Cutting Path. ]");
            gCode.Add("[ ]");

            */

            if (targets.Count > 0)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Plane ikPlane = new Plane(targets[i].Origin, Plane.WorldXY.XAxis, Plane.WorldXY.YAxis);
                    Plane targetPlane = new Plane(targets[i].Origin, targets[i].XAxis, targets[i].YAxis);
                    Transform remapToWorld = Transform.PlaneToPlane(ikPlane, Plane.WorldXY);
                    targetPlane.Transform(remapToWorld);
                    debug.Add(targetPlane);

                    Transform moveAlongAX = Transform.Translation(targetPlane.XAxis * 5);
                    Point3d ikPointA = new Point3d();
                    ikPointA.Transform(moveAlongAX);
                    debug.Add(ikPointA);

                    // Note the -1 here as the angle is usually taken as seen from the above plane with robot programming.
                    double jetARotation = Math.Atan2(ikPointA.Y, ikPointA.X);

                    Plane BPlane = new Plane(Plane.WorldXY.Origin, Plane.WorldXY.ZAxis, Plane.WorldXY.XAxis);
                    Transform rotateBPlane = Transform.Rotation(jetARotation, Plane.WorldXY.Origin);
                    BPlane.Transform(rotateBPlane);
                    debug.Add(BPlane);

                    Transform remapBPlaneToWorld = Transform.PlaneToPlane(BPlane, Plane.WorldXY);
                    Point3d ikPointB = new Point3d();
                    Transform moveAlongAZ = Transform.Translation(targetPlane.ZAxis * 5);
                    ikPointB.Transform(moveAlongAZ);
                    debug.Add(ikPointB);
                    ikPointB.Transform(remapBPlaneToWorld);


                    // Note the -1 here as the angle is usually taken as seen from the above plane with robot programming.
                    double jetBRotation = Math.Atan2(ikPointB.Y, ikPointB.X);

                    jetARotation = jetARotation * 180 / Math.PI;
                    jetARotation = Math.Round(jetARotation, 3);

                    jetBRotation = jetBRotation * 180 / Math.PI;
                    jetBRotation = Math.Round(jetBRotation, 3);

                    double xVal = targets[i].Origin.Y;
                    xVal = Math.Round(xVal, 3);
                    double yVal = (-1 * targets[i].Origin.X);
                    yVal = Math.Round(yVal, 3);

                    if (i < 1)
                    {
                        gCode.Add("G0 X" + xVal.ToString() + " " + "Y" + yVal.ToString() + " " + "A" + jetARotation.ToString() + " " + "B" + jetBRotation.ToString());
                    }
                    else
                    {
                        gCode.Add("G1 X" + xVal.ToString() + " " + "Y" + yVal.ToString() + " " + "A" + jetARotation.ToString() + " " + "B" + jetBRotation.ToString());
                    }

                    
                }
            }

            // Footers
            gCode.Add("[ Footers.");
            gCode.Add("[");
            gCode.Add("G40");

            if (export)
            {
                using (StreamWriter writer = new StreamWriter(path + @"\" + filename + ".cnc", false))
                {
                    for (int i = 0; i < gCode.Count; i++)
                    {
                        writer.WriteLine(gCode[i]);
                    }
                }
            }

            DA.SetDataList(0, gCode);
            //DA.SetDataList(1, debug);
        }
    }
}