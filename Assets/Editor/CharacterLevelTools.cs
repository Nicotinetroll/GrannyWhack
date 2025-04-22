#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using OctoberStudio;
using OctoberStudio.Abilities;

public static class CharacterLevelTools
{
    [MenuItem("October/Character Level/Add 1 Level")]
    public static void AddCharacterLevel()
    {
        var player = PlayerBehavior.Player;
        if (player == null)
        {
            Debug.LogWarning("No PlayerBehavior.Player found in scene.");
            return;
        }

        var data = player.Data;
        int lvl = CharacterLevelSystem.GetLevel(data) + 1;
        CharacterLevelSystem.SetLevel(data, lvl);
        SaveAndNotify(data, lvl);
    }

    [MenuItem("October/Character Level/Remove 1 Level")]
    public static void RemoveCharacterLevel()
    {
        var player = PlayerBehavior.Player;
        if (player == null) return;

        var data = player.Data;
        int lvl = Mathf.Max(1, CharacterLevelSystem.GetLevel(data) - 1);
        CharacterLevelSystem.SetLevel(data, lvl);
        SaveAndNotify(data, lvl);
    }

    [MenuItem("October/Character Level/Set to Max Level")]
    public static void MaxCharacterLevel()
    {
        var player = PlayerBehavior.Player;
        if (player == null) return;

        var data = player.Data;
        int max = CharacterLevelSystem.MaxLevel;
        CharacterLevelSystem.SetLevel(data, max);
        SaveAndNotify(data, max);
    }

    private static void SaveAndNotify(CharacterData data, int lvl)
    {
        // Persist immediately
        GameController.SaveManager.Save(true);
        Debug.Log($"[{data.Name}] Character level is now {lvl}.");
    }
}
#endif