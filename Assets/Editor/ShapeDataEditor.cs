using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShapeData))]
public class ShapeDataEditor : Editor
{
    private ShapeData shapeData;
    private SerializedProperty dimensions;
    private SerializedProperty shapeLayout;

    private bool showConfirmation = false;
    private Color defaultColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private void OnEnable()
    {
        shapeData = (ShapeData)target;
        dimensions = serializedObject.FindProperty("dimensions");
        shapeLayout = serializedObject.FindProperty("shapeLayout");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(dimensions);
        Vector2Int grid = dimensions.vector2IntValue;

        // Ensure shapeLayout is appropriately sized
        while (shapeLayout.arraySize < grid.x * grid.y)
        {
            shapeLayout.InsertArrayElementAtIndex(shapeLayout.arraySize);
            ResetCell(shapeLayout.GetArrayElementAtIndex(shapeLayout.arraySize - 1), grid);
        }

        // Update the inspector based on grid size
        int width = (int)grid.x;
        int height = (int)grid.y;
        int centerX = grid.x / 2;
        int centerY = grid.y / 2;
        float inspectorWidth = EditorGUIUtility.currentViewWidth;
        float buttonSize = Mathf.Min(50, (inspectorWidth - (grid.x * 4)) / grid.x); // Buffer for margins

        for (int y = height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < width; x++)
            {
                int index = y * grid.x + x;

                SerializedProperty cell = shapeLayout.GetArrayElementAtIndex(index);
                SerializedProperty positionProp = cell.FindPropertyRelative("Position");
                SerializedProperty contentTypeProp = cell.FindPropertyRelative("CellContentType");
                SerializedProperty isFilledProp = cell.FindPropertyRelative("IsFilled");

                // Set position to be relative to center
                Vector2Int position = new Vector2Int(x - centerX, y - centerY);
                positionProp.vector2IntValue = position;

                GameStats.CellContentTypes currentType = (GameStats.CellContentTypes)contentTypeProp.enumValueIndex;

                if (currentType == GameStats.CellContentTypes.Wall)
                {
                    currentType++;
                }

                string label = currentType.ToString();

                Color buttonColor = GetColorForContentType(currentType);
                GUIStyle style = CreateDynamicTextStyle(buttonSize, buttonColor);

                if (GUILayout.Button(label, style, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    int nextTypeIndex = (contentTypeProp.enumValueIndex + 1) % System.Enum.GetValues(typeof(GameStats.CellContentTypes)).Length;

                    if ((GameStats.CellContentTypes)nextTypeIndex == GameStats.CellContentTypes.Wall)
                    {
                        nextTypeIndex = (nextTypeIndex + 1) % System.Enum.GetValues(typeof(GameStats.CellContentTypes)).Length;
                    }

                    contentTypeProp.enumValueIndex = nextTypeIndex;
                    isFilledProp.boolValue = (GameStats.CellContentTypes)nextTypeIndex != GameStats.CellContentTypes.Empty;
                }
            }

            EditorGUILayout.EndHorizontal();
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
                shapeLayout.ClearArray();
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

    private void ResetCell(SerializedProperty cell, Vector2Int grid)
    {
        int index = shapeLayout.arraySize - 1;  // Assuming this method is called right after a new cell is added
        int x = index % grid.x;
        int y = index / grid.x;

        // Calculate center-based relative position
        int centerX = grid.x / 2;
        int centerY = grid.y / 2;
        Vector2Int position = new Vector2Int(x - centerX, y - centerY);

        // Assign the initial values
        cell.FindPropertyRelative("Position").vector2IntValue = position;
        cell.FindPropertyRelative("CellContentType").enumValueIndex = (int)GameStats.CellContentTypes.Empty;  // Default to Empty
        cell.FindPropertyRelative("IsFilled").boolValue = false;  // Not filled by default
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
        int fontSize = Mathf.Max(8, Mathf.FloorToInt(buttonSize * 0.3f)); // Ensure font isn't too small
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
