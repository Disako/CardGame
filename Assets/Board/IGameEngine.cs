using UnityEngine;
using UnityEditor;
using System;

public interface IGameEngine
{
    void PlayCreature(Guid cardID, int xPos, int yPos, FacingDirection facing);
    GameState GetInitialGameState();
    event EventHandler<GameStateEventArgs> GameStateChanged;
    void DrawCard();
}

public class GameStateEventArgs : EventArgs
{
    public GameStateEventArgs(GameState state) { State = state; }

    public readonly GameState State;
}