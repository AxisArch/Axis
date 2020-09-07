using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;
using System.Runtime.Serialization;
using GH_IO;
using MongoDB.Driver.Core.Operations;

namespace AxisConnect.GH_Componnts
{
    public class dbSet : GH_Component
    {
        DataBaseType m_type = DataBaseType.MongoDB;
        string user = "mephisto";
        string password = "AxisMongoDG";
        string server = "axispresets-levbh.mongodb.net";

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public dbSet()
          : base("Send Data", "sD",
              "Will send data to a Database",
              Axis.AxisInfo.Plugin, Axis.AxisInfo.TabOnline)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Send", "S", "Data to send to the data base", GH_ParamAccess.item);
            pManager.AddGenericParameter("Data", "D", "Data to send to the database", GH_ParamAccess.tree);
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
            Grasshopper.Kernel.Data.GH_Structure<IGH_Goo> dataInput = new Grasshopper.Kernel.Data.GH_Structure<IGH_Goo>();
            bool send = false;

            if (!DA.GetDataTree<IGH_Goo>(1, out dataInput)) return;
            if (!DA.GetData("Send", ref send)) return;

            GH_IDatabase client;

            switch (m_type) 
            {
                case DataBaseType.MongoDB:
                    var connection = new AxisConnect.GH_MongoClient(user, password, server);
                    connection.SetLocation(new Tuple<string, string>("AxisPresets", "General"));
                    client = connection as GH_IDatabase;
                    break;
                default:
                    throw new NotImplementedException($"{m_type.ToString()} has not yet been implemented");
            }

            if (!send) return;

            foreach (IGH_Goo obj in dataInput) 
            {
                if (obj != null && client!= null) client.SendData(obj);
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
        DataBaseType m_type = DataBaseType.MongoDB;
        string user = "mephisto";
        string password = "AxisMongoDG";
        string server = "axispresets-levbh.mongodb.net";

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public dbGet()
          : base("Get Data", "Get Data",
              "This will get data from a database",
              Axis.AxisInfo.Plugin, Axis.AxisInfo.TabOnline)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("Category", "C", "the selected category to be loaded", GH_ParamAccess.item);
            pManager.AddTextParameter("Item", "I", "The item to imort", GH_ParamAccess.item);

            pManager[0].Optional = true;
            //pManager[1].Optional = true;

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
            

            GH_IDatabase client;

            switch (m_type)
            {
                case DataBaseType.MongoDB:
                    var connection = new AxisConnect.GH_MongoClient(user, password, server);
                    connection.SetLocation(new Tuple<string, string>("AxisPresets", "General"));
                    client = connection as GH_IDatabase;
                    break;
                default:
                    throw new NotImplementedException($"{m_type.ToString()} has not yet been implemented");
            }


            List<KeyValuePair<string, string>> options = client.GetItems();

            GH_Document ghDoc = OnPingDocument();
            if (this.Params.Input[0].SourceCount == 0) Canvas.Component.SetValueList(ghDoc, this, 0, options, "Ithem");

            if (!DA.GetData( 0, ref ithem)) return;


            Grasshopper.Kernel.Data.GH_Structure<IGH_Goo> recivedData = new Grasshopper.Kernel.Data.GH_Structure<IGH_Goo>();
            recivedData.Append(client.GetObject(ithem));
            

            if (recivedData.Count() == 0) {AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Data not found"); return;}
            DA.SetDataTree(0, recivedData);

            return; 
            
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
}

namespace AxisConnect 
{
    interface GH_IDatabase
    {
        //bool SetLocation(Tuple<> location);
        bool SendData(IGH_Goo dataS);
        IGH_Goo GetObject(string ID);
        List<KeyValuePair<string, string>> GetItems();
    }
    class GH_MongoClient: GH_IDatabase
    {
        #region Class variables
        private string userName;
        private string password;
        private string server;
        private MongoClient connectionHandler;
        private MongoCollectionBase<BsonDocument> collection;
        #endregion

        #region Constructors
        public  GH_MongoClient() { }
        public GH_MongoClient(string userName, string password, string server) 
        {
            this.userName = userName;
            this.password = password;
            this.server = server;
            Connect();
        }

        #endregion

        #region Properties
        #endregion

        #region Methods
        public bool SetLocation(Tuple<string,string> location) 
        {
            if (connectionHandler == null) return false;
            collection = connectionHandler.GetDatabase(location.Item1).GetCollection<BsonDocument>(location.Item2) as MongoCollectionBase<BsonDocument>;
            return true;
        }

        bool Connect() 
        {
            this.connectionHandler = new MongoClient($"mongodb+srv://{userName}:{password}@{server}/test?retryWrites=true&w=majority");
            return true;

        }
        static BsonDocument CreateBysone(byte[] chunk, string objetName, Type objectType) 
        {
            //Document
            BsonDocument bsons = new BsonDocument();

            //Lable
            BsonElement name = new BsonElement("name", objetName);
            bsons.Add(name);

            BsonElement type = new BsonElement("type", objectType.FullName);
            bsons.Add(type);

            //Data
            BsonBinaryData dataChunk = new BsonBinaryData(chunk);
            BsonElement data = new BsonElement("data", dataChunk);
            bsons.Add(data);

            return bsons;
        }


        /// <summary>
        /// BsonDocument to MongoBD
        /// </summary>
        /// <param name="data">BsonDocument</param>
        /// <param name="collectionSettings">Collections</param>
        /// <returns>true if sucsessfull</returns>
        public bool SendData(IGH_Goo data)
        {
            var binary = data.GrasshopperObjectToByteArray();
            var bsons =  CreateBysone(binary, data.ToString(), data.GetType());

            if (collection == null) throw new Exception("No location has been set");

            collection.InsertOne(bsons);

            return true;
        }
        public IGH_Goo GetObject(string id) 
        {
            if (collection == null) throw new Exception("No location has been set");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
            var objects = collection.Find<BsonDocument>(filter).ToList();

            IGH_Goo recivedData = default(IGH_Goo);

            foreach (BsonDocument obj in objects)
            {
                var d = obj.GetValue("data").AsBsonBinaryData;
                var t = obj.GetValue("type").AsString;

                GH_IO.Serialization.GH_Archive archive = new GH_IO.Serialization.GH_Archive();
                bool sucsess = archive.Deserialize_Binary(d.AsByteArray);

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
                    if (T == null) break;

                    var instance = Activator.CreateInstance(T) as IGH_Goo;
                    instance.Read(chunk);
                    recivedData = instance ;
                }
            }

            return recivedData;
        }
        public List<KeyValuePair<string, string>> GetItems()
        {
            if (collection == null) throw new Exception("No location has been set");

            List<KeyValuePair<string, string>> valuePairs = new List<KeyValuePair<string, string>>();

            //var filterDef = new FilterDefinitionBuilder<BsonDocument>();
            //var filter = filterDef.In(x => x._id, new[] { "101", "102" });
            var docs = collection.Find<BsonDocument>(new BsonDocument()).ToList<BsonDocument>();

            foreach (BsonDocument doc in docs)
            {
                string name = doc.GetValue("name").AsString;
                var id = doc.GetValue("_id").AsObjectId.ToString();
                valuePairs.Add(new KeyValuePair<string, string>(name, '"' + id + '"'));
            }

            return valuePairs;
        }
        #endregion
    }
    public static class AxisConvert 
    {
        /// <summary>
        /// This will serialise a type to a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] GrasshopperObjectToByteArray(this GH_ISerializable data)
        {
            GH_IO.Serialization.GH_Archive archive = new GH_IO.Serialization.GH_Archive();
            archive.AppendObject(data as IGH_Goo, data.ToString());
            byte[] chunk = archive.Serialize_Binary();
            return chunk;
        }
        /// <summary>
        /// This will deserialise a chunk provided with the full type name
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static IGH_Goo ByteArrayToGrasshopperObject(this GH_IO.Serialization.GH_Chunk chunk, string typeFullName)
        {
            // Variable to hold type
            Type T = null;

            // Get all curently loaded assemblies
            var asemblyInfoList = Grasshopper.Instances.ComponentServer.Libraries;

            //Match strng to the first type
            foreach (var asemblyInfo in asemblyInfoList)
            {
                T = asemblyInfo.Assembly.GetType(typeFullName);
                if (T != null) break;
            }
            if (T == null) return null;

            // create default instance of that type
            var instance = Activator.CreateInstance(T) as IGH_Goo;

            // Deserialise chunk
            instance.Read(chunk);

            return instance;
        }
    }

    public enum DataBaseType 
    {
        MongoDB = 0
    }
};