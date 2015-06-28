using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Options;

using FeatureExtracter;

namespace TestBsonType
{
    public class PersonDocument
    {
        [BsonSerializer(typeof(TwoDimensionalArraySerializer<Int32>))]
        public Int32[,] Appears { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public Dictionary<string, HueHisto>[] Features { get; set; }

        [BsonSerializer(typeof(ArraySerializer<String>))]
        public string[] ColorFrames { get; set; }

        [BsonSerializer(typeof(ArraySerializer<String>))]
        public string[] BodyFrames { get; set; }

        [BsonSerializer(typeof(ArraySerializer<String>))]
        public string[] DepthFrames { get; set; }

        [BsonSerializer(typeof(ArraySerializer<String>))]
        public string[] BodyIndexFrames { get; set; }
    }

    public class VideoDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("gps")]
        public Int32[] GPS { get; private set; }

        [BsonElement("add")]
        public string Address { get; private set; }

        [BsonElement("start")]
        public DateTime StartTime { get; private set; }

        [BsonElement("end")]
        public DateTime EndTime { get; private set; }

        [BsonElement("people")]
        public PersonDocument[] People { get; set; }
    }
}
