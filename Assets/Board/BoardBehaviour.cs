using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using UnityEngine;

public class BoardBehaviour : MonoBehaviour {
    
    private IGameEngine GameEngine;
    private AI AI;

    public DeckBehaviour PlayerDeck;
    public DeckBehaviour OpponentDeck;

	// Use this for initialization
	void Start ()
    {
        var gameEngine = new LocalGameEngine();
        GameEngine = gameEngine;
        AI = new AI(gameEngine);
        State = GameEngine.GetInitialGameState();
        PlayerDeck.Initialize(State.DeckStates.Single(s => s.Owner == Team.Player), GameEngine);
        OpponentDeck.Initialize(State.DeckStates.Single(s => s.Owner == Team.Opponent), GameEngine);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public GameState State;
}

public class GameState
{
    public List<DeckState> DeckStates;
    public Team CurrentPlayer;

    public GameState Clone()
    {
        var newState = new GameState() {
            DeckStates = new List<DeckState>(),
            CurrentPlayer = CurrentPlayer
        };
        foreach(var deckState in DeckStates)
        {
            newState.DeckStates.Add(deckState.Clone());
        }
        return newState;
    }
}

public class GameStateEventArgs : EventArgs
{
    public GameStateEventArgs(GameState state) { State = state; }

    public readonly GameState State;
}

public interface IGameEngine
{
    GameState GetInitialGameState();
    event EventHandler<GameStateEventArgs> GameStateChanged;
    void DrawCard();
}

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

    private class DrawCardAction : PotentialAction
    {
        public DrawCardAction(LocalGameEngine gameEngine, Team team) : base(gameEngine, team) { }

        public override GameState TryAction(GameState state)
        {
            return GameEngine.StateIfDrawCard(state, Team);
        }

        public override void DoAction()
        {
            GameEngine.DrawCard(Team);
        }
    }

    private int ScoreGameState(GameState state)
    {
        return 1;
    }

    private readonly LocalGameEngine GameEngine;
    private readonly Team Team;
}

public class LocalGameEngine : IGameEngine
{
    public LocalGameEngine()
    {
        CurrentGameState = new GameState()
        {
            DeckStates = new List<DeckState>(),
            CurrentPlayer = (Team)UnityEngine.Random.Range((int)Team.Player, (int)Team.Opponent)
        };
        CurrentGameState.DeckStates.Add(new DeckState()
        {
            KnownCards = new List<CardState>(),
            CardsInDeck = 60,
            Owner = Team.Player
        });
        CurrentGameState.DeckStates.Add(new DeckState()
        {
            KnownCards = new List<CardState>(),
            CardsInDeck = 60,
            Owner = Team.Opponent
        });
        for (int i = 0; i < 60; i++)
        {
            CardsInPlayerDeck.Add(new CardState()
            {
                Owner = Team.Player,
                Definition = new CardDefinition()
                {
                    BaseStats = new Stats()
                    {
                        Top = UnityEngine.Random.Range(1, 10),
                        Left = UnityEngine.Random.Range(1, 10),
                        Right = UnityEngine.Random.Range(1, 10),
                        Bottom = UnityEngine.Random.Range(1, 10)
                    },
                    Text = "What the card does, this may take multiple lines."
                },
                ID = Guid.NewGuid(),
                CurrentZone = Zone.Deck
            });
            CardsInOpponentDeck.Add(new CardState()
            {
                Owner = Team.Opponent,
                Definition = new CardDefinition()
                {
                    BaseStats = new Stats()
                    {
                        Top = UnityEngine.Random.Range(1, 10),
                        Left = UnityEngine.Random.Range(1, 10),
                        Right = UnityEngine.Random.Range(1, 10),
                        Bottom = UnityEngine.Random.Range(1, 10)
                    },
                    Text = "What the card does, this may take multiple lines."
                },
                ID = Guid.NewGuid(),
                CurrentZone = Zone.Deck
            });
        }
    }

    private GameState CurrentGameState;

    private void OnGameStateChanged()
    {
        if (GameStateChanged != null)
            GameStateChanged(this, new GameStateEventArgs(GetCensoredGameState()));
        if (ReverseCensorGameStateChanged != null)
            ReverseCensorGameStateChanged(this, new GameStateEventArgs(GetCensoredGameState(Team.Opponent)));
    }

    public GameState StateIfDrawCard(GameState oldState, Team owner)
    {
        var newState = oldState.Clone();
        var drawnCard = GetCardsInDeck(owner)[0];
        drawnCard.CurrentZone = Zone.Hand;
        drawnCard.XIndex = newState.DeckStates.Single(s => s.Owner == owner).KnownCards.Count(c => c.CurrentZone == Zone.Hand);
        
        newState.DeckStates.Single(s => s.Owner == owner).KnownCards.Add(drawnCard);
        newState.DeckStates.Single(s => s.Owner == owner).CardsInDeck--;

        newState.CurrentPlayer = (Team)(((int)owner + 1) % 2);
        return newState;
    }

    public void DrawCard()
    {
        DrawCard(Team.Player);
    }

    public void DrawCard(Team owner)
    {
        if (GetCardsInDeck(owner).Count > 0 && CurrentGameState.CurrentPlayer == owner)
        {
            CurrentGameState = StateIfDrawCard(CurrentGameState, owner);

            GetCardsInDeck(owner).RemoveAt(0);

            OnGameStateChanged();
        }
    }

    private GameState GetCensoredGameState(Team team = Team.Player)
    {
        var state = CurrentGameState.Clone();
        foreach (var opponentDeck in state.DeckStates.Where(d => d.Owner != team))
        {
            foreach(var cardInHand in opponentDeck.KnownCards.Where(c => c.CurrentZone == Zone.Hand))
            {
                cardInHand.Definition = null;
            }
        }
        return state;
    }

    public GameState GetInitialGameState()
    {
        return GetCensoredGameState();
    }

    private List<CardState> GetCardsInDeck(Team owner)
    {
        if(owner == Team.Player)
        {
            return CardsInPlayerDeck;
        }
        else
        {
            return CardsInOpponentDeck;
        }
    }
    
    private List<CardState> CardsInPlayerDeck = new List<CardState>();
    private List<CardState> CardsInOpponentDeck = new List<CardState>();

    public event EventHandler<GameStateEventArgs> ReverseCensorGameStateChanged;
    public event EventHandler<GameStateEventArgs> GameStateChanged;
}