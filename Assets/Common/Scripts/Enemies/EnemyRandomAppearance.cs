using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class RandomEnemyAppearance : MonoBehaviour
{
    [Tooltip("All sprite variants this enemy can randomly use")]
    [SerializeField] private Sprite[] randomSprites;

    [Tooltip("Matching animator controllers for each sprite variant (same order)")]
    [SerializeField] private RuntimeAnimatorController[] randomAnimators;

    private void Awake()
    {
        if (randomSprites == null || randomSprites.Length == 0) return;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        var animator = GetComponent<Animator>();

        int index = Random.Range(0, randomSprites.Length);
        spriteRenderer.sprite = randomSprites[index];

        // 🔄 Match animator if provided and available
        if (randomAnimators != null && index < randomAnimators.Length)
        {
            animator.runtimeAnimatorController = randomAnimators[index];
        }
    }
}