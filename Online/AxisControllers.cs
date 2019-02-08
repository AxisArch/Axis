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
        public Controller myAxisController { get; set; }
        public string myAxisControllerType { get; }


        public AxisController() { }
        public AxisController(string ControllerType, Controller controller)
        {
           this.myAxisController = controller;
           this.myAxisControllerType = ControllerType;   
        } 

        public static implicit operator Controller(AxisController controller) {
            Controller myController = controller.myAxisController;
            return myController;
        }
        //public static implicit operator AxisController(Controller controller) { return controller; }
        public override string ToString(){ return "Axis.Controller";} 
    }


}