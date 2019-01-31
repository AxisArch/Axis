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
    public class KinectVideo : GH_Component
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
            get { return new Guid("940f78da-a562-469f-858e-0b8b87ae66dc"); }
        }

        // Active Kinect sensor.
        private KinectSensor kinectSensor = null;
        // Reader for color frames.
        private ColorFrameReader colorFrameReader = null;
        // Bitmap to display.
        private WriteableBitmap colorBitmap = null;
        // Current status text to display.
        private string statusText = null;
        // Frame desciption.
        private FrameDescription colorFrameDescription = null;

        private bool running = false;
        private int counter = 0;

        List<string> log = new List<string>();

        public KinectVideo() : base("Kinect Video", "Kinect Video", "Get the video stream from the Kinect.", "Axis", "Vision")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Activate", "Activate", "Activate the video streaming module.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Snapshot", "Snapshot", "Save the current snapshot to the default pictures folder.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the module.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Video bitmap stream from the Kinect sensor.", GH_ParamAccess.item);
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
                if (this.colorFrameReader != null)
                {
                    // ColorFrameReder is IDisposable.
                    this.colorFrameReader.Dispose();
                    this.colorFrameReader = null;
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
                    // Open the reader for the color frames.
                    this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
                    // Create the Color Frame Description from the ColorFrameSource using Bgra format.
                    this.colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                    // Create the bitmap to display.
                    this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                    // Open the sensor.
                    this.kinectSensor.Open();
                }

                // Update the status text.
                this.statusText = this.kinectSensor.IsAvailable ? "Running"
                                                                : "Kinect not available!";

                // ColorFrame is IDisposable
                using (ColorFrame colorFrame = this.colorFrameReader.AcquireLatestFrame())
                {
                    if (colorFrame != null)
                    {
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                        using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {
                            this.colorBitmap.Lock();

                            // Verify data and write the new color frame data to the display bitmap.
                            if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                            {
                                colorFrame.CopyConvertedFrameDataToIntPtr(
                                    this.colorBitmap.BackBuffer,
                                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                    ColorImageFormat.Bgra);

                                this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                            }

                            this.colorBitmap.Unlock();
                        }
                    }
                }

                // Wire handler for frame arrival
                //this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

                if (snapshot)
                {
                    if (this.colorBitmap != null)
                    {
                        // Create a png bitmap encoder which knows how to save a .png file.
                        BitmapEncoder encoder = new PngBitmapEncoder();

                        // Create frame from the writable bitmap and add to encoder.
                        encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                        string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                        string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                        string path = Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                        // Write the new file to disk.
                        try
                        {
                            // FileStream is IDisposable.
                            using (FileStream fs = new FileStream(path, FileMode.Create))
                            {
                                encoder.Save(fs);
                            }

                            this.log.Add("Image saved successfully.");
                        }
                        catch (IOException)
                        {
                            this.log.Add("Error saving image. IO Exception encountered.");
                        }
                    }
                }                

                // Update the iteration counter.
                counter++;
            }
            else
            {
                this.running = false;

                // Execute shutdown tasks.
                if (this.colorFrameReader != null)
                {
                    // ColorFrameReder is IDisposable.
                    this.colorFrameReader.Dispose();
                    this.colorFrameReader = null;
                }

                if (this.kinectSensor != null)
                {
                    this.kinectSensor.Close();
                    this.kinectSensor = null;
                }

                this.log.Add("Streaming disabled.");
            }

            DA.SetData(0, this.colorBitmap);
            DA.SetDataList(1, this.log);
        }

        /*
        // Handles the color frame data arriving from the sensor.
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }
        */
    }
}