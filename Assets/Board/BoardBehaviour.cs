using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using UnityEngine;

public class BoardBehaviour : MonoBehaviour {
    public static object DragStart;

    private IGameEngine GameEngine;
    private AI AI;

    public DeckBehaviour PlayerDeck;
    public DeckBehaviour OpponentDeck;

	// Use this for initialization
	void Start ()
    {
        var gameEngine = new LocalGameEngine();
        GameEngine = gameEngine;
        AI = new AI(gameEngine);
        State = GameEngine.GetInitialGameState();
        PlayerDeck.Initialize(State.DeckStates.Single(s => s.Owner == Team.Player), GameEngine);
        OpponentDeck.Initialize(State.DeckStates.Single(s => s.Owner == Team.Opponent), GameEngine);
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButtonUp(0))
        {
            if(DragStart is CardBehaviour)
            {
                var card = DragStart as CardBehaviour;
                if(card.State.CurrentZone == Zone.Hand && card.State.Owner == Team.Player)
                {
                    RaycastHit hit;
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if(Physics.Raycast(ray,out hit))
                    {
                        var pointClicked = hit.point;
                        if(hit.collider.GetComponent<BoardBehaviour>() != null)
                        {
                            var square = GetTargetSquare(pointClicked.x, pointClicked.z);

                            if(card.State.Owner == Team.Player && card.State.CurrentZone == Zone.Hand && !State.LocationHasCreature(square.X, square.Y))
                            {
                                GameEngine.PlayCreature(card.State.ID, square.X, square.Y, square.Facing);
                            }
                        }
                    }
                }
            }
            DragStart = null;
        }
    }

    private SquareTarget GetTargetSquare(float xTar, float zTar)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                var distanceFromLeft = xTar - (x - 1.5f) * SPACE_WIDTH - SPACE_CENTER_X;
                var distanceFromRight = (x - 0.5f) * SPACE_WIDTH + SPACE_CENTER_X - xTar;
                var distanceFromBottom = zTar - (z - 1.5f) * SPACE_HEIGHT - SPACE_CENTER_Z;
                var distanceFromTop = (z - 0.5f) * SPACE_HEIGHT + SPACE_CENTER_Z - zTar;
                if (distanceFromLeft > 0
                    && distanceFromRight > 0
                    && distanceFromTop > 0
                    && distanceFromBottom > 0)
                {
                    FacingDirection facing;
                    if(distanceFromLeft < distanceFromRight && distanceFromLeft < distanceFromTop && distanceFromLeft < distanceFromBottom)
                    {
                        facing = FacingDirection.Left;
                    }
                    else if(distanceFromRight < distanceFromTop && distanceFromRight < distanceFromBottom)
                    {
                        facing = FacingDirection.Right;
                    }
                    else if(distanceFromTop < distanceFromBottom)
                    {
                        facing = FacingDirection.Up;
                    }
                    else
                    {
                        facing = FacingDirection.Down;
                    }
                    return new SquareTarget()
                    {
                        X = x,
                        Y = z,
                        Facing = facing
                    };
                }
            }
        }
        return null;
    }

    private class SquareTarget { public int X; public int Y; public FacingDirection Facing; }

    public static float SPACE_HEIGHT = 1.1f;
    public static float SPACE_WIDTH = 1.1f;
    public static float SPACE_CENTER_X = 0f;
    public static float SPACE_CENTER_Z = -0.5f;

    public GameState State;
}




