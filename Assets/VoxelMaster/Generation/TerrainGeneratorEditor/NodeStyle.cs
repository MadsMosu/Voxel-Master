using UnityEngine;

public static partial class NodeStyle
{
    public static GUISkin nodeSkin;


    public static GUIStyle nodeBox;
    public static GUIStyle nodeBoxBold;

    public static GUIStyle nodeLabel;
    public static GUIStyle nodeLabelBold;
    public static GUIStyle nodeLabelCentered;
    public static GUIStyle nodeLabelBoldCentered;
    public static GUIStyle nodeLabelInput;
    public static GUIStyle nodeLabelOutput;
    public static GUIStyle nodeLabelInputBold;
    public static GUIStyle nodeLabelOutputBold;

    public static GUIStyle bodyStyle;

    public static GUIStyle selectedNode;


    public static Texture2D dot { get { return _dot != null ? _dot : _dot = Resources.Load<Texture2D>("dot"); } }
    private static Texture2D _dot;
    public static Texture2D dotOuter { get { return _dotOuter != null ? _dotOuter : _dotOuter = Resources.Load<Texture2D>("dot_outer"); } }
    private static Texture2D _dotOuter;
    public static Texture2D nodeBody { get { return _nodeBody != null ? _nodeBody : _nodeBody = Resources.Load<Texture2D>("node"); } }
    private static Texture2D _nodeBody;
    public static Texture2D nodeHighlight { get { return _nodeHighlight != null ? _nodeHighlight : _nodeHighlight = Resources.Load<Texture2D>("node_highlight"); } }
    private static Texture2D _nodeHighlight;



    public static Texture2D background { get { return _background != null ? _background : _background = Resources.Load<Texture2D>("background"); } }
    private static Texture2D _background;


    public static float portAspect { get { return dot != null ? ((float)dot.width) / dot.height : 1; } }

    public static Color wTextColor = new Color(0.8f, 0.8f, 0.8f);
    public static Color gTextColor = new Color(0.6f, 0.6f, 0.6f);

    public static void Init()
    {

        nodeSkin = Object.Instantiate(GUI.skin);
        GUI.skin = nodeSkin;

        foreach (GUIStyle style in GUI.skin)
        {
            style.fontSize = 12;
        }

        nodeSkin.label.normal.textColor = wTextColor;
        nodeLabel = nodeSkin.label;

        nodeSkin.box.normal.background = nodeBody;
        nodeSkin.box.normal.textColor = gTextColor;
        nodeSkin.box.active.textColor = gTextColor;

        nodeLabelBold = new GUIStyle(nodeLabel) { fontStyle = FontStyle.Bold };
        nodeLabelCentered = new GUIStyle(nodeLabel) { alignment = TextAnchor.MiddleCenter };
        nodeLabelBoldCentered = new GUIStyle(nodeLabelBold) { alignment = TextAnchor.MiddleCenter };

        nodeLabelInput = new GUIStyle(nodeLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 15, 0),
            padding = new RectOffset(10, 0, 0, 0)
        };
        nodeLabelInput.normal.textColor = gTextColor;

        nodeLabelOutput = new GUIStyle(nodeLabel)
        {
            alignment = TextAnchor.MiddleRight,
            margin = new RectOffset(0, 0, 15, 0),
            padding = new RectOffset(0, 10, 0, 0)
        };
        nodeLabelOutput.normal.textColor = gTextColor;

        nodeLabelInputBold = new GUIStyle(nodeLabelInput) { fontStyle = FontStyle.Bold };
        nodeLabelInputBold.normal.textColor = wTextColor;

        nodeLabelOutputBold = new GUIStyle(nodeLabelOutput) { fontStyle = FontStyle.Bold };
        nodeLabelOutputBold.normal.textColor = wTextColor;

        nodeBox = nodeSkin.box;
        nodeBoxBold = new GUIStyle(nodeBox) { fontStyle = FontStyle.Bold };

        bodyStyle = new GUIStyle();



    }
}