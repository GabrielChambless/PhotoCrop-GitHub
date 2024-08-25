using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HoleData))]
public class HoleDataEditor : Editor
{
    private HoleData holeData;
    private SerializedProperty gridSize;
    private SerializedProperty holeLayout;
    private SerializedProperty cellEntityLayout;

    private bool showHoleLayout = true;
    private bool showCellEntityLayout = false;
    private bool showConfirmation = false;
    private Color defaultColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private float buttonSize;

    private void OnEnable()
    {
        holeData = (HoleData)target;
        gridSize = serializedObject.FindProperty("gridSize");
        holeLayout = serializedObject.FindProperty("holeLayout");
        cellEntityLayout = serializedObject.FindProperty("cellEntityLayout");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(gridSize);
        EditorGUILayout.Space(10);

        Vector2Int grid = gridSize.vector2IntValue;

        // Ensure holeLayout is appropriately sized
        while (holeLayout.arraySize < grid.x * grid.y)
        {
            holeLayout.InsertArrayElementAtIndex(holeLayout.arraySize);
        }

        // Ensure cellEntityLayout is appropriately sized
        while (cellEntityLayout.arraySize < grid.x * grid.y)
        {
            cellEntityLayout.InsertArrayElementAtIndex(cellEntityLayout.arraySize);
        }

        // Update the inspector based on grid size
        int width = grid.x;
        int height = grid.y;

        // Calculate the optimal button size based on the inspector width
        float inspectorWidth = EditorGUIUtility.currentViewWidth;
        int buttonsPerRow = width;
        buttonSize = Mathf.Min(50, (inspectorWidth - (buttonsPerRow * 4)) / buttonsPerRow); // 4 is a buffer for margins

        // Toggle for Hole Layout
        showHoleLayout = EditorGUILayout.Foldout(showHoleLayout, "Hole Layout");
        if (showHoleLayout)
        {
            DisplayGrid(holeLayout, width, height, buttonSize, DrawHoleCellButton);
        }

        EditorGUILayout.Space(10);

        // Toggle for Cell Entity Layout
        showCellEntityLayout = EditorGUILayout.Foldout(showCellEntityLayout, "Cell Entity Layout");
        if (showCellEntityLayout)
        {
            DisplayGrid(cellEntityLayout, width, height, buttonSize, DrawCellEntityButton);
        }

        EditorGUILayout.Space(10);

        // Confirmation for clearing grid
        if (GUILayout.Button("Clear Grid"))
        {
            showConfirmation = true;
        }

        if (showConfirmation)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Confirm Clear"))
            {
                holeLayout.ClearArray();
                cellEntityLayout.ClearArray();
                showConfirmation = false;
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Cancel Clear"))
            {
                showConfirmation = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DisplayGrid(SerializedProperty layout, int width, int height, float buttonSize, System.Action<SerializedProperty, int> drawButton)
    {
        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                if (index < layout.arraySize) // Ensure the index is within bounds
                {
                    SerializedProperty cell = layout.GetArrayElementAtIndex(index);
                    drawButton(cell, index);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawHoleCellButton(SerializedProperty cell, int index)
    {
        SerializedProperty contentType = cell.FindPropertyRelative("CellContentType");
        SerializedProperty isFilled = cell.FindPropertyRelative("IsFilled");
        SerializedProperty position = cell.FindPropertyRelative("Position");

        // Set the position property
        position.vector2IntValue = new Vector2Int(index % gridSize.vector2IntValue.x, index / gridSize.vector2IntValue.x);

        GameStats.CellContentTypes currentType = (GameStats.CellContentTypes)contentType.enumValueIndex;
        string label = currentType.ToString();

        Color buttonColor = GetColorForContentType(currentType);
        GUIStyle style = CreateDynamicTextStyle(buttonSize, buttonColor);

        if (GUILayout.Button(label, style, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
        {
            int nextTypeIndex = (contentType.enumValueIndex + 1) % System.Enum.GetValues(typeof(GameStats.CellContentTypes)).Length;
            contentType.enumValueIndex = nextTypeIndex;
            isFilled.boolValue = (GameStats.CellContentTypes)nextTypeIndex != GameStats.CellContentTypes.Empty;
        }
    }

    private void DrawCellEntityButton(SerializedProperty cell, int index)
    {
        string label = cell.objectReferenceValue != null ? cell.objectReferenceValue.name : "Empty";

        Color buttonColor = cell.objectReferenceValue != null ? Color.green : defaultColor;
        GUIStyle style = CreateDynamicTextStyle(buttonSize, buttonColor);

        if (GUILayout.Button(label, style, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
        {
            EditorGUIUtility.ShowObjectPicker<CellEntityData>(null, false, "", index);
        }

        if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == index)
        {
            cell.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject() as CellEntityData;
        }
    }

    private GUIStyle CreateDynamicTextStyle(float buttonSize, Color backgroundColor)
    {
        GUIStyle buttonStyle = new GUIStyle();

        buttonStyle.normal.background = MakeTex(2, 2, backgroundColor);
        buttonStyle.active.background = MakeTex(2, 2, backgroundColor);
        buttonStyle.hover.background = MakeTex(2, 2, backgroundColor);

        buttonStyle.border = new RectOffset(1, 1, 1, 1); // Set a minimal border size to create outlines
        buttonStyle.margin = new RectOffset(2, 2, 2, 2); // Margins to separate buttons
        buttonStyle.padding = new RectOffset(2, 2, 2, 2); // Padding inside the button

        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.clipping = TextClipping.Clip; // Clip if necessary

        // Calculate the font size based on button size
        int fontSize = Mathf.Max(8, Mathf.FloorToInt(buttonSize * 0.2f)); // Ensure font isn't too small
        buttonStyle.fontSize = fontSize;

        if (backgroundColor == Color.white)
        {
            buttonStyle.normal.textColor = Color.black;
            buttonStyle.hover.textColor = Color.black;
            buttonStyle.active.textColor = Color.black;
        }
        else
        {
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;
        }

        return buttonStyle;
    }

    private Color GetColorForContentType(GameStats.CellContentTypes contentType)
    {
        switch (contentType)
        {
            case GameStats.CellContentTypes.Wall:
                return Color.gray;
            case GameStats.CellContentTypes.RedCell:
                return Color.red;
            case GameStats.CellContentTypes.GreenCell:
                return Color.green;
            case GameStats.CellContentTypes.BlueCell:
                return Color.blue;
            case GameStats.CellContentTypes.YellowCell:
                return Color.yellow;
            case GameStats.CellContentTypes.WhiteCell:
                return Color.white;
            case GameStats.CellContentTypes.BlackCell:
                return Color.black;
            default:
                return defaultColor;
        }
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = color;
        }

        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}