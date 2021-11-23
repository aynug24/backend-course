using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoGameRepository : IGameRepository
    {
        private readonly IMongoCollection<GameEntity> gameCollection;
        public const string CollectionName = "games";

        public MongoGameRepository(IMongoDatabase db)
        {
            gameCollection = db.GetCollection<GameEntity>(CollectionName);

            gameCollection.Indexes.CreateOne(
                new CreateIndexModel<GameEntity>(
                    Builders<GameEntity>.IndexKeys.Ascending(game => game.Status)
                )
            );
        }

        public GameEntity Insert(GameEntity game)
        {
            if (game.Id != Guid.Empty)
                throw new InvalidOperationException();

            var id = Guid.NewGuid();
            var entity = game.WithId(id);

            gameCollection.InsertOne(entity);

            return entity;
        }

        public GameEntity FindById(Guid gameId)
        {
            return gameCollection.Find(game => game.Id == gameId)
                .FirstOrDefault();
        }

        public void Update(GameEntity game)
        {
            gameCollection.ReplaceOne(
                otherUser => otherUser.Id == game.Id,
                game
            );
        }

        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            return gameCollection
                .Find(game => game.Status == GameStatus.WaitingToStart)
                .Limit(limit)
                .ToList();
        }

        public bool TryUpdateWaitingToStart(GameEntity game)
        {
            var replaceResult = gameCollection.ReplaceOne(
                otherGame => otherGame.Id == game.Id && otherGame.Status == GameStatus.WaitingToStart,
                game
            );

            // так если в игре ничего не изменилось, InMemoryRepo возвращает true, поэтому ModifiedCount такое себе
            return replaceResult.MatchedCount == 1;
        }
    }
}