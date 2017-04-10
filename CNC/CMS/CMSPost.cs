using System;
using System.Collections.Generic;
using System.IO;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.CMSPost
{
    public class CMSPost : GH_Component
    {
        public CMSPost(): base("CMS Router", "CMS Router", "Postprocessor for CMS Athena 5-axis router.", "Axis", "Post Processors")
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{0c68e6c8-94b8-440b-b6da-e685b8634c19}"); }
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconWarning;
            }
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Targets", "Targets", "Target planes for cutting, as lists of targets per cut.", GH_ParamAccess.list);
            pManager.AddTextParameter("Material", "Material", "Material, to be used to set speeds and feeds.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Thickness", "Thickness", "Material thickness for program offset.", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter("Tool", "Tool", "Tool change to include before programmed movement / for inverse kinematics.", GH_ParamAccess.item, 0);
            pManager.AddTextParameter("Path", "Path", "File path for code generation.", GH_ParamAccess.item);
            pManager.AddTextParameter("File", "File", "File name for code generation.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Export", "Export", "Export the file as .cnc to the path specified.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Output GCode for the CMS Router.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Debug", "Debug", "Debug stuff.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Plane> targets = new List<Plane>();
            string material = "MDF";
            int thickness = 100;
            int tool = 0;
            string path = @"C:\";
            string filename = "CMSCode";
            bool export = false;

            if (!DA.GetDataList(0, targets)) return;
            if (!DA.GetData(1, ref material)) return;
            if (!DA.GetData(2, ref thickness)) return;
            if (!DA.GetData(3, ref tool)) ;
            if (!DA.GetData(4, ref path)) ;
            if (!DA.GetData(5, ref filename)) ;
            if (!DA.GetData(6, ref export)) ;

            List<string> gCode = new List<string>();
            List<object> debug = new List<object>();

            // Headers
            gCode.Add("%");
            gCode.Add("O0001");
            gCode.Add("( Generated with Axis Machine Control v0.1 )");
            gCode.Add("( Author: Ryan Hughes )");
            gCode.Add("( Date: " + System.DateTime.Now.ToString() + " )");
            gCode.Add("");

            // Declarations
            gCode.Add("( Warning: Subject Position in Rhino is Z-Negativ.e )");
            gCode.Add("( Program is offset with the material thickness. )");
            gCode.Add("#560 = 55 ( Zeropoint)");
            gCode.Add("( Program offsets assume the use of the standard wooden positioning tools + suction devices. )");
            gCode.Add("#561 = 353.0 ( Offset Program in X )");
            gCode.Add("#562 = 353.0 ( Offset Program in Y )");
            gCode.Add("#563 = 150.0 ( Offset Program in Z )");
            gCode.Add("#564 = " + thickness.ToString() + " ( Subject Thickness )");
            gCode.Add("#565 = -50 ( Z Safety Offset )");
            gCode.Add("#566 = 75 ( Z Safety VED PLANSKIFTE )");
            gCode.Add("#563 = #563 + #564");
            gCode.Add("#569 = 60");
            gCode.Add("");

            // Startup Routine
            gCode.Add("( Startup Routine )");
            gCode.Add("G90 G40 G80 G49 G69");
            gCode.Add("G92.1 X0 Y0 Z0 B0 C0");
            gCode.Add("M25");
            gCode.Add("G0 G53 Z0");
            gCode.Add("G0 B0 C0");
            gCode.Add("G#560");
            gCode.Add("G52 X#561 Y#562 Z#563");
            gCode.Add("");

            /* Tool List
            * ( Tool Number  ; H/D ;  Tool Name )
            * ( 10 ; 13  ; Roughing 16 X 72mm (45mm cut depth) (MDF)) - 0
            * ( 16 ; 45  ; Ball Ø70mm) - 1
            * ( 12 ; 27  ; Finish 12 X 48mm) - 2
            * (  1 ; 55  ; Engrave 25mm 90 degree) - 3
            */

            if (tool == 0)
            {
                gCode.Add("( Tool Change - Get Tool )");
                gCode.Add("( Get Roughing 16mm x 72mm / Tool 0 )");
                gCode.Add("M6 T10");
                gCode.Add("M3 S20000");
                gCode.Add("#567 = #2213 + 135.0");
                gCode.Add("#568 = 0 + SQRT[#567 * #567 + 64] + #566 - 135");
                gCode.Add("G#560");
                gCode.Add("G5.1 Q1");
                gCode.Add("");
            }
            else if (tool == 1)
            {
                gCode.Add("( Tool Change - Get Tool )");
                gCode.Add("( Get Ball Ø70mm / Tool 1 )");
                gCode.Add("M6 T16");
                gCode.Add("M3 S20000");
                gCode.Add("#567 = #2245 + 135.0");
                gCode.Add("#568 = 0 + SQRT[#567 * #567 + 1225] + #566 - 135");
                gCode.Add("G#560");
                gCode.Add("G5.1 Q1");
                gCode.Add("");
            }
            else if (tool == 2)
            {
                gCode.Add("No tool data for Finish bit 12mm x 48mm - operation cancelled.");
                return;
            }
            else if (tool == 3)
            {
                gCode.Add("( Tool Change - Get Tool )");
                gCode.Add("( Get Engrave 25mm 90 degree / Tool 3 )");
                gCode.Add("M6 T1");
                gCode.Add("M3 S18000");
                gCode.Add("#567 = #2255 + 135.0");
                gCode.Add("#568 = 0 + SQRT[#567 * #567 + 625] + #566 - 135");
                gCode.Add("G#560");
                gCode.Add("G5.1 Q1");
                gCode.Add("");
            }

            if (targets.Count > 0)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    /*
                     * Something that needs to be considered here is the directionality of the B and C rotations
                     * on axes 4 and 5 of the router. It is here presumed that it operates the same as the water
                     * jet, but this may not be the case. 
                     */
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
                    double routerBRotation = Math.Atan2(ikPointA.Y, ikPointA.X);

                    Plane BPlane = new Plane(Plane.WorldXY.Origin, Plane.WorldXY.ZAxis, Plane.WorldXY.XAxis);
                    Transform rotateBPlane = Transform.Rotation(routerBRotation, Plane.WorldXY.Origin);
                    BPlane.Transform(rotateBPlane);
                    debug.Add(BPlane);

                    Transform remapBPlaneToWorld = Transform.PlaneToPlane(BPlane, Plane.WorldXY);
                    Point3d ikPointB = new Point3d();
                    Transform moveAlongAZ = Transform.Translation(targetPlane.ZAxis * 5);
                    ikPointB.Transform(moveAlongAZ);
                    debug.Add(ikPointB);
                    ikPointB.Transform(remapBPlaneToWorld);


                    // Note the -1 here as the angle is usually taken as seen from the above plane with robot programming.
                    double routerCRotation = Math.Atan2(ikPointB.Y, ikPointB.X);

                    routerBRotation = routerBRotation * 180 / Math.PI;
                    routerBRotation = Math.Round(routerBRotation, 3);

                    routerCRotation = routerCRotation * 180 / Math.PI;
                    routerCRotation = Math.Round(routerCRotation, 3);

                    double xVal = targets[i].Origin.Y;
                    xVal = Math.Round(xVal, 3);
                    double yVal = (-1 * targets[i].Origin.X);
                    yVal = Math.Round(yVal, 3);

                    if (i < 1)
                    {
                        gCode.Add("G0 X" + xVal.ToString() + " " + "Y" + yVal.ToString() + " " + "A" + routerBRotation.ToString() + " " + "B" + routerCRotation.ToString());
                    }
                    else
                    {
                        gCode.Add("G1 X" + xVal.ToString() + " " + "Y" + yVal.ToString() + " " + "A" + routerBRotation.ToString() + " " + "B" + routerCRotation.ToString());
                    }
                }
            }

            // Footers
            gCode.Add("( Footers )");
            gCode.Add("G49");
            gCode.Add("G69");
            gCode.Add("G0 B0 C0");
            gCode.Add("G0 G53 Z0");
            gCode.Add("G0 G53 X-2600 Y0");
            gCode.Add("M05");
            gCode.Add("G5.1 Q0");

            /*
             * The G codes related to locking and unlocking the B and C axes may need to be set and reset
             * at the beginning and end of each routine - suspected codes are M32 and M34 for locking the axes
             * and M31 and M33 for unlocking them. It seems that they are locked when in 3 axis mode, and free otherwise
             * - if this was not the case, then it would not be possible to call G0 B0 C0 for example.
             * */


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
 