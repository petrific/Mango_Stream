using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MangoStream.Producer
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var client = new MongoClient("mongodb://localhost:30007/?readPreference=secondaryPreferred&serverSelectionTimeoutMS=5000&connectTimeoutMS=10000&3t.uriVersion=3");
			var test = client.GetDatabase("Test");
			var col = test.GetCollection<TestElement>("Elements");
			Console.WriteLine("Insert number of elements to create or type \"exit\"");
			string res;
			do
			{
				Console.Write(">");
				res = Console.ReadLine();
				if (int.TryParse(res, out int result) && result > 0)
				{
					await GenerateValues(result, col);	
				}
				else 
				{
					Console.WriteLine("Invalid value");
				}

			} while (res != "exit");
		}


		private static async Task GenerateValues(int count, IMongoCollection<TestElement> col) 
		{
			Console.WriteLine($"Generating {count} values...");
			var elements = Enumerable.Range(0, count).Select(i =>
			new TestElement
			{
				Id = Guid.NewGuid(),
				Name = $"TestElement{i}",
				Value = i,
				Version = 0
			});

			Console.WriteLine($"Inserting {count} values to database...");
			await col.InsertManyAsync(elements);
			Console.WriteLine("Done!");
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
