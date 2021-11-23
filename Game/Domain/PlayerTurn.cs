using System;

namespace Game.Domain
{
    public class PlayerTurn
    {
        public string PlayerName { get; private set; }
        public PlayerDecision Decision { get; private set; }

        public PlayerTurn(string playerName, PlayerDecision decision)
        {
            PlayerName = playerName;
            Decision = decision;
        }
    }
}