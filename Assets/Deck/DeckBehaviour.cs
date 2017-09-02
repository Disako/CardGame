using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckBehaviour : MonoBehaviour {
    private void SetupTexture()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector2[] uvs = new Vector2[mesh.vertices.Length];
        // Side
        uvs[0] = new Vector2(0.0f, 0.5f);
        uvs[1] = new Vector2(1f, 0.5f);
        uvs[2] = new Vector2(0.0f, 1f);
        uvs[3] = new Vector2(1f, 1f);
        // Face
        uvs[9] = new Vector2(0.5f, 0.5f);
        uvs[8] = new Vector2(1f, 0.5f);
        uvs[5] = new Vector2(0.5f, 0.0f);
        uvs[4] = new Vector2(1f, 0.0f);
        // Side
        uvs[6] = new Vector2(0.0f, 0.5f);
        uvs[7] = new Vector2(1f, 0.5f);
        uvs[10] = new Vector2(0.0f, 1f);
        uvs[11] = new Vector2(1f, 1f);
        // Back
        uvs[12] = new Vector2(0.5f, 0.0f);
        uvs[13] = new Vector2(0.5f, 0.5f);
        uvs[14] = new Vector2(1f, 0.5f);
        uvs[15] = new Vector2(1f, 0.0f);
        // Side
        uvs[17] = new Vector2(0.0f, 0.5f);
        uvs[18] = new Vector2(1f, 0.5f);
        uvs[19] = new Vector2(0.0f, 1f);
        uvs[16] = new Vector2(1f, 1f);
        // Side        
        uvs[21] = new Vector2(0.0f, 0.5f);
        uvs[22] = new Vector2(1f, 0.5f);
        uvs[23] = new Vector2(0.0f, 1f);
        uvs[20] = new Vector2(1f, 1f);
        mesh.uv = uvs;
    }
    // Use this for initialization
    void Start () {
        SetupTexture();
    }

    public void Initialize(PlayerState state, IGameEngine gameEngine)
    {
        GameEngine = gameEngine;
        SetState(state);
        GameEngine.GameStateChanged += GameEngine_GameStateChanged;
    }

    private void GameEngine_GameStateChanged(object sender, GameStateEventArgs e)
    {
        SetState(e.State.PlayerStates.Single(s => s.Owner == State.Owner));
    }

    private void SetState(PlayerState state)
    {
        foreach(var card in state.KnownCards)
        {
            bool found = false;
            foreach(var existingCard in FindObjectsOfType<CardBehaviour>())
            {
                if(card.ID == existingCard.State.ID)
                {
                    found = true;
                    existingCard.SetState(card);
                    break;
                }
            }
            if(!found)
            {
                var newCard = Instantiate(CardTemplate).GetComponent<CardBehaviour>();
                newCard.Initialize(card, this);
            }
        }
        var cardsToRemove = new List<CardBehaviour>();
        
        foreach(var existingCard in FindObjectsOfType<CardBehaviour>())
        {
            if(existingCard.State.Owner == state.Owner && !state.KnownCards.Any(c => c.ID == existingCard.State.ID))
            {
                cardsToRemove.Add(existingCard);
            }
        }
        foreach(var card in cardsToRemove)
        {
            Destroy(card);
        }
        State = state;
    }

    // Update is called once per frame
    void Update () {
        transform.position = new Vector3(transform.position.x, -0.5f + GetDeckHeight(), transform.position.z);
	}

    public float GetDeckHeight()
    {
        if (GetCardsInDeck() == 0) return -1f;
        return Math.Min(0.005f * GetCardsInDeck(), 1f);
    }

    public PlayerState State { get; private set; }

    private void OnMouseDown()
    {
        if(State.Owner == Team.Player)
            DrawCard();
    }

    public GameObject CardTemplate;

    public void DrawCard()
    {
        if (State.CardsInDeck > 0)
        {
            GameEngine.DrawCard();
        }
    }

    private IGameEngine GameEngine;

    public int GetCardsInHand()
    {
        if (State == null) return 0;
        return State.KnownCards.Count(c => c.CurrentZone == Zone.Hand);
    }

    public int GetCardsInDeck()
    {
        if (State == null) return 0;
        return State.CardsInDeck;
    }
}




    
