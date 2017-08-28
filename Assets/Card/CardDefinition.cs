using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Collections;

public class CardDefinition
{
    public Stats BaseStats;
    public string Text;

    public CardDefinition Clone()
    {
        return new CardDefinition()
        {
            BaseStats = BaseStats,
            Text = Text
        };
    }
}

public struct Stats
{
    public int Top;
    public int Bottom;
    public int Left;
    public int Right;
    
    public int this[FacingDirection key]
    {
        get
        {
            switch (key)
            {
                case FacingDirection.Up:
                    return Top;
                case FacingDirection.Left:
                    return Left;
                case FacingDirection.Right:
                    return Right;
                case FacingDirection.Down:
                    return Bottom;
                default:
                    throw new Exception("Invalid facing direction");
            }
        }
        set
        {
            switch (key)
            {
                case FacingDirection.Up:
                    Top = value;
                    break;
                case FacingDirection.Left:
                    Left = value;
                    break;
                case FacingDirection.Right:
                    Right = value;
                    break;
                case FacingDirection.Down:
                    Bottom = value;
                    break;
                default:
                    throw new Exception("Invalid facing direction");
            }
        }
    }

}