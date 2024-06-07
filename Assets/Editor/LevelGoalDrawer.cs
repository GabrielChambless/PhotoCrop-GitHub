using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LevelGoal))]
public class LevelGoalDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var goalTypeProperty = property.FindPropertyRelative("goalType");
        var descriptionProperty = property.FindPropertyRelative("description");
        var isCompletedProperty = property.FindPropertyRelative("isCompleted");
        var shapeLimitProperty = property.FindPropertyRelative("shapeLimit");
        var cropLimitProperty = property.FindPropertyRelative("cropLimit");

        Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(fieldRect, goalTypeProperty);
        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(fieldRect, descriptionProperty);
        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(fieldRect, isCompletedProperty);
        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        LevelGoal.GoalTypes goalType = (LevelGoal.GoalTypes)goalTypeProperty.enumValueIndex;

        switch (goalType)
        {
            case LevelGoal.GoalTypes.WithinShapeLimit:
                EditorGUI.PropertyField(fieldRect, shapeLimitProperty);
                break;
            case LevelGoal.GoalTypes.WithinCropLimit:
                EditorGUI.PropertyField(fieldRect, cropLimitProperty);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 3; // goalType, description, isCompleted

        var goalTypeProperty = property.FindPropertyRelative("goalType");
        LevelGoal.GoalTypes goalType = (LevelGoal.GoalTypes)goalTypeProperty.enumValueIndex;

        switch (goalType)
        {
            case LevelGoal.GoalTypes.WithinShapeLimit:
            case LevelGoal.GoalTypes.WithinCropLimit:
                lines++;
                break;
        }

        return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }
}