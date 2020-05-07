using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using System.IO;

namespace TorrentMiner
{
    public class MongoDBOP
    {
        private MongoClient mc = null;
        private IMongoDatabase mdb;
        private MongoDatabase mdbs;
        public IMongoDatabase Mdb
        {
            get
            {
                return mdb;
            }

            set
            {
                mdb = value;
            }
        }

        public MongoDatabase Mdbs
        {
            get
            {
                return mdbs;
            }

            set
            {
                mdbs = value;
            }
        }

        public MongoDBOP(string host, int port, string db)
        {
            MongoClientSettings mcs = new MongoClientSettings();
            mcs.Server = new MongoServerAddress(host, port);
            mc = new MongoClient(mcs);
            mdb = mc.GetDatabase(db);
            mdbs = mc.GetServer().GetDatabase(mdb.DatabaseNamespace.DatabaseName);
        }

        public MongoDBOP(string host, int port, string db, string user, string password)
        {
            MongoClientSettings mcs = new MongoClientSettings();
            mcs.Server = new MongoServerAddress(host, port);
            if (user != "" && password != "")
            {
                mcs.Credential = new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity(db, user), new PasswordEvidence(password));
                
            }
            mc = new MongoClient(mcs);
            mdb = mc.GetDatabase(db);
            mdbs = mc.GetServer().GetDatabase(mdb.DatabaseNamespace.DatabaseName);
        }

        private double GetTimestamp()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
            double timeStamp = (DateTime.Now - startTime).TotalMilliseconds / (Math.Pow(10, 3) * 1.0); // 相差毫秒数
            return timeStamp;
        }
        
        public void Close()
        {
            mc.GetServer().Disconnect();
        }

        public bool CreateCollection(string set_name)
        {
            mdb.CreateCollection(set_name);
            IAsyncCursor<string> cols = mdb.ListCollectionNames();
            List<string> colNames = cols.ToList();
            if (!colNames.Contains(set_name))
            {
                return true;
            }
            return false;
        }

        public bool DropCollection(string set_name)
        {
            mdb.DropCollection(set_name);
            IAsyncCursor<string> cols = mdb.ListCollectionNames();
            List<string> colNames = cols.ToList();
            if (!colNames.Contains(set_name))
            {
                return true;
            }
            return false;
        }

        public string Insert(string set_name, Dictionary<object, object> dic)
        {
            BsonDocument bdoc = new BsonDocument(dic);
            mdb.GetCollection<BsonDocument>(set_name).InsertOne(bdoc);
            BsonValue oid = null;
            if (bdoc.TryGetValue("_id", out oid))
            {
                return oid.ToString();
            }
            else
            {
                return null;
            }
        }

        public bool Delete(string set_name, Dictionary<object, object> search)
        {
            if (search.ContainsKey("_id"))
            {
                search["_id"] = new ObjectId(search["_id"].ToString());
            }
            BsonDocument bfilter = new BsonDocument(search);
            BsonDocumentFilterDefinition<BsonDocument> filterDef = new BsonDocumentFilterDefinition<BsonDocument>(bfilter);
            DeleteResult dres = mdb.GetCollection<BsonDocument>(set_name).DeleteMany(bfilter);
            if (dres.DeletedCount >= 1)
            {
                return true;
            }
            return false;
        }

        public UpdateResult Update(string set_name, Dictionary<object, object> search, string _op, Dictionary<object, object> data)
        {
            if (search.ContainsKey("_id"))
            {
                search["_id"] = new ObjectId(search["_id"].ToString());
            }
            BsonDocument bfilter = new BsonDocument(search);
            BsonDocumentFilterDefinition<BsonDocument> filterDef = new BsonDocumentFilterDefinition<BsonDocument>(bfilter);
            Dictionary<object, object> op = new Dictionary<object, object>();
            op.Add(_op, data);
            BsonDocument bdata = new BsonDocument(op);
            BsonDocumentUpdateDefinition<BsonDocument> updateDef = new BsonDocumentUpdateDefinition<BsonDocument>(bdata);
            UpdateResult ures = mdb.GetCollection<BsonDocument>(set_name).UpdateMany(filterDef, updateDef);
            return ures;
        }

        public FilterDefinition<BsonDocument> FilterBuilder(object search, string logic_type = null)
        {
            FilterDefinition<BsonDocument> filterDef = null;
            if (logic_type == null && search is Dictionary<object, object>)
            {
                if ((search as Dictionary<object, object>).ContainsKey("_id"))
                {
                    (search as Dictionary<object, object>)["_id"] = new ObjectId((search as Dictionary<object, object>)["_id"].ToString());
                }
                BsonDocument bfilter = new BsonDocument(search as Dictionary<object, object>);
                filterDef = new BsonDocumentFilterDefinition<BsonDocument>(bfilter);
            }
            else if (logic_type == "or" && search is List<Dictionary<object, object>>)
            {
                foreach (Dictionary<object, object> item in search as List<Dictionary<object, object>>)
                {
                    if ((item as Dictionary<object, object>).ContainsKey("_id"))
                    {
                        (item as Dictionary<object, object>)["_id"] = new ObjectId((item as Dictionary<object, object>)["_id"].ToString());
                    }
                }
                Dictionary<object, object> filter_dic = new Dictionary<object, object>();
                filter_dic.Add("$or", search);
                BsonDocument bfilter = new BsonDocument(filter_dic);
                filterDef = new BsonDocumentFilterDefinition<BsonDocument>(bfilter);
            }
            else if (logic_type == "and" && search is List<Dictionary<object, object>>)
            {
                Dictionary<object, object> filter_dic = new Dictionary<object, object>();
                filter_dic.Add("$and", search);
                BsonDocument bfilter = new BsonDocument(filter_dic);
                filterDef = new BsonDocumentFilterDefinition<BsonDocument>(bfilter);
            }
            else
            {
                FilterDefinitionBuilder<BsonDocument> builderFilter = Builders<BsonDocument>.Filter;
                filterDef = builderFilter.Empty;
            }
            return filterDef;
        }

        public List<BsonDocument> FindByKVPair(string set_name, FilterDefinition<BsonDocument> filterDef, object sort_list = null, int limit = 0, int skip = 0)
        {
            SortDefinition<BsonDocument> sorts = null;
            if (sort_list != null)
            {
                SortDefinitionBuilder<BsonDocument> builderSort = Builders<BsonDocument>.Sort;
                List<SortDefinition<BsonDocument>> sort = new List<SortDefinition<BsonDocument>>();
                foreach (KeyValuePair<string, string> kvp in sort_list as Dictionary<string, string>)
                {
                    if (kvp.Value == "ASC")
                    {
                        sort.Add(builderSort.Ascending(kvp.Key));
                    }
                    else
                    {
                        sort.Add(builderSort.Descending(kvp.Key));
                    }
                }
                sorts = builderSort.Combine(sort.ToList());
            }
            IFindFluent<BsonDocument, BsonDocument> find_op = mdb.GetCollection<BsonDocument>(set_name).Find(filterDef);
            if (sorts != null)
            {
                find_op = find_op.Sort(sorts);
            }
            if (skip > 0)
            {
                find_op = find_op.Skip(skip);
            }
            if (limit > 0)
            {
                find_op = find_op.Limit(limit);
            }
            return find_op.ToList();
        }

        public long FindCountByKVPair(string set_name, FilterDefinition<BsonDocument> filterDef)
        {
            return mdb.GetCollection<BsonDocument>(set_name).CountDocuments(filterDef);
        }

        public string InsertFileStream(string set_name, string file_name, string ext, Stream file_stream, Dictionary<object, object> info = null)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            MongoGridFSCreateOptions mgfsco = new MongoGridFSCreateOptions();
            if (info == null)
            {
                info = new Dictionary<object, object>();
            }
            info.Add("filename", file_name);
            info.Add("timestamp", GetTimestamp());
            info.Add("content_type", ext);
            mgfsco.Metadata = new BsonDocument(info);
            MongoGridFSFileInfo file_info = mdbs.GetGridFS(mgfs).Upload(file_stream, file_name, mgfsco);
            if (file_info.Exists)
            {
                return file_info.Id.ToString();
            }
            return null;
        }

        public string InsertFileByPath(string set_name, string file_path, Dictionary<object, object> info = null)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            MongoGridFSCreateOptions mgfsco = new MongoGridFSCreateOptions();
            if (info == null)
            {
                info = new Dictionary<object, object>();
            }
            FileInfo fi = new FileInfo(file_path);
            info.Add("file_name", fi.Name);
            info.Add("timestamp", GetTimestamp());
            info.Add("content_type", fi.Extension);
            mgfsco.Metadata = new BsonDocument(info);
            if (fi.Exists)
            {
                using (FileStream fs = new FileStream(file_path, FileMode.Open))
                {
                    MongoGridFSFileInfo file_info = mdbs.GetGridFS(mgfs).Upload(fs, fi.Name, mgfsco);
                    if (file_info.Exists)
                    {
                        return file_info.Id.ToString();
                    }
                }
            }
            return null;
        }

        public List<string> InsertFilesByPath(string set_name, string directory, Dictionary<object, object> infos = null)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            List<string> ids = new List<string>();
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            if (dirInfo.Exists)
            {
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    MongoGridFSCreateOptions mgfsco = new MongoGridFSCreateOptions();
                    Dictionary<object, object> info = new Dictionary<object, object>();
                    info.Add("file_name", file.Name);
                    info.Add("timestamp", GetTimestamp());
                    info.Add("content_type", file.Extension);
                    if (infos != null)
                    {
                        info.ToList().ForEach(item => info.Add(item.Key, item.Value));
                    }
                    mgfsco.Metadata = new BsonDocument(info);
                    using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                    {
                        MongoGridFSFileInfo file_info = mdbs.GetGridFS(mgfs).Upload(fs, file.Name, mgfsco);
                        if (file_info.Exists)
                        {
                            ids.Add(file_info.Id.ToString());
                        }
                    }
                }
            }
            return ids;
        }

        public MongoGridFSFileInfo GetFileById(string set_name,string fid)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            MongoGridFSFileInfo file_info=mdbs.GetGridFS(mgfs).FindOneById(new ObjectId(fid));
            return file_info;
        }

        public MongoGridFSFileInfo GetFileByInfo(string set_name,Dictionary<object,object> search)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            mgfs.ReadPreference = ReadPreference.Primary;
            mgfs.VerifyMD5 = true;
            mgfs.UpdateMD5 = true;
            MongoGridFSFileInfo file_info = mdbs.GetGridFS(mgfs).FindOne(Query.Create(new BsonDocument(search)));
            return file_info;
        }

        public bool CheckFileById(string set_name, string fid)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            return mdbs.GetGridFS(mgfs).ExistsById(new ObjectId(fid));
        }

        public bool CheckFileByInfo(string set_name, Dictionary<object, object> search)
        {
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            return mdbs.GetGridFS(mgfs).Exists(Query.Create(new BsonDocument(search)));
        }

        public long DeleteFilesById(string set_name, List<string> fids)
        {
            long count = 0;
            MongoGridFSSettings mgfs = new MongoGridFSSettings();
            mgfs.Root = set_name;
            foreach (string fid in fids)
            {
                mdbs.GetGridFS(mgfs).DeleteById(new ObjectId(fid));
                if(!mdbs.GetGridFS(mgfs).ExistsById(new ObjectId(fid)))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
