using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public void DoCombat()
    {
        DoCombat(Team.Player);
    }

    public void DoCombat(Team owner)
    {
        if (owner == CurrentGameState.CurrentPlayer)
        {
            CurrentGameState = StateIfDoCombat(CurrentGameState);
            OnGameStateChanged();
        }
    }

    public GameState StateIfDoCombat(GameState originalState)
    {
        var newState = originalState.Clone();

        var cards = new CardState[3, 3];
        for (int x=0;x<3;x++)
        {
            for(int y=0;y<3;y++)
            {
                cards[x, y] = GetCardAtCoordinates(newState, x, y);
            }
        }
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (cards[x, y] != null &&
                    ((x > 0 && cards[x - 1, y] != null && cards[x - 1, y].Owner != cards[x, y].Owner && GetStatInDirection(cards[x - 1, y], FacingDirection.Right) > GetStatInDirection(cards[x, y], FacingDirection.Left))
                    || (x < 2 && cards[x + 1, y] != null && cards[x + 1, y].Owner != cards[x, y].Owner && GetStatInDirection(cards[x + 1, y], FacingDirection.Left) > GetStatInDirection(cards[x, y], FacingDirection.Right))
                    || (y > 0 && cards[x, y - 1] != null && cards[x, y - 1].Owner != cards[x, y].Owner && GetStatInDirection(cards[x, y - 1], FacingDirection.Up) > GetStatInDirection(cards[x, y], FacingDirection.Down))
                    || (y < 2 && cards[x, y + 1] != null && cards[x, y + 1].Owner != cards[x, y].Owner && GetStatInDirection(cards[x, y + 1], FacingDirection.Down) > GetStatInDirection(cards[x,y],FacingDirection.Up))))
                {
                    cards[x, y].CurrentZone = Zone.Discard;
                }
            }
        }

        foreach(var card in cards)
        {
            if(card != null && card.CurrentZone == Zone.Discard)
            {
                card.Facing = FacingDirection.Up;
                card.YIndex = 0;
                card.XIndex = int.MaxValue;
            }
        }

        foreach(var deck in newState.DeckStates)
        {
            int xIndex = 0;
            foreach(var card in deck.KnownCards.Where(c => c.CurrentZone == Zone.Discard).OrderBy(c => c.XIndex).ToArray())
            {
                card.XIndex = xIndex;
                xIndex++;
            }
        }

        SwapPlayers(newState);

        return newState;
    }



    private int GetStatInDirection(CardState state, FacingDirection facing)
    {
        var stats = state.GetActualStats();
        return stats[(FacingDirection)(((int)facing - (int)state.Facing + 4) % 4)];
    }

    private CardState GetCardAtCoordinates(GameState state, int x, int y)
    {
        return state.DeckStates.Select(d => d.KnownCards.FirstOrDefault(c => c.CurrentZone == Zone.InPlay && c.XIndex == x && c.YIndex == y)).FirstOrDefault(c => c != null);
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
            foreach (var cardInHand in opponentDeck.KnownCards.Where(c => c.CurrentZone == Zone.Hand))
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
        if (owner == Team.Player)
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