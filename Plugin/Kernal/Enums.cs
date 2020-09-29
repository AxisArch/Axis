using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Axis.Kernal
{

    /// <summary>
    /// List of manufacturers.
    /// </summary>
    public enum Manufacturer
    {
        ABB = 0,
        Kuka = 1,
        Universal = 2
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

    public enum ControllerState
    {
        MotorsOff = 0, 
        Init = 1,
        MotorsOn = 2,
        EmergencyStop = 9,
        UnknownMotorState = 99,
    }
    public enum Display
    {
        Lines = 0,
        Mesh = 1,
    }
    public enum UIElementType 
    {
        ComponentButton = 0,
    }
}
