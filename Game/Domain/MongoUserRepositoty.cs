using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {

            userCollection = database.GetCollection<UserEntity>(CollectionName);

            userCollection.Indexes.CreateOne(
                new CreateIndexModel<UserEntity>(
                    Builders<UserEntity>.IndexKeys.Ascending(user => user.Login),
                    new CreateIndexOptions { Unique = true }
                )
            );
        }

        public UserEntity Insert(UserEntity user)
        {
            if (user.Id != Guid.Empty)
                throw new InvalidOperationException();

            var id = Guid.NewGuid();
            var entity = Clone(id, user);

            userCollection.InsertOne(entity);

            return entity;
        }

        public UserEntity FindById(Guid id)
        {
            return userCollection.Find(user => user.Id == id)
                .FirstOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            var setsOnInsert = Builders<UserEntity>.Update
                .SetOnInsert("Id", Guid.NewGuid())
                .SetOnInsert("Login", login);

            var foundOrCreatedUser = userCollection.FindOneAndUpdate<UserEntity>(
                user => user.Login == login,
                setsOnInsert,
                new FindOneAndUpdateOptions<UserEntity, UserEntity>
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.After
                }
            );

            return foundOrCreatedUser;

            //var existing = userCollection.Find(user => user.Login == login).FirstOrDefault();
            //if (existing == null)
            //{
            //    existing = new UserEntity(Guid.NewGuid()) { Login = login };
            //    userCollection.InsertOne(existing);
            //}

            //return existing;
        }

        public void Update(UserEntity user)
        {
            userCollection.ReplaceOne(
                otherUser => otherUser.Id == user.Id,
                user
            );
        }

        public void Delete(Guid id)
        {
            userCollection.DeleteOne(user => user.Id == id);
        }

        // Для вывода списка всех пользователей (упорядоченных по логину)
        // страницы нумеруются с единицы
        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            // а почему bulk write есть, а bulk read - нет
            var countTask = userCollection.EstimatedDocumentCountAsync();
            var itemsTask = userCollection.Find(FilterDefinition<UserEntity>.Empty)
                .SortBy(user => user.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var totalCount = countTask.Result;
            var items = itemsTask.Result;
            
            return new PageList<UserEntity>(items, totalCount, pageNumber, pageSize);
        }

        // Не нужно реализовывать этот метод
        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            if (user.Id == Guid.Empty)
                throw new InvalidOperationException();

            var updateResult = userCollection.ReplaceOne(
                otherUser => otherUser.Id == user.Id,
                user,
                new ReplaceOptions {
                    IsUpsert = true
                }
            );

            // Вроде не должно быть Unacknowledged, мб неправ
            isInserted = updateResult.MatchedCount == 0;
        }

        private UserEntity Clone(Guid id, UserEntity user)
        {
            return new UserEntity(id, user.Login, user.LastName, user.FirstName, user.GamesPlayed, user.CurrentGameId);
        }
    }
}