using UnityEngine;
using UnityEditor;
using System;

public class CardState
{
    public Guid ID;
    public Team Owner;

    public Zone CurrentZone;

    public int XIndex;
    public int YIndex;
    public FacingDirection Facing;
    public CardDefinition Definition;

    public CardState Clone()
    {
        return new CardState()
        {
            ID = ID,
            Owner = Owner,
            CurrentZone = CurrentZone,
            XIndex = XIndex,
            YIndex = YIndex,
            Facing = Facing,
            Definition = Definition == null ? null : Definition.Clone()
        };
    }
}

public enum FacingDirection { Up = 0, Right = 1, Down = 2, Left = 3 }

public enum Zone
{
    Deck,
    Hand,
    Discard,
    InPlay,
    Banished
}