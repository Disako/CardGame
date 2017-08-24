using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardBehaviour : MonoBehaviour {

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
        uvs[9] = new Vector2(0.0f, 0.5f);
        uvs[8] = new Vector2(0.5f, 0.5f);
        uvs[5] = new Vector2(0.0f, 0.0f);
        uvs[4] = new Vector2(0.5f, 0.0f);
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

    public Shader textShader;

    private TextMesh AddText(float x, float y, TextAnchor anchor, int fontSize)
    {
        GameObject text = new GameObject();
        text.transform.parent = gameObject.transform;
        text.transform.localPosition = new Vector3(x, 1f, y);
        text.transform.localRotation = Quaternion.Euler(90, 0, 0);
        text.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        var mesh = text.AddComponent<TextMesh>();
        mesh.anchor = anchor;
        mesh.GetComponent<Renderer>().material.shader = textShader;
        mesh.fontSize = fontSize;
        return mesh;
    }

    private void WrapText(string text, TextMesh mesh, float rowLimit)
    {
        var renderer = mesh.GetComponent<Renderer>();
        string builder = "";
        mesh.text = "";   
        string[] parts = text.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            mesh.text += parts[i] + " ";
            if (renderer.bounds.extents.x > rowLimit)
            {
                mesh.text = builder.TrimEnd() + System.Environment.NewLine + parts[i] + " ";
            }
            builder = mesh.text;
        }
    }

    private void SetupText()
    {
        TopText = AddText(0, 0.5f, TextAnchor.UpperCenter, 32);
        LeftText = AddText(-0.49f, 0, TextAnchor.MiddleLeft, 32);
        RightText = AddText(0.49f, 0, TextAnchor.MiddleRight, 32);
        BottomText = AddText(0, -0.5f, TextAnchor.LowerCenter, 32);
        CardText = AddText(-0.43f, 0, TextAnchor.UpperLeft, 32);
        WrapText(State.Text, CardText, 0.4f);
    }

    private TextMesh TopText;
    private TextMesh LeftText;
    private TextMesh RightText;
    private TextMesh BottomText;
    private TextMesh CardText;

    public Stats GetActualStats()
    {
        if (State.Definition == null)
            return new Stats();
        return State.Definition.BaseStats;
    }

    // Use this for initialization
    void Start () {
        SetupTexture();
	}

    private static int lastMouseover = 0;

	// Update is called once per frame
	void Update () {
        if(IsFaceUp())
        {
            if (TopText == null) SetupText();

            var stats = GetActualStats();
            TopText.text = stats.Top.ToString();
            LeftText.text = stats.Left.ToString();
            BottomText.text = stats.Bottom.ToString();
            RightText.text = stats.Right.ToString();
        }
        if(MouseOver && !ActualMouseOver)
        {
            lastMouseover--;
            if (lastMouseover == 0)
                MouseOver = false;
        }
        else if (ActualMouseOver && lastMouseover == 0)
        {
            lastMouseover = 10;
            MouseOver = true;
        }
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0)
        {
            SetFocussed(false);
        }
        if (animationStep < animationTime)
        {
            animationStep += Time.deltaTime;
            if (animationStep > animationTime)
                animationStep = animationTime;
            var target = GetTargetPosition();
            currentPosition = new Position()
            {
                Location = LinearInterpolate(),
                Rotation = RotationalInterpolate()
            };
        }
        else if (!currentPosition.RoughlyEquals(GetTargetPosition()))
        {
            previousPosition = currentPosition;
            animationStep = 0;
            animationTime = 0.4f;
        }
    }

    public void SetFocussed(bool focussed)
    {
        if (focussed != Focussed)
        {
            if (focussed)
            {
                foreach (var card in FindObjectsOfType<CardBehaviour>())
                {
                    card.SetFocussed(false);
                }
            }
            previousPosition = currentPosition;
            animationStep = 0;
            animationTime = 0.4f;
            Focussed = focussed;
        }
    }

    public bool Focussed { get; private set; }

    private Quaternion RotationalInterpolate()
    {
        return Quaternion.Euler(RotationalInterpolate(c => c.x), RotationalInterpolate(c => c.y), RotationalInterpolate(c => c.z));
    }

    private float RotationalInterpolate(Func<Vector3, float> getCoordinate)
    {
        var old = getCoordinate(previousPosition.Rotation.eulerAngles);
        var target = getCoordinate(GetTargetPosition().Rotation.eulerAngles);

        var diff = target - old;

        while (diff < -180)
            diff += 360;
        while (diff > 180)
            diff -= 360;

        return old + diff * animationStep / animationTime;
    }

    private Vector3 LinearInterpolate()
    {
        return new Vector3(LinearInterpolate(c => c.x), LinearInterpolate(c => c.y), LinearInterpolate(c => c.z));
    }

    private float LinearInterpolate(Func<Vector3, float> getCoordinate)
    {
        var old = getCoordinate(previousPosition.Location);
        var target = getCoordinate(GetTargetPosition().Location);
        return old + (target - old) * animationStep / animationTime;
    }

    private void OnMouseOver()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0)
        {
            SetFocussed(true);
        }
    }

    public void SetState(CardState state)
    {
        if(state.CurrentZone != State.CurrentZone || state.Facing != State.Facing || state.XIndex != State.XIndex || state.YIndex != State.YIndex)
        {
            animationStep = 0;
            previousPosition = currentPosition;
            if (state.CurrentZone != State.CurrentZone)
                animationTime = 1f;
            else
                animationTime = 0.4f;
        }
        State = state;
    }

    private DeckBehaviour GetDeck()
    {
        return FindObjectsOfType<DeckBehaviour>().SingleOrDefault(d => d.State.Owner == State.Owner);
    }

    private int GetCardsInHand()
    {
        var deck = GetDeck();
        if (deck == null) return 0;
        return deck.GetCardsInHand();
    }

    private int GetCardsInDiscard()
    {
        return 1;
    }

    private int GetBanishedCards()
    {
        return 1;
    }

    private double GetDeckHeight()
    {
        return Deck.GetDeckHeight();
    }

    private DeckBehaviour Deck;

    public void Initialize(CardState state, DeckBehaviour deck)
    {
        Deck = deck;
        animationTime = 1f;
        State = state;
        currentPosition = GetTargetPosition();
        previousPosition = GetTargetPosition(Zone.Deck);
        animationStep = 0;
    }

    private Position previousPosition;
    private Position currentPosition
    {
        get
        {
            return new Position()
            {
                Location = transform.position,
                Rotation = transform.rotation
            };
        }
        set
        {
            transform.SetPositionAndRotation(value.Location, value.Rotation);
        }
    }

    private bool IsFaceUp()
    {
        return State.CurrentZone != Zone.Deck && (State.Owner == Team.Player || State.CurrentZone != Zone.Hand);
    }

    private bool MouseOver = false;
    private bool ActualMouseOver = false;

    private void OnMouseEnter()
    {
        ActualMouseOver = true;
    }

    private void OnMouseExit()
    {
        ActualMouseOver = false;
    }

    private Position GetTargetPosition()
    {
        return GetTargetPosition(State.CurrentZone);
    }

    private Position GetTargetPosition(Zone zone)
    { 
        if(Focussed && IsFaceUp())
        {
            return new Position()
            {
                Location = new Vector3(0, 5.29f, -4.12f),
                Rotation = Quaternion.Euler(-20, 0, 0)
            };
        }
        switch(zone)
        {
            case Zone.Hand:
                var cardsInHand = GetCardsInHand();
                if (cardsInHand <= 3)
                {
                    if (State.Owner == Team.Player)
                        return new Position()
                        {
                            Location = new Vector3(State.XIndex * 0.9f - (cardsInHand - 1) * 0.45f, 2.76f + (MouseOver ? 0.2f : 0.0f) + transform.localScale.y * State.XIndex, -4.17f - transform.localScale.y * State.XIndex + (MouseOver ? 0.2f : 0f)),
                            Rotation = Quaternion.Euler(-20, 0, 0)
                        };
                    else
                        return new Position()
                        {
                            Location = new Vector3(State.XIndex * 0.9f - (cardsInHand - 1) * 0.45f, 0.59f + transform.localScale.y * State.XIndex, 1.64f - transform.localScale.y * State.XIndex),
                            Rotation = Quaternion.Euler(260, 0, 180)
                        };
                }
                else
                {
                    if (State.Owner == Team.Player)
                        return new Position()
                        {
                            Location = new Vector3(-1.346f + 2.692f * State.XIndex / (cardsInHand - 1), 2.76f + (MouseOver ? 0.2f : 0.0f) + transform.localScale.y * State.XIndex, -4.17f - transform.localScale.y * State.XIndex + (MouseOver ? 0.2f : 0f)),
                            Rotation = Quaternion.Euler(-20, 0, 0)
                        };
                    else
                        return new Position()
                        {
                            Location = new Vector3(-1.346f + 2.692f * State.XIndex / (cardsInHand - 1), 0.59f + transform.localScale.y * State.XIndex, 1.64f - transform.localScale.y * State.XIndex),
                            Rotation = Quaternion.Euler(260, 0, 180)
                        };
                    throw new NotImplementedException();//-1.346f to 1.346
                }
            case Zone.Deck:
                if (State.Owner == Team.Player)
                    return new Position()
                    {
                        Location = new Vector3(-3.65f, (float)GetDeckHeight(), -2.73f),
                        Rotation = Quaternion.Euler(0, 0, 180)
                    };
                else
                    return new Position()
                    {
                        Location = new Vector3(4.08f, (float)GetDeckHeight(), 2.026f),
                        Rotation = Quaternion.Euler(0, 0, 180)
                    };
            case Zone.InPlay:
                return new Position()
                {
                    Location = new Vector3((State.XIndex - 1) * 1.1f, transform.localScale.y, (State.YIndex - 1f) * 1.1f - 0.5f),
                    Rotation = Quaternion.Euler(0, 90 * (int)State.Facing, 0)
                };
            case Zone.Discard:
                if (State.Owner == Team.Player)
                    return new Position()
                    {
                        Location = new Vector3(3.65f - 0.1f * Math.Min(10, GetCardsInDiscard() - State.XIndex - 1), (State.XIndex + 1) * transform.localScale.y, -2.73f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
                else
                    return new Position()
                    {
                        Location = new Vector3(-4.08f + 0.1f * Math.Min(10, GetCardsInDiscard() - State.XIndex - 1), (State.XIndex + 1) * transform.localScale.y, 2.026f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
            case Zone.Banished:
                if (State.Owner == Team.Player)
                    return new Position()
                    {
                        Location = new Vector3(2.55f - 0.1f * Math.Min(10, GetCardsInDiscard()) - 0.1f * Math.Min(10, GetBanishedCards() - State.XIndex - 1), (State.XIndex + 1) * transform.localScale.y, -2.73f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
                else
                    return new Position()
                    {
                        Location = new Vector3(-2.98f + 0.1f * Math.Min(10, GetCardsInDiscard()) + 0.1f * Math.Min(10, GetCardsInDiscard() - State.XIndex - 1), (State.XIndex + 1) * transform.localScale.y, 2.026f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
            default:
                throw new System.Exception("Unknown zone");
        }
    }

    private class Position
    {
        public Vector3 Location;
        public Quaternion Rotation;

        public bool RoughlyEquals(Position otherPosition)
        {
            return NearlyEquals(Location.x, otherPosition.Location.x)
                && NearlyEquals(Location.y, otherPosition.Location.y)
                && NearlyEquals(Location.z, otherPosition.Location.z)
                && NearlyEquals(Rotation.x, otherPosition.Rotation.x)
                && NearlyEquals(Rotation.y, otherPosition.Rotation.y)
                && NearlyEquals(Rotation.z, otherPosition.Rotation.z)
                && NearlyEquals(Rotation.w, otherPosition.Rotation.w);
        }

        private bool NearlyEquals(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001;
        }
    }

    private float animationStep;
    private float animationTime;

    public CardState State { get; private set; }
}

public enum Team { Player, Opponent }

public class CardState
{
    public Guid ID;
    public Team Owner;

    public Zone CurrentZone;

    public int XIndex;
    public int YIndex;
    public FacingDirection Facing;
    public string Text;
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
            Definition = Definition.Clone(),
            Text = Text
        };
    }
}

public class CardDefinition
{
    public Stats BaseStats;

    public CardDefinition Clone()
    {
        return new CardDefinition()
        {
            BaseStats = BaseStats
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

public struct Stats { public int Top; public int Bottom; public int Left; public int Right; }
