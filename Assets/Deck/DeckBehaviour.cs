using System;
using System.Collections;
using System.Collections.Generic;
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
	
	// Update is called once per frame
	void Update () {
        transform.position = new Vector3(transform.position.x, GetDeckHeight(), transform.position.z);
	}

    public float GetDeckHeight()
    {
        if (GetCardsInDeck() == 0) return -1f;
        return -0.5f + Math.Min(0.005f * GetCardsInDeck(), 1f);
    }

    public bool IsPlayer;
    public Team Owner
    {
        get { return IsPlayer ? Team.Player : Team.Opponent; }
        set { IsPlayer = value == Team.Player; }
    }

    private void OnMouseDown()
    {
        if(Owner == Team.Player)
            DrawCard();
    }

    public GameObject CardTemplate;

    public void DrawCard()
    {
        if (CardsInDeck > 0)
        {
            var card = Instantiate(CardTemplate).GetComponent<CardBehaviour>();
            card.Initialize(CardBehaviour.Zone.Deck, this);
            card.SetLocation(CardBehaviour.Zone.Hand, GetCardsInHand());
            CardsInHand++;
            CardsInDeck--;
        }
    }

    private int CardsInDeck = 60;
    private int CardsInHand = 0;

    public int GetCardsInHand()
    {
        return CardsInHand;
    }

    public int GetCardsInDeck()
    {
        return CardsInDeck;
    }
}
