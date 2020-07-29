using System;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MangoStream
{
	class Program
	{
		static void Main(string[] args)
		{
			ChangeStreamOperationType opFilter;
			string trailName;
			if (args.Length < 2 || !Enum.TryParse(args[0], out opFilter))
			{
				Console.WriteLine("Valid ChangeStreamOperationType filter required!");
				return;
			}

			trailName = args[1];

			Console.WriteLine($"Mango Stream Receiver will start listening for {opFilter} operations");
			var client = new MongoClient("mongodb://localhost:30007/?readPreference=secondaryPreferred&serverSelectionTimeoutMS=5000&connectTimeoutMS=10000&3t.uriVersion=3");
			var test = client.GetDatabase("Test");
			var col = test.GetCollection<TestElement>("Elements");
			var trails = test.GetCollection<MangoTrail>("MangoTrails");

			var currentTrail = trails.Find(t => t.TrailName == trailName).FirstOrDefault();
			var resumeOptions = new ChangeStreamOptions();

			if(currentTrail != null){
				resumeOptions.StartAfter = new BsonDocument("_data", currentTrail.Data);
			}

			var changeStream = col.Watch(resumeOptions);
			if (currentTrail == null) {
				currentTrail = new MangoTrail
				{
					Id = Guid.NewGuid(),
					TrailName = trailName,
					Data = changeStream.GetResumeToken()["_data"].AsString
				};
				trails.InsertOne(currentTrail);
			}

			Console.WriteLine($"Mango Stream Receiver is start listening for {opFilter} operations");
			int count = 0;
			while (changeStream.MoveNext()){
				if(changeStream.Current.Count() == 0){
					continue;
				}

				var items = changeStream.Current.Where(i => i.OperationType == opFilter).ToList();
				foreach (var item in items)
				{
					var doc = col.Find(e => e.Id == item.DocumentKey["_id"].AsGuid).First();
					Console.WriteLine($"Element {doc.Name} has been seen with update type {item.OperationType}, change count: {++count}");
				}

				currentTrail.Data = changeStream.GetResumeToken()["_data"].AsString;
				trails.ReplaceOne(t => t.TrailName == trailName, currentTrail, new ReplaceOptions { IsUpsert = true });
			}
			Console.ReadLine();
		}

		public class MangoTrail 
		{
			[BsonElement("_id")]
			public Guid Id { get; set; }

			[BsonElement("_data")]
			public string Data { get; set; }


			[BsonElement("trailName")]
			public string TrailName { get; set; }
		}
		
		public class TestElement 
		{
			[BsonId]
			public Guid Id { get; set; }

			[BsonElement("name")]
			public string Name { get; set; }

			[BsonElement("value")]
			public int Value { get; set; }

			[BsonElement("version")]
			public int Version { get; set; }
		}
	}
}
