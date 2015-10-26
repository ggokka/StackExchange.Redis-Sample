
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace stackexchangeredis
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
            IDatabase db = redis.GetDatabase();
            long count = 10000 * 10;
            DateTime start = DateTime.Now;

            #region all redis data write methods
            db.StringSet("key_test", "shaocan");
            db.HashSet("userinfo", "name", "shaocan");
            db.SetAdd("set_test", "user1");
            db.SetAdd("set_test", "user2");
            db.SortedSetAdd("sset_test", "user1", DateTime.Now.Ticks);
            db.SortedSetAdd("sset_test", "user2", DateTime.Now.Ticks);
            db.ListLeftPush("list_test", "user1");
            #endregion
            start = DateTime.Now;
            
            /* BinaryFormat */
            for (int i = 0; i < count; i++)
            {
                User user = new User { Id = i, Name = "YouNameIt" + i , Age = 20};
                string key = "myObject" + i;
                byte[] bytes;

                using (var stream = new MemoryStream())
                {
                    new BinaryFormatter().Serialize(stream, user);
                    bytes = stream.ToArray();
                }

                db.StringSet(key, bytes);
            }

            for (int i = 0; i < count; i++)
            {
                string key = "myObject" + i;
                User user = null;
                byte[] bytes = (byte[])db.StringGet(key);

                if (bytes != null)
                { 
                    using (var stream = new MemoryStream(bytes))
                    {
                        user = (User)new BinaryFormatter().Deserialize(stream);
                    }
                }
                Console.WriteLine(user.Name);
            }
            System.Console.WriteLine(string.Format("Binary Format {0} items takes {1} seconds" , count ,  (DateTime.Now - start).TotalSeconds));
            start = DateTime.Now;

            /* 10만건 */
            for (int i = 0; i < count; i++)
            {
                User user = new User { Id = i, Name = "훈민정음" + i, Age = 20 };
                string json = JsonConvert.SerializeObject(user);
           
                string key = "json" + i;
                db.StringSet(key, json);
            }

            for (int i = 0; i < count; i++)
            {
                string key = "json" + i;
                string json = db.StringGet(key);
                User user = (User)JsonConvert.DeserializeObject(json, typeof(User));
                Console.WriteLine(user.Name);
            }
            System.Console.WriteLine(string.Format("JSON Format {0} items takes {1} seconds", count, (DateTime.Now - start).TotalSeconds));
            start = DateTime.Now;

            //http://www.newtonsoft.com/json/help/html/SerializeDataSet.htm
            DataSet dataSet = new DataSet("dataSet");
            dataSet.Namespace = "NetFrameWork";
            DataTable table = new DataTable();
            DataColumn idColumn = new DataColumn("id", typeof(int));
            idColumn.AutoIncrement = true;

            DataColumn itemColumn = new DataColumn("item");
            table.Columns.Add(idColumn);
            table.Columns.Add(itemColumn);
            dataSet.Tables.Add(table);

            for (int i = 0; i < 2; i++)
            {
                DataRow newRow = table.NewRow();
                newRow["item"] = "[{홍}길:동] " + i;
                table.Rows.Add(newRow);
            }

            dataSet.AcceptChanges();

            string _json = JsonConvert.SerializeObject(dataSet, Formatting.Indented);
            db.StringSet("dataset1", _json);

            DataSet ds = (DataSet)JsonConvert.DeserializeObject(_json, typeof(DataSet));
            Console.WriteLine(ds.Tables[0].Rows[0]["item"].ToString());
            System.Console.ReadLine();
        }
    }

    [Serializable]
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
