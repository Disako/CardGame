using System;
using System.Collections;
using System.Collections.Generic;
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
            Debug.Log(parts[i]);
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
        WrapText("What the card does, this may take multiple lines.", CardText, 0.4f);
    }

    private TextMesh TopText;
    private TextMesh LeftText;
    private TextMesh RightText;
    private TextMesh BottomText;
    private TextMesh CardText;

    public struct Stats { public int Top; public int Bottom; public int Left; public int Right; }

    public Stats BaseStats { get; private set; }

    public Stats GetActualStats() { return BaseStats; }

    // Use this for initialization
    void Start () {
        SetupTexture();

        BaseStats = new Stats()
        {
            Top = UnityEngine.Random.Range(1, 10),
            Left = UnityEngine.Random.Range(1, 10),
            Right = UnityEngine.Random.Range(1, 10),
            Bottom = UnityEngine.Random.Range(1, 10)
        };
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
        if (animationStep < animationStepCount)
        {
            animationStep++;
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
            animationStepCount = 20;
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
            animationStepCount = 20;
            Focussed = focussed;
        }
    }

    public bool Focussed { get; private set; }
    public Team Owner { get { return Deck.Owner; } }

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

        return old + diff * animationStep / animationStepCount;
    }

    private Vector3 LinearInterpolate()
    {
        return new Vector3(LinearInterpolate(c => c.x), LinearInterpolate(c => c.y), LinearInterpolate(c => c.z));
    }

    private float LinearInterpolate(Func<Vector3, float> getCoordinate)
    {
        var old = getCoordinate(previousPosition.Location);
        var target = getCoordinate(GetTargetPosition().Location);
        return old + (target - old) * animationStep / animationStepCount;
    }

    private void OnMouseOver()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0)
        {
            SetFocussed(true);
        }
    }

    public Zone CurrentZone { get; private set; }

    private int GetCardsInHand()
    {
        return Deck.GetCardsInHand();
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

    public void Initialize(Zone zone, DeckBehaviour deck, int xIndex = 0, int yIndex = 0, FacingDirection facing = FacingDirection.Up, int stepCount = 60)
    {
        animationStepCount = stepCount;
        Deck = deck;
        CurrentZone = zone;
        XIndex = xIndex;
        YIndex = yIndex;
        Facing = facing;
        currentPosition = GetTargetPosition();
        previousPosition = currentPosition;
        animationStep = animationStepCount;
    }

    public void SetLocation(Zone zone, int xIndex = 0, int yIndex = 0, FacingDirection facing = FacingDirection.Up, int stepCount = 60)
    {
        previousPosition = currentPosition;
        animationStep = 0;
        CurrentZone = zone;
        XIndex = xIndex;
        YIndex = yIndex;
        Facing = facing;
        animationStepCount = stepCount;
    }

    public enum FacingDirection { Up = 0, Right = 1, Down = 2, Left = 3 }

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
        return CurrentZone != Zone.Deck && (Owner == Team.Player || CurrentZone != Zone.Hand);
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
        if(Focussed && IsFaceUp())
        {
            return new Position()
            {
                Location = new Vector3(0, 3.81f, -3.34f),
                Rotation = Quaternion.Euler(-20, 0, 0)
            };
        }
        switch(CurrentZone)
        {
            case Zone.Hand:
                var cardsInHand = GetCardsInHand();
                if (cardsInHand <= 3)
                {
                    if (Owner == Team.Player)
                        return new Position()
                        {
                            Location = new Vector3(XIndex * 0.9f - (cardsInHand - 1) * 0.45f, 1.55f + (MouseOver ? 0.2f : 0.0f) + transform.localScale.y * XIndex, -3.26f - transform.localScale.y * XIndex + (MouseOver ? 0.2f : 0f)),
                            Rotation = Quaternion.Euler(-20, 0, 0)
                        };
                    else
                        return new Position()
                        {
                            Location = new Vector3(XIndex * 0.9f - (cardsInHand - 1) * 0.45f, 0.59f + transform.localScale.y * XIndex, 1.64f - transform.localScale.y * XIndex),
                            Rotation = Quaternion.Euler(260, 0, 180)
                        };
                }
                else
                {
                    if (Owner == Team.Player)
                        return new Position()
                        {
                            Location = new Vector3(-1.346f + 2.692f * XIndex / (cardsInHand - 1), 1.55f + (MouseOver ? 0.2f : 0.0f) + transform.localScale.y * XIndex, -3.26f - transform.localScale.y * XIndex + (MouseOver ? 0.2f : 0f)),
                            Rotation = Quaternion.Euler(-20, 0, 0)
                        };
                    else
                        return new Position()
                        {
                            Location = new Vector3(-1.346f + 2.692f * XIndex / (cardsInHand - 1), 0.59f + transform.localScale.y * XIndex, 1.64f - transform.localScale.y * XIndex),
                            Rotation = Quaternion.Euler(260, 0, 180)
                        };
                    throw new NotImplementedException();//-1.346f to 1.346
                }
            case Zone.Deck:
                if (Owner == Team.Player)
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
                    Location = new Vector3((XIndex - 1) * 1.1f, transform.localScale.y, (YIndex - 1f) * 1.1f - 0.5f),
                    Rotation = Quaternion.Euler(0, 90 * (int)Facing, 0)
                };
            case Zone.Discard:
                if (Owner == Team.Player)
                    return new Position()
                    {
                        Location = new Vector3(3.65f - 0.1f * Math.Min(10, GetCardsInDiscard() - XIndex - 1), (XIndex + 1) * transform.localScale.y, -2.73f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
                else
                    return new Position()
                    {
                        Location = new Vector3(-4.08f + 0.1f * Math.Min(10, GetCardsInDiscard() - XIndex - 1), (XIndex + 1) * transform.localScale.y, 2.026f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
            case Zone.Banished:
                if (Owner == Team.Player)
                    return new Position()
                    {
                        Location = new Vector3(2.55f - 0.1f * Math.Min(10, GetCardsInDiscard()) - 0.1f * Math.Min(10, GetBanishedCards() - XIndex - 1), (XIndex + 1) * transform.localScale.y, -2.73f),
                        Rotation = Quaternion.Euler(0, 0, 0)
                    };
                else
                    return new Position()
                    {
                        Location = new Vector3(-2.98f + 0.1f * Math.Min(10, GetCardsInDiscard()) + 0.1f * Math.Min(10, GetCardsInDiscard() - XIndex - 1), (XIndex + 1) * transform.localScale.y, 2.026f),
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

    public int XIndex { get; private set; }
    public int YIndex { get; private set; }
    public FacingDirection Facing { get; private set; }
    private int animationStep;
    private int animationStepCount;

    public enum Zone
    {
        Deck,
        Hand,
        Discard,
        InPlay,
        Banished
    }
}

public enum Team { Player, Opponent }
