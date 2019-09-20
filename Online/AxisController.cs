using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axis.Online;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

namespace Axis.Online
{
    public class AxisController
    {
        public Controller axisController { get; set; }
        public string axisControllerModel { get;}
        public bool axisControllerState { get;  }

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
        // Overrides
        public override string ToString(){ return "Axis.Controller";} 
    }
}