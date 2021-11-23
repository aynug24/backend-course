using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    public class InMemoryGameRepository : IGameRepository
    {
        private readonly Dictionary<Guid, GameEntity> entities = new Dictionary<Guid, GameEntity>();

        public GameEntity Insert(GameEntity game)
        {
            if (game.Id != Guid.Empty)
                throw new Exception();

            var id = Guid.NewGuid();
            var entity = game.WithId(id);
            entities[id] = entity;
            return entity.WithId(id);
        }

        public GameEntity FindById(Guid id)
        {
            return entities.TryGetValue(id, out var entity) ? entity.WithId(id) : null;
        }

        public void Update(GameEntity game)
        {
            if (!entities.ContainsKey(game.Id))
                return;

            entities[game.Id] = game.WithId(game.Id);
        }

        public IList<GameEntity> FindWaitingToStart(int limit)
        {
            return entities
                .Select(pair => pair.Value)
                .Where(e => e.Status == GameStatus.WaitingToStart)
                .Take(limit)
                .Select(e => e.WithId(e.Id))
                .ToArray();
        }

        public bool TryUpdateWaitingToStart(GameEntity game)
        {
            if (!entities.TryGetValue(game.Id, out var savedGame)
                || savedGame.Status != GameStatus.WaitingToStart)
                return false;

            entities[game.Id] = game.WithId(game.Id);
            return true;
        }
    }
}