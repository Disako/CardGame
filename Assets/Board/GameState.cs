using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class GameState
{
    public bool LocationHasCreature(int xPos, int yPos)
    {
        return PlayerStates.Any(d => d.KnownCards.Any(c => c.CurrentZone == Zone.InPlay && c.XIndex == xPos && c.YIndex == yPos));
    }

    public List<PlayerState> PlayerStates;
    public Team CurrentPlayer;

    public GameState Clone()
    {
        var newState = new GameState()
        {
            PlayerStates = new List<PlayerState>(),
            CurrentPlayer = CurrentPlayer
        };
        foreach (var deckState in PlayerStates)
        {
            newState.PlayerStates.Add(deckState.Clone());
        }
        return newState;
    }
}