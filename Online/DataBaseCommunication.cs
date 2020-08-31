using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.Serialization;

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
            List<GH_ObjectWrapper> dataInput = new List<GH_ObjectWrapper>();
            bool send = false;

            if (!DA.GetDataList<GH_ObjectWrapper>("Data", dataInput)) return;
            if (!DA.GetData("Send", ref send)) return;

            var client = new MongoClient(
                $"mongodb+srv://{user}:{password}@{server}/test?retryWrites=true&w=majority"
            );

            var database = client.GetDatabase("AxisPresets");

            string collectionName = "Robots";

            if (send)
            {
                foreach (GH_ObjectWrapper obj in dataInput) 
                {
                    if (typeof(GH_IO.GH_ISerializable).IsAssignableFrom(obj.GetType()))
                    {
                        //Document
                        BsonDocument bsons = new BsonDocument();

                        //Lable
                        BsonElement bson1 = new BsonElement("name", obj.Value.ToString());
                        bsons.Add(bson1);

                        BsonElement bson2 = new BsonElement("type", obj.Value.GetType().FullName.ToString());
                        bsons.Add(bson2);

                        //Data
                        GH_IO.Serialization.GH_Archive a = new GH_IO.Serialization.GH_Archive();
                        a.AppendObject(obj.Value as IGH_Goo, obj.Value.ToString());
                        byte[] chunk = a.Serialize_Binary();


                        BsonBinaryData data = new BsonBinaryData(chunk);
                        BsonElement bson3 = new BsonElement("data", data);
                        bsons.Add(bson3);

                        //Send
                        var collection = database.GetCollection<object>(collectionName);
                        collection.InsertOne(bsons);

                    };
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
            pManager.AddGenericParameter("Data", "D", "Data from data base", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string category = string.Empty;
            string ithem = string.Empty;

            List<KeyValuePair<string, string>> categorys = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> options = new List<KeyValuePair<string, string>>();


            GH_ObjectWrapper data = new GH_ObjectWrapper();

            var client = new MongoClient(
                $"mongodb+srv://{user}:{password}@{server}/test?retryWrites=true&w=majority"
            ); 
            var database = client.GetDatabase("AxisPresets");


            var test = database.ListCollectionNames();
            List<string> colloectionNames = database.ListCollectionNames().ToList<string>();
            foreach (string s in colloectionNames) 
            {
                categorys.Add(new KeyValuePair<string, string>( s, '"'+s+'"'));
            }


            GH_Document ghDoc = OnPingDocument();
            if (this.Params.Input[0].SourceCount == 0) Canvas.Component.SetValueList(ghDoc, this, 0, categorys, "Categorys");

            if (!DA.GetData("Category", ref category)) return;

            //options = database.GetCollection<AxisDB.MongoBDRobot>(category)
            //var c = database.GetCollection<AxisDB.MongoBDRobot>(category);
            var docs = database.GetCollection<BsonDocument>(category).Find(new BsonDocument()).ToList<BsonDocument>();

            foreach (BsonDocument doc in docs) 
            {
                var e = doc.GetElement("_v");

                BsonDocument doc2 = e.Value as BsonDocument;
                string name = doc2.GetValue("name").AsString;
                var id = doc.GetValue("_id").AsObjectId.ToString();// valueOf();
                options.Add(new KeyValuePair<string, string>( name, '"'+id+'"'));
            }

            if (this.Params.Input[1].SourceCount == 0) Canvas.Component.SetValueList(ghDoc, this, 1, options, "Ithem");

            if (!DA.GetData( 1, ref ithem)) return;

            if (category != null)
            {
                var collection = database.GetCollection<BsonDocument>(category);
                
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(ithem));
                var objects = collection.Find<BsonDocument>(filter).ToList();

                Grasshopper.Kernel.Data.GH_Structure<IGH_Goo> recivedData = new Grasshopper.Kernel.Data.GH_Structure<IGH_Goo>();

                foreach (BsonDocument obj in objects) 
                {
                    BsonDocument value = obj.GetValue("_v") as BsonDocument;
                    var d = value.GetValue("data").AsBsonBinaryData;
                    var t = value.GetValue("type").AsString;

                    GH_IO.Serialization.GH_Archive archive = new GH_IO.Serialization.GH_Archive();
                    bool sucsess= archive.Deserialize_Binary(d.AsByteArray);

                    var rootNode = archive.GetRootNode;
                    foreach (GH_IO.Serialization.GH_Chunk chunk in rootNode.Chunks) 
                    {
                        Type T = null;
                        var asemblyInfoList = Grasshopper.Instances.ComponentServer.Libraries;
                        foreach (var asemblyInfo in asemblyInfoList) 
                        {
                            T = asemblyInfo.Assembly.GetType(t);
                            if (T != null) break;
                        }
                        if (T == null) return;

                        var instance = Activator.CreateInstance(T) as IGH_Goo;
                        instance.Read(chunk);
                        recivedData.Append(instance);
                    }
                }

                if (objects.Count == 0) {AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Data not found"); return;}
                DA.SetDataTree(0, recivedData);

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



    }

}