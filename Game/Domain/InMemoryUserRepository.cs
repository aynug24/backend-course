using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Guid adminId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private const string AdminLogin = "Admin";
        private readonly Dictionary<Guid, UserEntity> entities = new Dictionary<Guid, UserEntity>();

        public InMemoryUserRepository()
        {
            AddAdmin();
        }

        private void AddAdmin()
        {
            var user = new UserEntity(adminId, AdminLogin, "Halliday", "James", 999, null);
            entities[user.Id] = user;
        }

        public UserEntity Insert(UserEntity user)
        {
            if (user.Id != Guid.Empty)
                throw new InvalidOperationException();

            var id = Guid.NewGuid();
            var entity = user.WithId(id);
            entities[id] = entity;
            return entity.WithId(id);
        }

        public UserEntity FindById(Guid id)
        {
            return entities.TryGetValue(id, out var entity) ? entity.WithId(id) : null;
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            var existingUser = entities.Values.FirstOrDefault(u => u.Login == login);
            if (existingUser != null)
                return existingUser.WithId(existingUser.Id);

            var user = new UserEntity {Login = login};
            var entity = user.WithId(Guid.NewGuid());
            entities[entity.Id] = entity;
            return entity.WithId(entity.Id);
        }

        public void Update(UserEntity user)
        {
            if (!entities.ContainsKey(user.Id))
                return;

            entities[user.Id] = user.WithId(user.Id);
        }

        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            if (user.Id == Guid.Empty)
                throw new InvalidOperationException();

            var id = user.Id;
            if (entities.ContainsKey(id))
            {
                entities[id] = user.WithId(id);
                isInserted = false;
                return;
            }

            var entity = user.WithId(id);
            entities[id] = entity;
            isInserted = true;
        }

        public void Delete(Guid id)
        {
            entities.Remove(id);
        }

        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            var count = entities.Count;
            var items = entities.Values
                .OrderBy(u => u.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => u.WithId(u.Id))
                .ToList();
            return new PageList<UserEntity>(items, count, pageNumber, pageSize);
        }

        public void UpdatePlayersOnGameFinished(IEnumerable<Guid> playersIds)
        {
            foreach (var userId in playersIds)
            {
                if (!entities.TryGetValue(userId, out var user))
                    continue;

                user.CurrentGameId = null;
                user.GamesPlayed++;
            }
        }
    }
}