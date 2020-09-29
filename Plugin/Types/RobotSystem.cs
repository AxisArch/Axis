using Axis;
using Axis.Kernal;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Types
{
    /// ABB

    /// <summary>
    /// Create custom industrial robot configurations.
    /// </summary>
    public sealed class Abb6DOFRobot : Robot
    {
        public override Manufacturer Manufacturer => Manufacturer.ABB;

        #region Propperties

        public override Plane Flange { get => this.AxisPlanes[5].Clone(); }
        public double WristOffsetLength { get => this.AxisPlanes[5].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double LowerArmLength { get => this.AxisPlanes[1].Origin.DistanceTo(this.AxisPlanes[2].Origin); }
        public double UpperArmLength { get => this.AxisPlanes[2].Origin.DistanceTo(this.AxisPlanes[4].Origin); }
        public double AxisFourOffsetAngle { get => Math.Atan2(this.AxisPlanes[4].Origin.Z - this.AxisPlanes[2].Origin.Z, this.AxisPlanes[4].Origin.X - this.AxisPlanes[2].Origin.X); }  // =>0.22177 for IRB 6620 //This currently limitting the robot to be in a XZ configuration

        #endregion Propperties

        #region Constructors

        public Abb6DOFRobot()
        {
        }

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
        public Abb6DOFRobot(Plane[] axisPlanes, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
        {
            this.AxisPlanes = axisPlanes.ToList();

            this.MinAngles = minAngles;
            this.MaxAngles = maxAngles;

            this.RobMeshes = robMeshes;
            this.RobBasePlane = AxisPlanes[0];

            this.Indices = indices;

            ChangeBasePlane(basePlane);

            LineModle = LineDrawing();
        }

        /// <summary>
        /// Built-in default robot - ABB IRB 120.
        /// </summary>
        public static Robot Default { get => IRB120; }


        #region Static Robot Library

        /*
         * @todo Reduce library
         * @body Loading all the static classes slows down the plugin. Or make the loading process parallel
         */

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

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
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

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
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

        public static Robot IRB140_6kg_081m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB140_6kg_081mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(70.000, 0.000, 352.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(70.000, 0.000, 712.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(288.500, 0.000, 712.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(450.000, 0.000, 712.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(515.000, 0.000, 712.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -90, -230, -200, -115, -400 };
                List<double> maxAngles = new List<double> { 180, 110, 50, 200, 115, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 140 (6kg-0.81m)";
                return robot;
            }
        }

        public static Robot IRB1520ID_4kg_150m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB1520ID_4kg_150mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 29.000, -29.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(160.000, -72.000, 424.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(160.000, -92.000, 1014.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(377.000, 29.676, 1214.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(883.000, 81.500, 1214.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1083.000, 29.000, 1214.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -90, -100, -155, -135, -200 };
                List<double> maxAngles = new List<double> { 170, 150, 80, 155, 135, 200 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 1520ID (4kg-1.50m)";
                return robot;
            }
        }

        public static Robot IRB1600_6kg_120m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB1600_6kg_120mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -139.500, 486.500), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -111.500, 961.500), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(464.000, -4.500, 961.500), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(750.000, -28.607, 961.500), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(821.969, -4.107, 961.500), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -63, -235, -200, -115, -400 };
                List<double> maxAngles = new List<double> { 180, 136, 55, 200, 115, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 1600 (6kg-1.20m)";
                return robot;
            }
        }

        public static Robot IRB1660ID_4kg_155m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB1660ID_4kg_155mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -138.000, 486.500), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -110.000, 1186.500), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(281.000, 0.000, 1296.500), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(828.000, 70.000, 1296.500), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(963.000, 0.000, 1296.500), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -90, -238, -175, -120, -400 };
                List<double> maxAngles = new List<double> { 180, 150, 79, 175, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 1660ID (4kg-1.55m)";
                return robot;
            }
        }

        public static Robot IRB2600_12kg_185m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB2600_12kg_185mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.008, 0.004, 10.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -140.400, 445.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -145.000, 1345.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(437.500, 0.000, 1460.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(945.000, -20.500, 1460.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1030.000, 0.000, 1460.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -95, -180, -400, -120, -400 };
                List<double> maxAngles = new List<double> { 180, 155, 75, 400, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 2600 (12kg-1.85m)";
                return robot;
            }
        }

        public static Robot IRB2600_12kg_20kg_165m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB2600_12kg_20kg_165mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -140.400, 445.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -145.000, 1145.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(437.500, 0.000, 1260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(945.000, -20.500, 1260.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1030.000, 0.000, 1260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -95, -180, -400, -120, -400 };
                List<double> maxAngles = new List<double> { 180, 155, 75, 400, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 2600 (12kg/20kg-1.65m)";
                return robot;
            }
        }

        public static Robot IRB2600ID_15kg_185m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB2600ID_15kg_185mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.008, 0.004, 10.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -140.400, 445.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -145.000, 1345.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(388.000, 0.000, 1495.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(936.000, 80.000, 1495.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1071.000, 0.000, 1495.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -95, -180, -175, -120, -400 };
                List<double> maxAngles = new List<double> { 180, 155, 75, 175, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 2600ID (15kg-1.85m)";
                return robot;
            }
        }

        public static Robot IRB2600ID_8kg_200m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB2600ID_8kg_200mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.008, 0.004, 10.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(150.000, -140.400, 445.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(150.000, -145.000, 1345.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(388.000, 0.000, 1495.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1088.000, 70.000, 1495.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1288.000, 0.000, 1495.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -95, -180, -175, -120, -400 };
                List<double> maxAngles = new List<double> { 180, 155, 75, 175, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 2600ID (8kg-2.00m)";
                return robot;
            }
        }

        public static Robot IRB4600_20kg_250m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB4600_20kg_250mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(175.000, -122.500, 495.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(175.000, -146.200, 1590.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(515.100, 0.000, 1765.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1405.500, -20.500, 1765.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1490.500, 0.000, 1765.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -90, -180, -400, -125, -400 };
                List<double> maxAngles = new List<double> { 180, 150, 75, 400, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 4600 (20kg-2.50m)";
                return robot;
            }
        }

        public static Robot IRB4600_45kg_60kg_205m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB4600_45kg_60kg_205mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(175.000, -122.500, 495.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(175.000, -146.200, 1395.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(515.100, 0.000, 1570.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1135.000, -67.000, 1570.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1270.000, 0.000, 1570.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -90, -180, -400, -125, -400 };
                List<double> maxAngles = new List<double> { 180, 150, 75, 400, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 4600 (45kg/60kg-2.05m)";
                return robot;
            }
        }

        public static Robot IRB6650S_100kg_350m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6650S_100kg_350mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(600.000, -27.000, 630.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(600.000, -272.000, 1910.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(830.465, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(2192.500, -93.000, 2110.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2541.500, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -40, -180, -300, -120, -360 };
                List<double> maxAngles = new List<double> { 180, 160, 70, 300, 120, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6650S (100kg-3.50m)";
                return robot;
            }
        }

        public static Robot IRB6650S_125kg_350m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6650S_125kg_350mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(600.000, -27.000, 630.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(600.000, -272.000, 1910.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(830.465, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(2192.500, -93.000, 2110.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2392.000, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -40, -180, -300, -120, -360 };
                List<double> maxAngles = new List<double> { 180, 160, 70, 300, 120, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6650S (125kg-3.50m)";
                return robot;
            }
        }

        public static Robot IRB6650S_190kg_300m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6650S_190kg_300mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(600.000, -27.000, 630.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(600.000, -272.000, 1910.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(830.465, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1742.500, -93.000, 2110.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2091.500, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -40, -180, -300, -120, -360 };
                List<double> maxAngles = new List<double> { 180, 160, 70, 300, 120, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6650S (190kg-3.00m)";
                return robot;
            }
        }

        public static Robot IRB6650S_200kg_300m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6650S_200kg_300mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(600.000, -27.000, 630.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(600.000, -272.000, 1910.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(830.465, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1742.500, -93.000, 2110.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1942.000, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -40, -180, -300, -120, -360 };
                List<double> maxAngles = new List<double> { 180, 160, 70, 300, 120, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6650S (200kg-3.00m)";
                return robot;
            }
        }

        public static Robot IRB6650S_90kg_390m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6650S_90kg_390mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(600.000, -27.000, 630.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(600.000, -281.500, 1910.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(830.465, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(2642.000, -93.000, 2110.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2842.000, 0.000, 2110.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -40, -180, -300, -120, -360 };
                List<double> maxAngles = new List<double> { 180, 160, 70, 300, 120, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6650S (90kg-3.90m)";
                return robot;
            }
        }

        public static Robot IRB6700_150kg_320m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6700_150kg_320mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -275.000, 2060.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1912.500, -100.000, 2260.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2112.500, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 170, 85, 70, 300, 130, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6700 (150kg-3.20m)";
                return robot;
            }
        }

        public static Robot IRB6700_155kg_285m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6700_155kg_285mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -271.000, 1905.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2105.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1712.500, -92.000, 2105.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1912.500, 0.000, 2105.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 170, 85, 70, 300, 130, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6700 (155kg-2.85m)";
                return robot;
            }
        }

        public static Robot IRB6700_175kg_305m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6700_175kg_305mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -271.000, 1905.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2105.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1912.500, -100.000, 2115.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(2112.500, 0.000, 2115.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 170, 85, 70, 300, 130, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6700 (175kg-3.05m)";
                return robot;
            }
        }

        public static Robot IRB6700_200kg_260m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6700_200kg_260mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -271.000, 1905.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2105.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1462.500, -100.000, 2105.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1662.500, 0.000, 2105.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 170, 85, 70, 300, 130, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6700 (200kg-2.60m)";
                return robot;
            }
        }

        public static Robot IRB6700_205kg_280m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6700_205kg_280mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -275.000, 2060.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1502.500, -100.000, 2260.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1702.500, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -65, -180, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 170, 85, 70, 300, 130, 360 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6700 (205kg-2.80m)";
                return robot;
            }
        }

        public static Robot IRB6790_205kg_280m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6790_205kg_280mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -275.000, 2060.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1502.500, -100.000, 2260.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1702.500, 0.000, 2260.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -85, -70, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 1, 2, 3, 4, 5, 6 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6790 (205kg-2.80m)";
                return robot;
            }
        }

        public static Robot IRB6790_235kg_265m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB6790_235kg_265mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(320.000, -133.500, 780.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(320.000, -275.000, 1915.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(553.687, 0.000, 2115.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1502.500, -100.000, 2115.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1702.500, 0.000, 2115.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -170, -85, -70, -300, -130, -360 };
                List<double> maxAngles = new List<double> { 1, 2, 3, 4, 5, 6 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 6790 (235kg-2.65m)";
                return robot;
            }
        }

        public static Robot IRB4600_40kg_255m
        {
            get
            {
                // Deserialize the list of robot meshes
                List<Mesh> robMeshes;
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter deserializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Axis.Resources.RobotMehes.IRB4600_40kg_255mmesh))
                {
                    System.Runtime.Serialization.IFormatter br = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    robMeshes = (List<Mesh>)br.Deserialize(ms);
                }

                Plane[] axisPlanes = new Plane[6] {
               new Plane(new Point3d(0.000, 0.000, 0.000), new Vector3d(1,0,0) , new Vector3d(0,1,0)),
               new Plane(new Point3d(175.000, -122.500, 495.000), new Vector3d(0,0,1) , new Vector3d(1,0,0)),
               new Plane(new Point3d(175.000, -146.200, 1590.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(515.100, 0.000, 1765.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
               new Plane(new Point3d(1445.000, -67.000, 1765.000), new Vector3d(1,0,0) , new Vector3d(0,0,-1)),
               new Plane(new Point3d(1580.000, 0.000, 1765.000), new Vector3d(0,0,-1) , new Vector3d(0,1,0)),
          };
                List<double> minAngles = new List<double> { -180, -90, -180, -400, -125, -400 };
                List<double> maxAngles = new List<double> { 180, 150, 75, 400, 120, 400 };
                Plane basePlane = Plane.WorldXY;
                List<int> indices = new List<int> { 0, 0, 0, 0, 0, 0, };

                var robot = new Abb6DOFRobot(axisPlanes, minAngles, maxAngles, robMeshes, basePlane, indices);
                robot.Name = "IRB 4600 (40kg-2.55m)";
                return robot;
            }
        }


        #endregion Static Robot Library

        #endregion Constructors

        #region Methods

        public override Pose GetPose()
        {
            var pose = new Abb6DOFRobot.ABB6DOFPose(this);
            return pose as Pose;
        }

        public override Pose GetPose(Target target)
        {
            var pose = new Abb6DOFRobot.ABB6DOFPose(this, target as ABBTarget);
            return pose as Pose;
        }

        #endregion Methods

        #region Interfaces

        public override IGH_Goo Duplicate()
        {
            Robot robot = new Abb6DOFRobot(this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            robot.ReferenceID = this.ReferenceID;
            return robot;
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            var robot = new Abb6DOFRobot(this.AxisPlanes.ToArray(), this.MinAngles, this.MaxAngles, this.RobMeshes.Select(m => m.DuplicateMesh()).ToList(), this.RobBasePlane.Clone(), this.Indices);
            if (this.Name != string.Empty) robot.Name = this.Name;
            return robot;
        }

        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }

        #endregion Interfaces

        private sealed class ABB6DOFPose : Robot.Pose
        {
            #region Variables

            public Abb6DOFRobot robot;

            #endregion Variables

            #region Propperties

            public override Robot Robot => robot as Robot;

            public override Plane Flange
            {
                get
                {
                    Plane flange = this.robot.AxisPlanes[5].Clone();
                    flange.Transform(Forward[5]);
                    return flange;
                }
            }

            #endregion Propperties

            #region Constructors

            public ABB6DOFPose(Abb6DOFRobot robot)
            {
                this.robot = robot;
                this.SetPose(Axis.Types.ABBTarget.Default);
            }

            public ABB6DOFPose(Abb6DOFRobot robot, Axis.Types.ABBTarget target)
            {
                this.robot = robot;
                this.SetPose(target);
            }

            #endregion Constructors

            #region Methods

            //Public
            public override void SetPose(Target target)
            {
                this.target = target;

                double[] radAngles = new double[6];
                double[] degAngles = new double[6];
                bool outOfReach = true; bool outOfRoation = true; bool wristSing = true; bool overHeadSig = true;
                JointState[] jointStates = new JointState[6];

                //Invers and farward kinematics devided by manufacturar

                switch (target.Method)
                {
                    case MotionType.Linear:
                    case MotionType.Joint:

                        // Compute the flane position by moving the TCP to the base of the tool
                        Plane flange = target.Plane.Clone();
                        Rhino.Geometry.Transform t1 = Rhino.Geometry.Transform.PlaneToPlane(flange, Plane.WorldXY);
                        Rhino.Geometry.Transform t2; t1.TryGetInverse(out t2);
                        flange.Transform(t2 * target.Tool.FlangeOffset * t1);

                        //double[,] anglesSet = newTargetInverseKinematics(target, out overHeadSig, out outOfReach);
                        List<List<double>> anglesSet = TargetInverseKinematics(flange, out overHeadSig, out outOfReach);

                        // Select Solution based on Indecies
                        //for (int i = 0; i < anglesSet.Length / anglesSet.GetLength(1); ++i) degAngles[i] = anglesSet[i, this.Robot.Indices[i]];
                        radAngles = anglesSet.Zip<List<double>, int, double>(this.robot.Indices, (solutions, selIdx) => solutions[selIdx]).ToArray();
                        degAngles = radAngles.Select(a => a.ToDegrees()).ToArray();

                        jointStates = CheckJointAngles(degAngles, this.robot, checkSingularity: true);

                        break;

                    case MotionType.AbsoluteJoint:
                        degAngles = target.JointAngles.ToArray();
                        radAngles = degAngles.Select(a => a.ToRadians()).ToArray();

                        jointStates = CheckJointAngles(degAngles, this.robot);

                        break;

                    default:
                        throw new NotImplementedException($"This movment: {target.Method.ToString()} has not jet been implemented for {this.robot.Manufacturer.ToString()}"); ;
                }

                //SetSignals(jointStates, out outOfReach, out outOfRoation, out wristSing, out overHeadSig);
                this.jointStates = jointStates;

                this.radAngles = radAngles;

                this.Forward = ForwardKinematics(radAngles, this.robot);
            }

            //Private
            /// <summary>
            /// Closed form inverse kinematics for a 6 DOF industrial robot. Returns flags for validity and error types.
            /// </summary>
            /// <param name="target"></param>
            /// <param name="overheadSing"></param>
            /// <param name="outOfReach"></param>
            /// <returns></returns>
            protected override List<List<double>> TargetInverseKinematics(Plane target, out bool overheadSing, out bool outOfReach, double singularityTol = 5)
            {
                //////////////////////////////////////////////////////////////
                //// Setup of the vartiables needed for Iverse Kinematics ////
                //////////////////////////////////////////////////////////////

                //Setting up Planes
                Plane P0 = this.robot.AxisPlanes[0].Clone();
                Plane P1 = this.robot.AxisPlanes[1].Clone();
                Plane P2 = this.robot.AxisPlanes[2].Clone();
                Plane P3 = this.robot.AxisPlanes[3].Clone();
                Plane P4 = this.robot.AxisPlanes[4].Clone();
                Plane P5 = this.robot.AxisPlanes[5].Clone();
                Plane flange = target.Clone();

                // Setting Up Lengths
                double wristOffsetLength = this.robot.WristOffsetLength;
                double lowerArmLength = this.robot.LowerArmLength;
                double upperArmLength = this.robot.UpperArmLength;
                double axisFourOffsetAngle = this.robot.AxisFourOffsetAngle;

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

            #endregion Methods

            #region Interfaces

            public override IGH_Goo Duplicate()
            {
                return new ABB6DOFPose(this.robot, this.target as ABBTarget);
            }

            #endregion Interfaces
        }
    }

    /// <summary>
    /// Class represeing an endefector for a robotic manipulator
    /// </summary>
    public sealed class ABBTool : Tool
    {
        #region Propperties

        public override Manufacturer Manufacturer { get; } = Manufacturer.ABB;

        public Vector3d RelTool { get; private set; }
        public double Weight { get; private set; }

        public override string Declaration
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

        #endregion Propperties

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ABBTool() { }

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="TCP"></param>
        /// <param name="weight"></param>
        /// <param name="mesh"></param>
        /// <param name="type"></param>
        /// <param name="relToolOffset"></param>
        public ABBTool(string name, Plane TCP, double weight, List<Mesh> mesh, Vector3d relToolOffset)
        {
            this.Name = name;
            this.TCP = TCP;
            this.Weight = weight;
            this.Geometries = (mesh != null) ? mesh.ToArray() : new Mesh[0];
            this.RelTool = relToolOffset;
        }

        /// <summary>
        /// Default tool.
        /// </summary>
        public static Tool Default { get => new ABBTool("DefaultTool", Plane.WorldXY, 1.5, null, Vector3d.Zero); }

        #endregion Constructors

        #region Interfaces

        public override void ClearCaches()
        {
            base.ClearCaches();
            this.Weight = double.NaN;
            this.RelTool = Vector3d.Unset;
        }

        public override IGH_Goo Duplicate()
        {
            return new ABBTool(this.Name, this.TCP, this.Weight, this.Geometries.Select(m => (Mesh)m.Duplicate()).ToList(), this.RelTool);
        }

        public override bool Read(GH_IReader reader)
        {
            if (reader.ChunkExists("FlangeOffset"))
            {
                var chunk3 = reader.FindChunk("RelTool");
                if (chunk3 != null)
                {
                    var data = new GH_Vector();
                    var vec = new Vector3d();
                    data.Read(chunk3);
                    GH_Convert.ToVector3d(data, ref vec, GH_Conversion.Both);
                    this.RelTool = vec;
                }
            }

            if (reader.ItemExists("Weight")) this.Weight = reader.GetDouble("Weight");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDouble("Weight", this.Weight);
            GH_Vector gH_relTool = new GH_Vector(this.RelTool);
            gH_relTool.Write(writer.CreateChunk("RelTool"));
            return base.Write(writer);
        }

        #endregion Interfaces
    }

   
    
    /// KUKA

    public sealed class Kuka6DOFRobot : Robot
    {
        public override Manufacturer Manufacturer => Manufacturer.Kuka;

        public override Plane Flange => throw new NotImplementedException();

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            throw new NotImplementedException();
        }

        #region Methods

        public override Pose GetPose() => new Kuka6DOFPose(ABBTarget.Default);

        public override Pose GetPose(Target target) => new Kuka6DOFPose(target);

        #endregion Methods

        private sealed class Kuka6DOFPose : Robot.Pose
        {
            #region Constructors

            public Kuka6DOFPose(Target target)
            {
            }

            public override Robot Robot => throw new NotImplementedException();

            public override Plane Flange => throw new NotImplementedException();

            #endregion Constructors

            public override IGH_Goo Duplicate()
            {
                throw new NotImplementedException();
            }

            public override void SetPose(Kernal.Target target)
            {
                this.target = target;

                double[] radAngles = new double[6];
                double[] degAngles = new double[6];
                //bool outOfReach = true; bool outOfRoation = true; bool wristSing = true; bool overHeadSig = true;
                JointState[] jointStates = new JointState[6];

                switch (target.Method)
                {
                    case MotionType.AbsoluteJoint:

                        degAngles = target.JointAngles.ToArray();
                        jointStates = CheckJointAngles(degAngles, this.Robot);

                        radAngles = degAngles.Select(value => value.ToRadians()).ToArray();
                        break;

                    default:
                        throw new NotImplementedException($"This movment: {target.Method.ToString()} has not jet been implemented for {this.Robot.Manufacturer.ToString()}");
                }

                //SetSignals(jointStates, out outOfReach, out outOfRoation, out wristSing, out overHeadSig);
                this.jointStates = jointStates;

                this.radAngles = radAngles;

                this.Forward = ForwardKinematics(radAngles, this.Robot);
            }

            protected override List<List<double>> TargetInverseKinematics(Plane target, out bool overheadSing, out bool outOfReach, double singularityTol = 5)
            {
                throw new NotImplementedException();
            }
        }
    }
}