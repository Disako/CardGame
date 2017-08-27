using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using UnityEngine;

public class BoardBehaviour : MonoBehaviour {
    public static object DragStart;

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
        if (Input.GetMouseButtonUp(0))
        {
            if(DragStart is CardBehaviour)
            {
                var card = DragStart as CardBehaviour;
                if(card.State.CurrentZone == Zone.Hand && card.State.Owner == Team.Player)
                {
                    RaycastHit hit;
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if(Physics.Raycast(ray,out hit))
                    {
                        var pointClicked = hit.point;
                        if(hit.collider.GetComponent<BoardBehaviour>() != null)
                        {
                            var square = GetTargetSquare(pointClicked.x, pointClicked.z);

                            if(card.State.Owner == Team.Player && card.State.CurrentZone == Zone.Hand && !State.LocationHasCreature(square.X, square.Y))
                            {
                                GameEngine.PlayCreature(card.State.ID, square.X, square.Y, square.Facing);
                            }
                        }
                    }
                }
            }
            DragStart = null;
        }
    }

    private SquareTarget GetTargetSquare(float xTar, float zTar)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                var distanceFromLeft = xTar - (x - 1.5f) * SPACE_WIDTH - SPACE_CENTER_X;
                var distanceFromRight = (x - 0.5f) * SPACE_WIDTH + SPACE_CENTER_X - xTar;
                var distanceFromBottom = zTar - (z - 1.5f) * SPACE_HEIGHT - SPACE_CENTER_Z;
                var distanceFromTop = (z - 0.5f) * SPACE_HEIGHT + SPACE_CENTER_Z - zTar;
                if (distanceFromLeft > 0
                    && distanceFromRight > 0
                    && distanceFromTop > 0
                    && distanceFromBottom > 0)
                {
                    FacingDirection facing;
                    if(distanceFromLeft < distanceFromRight && distanceFromLeft < distanceFromTop && distanceFromLeft < distanceFromBottom)
                    {
                        facing = FacingDirection.Left;
                    }
                    else if(distanceFromRight < distanceFromTop && distanceFromRight < distanceFromBottom)
                    {
                        facing = FacingDirection.Right;
                    }
                    else if(distanceFromTop < distanceFromBottom)
                    {
                        facing = FacingDirection.Up;
                    }
                    else
                    {
                        facing = FacingDirection.Down;
                    }
                    return new SquareTarget()
                    {
                        X = x,
                        Y = z,
                        Facing = facing
                    };
                }
            }
        }
        return null;
    }

    private class SquareTarget { public int X; public int Y; public FacingDirection Facing; }

    public static float SPACE_HEIGHT = 1.1f;
    public static float SPACE_WIDTH = 1.1f;
    public static float SPACE_CENTER_X = 0f;
    public static float SPACE_CENTER_Z = -0.5f;

    public GameState State;
}

public class GameState
{
    public bool LocationHasCreature(int xPos, int yPos)
    {
        return DeckStates.Any(d => d.KnownCards.Any(c => c.CurrentZone == Zone.InPlay && c.XIndex == xPos && c.YIndex == yPos));
    }

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
    void PlayCreature(Guid cardID, int xPos, int yPos, FacingDirection facing);
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

        foreach (var card in currentState.DeckStates.SingleOrDefault(d => d.Owner == Team).KnownCards.Where(c => c.CurrentZone == Zone.Hand))
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if(!currentState.LocationHasCreature(x,y))
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
            return GameEngine.StateIfPlayCreature(state, Card, XPos, YPos, Facing);
        }

        public override void DoAction()
        {
            GameEngine.PlayCreature(Card, XPos, YPos, Facing);
        }
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

    private System.Random RandomForScoring = new System.Random();

    private int ScoreGameState(GameState state)
    {
        return RandomForScoring.Next();
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

    private void ResetHandIndexes(DeckState deck)
    {
        int x = 0;
        foreach (var handCard in deck.KnownCards.Where(c => c.CurrentZone == Zone.Hand).OrderBy(c => c.XIndex).ToArray())
        {
            handCard.XIndex = x;
            x++;
        }
    }

    public GameState StateIfPlayCreature(GameState oldState, CardState card, int xPos, int yPos, FacingDirection facing)
    {
        var newState = oldState.Clone();

        var deck = newState.DeckStates.Single(d => d.Owner == card.Owner);
        var newCard = deck.KnownCards.SingleOrDefault(c => c.ID == card.ID);
        newCard.CurrentZone = Zone.InPlay;
        newCard.XIndex = xPos;
        newCard.YIndex = yPos;
        newCard.Facing = facing;

        ResetHandIndexes(deck);

        SwapPlayers(newState);
        return newState;
    }

    private void SwapPlayers(GameState state)
    {
        state.CurrentPlayer = (Team)(((int)state.CurrentPlayer + 1) % 2);
    }

    public GameState StateIfDrawCard(GameState oldState, Team owner)
    {
        var newState = oldState.Clone();
        var drawnCard = GetCardsInDeck(owner)[0];
        drawnCard.CurrentZone = Zone.Hand;
        drawnCard.XIndex = newState.DeckStates.Single(s => s.Owner == owner).KnownCards.Count(c => c.CurrentZone == Zone.Hand);
        
        newState.DeckStates.Single(s => s.Owner == owner).KnownCards.Add(drawnCard);
        newState.DeckStates.Single(s => s.Owner == owner).CardsInDeck--;
        
        SwapPlayers(newState);
        return newState;
    }

    public void PlayCreature(Guid cardID, int xPos, int yPos, FacingDirection facing)
    {
        var card = CurrentGameState.DeckStates.Single(d => d.Owner == Team.Player).KnownCards.SingleOrDefault(c => c.ID == cardID);
        if (card != null)
            PlayCreature(card, xPos, yPos, facing);
    }

    private bool PosInRange(int xPos, int yPos)
    {
        return xPos >= 0 && xPos < 3 && yPos >= 0 && yPos < 3;
    }

    public void PlayCreature(CardState card, int xPos, int yPos, FacingDirection facing)
    {
        if (card.CurrentZone == Zone.Hand && CurrentGameState.CurrentPlayer == card.Owner && PosInRange(xPos, yPos) && !CurrentGameState.LocationHasCreature(xPos, yPos))
        {
            CurrentGameState = StateIfPlayCreature(CurrentGameState, card, xPos, yPos, facing);
            OnGameStateChanged();
        }
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