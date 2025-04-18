#if UNITY_EDITOR
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using OctoberStudio.Abilities;

namespace OctoberStudio.Editor
{
    [CustomEditor(typeof(AbilityData), true)]
    public class AbilityDataEditor : UnityEditor.Editor
    {
        /* cached properties */
        SerializedProperty isCharSpecificProp;
        SerializedProperty allowedCharNameProp;
        SerializedProperty minLevelProp;

        /* popup data */
        List<CharacterData> chars = new();
        string[] names;

        void OnEnable()
        {
            isCharSpecificProp  = serializedObject.FindProperty("isCharacterSpecific");
            allowedCharNameProp = serializedObject.FindProperty("allowedCharacterName");
            minLevelProp        = serializedObject.FindProperty("minCharacterLevel");

            LoadCharacters();
        }

        /* pull character list from CharactersDatabase via reflection */
        void LoadCharacters()
        {
            chars.Clear();

            string guid = AssetDatabase.FindAssets("t:CharactersDatabase").FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) return;

            var db   = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (db == null) return;

            IEnumerable<CharacterData> list = null;
            System.Type t = db.GetType();

            bool IsCharList(System.Type st)
            {
                if (!typeof(IEnumerable).IsAssignableFrom(st)) return false;
                var elem = st.IsArray ? st.GetElementType()
                                      : st.GetGenericArguments().FirstOrDefault();
                return elem != null && typeof(CharacterData).IsAssignableFrom(elem);
            }

            // props
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsCharList(p.PropertyType))
                {
                    list = p.GetValue(db) as IEnumerable<CharacterData>;
                    break;
                }
            }
            // fields
            if (list == null)
            {
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (IsCharList(f.FieldType))
                    {
                        list = f.GetValue(db) as IEnumerable<CharacterData>;
                        break;
                    }
                }
            }
            if (list == null) return;

            chars  = list.Where(c => c != null).ToList();
            names  = chars.Select(c => c.Name).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            /* draw the toggle first */
            EditorGUILayout.PropertyField(isCharSpecificProp);

            /* draw popup + level directly beneath when active */
            if (isCharSpecificProp.boolValue)
            {
                if (chars.Count == 0)
                {
                    EditorGUILayout.HelpBox("CharactersDatabase not found or empty.", MessageType.Warning);
                }
                else
                {
                    int idx = Mathf.Max(0, System.Array.IndexOf(names, allowedCharNameProp.stringValue));
                    idx     = EditorGUILayout.Popup("Allowed Character", idx, names);
                    allowedCharNameProp.stringValue = names[idx];
                }

                EditorGUILayout.PropertyField(minLevelProp);
                EditorGUILayout.Space(4);
            }

            /* draw the rest of the fields */
            DrawPropertiesExcluding(serializedObject,
                "isCharacterSpecific",
                "allowedCharacterName",
                "minCharacterLevel");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
