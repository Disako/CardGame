using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PlayerState
{
    public int CardsInDeck = 60;
    public Team Owner;
    public List<CardState> KnownCards;
    public int Life = 10;

    public PlayerState Clone()
    {
        var newState = new PlayerState()
        {
            CardsInDeck = CardsInDeck,
            Owner = Owner,
            KnownCards = new List<CardState>(),
            Life = Life
        };
        foreach (var knownCard in KnownCards)
        {
            newState.KnownCards.Add(knownCard.Clone());
        }
        return newState;
    }
}