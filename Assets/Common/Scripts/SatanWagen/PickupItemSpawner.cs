using UnityEngine;
using UnityEngine.InputSystem;
using OctoberStudio;
using OctoberStudio.Drop;

public class PickupItemSpawner : MonoBehaviour
{
    [SerializeField] DropType dropType = DropType.Bomb; // Set in inspector
    [SerializeField] float spawnDistance = 2f;

    private void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (PlayerBehavior.Player == null)
            {
                Debug.LogWarning("Player is missing!");
                return;
            }

            // 🛠 Use Vector3 from the start
            Vector3 spawnPos = PlayerBehavior.Player.transform.position +
                               (Vector3)(PlayerBehavior.Player.LookDirection.normalized * spawnDistance);

            // 👇 No need to touch `.z` anymore, it's already Vector3
            StageController.DropManager.Drop(dropType, spawnPos);

            Debug.Log($"🎁 Spawned pickup: {dropType} at {spawnPos}");
        }
    }
}