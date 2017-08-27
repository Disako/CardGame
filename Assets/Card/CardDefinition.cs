using UnityEngine;
using UnityEditor;


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

public struct Stats { public int Top; public int Bottom; public int Left; public int Right; }