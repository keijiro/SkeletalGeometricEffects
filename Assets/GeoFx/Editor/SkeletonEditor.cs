// Geometry shader instancing with skeletal animations
// https://github.com/keijiro/SkeletalGeometricEffects

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace GeoFx
{
    [CustomEditor(typeof(Skeleton)), CanEditMultipleObjects]
    sealed class SkeletonEditor : Editor
    {
        SerializedProperty _sourceAnimator;

        SerializedProperty _baseRadius;
        SerializedProperty _stripWidth;
        SerializedProperty _stripSpeed;
        SerializedProperty _stripLength;

        SerializedProperty _waveWidth;
        SerializedProperty _waveSpeed;
        SerializedProperty _distortion;

        SerializedProperty _baseColor;
        SerializedProperty _hueShift;
        SerializedProperty _metallic;
        SerializedProperty _smoothness;
        SerializedProperty _emissiveColor;
        SerializedProperty _hilight;

        SerializedProperty _debug;

        ReorderableList _boneList;

        void OnEnable()
        {
            _sourceAnimator = serializedObject.FindProperty("_sourceAnimator");

            _baseRadius = serializedObject.FindProperty("_baseRadius");
            _stripWidth = serializedObject.FindProperty("_stripWidth");
            _stripSpeed = serializedObject.FindProperty("_stripSpeed");
            _stripLength = serializedObject.FindProperty("_stripLength");

            _waveWidth = serializedObject.FindProperty("_waveWidth");
            _waveSpeed = serializedObject.FindProperty("_waveSpeed");
            _distortion = serializedObject.FindProperty("_distortion");

            _baseColor = serializedObject.FindProperty("_baseColor");
            _hueShift = serializedObject.FindProperty("_hueShift");
            _metallic = serializedObject.FindProperty("_metallic");
            _smoothness = serializedObject.FindProperty("_smoothness");
            _emissiveColor = serializedObject.FindProperty("_emissiveColor");
            _hilight = serializedObject.FindProperty("_hilight");

            _debug = serializedObject.FindProperty("_debug");

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

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_baseRadius);
            EditorGUILayout.PropertyField(_stripWidth);
            EditorGUILayout.PropertyField(_stripSpeed);
            EditorGUILayout.PropertyField(_stripLength);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_waveWidth);
            EditorGUILayout.PropertyField(_waveSpeed);
            EditorGUILayout.PropertyField(_distortion);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_baseColor);
            EditorGUILayout.PropertyField(_hueShift);
            EditorGUILayout.PropertyField(_metallic);
            EditorGUILayout.PropertyField(_smoothness);
            EditorGUILayout.PropertyField(_emissiveColor);
            EditorGUILayout.PropertyField(_hilight);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_debug);

            EditorGUILayout.Space();

            _boneList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
