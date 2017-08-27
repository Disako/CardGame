using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DeckState
{
    public int CardsInDeck = 60;
    public Team Owner;
    public List<CardState> KnownCards;

    public DeckState Clone()
    {
        var newState = new DeckState()
        {
            CardsInDeck = CardsInDeck,
            Owner = Owner,
            KnownCards = new List<CardState>()
        };
        foreach (var knownCard in KnownCards)
        {
            newState.KnownCards.Add(knownCard.Clone());
        }
        return newState;
    }
}