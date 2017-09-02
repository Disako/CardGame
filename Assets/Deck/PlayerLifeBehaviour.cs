using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLifeBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    public GameObject Deck;
	
	// Update is called once per frame
	void Update () {
        if (Deck != null)
        {
            var deck = Deck.GetComponent<DeckBehaviour>();

            var text = gameObject.GetComponent<UnityEngine.UI.Text>();
            
            if(text != null)
                text.text = "Life: " + deck.State.Life;
        }
	}
}
