using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Game.Domain;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class BsonSerializationTest
    {
        [Test]
        public void CanSerializeUser()
        {
            var userEntity = new UserEntity(Guid.NewGuid(), "someUserName", "", "", 0, Guid.NewGuid());
            AssertCorrectSerialization(userEntity);
        }

        [Test]
        public void CanSerializeGame()
        {
            var players = new List<Player>
            {
                new Player(Guid.NewGuid(), "name1") { Decision = PlayerDecision.Paper, Score = 42 },
                new Player(Guid.NewGuid(), "name2") { Decision = PlayerDecision.Rock, Score = 40 }
            };
            var entity = new GameEntity(Guid.NewGuid(), GameStatus.Playing, 10, 2, players);
            AssertCorrectSerialization(entity);
        }

        [Test]
        public void CanSerializeNotStartedGame()
        {
            var entity = new GameEntity(Guid.NewGuid(), GameStatus.WaitingToStart, 10, 0, new List<Player>());
            AssertCorrectSerialization(entity);
        }

        [Test]
        public void CanSerializeNotStartedGameWithPlayers()
        {
            var players = new List<Player> { new Player(Guid.NewGuid(), "name") };
            var entity = new GameEntity(Guid.NewGuid(), GameStatus.WaitingToStart, 10, 2, players);
            AssertCorrectSerialization(entity);
        }

        [Test]
        public void CanSerializeGameTurn()
        {
            var turn0 = new GameTurnEntity(Guid.Empty);
            AssertCorrectSerialization(turn0);

            var turn1 = new GameTurnEntity(Guid.Empty, Guid.Empty, null, Guid.Empty, DateTime.MinValue);
            AssertCorrectSerialization(turn1);

            var now = DateTime.UtcNow;
            var nowWithMsPrecision = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));

            var turn2 = new GameTurnEntity(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new List<Player>
                {
                    new(Guid.NewGuid(), "Player1") { Decision = PlayerDecision.Paper },
                    new(Guid.NewGuid(), "Player2") { Decision = PlayerDecision.Rock }
                },
                Guid.NewGuid(),
                nowWithMsPrecision
            );
            AssertCorrectSerialization(turn2);
        }

        private static void AssertCorrectSerialization<TEntity>(TEntity entity)
        {
            var memoryStream = new MemoryStream();
            BsonSerializer.Serialize(new BsonBinaryWriter(memoryStream), entity);
            var bytes = memoryStream.ToArray();

            var deserializedEntity = BsonSerializer.Deserialize<TEntity>(new MemoryStream(bytes));

            Console.WriteLine(deserializedEntity);
            deserializedEntity.Should().BeEquivalentTo(entity);
        }
    }
}