using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;
using NatNetML;

namespace Axis.Vision
{
    public class OptiTrackStream : GH_Component
    {

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Vision;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("0741cfd7-0924-4c37-8e35-96dbf2b6d0d6"); }
        }

        /*  [NatNet] Network connection configuration    */
        private static NatNetClientML mNatNet;    // The client instance
        private static string mStrLocalIP = "127.0.0.1";   // Local IP address (string)
        private static string mStrServerIP = "127.0.0.1";  // Server IP address (string)
        private static ConnectionType mConnectionType = ConnectionType.Multicast; // Multicast or Unicast mode

        /*  List for saving each of datadescriptors */
        private static List<DataDescriptor> mDataDescriptor = new List<DataDescriptor>();

        /*  Lists and Hashtables for saving data descriptions   */
        public static Hashtable mHtSkelRBs = new Hashtable();
        public static List<MarkerSet> mMarkerSet = new List<MarkerSet>();
        public static List<RigidBody> mRigidBodies = new List<RigidBody>();
        public static List<Skeleton> mSkeletons = new List<Skeleton>();
        public static List<ForcePlate> mForcePlates = new List<ForcePlate>();

        /*  boolean value for detecting change in asset */
        private static bool mAssetChanged = false;

        private static bool connectionConfirmed = false;
        private static int counter;

        // Flags are appended to a list for checking at SolveInstance.
        private static bool RigidBody = false;
        private static bool Skeleton = false;
        private static bool ForcePlate = false;

        protected static List<string> log = new List<string>();

        // Marker points
        protected static List<Point3d> mPoints = new List<Point3d>();
        protected static List<string> mLabels = new List<string>();

        // Rigid Body data.
        private static List<string> rigidBodyNames = new List<string>();
        private static List<double> rigidBodyPos = new List<double>();
        private static List<double> rigidBodyQuat = new List<double>();

        public OptiTrackStream() : base("OptiTrack Stream", "OptiTrack Stream", "Receive streaming data from a running Motive broadcast.", "Axis", "Vision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the streaming module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the streaming module.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Local IP", "Local IP", "IP address for the receiver.", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Server IP", "Server IP", "IP address for the server.", GH_ParamAccess.item, "127.0.0.1");
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "Status of the streaming service.", GH_ParamAccess.list);
            pManager.AddPointParameter("Markers", "Markers", "Generic, unlabelled marker data.", GH_ParamAccess.list);
            pManager.AddTextParameter("Labels", "Labels", "Marker labels.", GH_ParamAccess.list);
            pManager.AddTextParameter("RB Name", "RB Name", "List of Rigid Body name data.", GH_ParamAccess.list);
            pManager.AddTextParameter("RB Position", "RB Pos", "List of Rigid Body position data.", GH_ParamAccess.list);
            pManager.AddTextParameter("RB Quaternion", "RB Quat", "List of Rigid Body quaternion data.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool activate = false;
            bool reset = false;
            string localIP = "127.0.0.1";
            string serverIP = "127.0.0.1";

            if (!DA.GetData(0, ref activate)) return;
            if (!DA.GetData(1, ref reset)) return;
            if (!DA.GetData(2, ref localIP)) return;
            if (!DA.GetData(3, ref serverIP)) return;

            if (reset)
            {
                /*  Clear out existing lists */
                mDataDescriptor.Clear();
                mHtSkelRBs.Clear();
                mRigidBodies.Clear();
                mSkeletons.Clear();
                mForcePlates.Clear();
                log.Clear();

                counter = 0;
            }

            if (activate && counter == 0) // Activate the streaming module and attempt to find a Motive broadcast.
            {
                log.Add("NatNet managed client application starting.");

                // If not default, set the IP address
                mStrLocalIP = localIP;
                log.Add("Local IP set.");
                mStrServerIP = serverIP;
                log.Add("Server IP set.");

                // Attempt connection to the server.
                log.Add("Attempting connection to server...");
                connectToServer();
                log.Add("Fetching the Server Descriptor.");
                connectionConfirmed = fetchServerDescriptor(); //Fetch and parse data descriptor

                log.Add("Fetching the Frame Data.");
                /*  [NatNet] Assigning a event handler function for fetching frame data each time a frame is received   */
                mNatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(fetchFrameData);
                log.Add("Success: Data Port Connected.");

                counter++;
                ExpireSolution(true);
            }
            else if (activate && connectionConfirmed) // If the service is activated and we have a confirmed connection to the server.
            {
                /*  [NatNet] Assigning a event handler function for fetching frame data each time a frame is received   */
                mNatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(fetchFrameData);

                // Exception handler for updated assets list.
                if (mAssetChanged == true)
                {
                    log.Add("Change in the list of the assets. Refetching the descriptions");

                    /*  Clear out existing lists */
                    mDataDescriptor.Clear();
                    mHtSkelRBs.Clear();
                    mRigidBodies.Clear();
                    mSkeletons.Clear();
                    mForcePlates.Clear();

                    /* [NatNet] Re-fetch the updated list of descriptors  */
                    fetchDataDescriptor();
                    mAssetChanged = false;
                }

                /*  [NatNet] Disabling data handling function   */
                mNatNet.OnFrameReady -= fetchFrameData;

                counter++;
                ExpireSolution(true);
            }
            else if (!activate)
            {
                /*  Clearing Saved Descriptions */
                mRigidBodies.Clear();
                mSkeletons.Clear();
                mHtSkelRBs.Clear();
                mForcePlates.Clear();

                if (connectionConfirmed)
                {
                    mNatNet.Disconnect();
                }
                log.Clear();
                log.Add("Service stopped. Activate module to begin streaming.");
            }
            else { }

            DA.SetDataList(0, log);
            DA.SetDataList(1, mPoints);
            DA.SetDataList(2, mLabels);
            DA.SetDataList(3, rigidBodyNames);
            DA.SetDataList(4, rigidBodyPos);
            DA.SetDataList(5, rigidBodyQuat);
        }

        /// <summary>
        /// [NatNet] parseFrameData will be called when a frame of Mocap
        /// data has is received from the server application.
        ///
        /// Note: This callback is on the network service thread, so it is
        /// important to return from this function quickly as possible 
        /// to prevent incoming frames of data from buffering up on the
        /// network socket.
        ///
        /// Note: "data" is a reference structure to the current frame of data.
        /// NatNet re-uses this same instance for each incoming frame, so it should
        /// not be kept (the values contained in "data" will become replaced after
        /// this callback function has exited).
        /// </summary>
        /// <param name="data">The actual frame of mocap data</param>
        /// <param name="client">The NatNet client instance</param>
        static void fetchFrameData(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {

            /*  Exception handler for cases where assets are added or removed.
                Data description is re-obtained in the main function so that contents
                in the frame handler is kept minimal. */
            if ((data.bTrackingModelsChanged == true || data.nRigidBodies != mRigidBodies.Count || data.nSkeletons != mSkeletons.Count || data.nForcePlates != mForcePlates.Count))
            {
                mAssetChanged = true;
            }

            /*  Processing and ouputting frame data every 200th frame.
                This conditional statement is included in order to simplify the program output */
            if (data.iFrame % 4 == 0) // 120 FPS Flex 13 cameras (every 4th frame will give a solid 30 FPS in GH)
            {
                if (data.bRecording == false)
                {
                    //log.Add("Frame # " + data.iFrame.ToString() + " Received:");
                }
                else if (data.bRecording == true)
                {
                    //log.Add("[Recording] Frame #" + data.iFrame.ToString() + " Received:");
                }

                processFrameData(data);
            }
        }

        static void processFrameData(NatNetML.FrameOfMocapData data)
        {
            
            //mPoints.Clear();
            //// Standard marker streaming.
            //for (int i = 0; i < data.nOtherMarkers; i++)
            //{
            //    // Recreate point data.
            //    Point3d markerPoint = new Point3d(Math.Round(data.LabeledMarkers[i].x, 4), Math.Round(data.LabeledMarkers[i].y, 4), Math.Round(data.LabeledMarkers[i].z, 4));
            //    mPoints.Add(markerPoint);
            //    /*
            //    for (int j = 0; j < data.MarkerSets[i].nMarkers; j++)
            //    {
            //        // Recreate point data.
            //        Point3d markerPoint = new Point3d(Math.Round(data.LabeledMarkers[i].x, 4), Math.Round(data.LabeledMarkers[i].y, 4), Math.Round(data.LabeledMarkers[i].z, 4));
            //        mPoints.Add(markerPoint);
            //    }     
            //    */

            //    /*
            //    // Create marker labels.
            //    string markerLabel = data.OtherMarkers[i].ID.ToString();
            //    mLabels.Add(markerLabel);
            //    */
            //}           

            if (RigidBody)
            {
                rigidBodyNames.Clear();
                rigidBodyPos.Clear();
                rigidBodyQuat.Clear();

                /*  Parsing Rigid Body Frame Data   */
                for (int i = 0; i < mRigidBodies.Count; i++)
                {
                    // int rbID = mRigidBodies[i].ID;              // Fetching rigid body IDs from the saved descriptions

                    for (int j = 0; j < data.nRigidBodies; j++)
                    {
                        NatNetML.RigidBody rb = mRigidBodies[i];                // Saved rigid body descriptions
                        NatNetML.RigidBodyData rbData = data.RigidBodies[j];    // Received rigid body descriptions

                        if (rbData.Tracked == true)
                        {
                            // Rigid Body ID
                            rigidBodyNames.Add(rb.Name);

                            // Rigid Body Position
                            rigidBodyPos.Add(Math.Round(rbData.x, 4));
                            rigidBodyPos.Add(Math.Round(rbData.y, 4));
                            rigidBodyPos.Add(Math.Round(rbData.z, 4));

                            // Rigid Body Euler Orientation
                            //float[] quat = new float[4] { rbData.qx, rbData.qy, rbData.qz, rbData.qw };

                            // Attempting to normalise the rotation notation to fit with robot programming (WXYZ).
                            rigidBodyQuat.Add(Math.Round(rbData.qw, 6));
                            rigidBodyQuat.Add(Math.Round(rbData.qx, 6));
                            rigidBodyQuat.Add(Math.Round(rbData.qy, 6));
                            rigidBodyQuat.Add(Math.Round(rbData.qz, 6));
                            /*
                            float[] eulers = new float[3];

                            eulers = NatNetClientML.QuatToEuler(quat, NATEulerOrder.NAT_XYZr); //Converting quat orientation into XYZ Euler representation.
                            double xrot = RadiansToDegrees(eulers[0]);
                            double yrot = RadiansToDegrees(eulers[1]);
                            double zrot = RadiansToDegrees(eulers[2]);

                            Console.WriteLine("\t\tori ({0:N3}, {1:N3}, {2:N3})", xrot, yrot, zrot);
                            */
                        }
                        else
                        {
                            log.Add(rb.Name.ToString() + " is not tracked in current frame.");
                        }
                    }
                }
            }
            
            //if (Skeleton)
            //{
            //    /* Parsing Skeleton Frame Data  */
            //    for (int i = 0; i < mSkeletons.Count; i++)      // Fetching skeleton IDs from the saved descriptions
            //    {
            //        int sklID = mSkeletons[i].ID;

            //        for (int j = 0; j < data.nSkeletons; j++)
            //        {
            //            if (sklID == data.Skeletons[j].ID)      // When skeleton ID of the description matches skeleton ID of the frame data.
            //            {
            //                NatNetML.Skeleton skl = mSkeletons[i];              // Saved skeleton descriptions
            //                NatNetML.SkeletonData sklData = data.Skeletons[j];  // Received skeleton frame data

            //                Console.WriteLine("\tSkeleton ({0}):", skl.Name);
            //                Console.WriteLine("\t\tSegment count: {0}", sklData.nRigidBodies);

            //                /*  Now, for each of the skeleton segments  */
            //                for (int k = 0; k < sklData.nRigidBodies; k++)
            //                {
            //                    NatNetML.RigidBodyData boneData = sklData.RigidBodies[k];

            //                    /*  Decoding skeleton bone ID   */
            //                    int skeletonID = HighWord(boneData.ID);
            //                    int rigidBodyID = LowWord(boneData.ID);
            //                    int uniqueID = skeletonID * 1000 + rigidBodyID;
            //                    int key = uniqueID.GetHashCode();

            //                    NatNetML.RigidBody bone = (RigidBody)mHtSkelRBs[key];   //Fetching saved skeleton bone descriptions

            //                    //Outputting only the hip segment data for the purpose of this sample.
            //                    if (k == 0)
            //                        Console.WriteLine("\t\t{0:N3}: pos({1:N3}, {2:N3}, {3:N3})", bone.Name, boneData.x, boneData.y, boneData.z);
            //                }
            //            }
            //        }
            //    }
            //}
            
            //if (ForcePlate)
            //{
            //    /*  Parsing Force Plate Frame Data  */
            //    for (int i = 0; i < mForcePlates.Count; i++)
            //    {
            //        int fpID = mForcePlates[i].ID;                  // Fetching force plate IDs from the saved descriptions

            //        for (int j = 0; j < data.nForcePlates; j++)
            //        {
            //            if (fpID == data.ForcePlates[j].ID)         // When force plate ID of the descriptions matches force plate ID of the frame data.
            //            {
            //                NatNetML.ForcePlate fp = mForcePlates[i];                // Saved force plate descriptions
            //                NatNetML.ForcePlateData fpData = data.ForcePlates[i];    // Received forceplate frame data

            //                Console.WriteLine("\tForce Plate ({0}):", fp.Serial);

            //                // Here we will be printing out only the first force plate "subsample" (index 0) that was collected with the mocap frame.
            //                for (int k = 0; k < fpData.nChannels; k++)
            //                {
            //                    Console.WriteLine("\t\tChannel {0}: {1}", fp.ChannelNames[k], fpData.ChannelData[k].Values[0]);
            //                }
            //            }
            //        }
            //    }
            //}
        }

        static void connectToServer()
        {
            /*  [NatNet] Instantiate the client object  */
            mNatNet = new NatNetML.NatNetClientML();

            /*  [NatNet] Checking verions of the NatNet SDK library  */
            int[] verNatNet = new int[4];           // Saving NatNet SDK version number
            verNatNet = mNatNet.NatNetVersion();
            // Console.WriteLine("NatNet SDK Version: {0}.{1}.{2}.{3}", verNatNet[0], verNatNet[1], verNatNet[2], verNatNet[3]);

            /*  [NatNet] Connecting to the Server    */
            // Console.WriteLine("\nConnecting...\n\tLocal IP address: {0}\n\tServer IP Address: {1}\n\n", mStrLocalIP, mStrServerIP);

            NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();
            connectParams.ConnectionType = mConnectionType;
            connectParams.ServerAddress = mStrServerIP;
            connectParams.LocalAddress = mStrLocalIP;
            mNatNet.Connect(connectParams);
        }

        static bool fetchServerDescriptor()
        {
            NatNetML.ServerDescription m_ServerDescriptor = new NatNetML.ServerDescription();
            int errorCode = mNatNet.GetServerDescription(m_ServerDescriptor);

            if (errorCode == 0)
            {
                log.Add("Success: Connected to the server.");
                parseSeverDescriptor(m_ServerDescriptor);
                return true;
            }
            else
            {
                log.Add("Error: Failed to connect. Check the connection settings.");
                log.Add("Program terminated.");
                return false;
            }
        }

        static void parseSeverDescriptor(NatNetML.ServerDescription server)
        {
            log.Add("Server Info:");
            log.Add("Host: " + server.HostComputerName);
            log.Add("Application Name: " + server.HostApp);
            // Console.WriteLine("\tApplication Version: {0}.{1}.{2}.{3}", server.HostAppVersion[0], server.HostAppVersion[1], server.HostAppVersion[2], server.HostAppVersion[3]);
            // Console.WriteLine("\tNatNet Version: {0}.{1}.{2}.{3}\n", server.NatNetVersion[0], server.NatNetVersion[1], server.NatNetVersion[2], server.NatNetVersion[3]);
        }

        static void fetchDataDescriptor()
        {
            /*  [NatNet] Fetch Data Descriptions. Instantiate objects for saving data descriptions and frame data    */
            bool result = mNatNet.GetDataDescriptions(out mDataDescriptor);
            if (result)
            {
                log.Add("Success: Data Descriptions obtained from the server.");
                parseDataDescriptor(mDataDescriptor);
            }
            else
            {
                log.Add("Error: Could not get the Data Descriptions");
            }
            log.Add(" ");
        }

        static void parseDataDescriptor(List<NatNetML.DataDescriptor> description)
        {
            //  [NatNet] Request a description of the Active Model List from the server. 
            //  This sample will list only names of the data sets, but you can access 
            int numDataSet = description.Count;
            log.Add("Total " + numDataSet.ToString() + " data sets in the capture:");

            for (int i = 0; i < numDataSet; ++i)
            {
                int dataSetType = description[i].type;
                // Parse Data Descriptions for each data sets and save them in the delcared lists and hashtables for later uses.
                switch (dataSetType)
                {
                    case ((int)NatNetML.DataDescriptorType.eMarkerSetData):
                        NatNetML.MarkerSet mkset = (NatNetML.MarkerSet)description[i];
                        log.Add("MarkerSet (" + mkset.Name + ")");
                        break;


                    case ((int)NatNetML.DataDescriptorType.eRigidbodyData):
                        NatNetML.RigidBody rb = (NatNetML.RigidBody)description[i];
                        log.Add("RigidBody (" + rb.Name + ")");

                        // Saving Rigid Body Descriptions
                        mRigidBodies.Add(rb);
                        break;


                    //case ((int)NatNetML.DataDescriptorType.eSkeletonData):
                    //    NatNetML.Skeleton skl = (NatNetML.Skeleton)description[i];
                    //    log.Add("Skeleton (" + skl.Name + ", Bones:");

                    //    //Saving Skeleton Descriptions
                    //    mSkeletons.Add(skl);

                    //    // Saving Individual Bone Descriptions
                    //    for (int j = 0; j < skl.nRigidBodies; j++)
                    //    {

                    //        Console.WriteLine("\t\t{0}. {1}", j + 1, skl.RigidBodies[j].Name);
                    //        int uniqueID = skl.ID * 1000 + skl.RigidBodies[j].ID;
                    //        int key = uniqueID.GetHashCode();
                    //        mHtSkelRBs.Add(key, skl.RigidBodies[j]); //Saving the bone segments onto the hashtable
                    //    }
                    //    break;


                    //case ((int)NatNetML.DataDescriptorType.eForcePlateData):
                    //    NatNetML.ForcePlate fp = (NatNetML.ForcePlate)description[i];
                    //    Console.WriteLine("\tForcePlate ({0})", fp.Serial);

                    //    // Saving Force Plate Channel Names
                    //    mForcePlates.Add(fp);

                    //    for (int j = 0; j < fp.ChannelCount; j++)
                    //    {
                    //        Console.WriteLine("\t\tChannel {0}: {1}", j + 1, fp.ChannelNames[j]);
                    //    }
                    //    break;

                    default:
                        // When a Data Set does not match any of the descriptions provided by the SDK.
                        log.Add("Error: Invalid Data Set");
                        break;
                }
            }
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem rBody = Menu_AppendItem(menu, "Rigid Body", Menu_rBodyClick, true, RigidBody); // Append the item to the menu.
            rBody.ToolTipText = "When checked, surface will stream Rigid Body data."; // Specifically assign a tooltip text to the menu item.

            ToolStripMenuItem skeleton = Menu_AppendItem(menu, "Skeleton", Menu_skeletonClick, true, Skeleton);
            skeleton.ToolTipText = "When checked, surface will stream Skeleton data.";

            ToolStripMenuItem fPlate = Menu_AppendItem(menu, "Force Plate", Menu_fPlateClick, true, ForcePlate);
            fPlate.ToolTipText = "When checked, surface will stream Force Plate data.";
        }

        private void Menu_rBodyClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Rigid Body");
            RigidBody = !RigidBody;
            ExpireSolution(true);
        }

        private void Menu_skeletonClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Skeleton");
            Skeleton = !Skeleton;
            ExpireSolution(true);
        }

        private void Menu_fPlateClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Force Plate");
            ForcePlate = !ForcePlate;
            ExpireSolution(true);
        }

        // Functions
        static double RadiansToDegrees(double dRads)
        {
            return dRads * (180.0f / Math.PI);
        }

        static int LowWord(int number)
        {
            return number & 0xFFFF;
        }

        static int HighWord(int number)
        {
            return ((number >> 16) & 0xFFFF);
        }
    }
}