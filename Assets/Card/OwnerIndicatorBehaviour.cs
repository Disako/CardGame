using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OwnerIndicatorBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        var cardState = GetComponentInParent<CardBehaviour>().State;

        var renderer = gameObject.GetComponent<MeshRenderer>();
        if (cardState.CurrentZone == Zone.InPlay)
        {
            renderer.enabled = true;
            renderer.material.color = cardState.Owner == Team.Opponent ? Color.red : Color.blue;
        }
        else
        {
            renderer.enabled = false;
        }
	}
}
