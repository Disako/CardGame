using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
        var newState = new GameState()
        {
            DeckStates = new List<DeckState>(),
            CurrentPlayer = CurrentPlayer
        };
        foreach (var deckState in DeckStates)
        {
            newState.DeckStates.Add(deckState.Clone());
        }
        return newState;
    }
}