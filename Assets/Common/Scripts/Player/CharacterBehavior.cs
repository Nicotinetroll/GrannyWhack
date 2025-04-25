using UnityEngine;

namespace OctoberStudio
{
    /// <summary>
    /// Drives the Animator of a character (player or enemy).
    /// • Tick <b>Use Four-Way Directions</b> for the player prefab
    ///   to enable “DirY” (up / down); leave unticked for enemies.
    /// </summary>
    public class CharacterBehavior : MonoBehaviour
    {
        /* ───────── inspector ───────── */
        [Header("Animation References")]
        [SerializeField] Animator       animator;        // child Animator
        [SerializeField] SpriteRenderer spriteRenderer;  // child SpriteRenderer

        [Header("Behaviour Flags")]
        [SerializeField] bool useFourWayDirections = true;   // player only

        [Header("State Names")]
        [SerializeField] string idleStateName   = "Idle";
        [SerializeField] string runStateName    = "Run";
        [SerializeField] string defeatStateName = "Defeat";
        [SerializeField] string reviveStateName = "Revive";

        /* ───────── hashes ───────── */
        static readonly int SPEED_HASH = Animator.StringToHash("Speed");
        static readonly int DIRY_HASH  = Animator.StringToHash("DirY");

        int defeatHash;
        int reviveHash;

        /* ───────── caches ───────── */
        Vector3 lastPos;
        float   smoothSpeed;

        public  float LastDirY { get; private set; }   // cached for other systems

        /* ─────────────────────── Awake ─────────────────────── */
        void Awake()
        {
            if (!animator)        animator        = GetComponentInChildren<Animator>(true);
            if (!spriteRenderer)  spriteRenderer  = GetComponentInChildren<SpriteRenderer>(true);

            defeatHash = Animator.StringToHash(defeatStateName);
            reviveHash = Animator.StringToHash(reviveStateName);
        }

        /* ────────────────── runtime API ────────────────── */

        /// <summary>Called each frame by <c>PlayerBehavior</c> / enemy AI.</summary>
        public void SetSpeed(float inputMagnitude)
        {
            /* Only enemies derive DirY from movement here.
               For the player, DirY comes from <b>SetDirection()</b>. */
            if (!useFourWayDirections)
            {
                Vector3 delta = transform.position - lastPos;
                lastPos = transform.position;

                if (delta.sqrMagnitude > 0.0001f)
                {
                    float dirY = Mathf.Clamp(delta.y, -1f, 1f);
                    animator.SetFloat(DIRY_HASH, dirY);
                }
            }

            /* smooth the Speed parameter so blend-trees look nicer */
            smoothSpeed = Mathf.Lerp(smoothSpeed,
                                     inputMagnitude,
                                     20f * Time.deltaTime);
            animator.SetFloat(SPEED_HASH, smoothSpeed);
        }

        /// <summary>
        /// Sets vertical direction (+1 up, 0 side, -1 down) – used by the player.
        /// Call every frame (moving or idle) to keep correct pose.
        /// </summary>
        public void SetDirection(float dirY)
        {
            LastDirY = dirY;
            animator.SetFloat(DIRY_HASH, dirY);
        }

        /* ───────── helpers ───────── */
        public void SetSortingOrder(int order)
        {
            if (spriteRenderer) spriteRenderer.sortingOrder = order;
        }

        public void FlashHit()            => animator.SetTrigger("Hit");
        public void PlayDefeatAnimation() => animator.Play(defeatHash, 0, 0f);
        public void PlayReviveAnimation() => animator.Play(reviveHash,  0, 0f);
    }
}
