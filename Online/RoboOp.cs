using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Axis.Tools;

namespace RoboOp
{
    public class Robot
    {
        public Socket sender;          //  The client socket
        public String hostAddress;     // This is your robot controller's address
        public int port = 1025;        // This is your robot controller's port
        public bool isSetup = false;

        public static String z0 = "[FALSE,0.3,0.3,0.3,0.03,0.3,0.03]";
        public static String z5 = "[FALSE,5,5,5,0.8,8,0.8]";
        public static String z15 = "[FALSE,15,23,23,2.3,23,2.3]";
        public static String z50 = "[FALSE,50,75,75,7.5,75,7.5]";
        public static String z100 = "[FALSE,100,150,150,15,150,15]";
        public static String z200 = "[FALSE,200,300,300,30,300,30]";


	    // Sets up Robot object to communicate between Grasshopper and an industrial robot.
        public Robot(String ip, int port)
        {
            this.hostAddress = ip;
            this.port = port;

            //Test code for quaternion conversion
            Matrix<double> T = DenseMatrix.Create(4, 4, 0);
            T[0, 0] = -1; T[0, 1] = 0; T[0, 2] = 0; T[0, 3] = 0;
            T[1, 0] = 0; T[1, 1] = 1; T[1, 2] = 0; T[1, 3] = 0;
            T[2, 0] = 0; T[2, 1] = 0; T[2, 2] = -1; T[2, 3] = 0;
            T[3, 0] = 0; T[3, 1] = 0; T[3, 2] = 0; T[3, 3] = 1;
            T[0, 0] = -1;
            Matrix Q = MathUtil.T2Quaternion(T);
            Console.WriteLine("Q " + Q);
            Matrix Tnew = MathUtil.Quaternion2T(Q, new DenseVector(new double[] { 0, 0, 0 }));
            Console.WriteLine("Tnew " + Tnew);
        }

        /**
	    * Connect the computer to the robot's server 
	    * to stream live commands to the robot.
	    * 
	    * @return msg from robot
	    * 
	    * @throws IOException 
	    * @throws UnknownHostException 
	    */
        public String connect()
        {
            // Connect to a remote device.
            String msg;
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses hostAddress on robot.
                IPAddress ipAddress = IPAddress.Parse(hostAddress);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                this.sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                this.sender.Connect(remoteEP);
                
                string message = ("Socket connected to {0}" + sender.RemoteEndPoint.ToString());

                //  msg = messageReceived();
                // Console.WriteLine("Connect message from robot " + msg);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

            isSetup = true;
            msg = "Connection to the robot open on port " + port + ".";
            return msg;
        }

        public String goToMain()
        {
            String key = "main";
            String val = "";
            sendMessage((key + "/" + val + ";"), true);
            return "PP Set to Main";
        } 

        /**
        * Tells the robot move relative to the current tool position.
        * <br/>
        * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc379.html">RelTool</a>
        * 
        * @param x		- translation from current x position
        * @param y 	- translation from current y position
        * @param z 	- translation from current z position
        * @param rx	- translation from current x orientation
        * @param ry	- translation from current y orientation
        * @param rz	- translation from current z orientation
        */
        public void moveOffset(int x, int y, int z, int rx, int ry, int rz)
        {
            String key = "offset";
            String val = "[" + x + "," + y + "," + z + "," + rx + "," + ry + "," + rz + "]";
            Console.WriteLine("offsetting");
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
        * Recreates a robot target from the position, orientation, configuration, & external axis of the robot.
        * 
        * @return string representation of the current robot target
        */
        public String getRobTarget()
        {
            Rhino.Geometry.Point3d pos = getPosition();
            Rhino.Geometry.Quaternion orient = getOrientation();
            float[] config = getConfiguration();
            float[] extax = getExternalAxes();

            String targ = "[[" + pos[0] + "," + pos[1] + "," + pos[2] + "]," +
                     "[" + orient.A + "," + orient.B + "," + orient.C + "," + orient.D + "]," +
                     "[" + config[0] + "," + config[1] + "," + config[2] + "," + config[3] + "]," +
                     "[" + extax[0] + "," + extax[1] + "," + extax[2] + "," + extax[3] + "," + extax[4] + "," + extax[5] + "]]";

            return targ;
        }


        /**
        * Asks for the current position of the robot.
        * <br/>
        * Link to doc: <i><a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc297.html">CPos</a></i>
        * 
        * @return [x,y,z] coordinates of robot TCP
        */
        public Rhino.Geometry.Point3d getPosition()
        {
            float[] coords = new float[3];

            String key = "query";
            String val = "pos";
            sendMessage((key + "/" + val + ";"), true);
            string msg = messageReceived();

            String[] temp = msg.Substring(1, msg.Length - 4).Split(',');

            for (int i = 0; i < temp.Length; i++)
            {
                coords[i] = float.Parse(temp[i]);
            }

            // Create the 3D point representing the location of the robot TCP.
            Rhino.Geometry.Point3d pos = new Rhino.Geometry.Point3d(coords[0], coords[1], coords[2]);

            return pos;
        }

        /**
        * Updates the robot's position using a linear move.
        * <br/>
        * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../IntroductionRAPIDProgOpManual/doc16.html">MoveL</a>
        * @param x - xPos in world coordinates
        * @param y - yPos in world coordinates
        * @param z - zPos in world coordinates
        */
        public void setPosition(double x, double y, double z)
        {
            String key = "pos";
            String val = "[" + x + "," + y + "," + z + "]";
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
        * Orientation of the TCP
        * <br/>
        * Link to doc: <a href+"http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc468.html">orient</a>
        * @return orientation in quaternions
        */
        public Rhino.Geometry.Quaternion getOrientation()
        {
            float[] vals = new float[4];

            String key = "query";
            String val = "orient";
            sendMessage((key + "/" + val + ";"), true);
            // Location of robot received from controller.
            string msg = messageReceived();

            String[] temp = msg.Substring(1, msg.Length - 4).Split(',');
            for (int i = 0; i < temp.Length; i++)
            {
                vals[i] = float.Parse(temp[i]);
            }

            Rhino.Geometry.Quaternion quat = new Rhino.Geometry.Quaternion(vals[0], vals[1], vals[2], vals[3]);

            return quat;
        }

        /**
	    * Updates the robot's orientation using a linear move.
	    * <br/>
	    * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc355.html">OrientZYX</a>
	    * @param rx - xRot in Euler Angles
	    * @param ry - yRot in Euler Angles
	    * @param rz - zRot in Euler Angles
	    */
        public void setOrientation(double rx, double ry, double rz)
        {
            String key = "orient";
            String val = "[" + rx + "," + ry + "," + rz + "]";
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
	    * Configuration of the Robot
	    * <br/>
	    * Link to doc: <a href+"http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc439.html">confdata</a>
	    * 
	    * @return array of config values
	    */
        public float[] getConfiguration()
        {
            float[] vals = new float[4];

            String key = "query";
            String val = "config";
            sendMessage((key + "/" + val + ";"), true);
            string msg = messageReceived();

            // block until the new data from the robot is received
            //  String msg = messageReceived();
            Console.WriteLine("robot's config: " + msg);

            String[] temp = msg.Substring(1, msg.Length - 1).Split(',');
            // PApplet.split(msg.substring(1, msg.length() - 1), ",");//orignial java code for splitting string
            for (int i = 0; i < temp.Length; i++)
            {
                vals[i] = float.Parse(temp[i]);
            }

            return vals;
        }

        public void setConfiguration(String config)
        {
            String key = "config";
            String val = config;
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
        * Flips the signal for a given Digital Out (DO).
        * @param pin
        */
        public void invertDO(String pin)
        {
            String key = "DO";
            String val = pin;
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
        * Waits until the robot has reached a position 
        * before executing the next line of code.
        */
        public void waitUntilPos()
        {
            String key = "wait";
            String val = "InPos";
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
	    * External Axis
	    * @return array of external axis values
	    */
        public float[] getExternalAxes()
        {
            float[] vals = new float[6];

            String key = "query";
            String val = "extax";
            sendMessage((key + "/" + val + ";"), true);
            string msg = messageReceived();

            // block until the new data from the robot is received
            // String msg = messageReceived();
            Console.WriteLine("robot's external axes: " + msg);

            String[] temp = msg.Substring(1, msg.Length - 1).Split(',');
            // String[] temp = PApplet.split(msg.substring(1, msg.length() - 1), ",");
            for (int i = 0; i < temp.Length; i++)
            {
                vals[i] = float.Parse(temp[i]);
            }

            return vals;
        }

        /**
        * Sets the external axes of the robot.
        * <br/>
        * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc451.html">external joints</a>
        * <br/><br/>
        * *Assumes string is <b>formatted properly.</b>
        * @param extax - string representation of extax
        */
        public void setExternalAxes(String extax)
        {
            String key = "extax";
            String val = extax;
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
        * Requests the speed data from the robot.
        * @return speed data as array 
        */
        public float[] getSpeed()
        {
            float[] vals = new float[4];

            String key = "query";
            String val = "speed";
            sendMessage((key + "/" + val + ";"), true);
            string msg = messageReceived();

            String[] temp = msg.Substring(1, msg.Length - 1).Split(',');
            for (int i = 0; i < temp.Length; i++)
            {
                vals[i] = float.Parse(temp[i]);
            }

            return vals;
        }

        /**
         * Sets the speed datatype for the robot.
         * <br/>
         * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc486.html">speeddadta</a>
         * 
         * @param tool		- velocity of the tool center point (TCP) in mm/s
         * @param orient	- reorientation velocity of the TCP expressed in degrees/s
         * @param extLinear - velocity of linear external axes in mm/s
         * @param extRot 	- velocity of rotating external axes in degrees/s
         */
        public void setSpeed(int tool, int orient, int extLinear, int extRot)
        {
            String key = "speed";
            String val = "[" + tool + "," + orient + "," + extLinear + "," + extRot + "]";
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
         * Requests the zonedata from the robot.
         * 
         * @return zone data as a string.
         */
        public String getZone()
        {
            String key = "query";
            String val = "zone";
            sendMessage((key + "/" + val + ";"), true);
            string msg = messageReceived();
            
            return msg;
        }


        /**
         * Sets the zone datatype for the robot. 
         * 
         * <br/>
         * Link to doc: <a href="http://developercenter.robotstudio.com:80/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc509.html">zonedadta</a>
         * 
         * @param z - zone to set 
         */
        public void setZone(String z)
        {
            String key = "zone";
            String val = z;
            sendMessage((key + "/" + val + ";"), true);
        }


        /**
         * Ends the robot program and closes the socket connection on the server.
         */
        public void quit()
        {
            String key = "flag";
            String val = "exit";
            sendMessage((key + "/" + val + ";"), false);
        }

        /**
         * Returns true if the client and server are connected.
         * @return
         */
        public bool IsSetup()
        {
            return isSetup;
        }

        /**
	    * Send a string command / target / position to the robot.
	    * <br/> 
	    * The string must be formatted exactly to the RAPID data type. 
	    * <br/> <br/> 
	    * You have the option to not wait for the robot to send a confirmation message
	    * back.  <i>messageReceived()</i> blocks until a message is sent back, so your program
	    * will hang if, for example, you're doing a big move command.
	    * 
	    * @param msg - string to send
	    * @param wait - whether you want to wait to received a msg back from the robot
	    * @return message from robot (either empty string or message)
	    * @throws InterruptedException 
	    */
        private String sendMessage(String msg, bool wait)
        {
            byte[] msgBytes = Encoding.ASCII.GetBytes(msg);
            this.sender.Send(msgBytes);

            if (wait)
                return messageReceived();
            else
                return "";
        }
        

        /**
        * Waits until the robot sends back a message
        * @return - message sent by robot
        */
        private String messageReceived()
        {
            String temp = "";
            try
            {
                byte[] bytes = new byte[1024];
                String robotInput = "";
                int bytesRec = this.sender.Receive(bytes);
                robotInput = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                temp = robotInput;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return temp;
        }

        /**
         * Close client
          **/
        public void closeClient()
        {
            isSetup = false;
            try
            {
                if (this.sender != null)
                {
                    // Release the socket.
                    this.sender.Close();
                    this.sender = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        //Unused code from Robo.Op (Java)
        /**
	     * STRING IS TOO LONG for RAPID. COME UP WITH ALTERNATIVE TO SEND ROBTARGET.
	     * <br/><br/>
	     * Sends a robotTarget to the program.  
	     * <br/>
	     * Use if you want to update configuration or external axis data.
	     * <br/><br/>
	     * Use <i>moveTo</i> or <i>moveOffset</i> to update postion or orientation.
	     * @param pos
	     * @param orient
	     * @param config
	     * @param extax
	     */
        /* @SuppressWarnings("unused")

         private void setRobTarget(float[] pos, float[] orient, float[] config, float[] extax)
             {
                 String key = "robTarg";
                 String val = "[[" + pos[0] + "," + pos[1] + "," + pos[2] + "], " +
                              "[" + orient[0] + "," + orient[1] + "," + orient[2] + "," + orient[3] + "], " +
                              "[" + config[0] + "," + config[1] + "," + config[2] + "," + config[3] + "], " +
                              "[" + extax[0] + "," + extax[1] + "," + extax[2] + "," + extax[3] + "," + extax[4] + "," + extax[5] + "]]";

                 sendMessage((key + "/" + val + ";"), true);
             }*/

        /**
         * Tells the robot to do a joint move to a given point with a given orientation.
         * <br/>
         * Link to doc: <i><a href="http://developercenter.robotstudio.com/Index.aspx?DevCenter=DevCenterManualStore&OpenDocument&Url=../RapidIFDTechRefManual/doc98.html">MoveJ</a></i>
         * 
         * @param x		- x position
         * @param y 	- y position
         * @param z 	- z position
         * @param rx	- x orientation
         * @param ry	- y orientation
         * @param rz	- z orientation
         */
        public void moveTo(double x, double y, double z, double q1, double q2, double q3, double q4)
        {
            // Round the coordinates and orientations to keep the message as short as possible.
            x = Math.Round(x, 2);
            y = Math.Round(y, 2);
            z = Math.Round(z, 2);
            
            q1 = Math.Round(q1, 3);
            q2 = Math.Round(q2, 3);
            q3 = Math.Round(q3, 3);
            q4 = Math.Round(q4, 3);

            String key = "joint";
            String val = "[" + x + "," + y + "," + z + "," + q1 + "," + q2 + "," + q3 + "," + q4 + "]";
            sendMessage((key + "/" + val + ";"), true);
        }

        /**
	     * STRING IS TOO LONG for RAPID. COME UP WITH ALTERNATIVE TO SEND ROBTARGET.
	     * 
	     * Sends a robotTarget to the program.  
	     * <br/>
	     * Use if you want to update configuration or external axis data.
	     * <br/><br/>
	     * Use <i>moveTo</i> or <i>moveOffset</i> to update postion or orientation. 
	     * <br/><br/>
	     * *Assumes string is <b>formatted properly.</b>
	     * 
	     * @param target - string representation of the robTarget
	     */
         private void setRobTarget(String target)
             {
                 String key = "robTarg";
                 String val = target;
                 sendMessage((key + "/" + val + ";"), true);
             }
         }
    }
