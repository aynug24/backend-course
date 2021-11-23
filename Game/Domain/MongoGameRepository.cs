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
                    //new CreateIndexOptions { Unique = true }
                )
            );
        }

        public GameEntity Insert(GameEntity game)
        {
            if (game.Id != Guid.Empty)
                throw new InvalidOperationException();

            var id = Guid.NewGuid();
            var entity = Clone(id, game);

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

        // Возвращает не более чем limit игр со статусом GameStatus.WaitingToStart
        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            return gameCollection
                .Find(game => game.Status == GameStatus.WaitingToStart)
                .Limit(limit)
                .ToList();
        }

        // Обновляет игру, если она находится в статусе GameStatus.WaitingToStart
        public bool TryUpdateWaitingToStart(GameEntity game)
        {

            var replaceResult = gameCollection.ReplaceOne(
                otherGame => otherGame.Id == game.Id && otherGame.Status == GameStatus.WaitingToStart,
                game
            );

            // так если в игре ничего не изменилось, InMemoryRepo возвращает true, поэтому ModifiedCount такое себе
            return replaceResult.MatchedCount == 1;
        }

        private GameEntity Clone(Guid id, GameEntity game)
        {
            return new GameEntity(id, game.Status, game.TurnsCount, game.CurrentTurnIndex, game.Players.ToList());
        }
    }
}