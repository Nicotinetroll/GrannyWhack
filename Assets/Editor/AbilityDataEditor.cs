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
        /* cached SerializedProperties */
        SerializedProperty isCharSpecificProp;
        SerializedProperty allowedCharNameProp;      // ← string
        SerializedProperty minLevelProp;

        /* popup data */
        List<CharacterData> chars = new();
        string[] names;

        void OnEnable()
        {
            isCharSpecificProp   = serializedObject.FindProperty("isCharacterSpecific");
            allowedCharNameProp  = serializedObject.FindProperty("allowedCharacterName");
            minLevelProp         = serializedObject.FindProperty("minCharacterLevel");

            LoadCharacters();
        }

        void LoadCharacters()
        {
            chars.Clear();

            /* find any CharactersDatabase asset */
            string guid = AssetDatabase.FindAssets("t:CharactersDatabase").FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var db   = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (db == null) return;

            /* reflection: fetch IEnumerable<CharacterData> from DB */
            IEnumerable<CharacterData> list = null;
            var type = db.GetType();

            // search props
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsCharacterList(p.PropertyType))
                {
                    list = p.GetValue(db) as IEnumerable<CharacterData>;
                    break;
                }
            }
            // search fields if not found
            if (list == null)
            {
                foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (IsCharacterList(f.FieldType))
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

        static bool IsCharacterList(System.Type t)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(t)) return false;
            var elem = t.IsArray ? t.GetElementType()
                                 : t.GetGenericArguments().FirstOrDefault();
            return elem != null && typeof(CharacterData).IsAssignableFrom(elem);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject,
                "allowedCharacterName",
                "minCharacterLevel");

            if (isCharSpecificProp.boolValue)
            {
                EditorGUILayout.Space(2);

                if (chars.Count == 0)
                {
                    EditorGUILayout.HelpBox("CharactersDatabase not found or contains no characters.", MessageType.Warning);
                }
                else
                {
                    /* current index from stored name */
                    string current = allowedCharNameProp.stringValue;
                    int index      = Mathf.Max(0, System.Array.IndexOf(names, current));
                    index          = EditorGUILayout.Popup("Allowed Character", index, names);
                    allowedCharNameProp.stringValue = names[index];
                }

                EditorGUILayout.PropertyField(minLevelProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
