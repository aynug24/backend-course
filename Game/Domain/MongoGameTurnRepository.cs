using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameTurnRepository : IGameTurnRepository
    {
        private readonly IMongoCollection<GameTurnEntity> turnCollection;
        public const string CollectionName = "game-turns";

        public MongoGameTurnRepository(IMongoDatabase db)
        {
            turnCollection = db.GetCollection<GameTurnEntity>(CollectionName);

            turnCollection.Indexes.CreateOne(
                new CreateIndexModel<GameTurnEntity>(
                    Builders<GameTurnEntity>.IndexKeys
                        .Ascending(turn => turn.GameId)
                        .Descending(turn => turn.Date)
                )
            );
        }

        public GameTurnEntity Insert(GameTurnEntity gameTurn)
        {
            if (gameTurn.Id != Guid.Empty)
                throw new InvalidOperationException();

            var id = Guid.NewGuid();
            var entity = gameTurn.WithId(id);

            turnCollection.InsertOne(entity);

            return entity;
        }

        public IList<GameTurnEntity> GetLastTurns(GameEntity game, int limit)
        {
            var items = turnCollection.Find(turn => turn.GameId == game.Id)
                .SortByDescending(turn => turn.Date)
                .Limit(limit)
                .ToList();

            items.Reverse();

            return items;
        }
    }
}