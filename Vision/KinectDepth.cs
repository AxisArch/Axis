using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Vision
{
    public class KinectDepth : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.iconCore;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("353b5508-824a-4451-87a1-2a346f0e2ebd"); }
        }

        // Active Kinect sensor.
        private KinectSensor kinectSensor = null;
        // Map depth range to byte range.
        private const int MapDepthToByte = 8000 / 256;
        // Reader for color frames.
        private DepthFrameReader depthFrameReader = null;
        // Bitmap to display.
        private WriteableBitmap depthBitmap = null;
        // Current status text to display.
        private string statusText = null;
        // Frame desciption.
        private FrameDescription depthFrameDescription = null;
        // Intermediate storage for frame data converted to color.
        private byte[] depthPixels = null;

        private bool running = false;
        private int counter = 0;

        List<string> log = new List<string>();

        public KinectDepth() : base("Kinect Depth", "Kinect Depth", "Get the depth stream from the Kinect.", "Axis", "Vision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the depth streaming module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Snapshot", "Snapshot", "Save the current cloud to the default pictures folder.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the module.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Cloud", "Cloud", "Cloud depth stream from the Kinect sensor.", GH_ParamAccess.item);
            pManager.AddTextParameter("Log", "Log", "Status log.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool activate = false;
            bool snapshot = false;
            bool reset = false;

            if (!DA.GetData(0, ref activate)) ;
            if (!DA.GetData(1, ref snapshot)) ;
            if (!DA.GetData(2, ref reset)) ;

            if (reset)
            {
                this.running = false;
                this.counter = 0;

                // Execute shutdown tasks.
                if (this.depthFrameReader != null)
                {
                    // ColorFrameReder is IDisposable.
                    this.depthFrameReader.Dispose();
                    this.depthFrameReader = null;
                }

                if (this.kinectSensor != null)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }

                this.log.Clear();
                this.log.Add("Sensor disposed. Module reset.");
            }

            if (activate)
            {
                this.running = true;

                if (counter == 0 || kinectSensor == null)
                {
                    // Get the Kinect sensor object.
                    this.kinectSensor = KinectSensor.GetDefault();
                    // Open the reader for the depth frames.
                    this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();
                    // Create the Depth Frame Description from DepthFrameSource.
                    this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
                    // Allocate space to put the pixels being received and converted.
                    this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
                    // Create the bitmap to display.
                    this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
                    // Open the sensor.
                    this.kinectSensor.Open();
                }

                // Update the status text.
                this.statusText = this.kinectSensor.IsAvailable ? "Running"
                                                                : "Kinect not available!";

                bool depthFrameProcessed = false;

                using (DepthFrame depthFrame = this.depthFrameReader.AcquireLatestFrame())
                {
                    if (depthFrame != null)
                    {
                        // the fastest way to process the body index data is to directly access 
                        // the underlying buffer
                        using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                        {
                            // verify data and write the color data to the display bitmap
                            if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                                (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                            {
                                // Note: In order to see the full range of depth (including the less reliable far field depth)
                                // we are setting maxDepth to the extreme potential depth threshold
                                ushort maxDepth = ushort.MaxValue;

                                // If you wish to filter by reliable depth distance, uncomment the following line:
                                //ushort maxDepth = depthFrame.DepthMaxReliableDistance;

                                this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);

                                depthFrameProcessed = true;
                            }
                        }
                    }
                }

                if (depthFrameProcessed)
                {
                    this.RenderDepthPixels();
                }

                if (this.depthBitmap != null)
                {
                    // create a png bitmap encoder which knows how to save a .png file
                    BitmapEncoder encoder = new PngBitmapEncoder();

                    // create frame from the writable bitmap and add to encoder
                    encoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

                    string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                    string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                    string path = Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");

                    // write the new file to disk
                    try
                    {
                        // FileStream is IDisposable
                        using (FileStream fs = new FileStream(path, FileMode.Create))
                        {
                            encoder.Save(fs);
                        }

                        this.statusText = string.Format(CultureInfo.CurrentCulture, "Saved screenshot to {0}", path);
                    }
                    catch (IOException)
                    {
                        this.statusText = string.Format(CultureInfo.CurrentCulture, "Failed to write screenshot to {0}", path);
                    }
                }

                // Update the iteration counter.
                counter++;
            }
            else
            {
                this.running = false;

                // Execute shutdown tasks.
                if (this.depthFrameReader != null)
                {
                    // ColorFrameReder is IDisposable.
                    this.depthFrameReader.Dispose();
                    this.depthFrameReader = null;
                }

                if (this.kinectSensor != null)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }

                this.log.Add("Streaming disabled.");
            }

            DA.SetData(0, this.depthBitmap);
            DA.SetDataList(1, this.log);
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }
    }
}