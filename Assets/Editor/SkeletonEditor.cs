// Geometry shader instancing with skeletal animations
// https://github.com/keijiro/SkeletalGeometricEffects

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(Skeleton)), CanEditMultipleObjects]
sealed class SkeletonEditor : Editor
{
    SerializedProperty _sourceAnimator;
    SerializedProperty _baseColor;
    SerializedProperty _metallic;
    SerializedProperty _smoothness;

    ReorderableList _boneList;

    void OnEnable()
    {
        _sourceAnimator = serializedObject.FindProperty("_sourceAnimator");
        _baseColor = serializedObject.FindProperty("_baseColor");
        _metallic = serializedObject.FindProperty("_metallic");
        _smoothness = serializedObject.FindProperty("_smoothness");

        _boneList = new ReorderableList(
            serializedObject,
            serializedObject.FindProperty("_boneList"),
            true, // draggable
            true, // displayHeader
            true, // displayAddButton
            true  // displayRemoveButton
        );

        _boneList.drawHeaderCallback = (Rect rect) => {  
            EditorGUI.LabelField(rect, "Bone List");
        };

        _boneList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var width = rect.width / 4;
            var space = 5;

            rect.y += 2;
            rect.width = width - space;
            rect.height = EditorGUIUtility.singleLineHeight;

            var bone = _boneList.serializedProperty.GetArrayElementAtIndex(index);
            var prop = bone.FindPropertyRelative("JointFrom");
            EditorGUI.PropertyField(rect, prop, GUIContent.none);

            rect.x += width;

            prop = bone.FindPropertyRelative("JointTo");
            EditorGUI.PropertyField(rect, prop, GUIContent.none);

            rect.x += width;
            rect.width = width * 2 - space;

            prop = bone.FindPropertyRelative("Radius");
            if (width < 60)
                EditorGUI.PropertyField(rect, prop, GUIContent.none);
            else
                EditorGUI.Slider(rect, prop, 0, 1, GUIContent.none);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_sourceAnimator);
        EditorGUILayout.PropertyField(_baseColor);
        EditorGUILayout.PropertyField(_metallic);
        EditorGUILayout.PropertyField(_smoothness);

        _boneList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
