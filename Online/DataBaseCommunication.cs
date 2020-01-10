using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Axis.Online
{

    public class dbSet : GH_Component
    {
        string user = "mephisto";
        string password = "AxisMongoDG";
        string server = "axispresets-levbh.mongodb.net";

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public dbSet()
          : base("Send Data", "sD",
              "Will send data to a Database",
              AxisInfo.Plugin, AxisInfo.TabOnline)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Send", "S", "Data to send to the data base", GH_ParamAccess.item);
            pManager.AddGenericParameter("Data", "D", "Data to send to the database", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_ObjectWrapper> data = new List<GH_ObjectWrapper>();
            bool send = false;

            if (!DA.GetDataList<GH_ObjectWrapper>("Data", data)) return;
            if (!DA.GetData("Send", ref send)) return;

            var client = new MongoClient(
                $"mongodb+srv://{user}:{password}@{server}/test?retryWrites=true&w=majority"
            );

            var database = client.GetDatabase("AxisPresets");

            if (send)
            {
                foreach (GH_ObjectWrapper obj in data)
                {
                    if (typeof(Axis.Core.Manipulator).IsAssignableFrom(obj.Value.GetType()))
                    {
                        Axis.Core.Manipulator robot = obj.Value as Axis.Core.Manipulator;
                        var collection = database.GetCollection<AxisDB.MongoBDRobot>("Robots");


                        AxisDB.MongoBDRobot r = (AxisDB.MongoBDRobot)robot;
                        r.name = "IRB 120";

                        collection.InsertOne(r);


                    }
                    else if (typeof(Axis.Core.Tool).IsAssignableFrom(obj.Value.GetType()))
                    {
                        Axis.Core.Tool tool = obj.Value as Axis.Core.Tool;
                        var collection = database.GetCollection<AxisDB.MongoBDTool>("Tools");

                        AxisDB.MongoBDTool t = tool;
                        t.name = "Tool";

                        collection.InsertOne(t);

                    }
                    else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Sending the current datatype is not supported");

                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af9d98ed-d395-430c-81c4-8b84d99e19f5"); }
        }


    } //Send Data
    public class dbGet : GH_Component
    {
        string user = "mephisto";
        string password = "AxisMongoDG";
        string server = "axispresets-levbh.mongodb.net";

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public dbGet()
          : base("Get Data", "Get Data",
              "This will get data from a database",
              AxisInfo.Plugin, AxisInfo.TabOnline)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Category", "C", "the selected category to be loaded", GH_ParamAccess.item);
            pManager.AddTextParameter("Item", "I", "The item to imort", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "Data from data base", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string category = string.Empty;
            string ithem = string.Empty;

            Dictionary<string, string> categorys = new Dictionary<string, string>();
            Dictionary<string, string> options = new Dictionary<string, string>();


            GH_ObjectWrapper data = new GH_ObjectWrapper();

            var client = new MongoClient(
                $"mongodb+srv://{user}:{password}@{server}/test?retryWrites=true&w=majority"
            ); //"mongodb+srv://<username>:<password>@<cluster-address>/test?w=majority"
            var database = client.GetDatabase("AxisPresets");

            var test = database.ListCollectionsAsync().Result;
            List<string> colloectionNames = database.ListCollectionNames().ToList<string>();
            foreach (string s in colloectionNames) 
            {
                categorys.Add( s, s);
            }

            //Auto populate categorys

            if (!DA.GetData("Category", ref category)) return;

            //options = database.GetCollection<AxisDB.MongoBDRobot>(category)
            //var c = database.GetCollection<AxisDB.MongoBDRobot>(category);
            var docs = database.GetCollection<BsonDocument>(category).Find(new BsonDocument()).ToList<BsonDocument>();

            foreach (BsonDocument doc in docs) 
            {
                string name = doc.GetValue("name").AsString;
                string id = doc.GetValue("id").AsString;
                options.Add(name, id);
            }

            //Auto add thems value lis

            if (!DA.GetData("Item", ref ithem)) return;

            if (category != null)
            {
                if (category == "Robots" & ithem != null)
                {
                    var collection = database.GetCollection<AxisDB.MongoBDRobot>(ithem);
                    List<AxisDB.MongoBDRobot> robots = collection.Find(r => r._id == new ObjectId(ithem)).ToListAsync().Result;
                    DA.SetData("Data", (Axis.Core.Manipulator)robots[0]);

                }
                else if (category == "Tools" & ithem != null)
                {
                    var collection = database.GetCollection<AxisDB.MongoBDTool>(ithem);
                    List<AxisDB.MongoBDTool> tools = collection.Find(t => t._id == new ObjectId(ithem)).ToListAsync().Result;
                    DA.SetData("Data", (Axis.Core.Tool)tools[0]);
                }
                return; 
            }
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("69D21B27-6E63-4A24-97F9-538C93275859"); }
        }
    } //Retreving Data
    public class AxisDB {
        public class MongoBDRobot
    {
        public ObjectId _id { get; set; }
        public string name { get; set; }
        public int Manufacturer { get; set; }
        public BsonDocument RobBasePlane { get; set; }
        public BsonDocument AxisPoints { get; set; }
        public BsonDocument RobMeshes { get; set; }
        public BsonDocument MinAngles { get; set; }
        public BsonDocument MaxAngles { get; set; }
        public BsonDocument Indices { get; set; }


        public MongoBDRobot() { }
        public static implicit operator Axis.Core.Manipulator(MongoBDRobot rob)
        {

            Axis.Core.Manipulator r = new Axis.Core.Manipulator(
                (Axis.Core.Manufacturer)rob.Manufacturer,
                UnPackList<Point3d>(rob.AxisPoints),
                UnPackList<double>(rob.MinAngles),
                UnPackList<double>(rob.MaxAngles),
                UnPackList<Mesh>(rob.RobMeshes),
                UnPack<Plane>(rob.RobBasePlane),
                UnPackList<int>(rob.Indices)
                );

            r.name = rob.name;
            return r;
        }
        public static implicit operator MongoBDRobot(Axis.Core.Manipulator rob)
        {
            MongoBDRobot r = new MongoBDRobot();
            r.name = rob.name;
            r.Manufacturer = (int)rob.Manufacturer;
            r.RobBasePlane = Pack(rob.RobBasePlane.ToBsonDocument());
            r.AxisPoints = PackList(rob.AxisPoints);
            r.RobMeshes = PackList(rob.RobMeshes);
            r.MinAngles = PackList(rob.MinAngles);
            r.MaxAngles = PackList(rob.MaxAngles);
            r.Indices = PackList(rob.Indices);

            return r;
        }
    }
        public class MongoBDTool
    {
        public ObjectId _id { get; set; }
        public string name { get; set; }
        public int Manufacturer { get; set; }
        public string Name { get; set; }
        public BsonDocument TCP { get; set; }
        public double Weight { get; set; }
        public BsonDocument Geometry { get; set; }
        public BsonDocument FlangeOffset { get; }
        public bool Type { get; }
        public BsonDocument relTool { get; }


        public MongoBDTool() { }
        public static implicit operator Axis.Core.Tool(MongoBDTool tool)
        {
            Axis.Core.Tool t = new Axis.Core.Tool(
                tool.name,
                UnPack< Plane>(tool.TCP),
                tool.Weight,
                UnPackList<Mesh>(tool.Geometry),
                (Axis.Core.Manufacturer)tool.Manufacturer,
                UnPack<Vector3d>(tool.relTool)
                );
            return t;;
        }
        public static implicit operator MongoBDTool(Axis.Core.Tool tool)
        {
            MongoBDTool t = new MongoBDTool();
            return t;
        }
    }

        public static BsonDocument PackList<T>(List<T> data)
        {
            BsonDocument doc = new BsonDocument();
            doc.Add("type", data.GetType().ToString());

            for (int i = 0; i < data.Count(); ++i)
            {
                if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(data[i].GetType()))
                {
                    System.Runtime.Serialization.ISerializable s = data[i] as System.Runtime.Serialization.ISerializable;
                    doc.Add(i.ToString(), ObjectToByteArray(s));
                }
                else if (typeof(int).IsAssignableFrom(data[i].GetType()))
                {
                    int d = Convert.ToInt32(data[i]);
                    doc.Add(i.ToString(), d);
                }
                else if (typeof(double).IsAssignableFrom(data[i].GetType()))
                {
                    double d = Convert.ToDouble(data[i]);
                    doc.Add(i.ToString(), d);
                }
                else if (typeof(string).IsAssignableFrom(data[i].GetType()))
                {
                    doc.Add(i.ToString(), data[i].ToString());
                }
            }
            return doc;
        }
        public static List<T> UnPackList<T>(BsonDocument data)
        {
            List<T> output = new List<T>();

            foreach (BsonElement e in data.Elements)
            {
                if (e.Name == "type") { }
                else
                {
                    if (e.Value.BsonType == BsonType.Binary)
                    {
                        byte[] b = e.Value.RawValue as byte[];
                        T v = FromByteArrayTo<T>(b);
                        output.Add(v);
                    }
                    else
                    {
                        output.Add((T)e.Value.RawValue);
                    }
                }
            }

            return output;
        }
        public static BsonDocument Pack<T>(T data)
        {
            BsonDocument doc = new BsonDocument();
            doc.Add("type", data.GetType().ToString());


            if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(data.GetType()))
            {
                System.Runtime.Serialization.ISerializable s = data as System.Runtime.Serialization.ISerializable;
                doc.Add("object", ObjectToByteArray(s));
            }
            else if (typeof(int).IsAssignableFrom(data.GetType()))
            {
                int d = Convert.ToInt32(data);
                doc.Add("object", d);
            }
            else if (typeof(double).IsAssignableFrom(data.GetType()))
            {
                double d = Convert.ToDouble(data);
                doc.Add("object".ToString(), d);
            }
            else if (typeof(string).IsAssignableFrom(data.GetType()))
            {
                doc.Add("object".ToString(), data.ToString());
            }
            
            return doc;
        }
        public static T UnPack<T>(BsonDocument data)
        {

            if (data.Values.First().BsonType == BsonType.Binary)
            {
                byte[] b = data.Values.First().RawValue as byte[];
                return FromByteArrayTo<T>(b);
            }
            else
            {
                return  (T)data.Values.First().RawValue;
            }

        }


        public static byte[] ObjectToByteArray(Object obj)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var ms = new System.IO.MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static T FromByteArrayTo<T>(byte[] data)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var ms = new System.IO.MemoryStream(data))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;

            }
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    } 

}