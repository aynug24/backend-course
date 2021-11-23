using System;
using System.Diagnostics;
using System.Linq;
using Game.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace ConsoleApp
{
    class Program
    {
        private readonly IUserRepository userRepo;
        private readonly IGameRepository gameRepo;
        private readonly IGameTurnRepository turnRepo;
        private readonly Random random = new Random();

        private Program(string[] args)
        {
            var db = GetMongoDb();
            userRepo = new MongoUserRepository(db);
            gameRepo = new MongoGameRepository(db);
            turnRepo = new MongoGameTurnRepository(db);
        }

        public static void Main(string[] args)
        {
            new Program(args).RunMenuLoop();
        }

        public static IMongoDatabase GetMongoDb()
        {
            // в учебных целях можно и оставить логин-пароль))
            // в реальном - не стоит, конечно
            var mongoConnectionString = "mongodb+srv://mongo:mongo_pwd@cluster0.aof6j.mongodb.net/mongo_test?retryWrites=true&w=majority";
            var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);
            //mongoClientSettings.ClusterConfigurator = cb =>
            //{
            //    cb.Subscribe<CommandStartedEvent>(e =>
            //    {
            //        Debug.WriteLine($"{e.CommandName} - {e.Command.ToJson()}");
            //    });
            //};
            var mongoClient = new MongoClient(mongoClientSettings);
            return mongoClient.GetDatabase("game-tests");
        }

        private void RunMenuLoop()
        {
            var humanUser = userRepo.GetOrCreateByLogin("Human");
            var aiUser = userRepo.GetOrCreateByLogin("AI");
            var game = FindCurrentGame(humanUser) ?? StartNewGame(humanUser);
            if (!TryJoinGame(game, aiUser))
            {
                Console.WriteLine("Can't add AI user to the game");
                return;
            }

            while (HandleOneGameTurn(humanUser.Id))
            {
            }

            Console.WriteLine("Game is finished");
            Console.ReadLine();
        }

        private GameEntity StartNewGame(UserEntity user)
        {
            Console.WriteLine("Enter desired number of turns in game:");
            if (!int.TryParse(Console.ReadLine(), out var turnsCount))
            {
                turnsCount = 5;
                Console.WriteLine($"Bad input. Used default value for turns count: {turnsCount}");
            }

            var game = new GameEntity(turnsCount);
            game.AddPlayer(user);
            var savedGame = gameRepo.Insert(game);

            user.CurrentGameId = savedGame.Id;
            userRepo.Update(user);

            return savedGame;
        }

        private bool TryJoinGame(GameEntity game, UserEntity user)
        {
            if (IsUserInGame(user, game))
                return true;

            if (user.CurrentGameId.HasValue)
                return false;

            if (game.Status != GameStatus.WaitingToStart)
                return false;

            game.AddPlayer(user);
            if (!gameRepo.TryUpdateWaitingToStart(game))
                return false;

            user.CurrentGameId = game.Id;
            userRepo.Update(user);

            return true;
        }

        private static bool IsUserInGame(UserEntity user, GameEntity game)
        {
            return user.CurrentGameId.HasValue
                   && user.CurrentGameId.Value == game.Id
                   && game.Players.Any(p => p.UserId == user.Id);
        }

        private GameEntity FindCurrentGame(UserEntity humanUser)
        {
            if (humanUser.CurrentGameId == null) return null;
            var game = gameRepo.FindById(humanUser.CurrentGameId.Value);
            if (game == null) return null;
            return game.Status switch
            {
                GameStatus.WaitingToStart or GameStatus.Playing => game,
                GameStatus.Finished or GameStatus.Canceled => null,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private bool HandleOneGameTurn(Guid humanUserId)
        {
            var game = GetGameByUser(humanUserId);

            if (game.IsFinished())
            {
                var playersIds = game.Players.Select(player => player.UserId);
                userRepo.UpdatePlayersOnGameFinished(playersIds);
                return false;
            }

            PlayerDecision? decision = AskHumanDecision();
            if (!decision.HasValue)
                return false;
            game.SetPlayerDecision(humanUserId, decision.Value);

            var aiPlayer = game.Players.First(p => p.UserId != humanUserId);
            game.SetPlayerDecision(aiPlayer.UserId, GetAiDecision());

            if (game.HaveDecisionOfEveryPlayer)
            {
                var turnResult = game.FinishTurn();
                turnRepo.Insert(turnResult);
            }

            ShowScore(game);
            gameRepo.Update(game);
            return true;
        }

        private GameEntity GetGameByUser(Guid userId)
        {
            var user = userRepo.FindById(userId) ?? throw new Exception($"Unknown user with id {userId}");
            var userCurrentGameId = user.CurrentGameId ?? throw new Exception($"No current game for user: {user}");
            return gameRepo.FindById(userCurrentGameId);
        }

        private PlayerDecision GetAiDecision()
        {
            return (PlayerDecision)Math.Min(3, 1 + random.Next(4));
        }

        private static PlayerDecision? AskHumanDecision()
        {
            Console.WriteLine();
            Console.WriteLine("Select your next decision:");
            Console.WriteLine("1 - Rock");
            Console.WriteLine("2 - Scissors");
            Console.WriteLine("3 - Paper");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == '1') return PlayerDecision.Rock;
                if (key.KeyChar == '2') return PlayerDecision.Scissors;
                if (key.KeyChar == '3') return PlayerDecision.Paper;
                if (key.Key == ConsoleKey.Escape) return null;
            }
        }

        private void ShowScore(GameEntity game)
        {
            var players = game.Players;

            Console.WriteLine();
            Console.WriteLine("Last turns:");

            var lastTurns = turnRepo.GetLastTurns(game, 5);
            foreach (var turn in lastTurns)
            {
                var fmtedTurn = turn.Format();
                Console.WriteLine(fmtedTurn);
            }
            Console.WriteLine();
            Console.WriteLine($"Score: {players[0].Name} {players[0].Score} : {players[1].Score} {players[1].Name}");
        }
    }
}
