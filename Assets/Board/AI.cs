using UnityEngine;
using UnityEditor;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class AI
{
    public AI(LocalGameEngine engine, Team team = Team.Opponent)
    {
        GameEngine = engine;
        Team = team;
        engine.ReverseCensorGameStateChanged += Engine_GameStateChanged;
    }

    private void Engine_GameStateChanged(object sender, GameStateEventArgs e)
    {
        if (e.State.CurrentPlayer == Team)
        {
            var logicWorker = new BackgroundWorker();
            logicWorker.DoWork += LogicWorker_DoWork;
            logicWorker.RunWorkerCompleted += LogicWorker_RunWorkerCompleted;
            logicWorker.RunWorkerAsync(e.State);
        }
    }

    private void LogicWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        var bestAction = (PotentialAction)e.Result;
        if (bestAction != null)
            bestAction.DoAction();
    }

    private void LogicWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        var gameState = (GameState)e.Argument;
        var logicStart = DateTime.Now;
        var potentialActions = GetPossibleActions(gameState);

        PotentialAction bestAction = null;
        int bestScore = int.MinValue;

        foreach (var potentialAction in potentialActions)
        {
            var score = ScoreGameState(potentialAction.TryAction(gameState));
            if (score > bestScore || bestAction == null)
            {
                bestScore = score;
                bestAction = potentialAction;
            }
        }

        var logicEnd = DateTime.Now;
        var logicTime = logicEnd.Subtract(logicStart).Milliseconds;

        // If logic is less than a second, keep waiting until the second is over to give impression of AI thinking
        if (logicTime < 1000)
            Thread.Sleep((int)((1000 - logicTime)));

        e.Result = bestAction;
    }

    private List<PotentialAction> GetPossibleActions(GameState currentState)
    {
        var potentialActions = new List<PotentialAction>();
        if (currentState.DeckStates.Single(d => d.Owner == Team).CardsInDeck > 0)
            potentialActions.Add(new DrawCardAction(GameEngine, Team));
        potentialActions.Add(new DoCombatAction(GameEngine, Team));

        foreach (var card in currentState.DeckStates.SingleOrDefault(d => d.Owner == Team).KnownCards.Where(c => c.CurrentZone == Zone.Hand))
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (!currentState.LocationHasCreature(x, y))
                    {
                        foreach (FacingDirection facing in Enum.GetValues(typeof(FacingDirection)))
                        {
                            potentialActions.Add(new PlayCreatureAction(GameEngine, Team, card, x, y, facing));
                        }
                    }
                }
            }
        }

        return potentialActions;
    }

    private abstract class PotentialAction
    {
        public PotentialAction(LocalGameEngine gameEngine, Team team)
        {
            GameEngine = gameEngine;
            Team = team;
        }

        protected readonly LocalGameEngine GameEngine;
        protected readonly Team Team;

        public abstract GameState TryAction(GameState state);
        public abstract void DoAction();
    }

    private class PlayCreatureAction : PotentialAction
    {
        public PlayCreatureAction(LocalGameEngine gameEngine, Team team, CardState card, int xPos, int yPos, FacingDirection facing) : base(gameEngine, team)
        {
            Card = card;
            XPos = xPos;
            YPos = yPos;
            Facing = facing;
        }

        private readonly CardState Card;
        private readonly int XPos, YPos;
        private readonly FacingDirection Facing;

        public override GameState TryAction(GameState state)
        {
            return GameEngine.StateIfDoCombat(GameEngine.StateIfPlayCreature(state, Card, XPos, YPos, Facing));
        }

        public override void DoAction()
        {
            GameEngine.PlayCreature(Card, XPos, YPos, Facing);
        }
    }

    private class DoCombatAction : PotentialAction
    {
        public DoCombatAction(LocalGameEngine gameEngine, Team team) : base(gameEngine, team) { }

        public override GameState TryAction(GameState state)
        {
            return GameEngine.StateIfDoCombat(state);
        }

        public override void DoAction()
        {
            GameEngine.DoCombat(Team);
        }
    }

    private class DrawCardAction : PotentialAction
    {
        public DrawCardAction(LocalGameEngine gameEngine, Team team) : base(gameEngine, team) { }

        public override GameState TryAction(GameState state)
        {
            return GameEngine.StateIfDoCombat(GameEngine.StateIfDrawCard(state, Team));
        }

        public override void DoAction()
        {
            GameEngine.DrawCard(Team);
        }
    }

    private System.Random RandomForScoring = new System.Random();

    private int ScoreGameState(GameState state)
    {
        int score = 0;
        foreach(var deck in state.DeckStates)
        {
            int cardInHandScore = 10;
            var multiplier = deck.Owner == Team ? 1 : -1;
            foreach (var card in deck.KnownCards)
            {
                switch (card.CurrentZone)
                {
                    case Zone.Hand:
                        score += cardInHandScore * multiplier;
                        cardInHandScore--;
                        break;
                    case Zone.InPlay:
                        score += 10 * multiplier;
                        break;
                }
            }
        }
        return score;
    }

    private readonly LocalGameEngine GameEngine;
    private readonly Team Team;
}