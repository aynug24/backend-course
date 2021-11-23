using System;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Tests
{
    public static class TestMongoDatabase
    {
        public static IMongoDatabase Create()
        {
            //var mongoConnectionString = Environment.GetEnvironmentVariable("PROJECT5100_MONGO_CONNECTION_STRING")
            //                            ?? "mongodb://localhost:27017";
            var mongoConnectionString = "mongodb+srv://mongo:mongo_pwd@cluster0.aof6j.mongodb.net/mongo_test?retryWrites=true&w=majority";
            var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);
            mongoClientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    Debug.WriteLine($"{e.CommandName} - {e.Command.ToJson()}");
                });
            };
            var mongoClient = new MongoClient(mongoClientSettings);
            return mongoClient.GetDatabase("game-tests");
        }
    }
}