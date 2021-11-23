using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameTurnEntity
    {
        public Guid Id { get; private set; }

        public Guid GameId { get; init; }

        public List<Player> Players { get; init; }

        // Guid of winning player
        public Guid WinnerId { get; init; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Date { get; init; }

        public GameTurnEntity(Guid id)
        {
            Id = id;
        }

        public GameTurnEntity(Guid gameId, List<Player> players, Guid winnerId, DateTime date)
            : this(Guid.Empty, gameId, players, winnerId, date)
        { }

        [BsonConstructor]
        public GameTurnEntity(Guid id, Guid gameId, List<Player> players, Guid winnerId, DateTime date)
        {
            Id = id;
            GameId = gameId;
            Players = players;
            WinnerId = winnerId;
            Date = date;
        }

        public GameTurnEntity WithId(Guid id)
        {
            return new(id)
            {
                GameId = GameId,
                Players = Players,
                WinnerId = WinnerId,
                Date = Date
            };
        }

        // будем считать, что эта штука в интерфейсе, потом его реализации, потом создаётся новая, всё такое,
        // но в рамках задания оставим тут
        public string Format()
        {
            var fmtedDecisions = new Dictionary<PlayerDecision, string>
            {
                { PlayerDecision.Rock, "o" },
                { PlayerDecision.Paper, "□" },
                { PlayerDecision.Scissors, "x" }
            };

            if (Players.Any(player => !player.Decision.HasValue))
            {
                throw new InvalidOperationException();
            }

            var fmtedPlayers = Players
                .Select(player => player.UserId == WinnerId ? $"((({player.Name})))" : player.Name)
                .Select((fmtedPlayer, i) => $"{fmtedPlayer} {fmtedDecisions[Players[i].Decision!.Value]}");

            return string.Join(" : ", fmtedPlayers);
        }

    }
}