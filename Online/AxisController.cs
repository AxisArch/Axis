using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Axis.Core;
using Axis.Online;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Grasshopper.Kernel.Types;

namespace Axis.Online
{
    public class AxisController : GH_Goo<AxisController>
    {
        public Controller axisController { get; set; }
        public string axisControllerModel { get; }
        public bool axisControllerState { get; }
        public Manufacturer type {get;}

        // Constructors
        public AxisController(Controller controller)
        {
            this.axisController = controller;
            this.axisControllerModel = controller.SystemName;
            this.axisControllerState = true;

        }
        public AxisController(Controller controller, string model, bool conected)
        {
            this.axisController = controller;
            this.axisControllerModel = model;
            this.axisControllerState = conected;
        }
       
        // Custom casting
        public static implicit operator Controller(AxisController controller) {
            
            Type type = controller.axisController.GetType(); 
            if (type.Equals(typeof(Controller)))
            {
                Controller abbController = controller.axisController;
                return abbController;
            }
            else { Controller abbController = null;  return abbController; }
        }

        public override string TypeName => "Robot Controller";
        public override string TypeDescription => "Connection to a robot controller";
        public override bool IsValid => axisControllerState;
        public override string ToString(){ return $"{type.ToString()} Robor Controller";}
        public override IGH_Goo Duplicate()
        {
            return this;
        }
    }
}