using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardBehaviour : MonoBehaviour {
    
    private IGameEngine GameEngine;

    public DeckBehaviour PlayerDeck;
    public DeckBehaviour OpponentDeck;

	// Use this for initialization
	void Start ()
    {
        GameEngine = new LocalGameEngine();
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

    public GameState Clone()
    {
        var newState = new GameState() {
            DeckStates = new List<DeckState>()
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

public class LocalGameEngine : IGameEngine
{
    public LocalGameEngine()
    {
        CurrentGameState = new GameState()
        {
            DeckStates = new List<DeckState>()
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
                    }
                },
                Text = "What the card does, this may take multiple lines.",
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
                    }
                },
                Text = "What the card does, this may take multiple lines.",
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
    }

    public void DrawCard()
    {
        if (CardsInPlayerDeck.Count > 0)
        {
            DrawCard(Team.Player);
        }
    }

    private void DrawCard(Team owner)
    {
        var drawnCard = GetCardsInDeck(owner)[0];
        drawnCard.CurrentZone = Zone.Hand;
        drawnCard.XIndex = CurrentGameState.DeckStates.Single(s => s.Owner == owner).KnownCards.Count(c => c.CurrentZone == Zone.Hand);
        CardsInPlayerDeck.RemoveAt(0);
        CurrentGameState.DeckStates.Single(s => s.Owner == owner).KnownCards.Add(drawnCard);
        CurrentGameState.DeckStates.Single(s => s.Owner == owner).CardsInDeck--;

        OnGameStateChanged();
    }

    private GameState GetCensoredGameState()
    {
        var state = CurrentGameState.Clone();
        foreach (var opponentDeck in state.DeckStates.Where(d => d.Owner == Team.Opponent))
        {
            foreach(var cardInHand in opponentDeck.KnownCards.Where(c => c.CurrentZone == Zone.Deck))
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

    public event EventHandler<GameStateEventArgs> GameStateChanged;
}