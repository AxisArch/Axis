using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Axis.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class UnitTestABB
    {
        /// <summary>
        /// Xunit Test to creat a linear target
        /// </summary>
        [Fact]
        public void RAPIDCodeMoveL()
        {
            string linearTargetABBExpexted = "MoveL [[0,0,0],[1,0,0,0],cData,[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0 \\WObj:=wobj0;";
            //string linearTargetABBExpexted = "MoveL [[0,0,0],[1,0,0,0],[0,0,0,0],[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0 \\WObj:=wobj0;";

            Axis.Targets.Speed speed;
            Axis.Util.ABBSpeeds().TryGetValue(5, out speed);
            Axis.Targets.Zone zone;
            Axis.Util.ABBZones().TryGetValue(0, out zone);

            Axis.Targets.Target linearTarget = new Axis.Targets.Target(
                Plane.WorldXY,
                Axis.Targets.MotionType.Linear,
                speed,
                zone,
                Axis.Core.Tool.Default,
                new Axis.Targets.CSystem("wobj0", Plane.WorldXY, false, Plane.WorldXY),
                0, //Ext Rot
                0, //Ext Lin
                Axis.Core.Manufacturer.ABB);

            Assert.Equal(linearTargetABBExpexted, linearTarget.StrRob);
        }

        [Fact]
        public void RAPIDCodeMoveJ()
        {
            string jointTargetABBExpexted = "MoveJ [[0,0,0],[1,0,0,0],cData,[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0 \\WObj:=wobj0;";
            //string linearTargetABBExpexted = "MoveJ [[0,0,0],[1,0,0,0],[0,0,0,0],[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0 \\WObj:=wobj0;";

            Axis.Targets.Speed speed;
            Axis.Util.ABBSpeeds().TryGetValue(5, out speed);
            Axis.Targets.Zone zone;
            Axis.Util.ABBZones().TryGetValue(0, out zone);

            Axis.Targets.Target joinTarget = new Axis.Targets.Target(
                Plane.WorldXY,
                Axis.Targets.MotionType.Joint,
                speed,
                zone,
                Axis.Core.Tool.Default,
                new Axis.Targets.CSystem("wobj0", Plane.WorldXY, false, Plane.WorldXY),
                0, //Ext Rot
                0, //Ext Lin
                Axis.Core.Manufacturer.ABB);
            Assert.Equal(jointTargetABBExpexted, joinTarget.StrRob);
        }

        [Fact]
        public void RAPIDCodeMoveAbsJ()
        {
            string absJointTargetABBExpexted = "MoveAbsJ [[0,0,0,0,0,0],[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0;";
            //string linearTargetABBExpexted = "MoveAbsJ [[0,0,0,0,0,0],[0,0,9E9,9E9,9E9,9E9]], v5, z0, tool0;";

            Axis.Targets.Speed speed;
            Axis.Util.ABBSpeeds().TryGetValue(5, out speed);
            Axis.Targets.Zone zone;
            Axis.Util.ABBZones().TryGetValue(0, out zone);

            Axis.Targets.Target absJointTarget = new Axis.Targets.Target(
                new List<double>() { 0, 0, 0, 0, 0, 0 },
                speed,
                zone,
                Axis.Core.Tool.Default,
                0,
                0,
                Core.Manufacturer.ABB);
            Assert.Equal(absJointTargetABBExpexted, absJointTarget.StrRob);
        }
    }

    #region Example Code

    [Collection("Rhino Collection")]
    public class XunitExampleTests
    {
        /// <summary>
        /// Xunit Test to Transform a brep using a translation
        /// </summary>
        [Fact]
        public void Brep_Translation()
        {
            // Arrange
            var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(100, 100, 100));
            var brep = bb.ToBrep();
            var t = Transform.Translation(new Vector3d(30, 40, 50));

            // Act
            brep.Transform(t);

            // Assert
            Assert.Equal(brep.GetBoundingBox(true).Center, new Point3d(80, 90, 100));
        }

        /// <summary>
        /// Xunit Test to Intersect sphere with a plane to generate a circle
        /// </summary>
        [Fact]
        public void Brep_Intersection()
        {
            // Arrange
            var radius = 4.0;
            var brep = Brep.CreateFromSphere(new Sphere(new Point3d(), radius));
            var cuttingPlane = Plane.WorldXY;

            // Act
            Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, cuttingPlane, 0.001, out var curves, out var points);

            // Assert
            Assert.Single(curves);
            Assert.Equal(2 * Math.PI * radius, curves[0].GetLength());
        }

        /// <summary>
        /// Xunit Test to ensure Centroid of GH_Box outputs a GH_Point
        /// </summary>
        [Fact]
        public void GHBox_Centroid_ReturnsGHPoint()
        {
            // Arrange
            var myBox = new GH_Box(new Box());

            // Act
            var result = myBox.Boundingbox.Center;

            // Assert
            Assert.IsType<Point3d>(result);
        }
    }

    #endregion Example Code
}