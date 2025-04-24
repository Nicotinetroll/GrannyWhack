using UnityEngine;

namespace OctoberStudio
{
    /// <summary>
    /// Handles animation + sprite-flip for both player and enemy characters.
    /// Put this script on the root of the prefab, drag the child
    /// “Renderer” (Animator + SpriteRenderer) into the fields.
    /// • Tick <b>Use Four-Way Directions</b> for the PLAYER only
    ///   (adds DirY support for up / down).
    /// • Leave the box unchecked for enemies — they stay left / right.
    /// </summary>
    public class CharacterBehavior : MonoBehaviour
    {
        /* ───────────────────────── inspector ───────────────────────── */
        [Header("Animation References")]
        [SerializeField] Animator       animator;        // child Animator
        [SerializeField] SpriteRenderer spriteRenderer;  // child SpriteRenderer

        [Header("Behaviour Flags")]
        [Tooltip("Tick only on the player prefab to play up / down clips " +
                 "(parameter DirY).  Leave OFF for enemies.")]
        [SerializeField] bool useFourWayDirections = true;

        [Header("State Names (per-prefab)")]
        [SerializeField] string idleStateName   = "Idle";
        [SerializeField] string runStateName    = "Run";
        [SerializeField] string defeatStateName = "Defeat";
        [SerializeField] string reviveStateName = "Revive";

        /* ───────────────────── hashed parameters ───────────────────── */
        static readonly int SPEED_HASH = Animator.StringToHash("Speed");
        static readonly int DIRY_HASH  = Animator.StringToHash("DirY");

        int defeatHash;
        int reviveHash;

        /* ───────────────────── internal caches ─────────────────────── */
        Vector3 lastPos;
        float   smoothSpeed;

        /* ──────────────────────── Awake ─────────────────────────────── */
        void Awake()
        {
            /* auto-discover refs if not wired */
            if (!animator)
                animator = GetComponentInChildren<Animator>(true);
            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

            defeatHash = Animator.StringToHash(defeatStateName);
            reviveHash = Animator.StringToHash(reviveStateName);
        }

        /* ───────────────────── SetSpeed (called each frame) ─────────── */
        public void SetSpeed(float inputMagnitude)
        {
            float dirY = 0f;

            if (useFourWayDirections)             /* PLAYER */
            {
                /* read raw input directly so DirY is instant and stable */
                Vector2 mv = GameController.InputManager.MovementValue;
                if      (mv.y >  0.3f) dirY =  1f;
                else if (mv.y < -0.3f) dirY = -1f;
            }
            else                                   /* ENEMY */
            {
                Vector3 delta = transform.position - lastPos;
                lastPos = transform.position;

                if (delta.sqrMagnitude > 0.0001f)
                    dirY = Mathf.Clamp(delta.y, -1f, 1f);
            }

            smoothSpeed = Mathf.Lerp(smoothSpeed, inputMagnitude, 20f * Time.deltaTime);

            animator.SetFloat(SPEED_HASH, smoothSpeed);
            if (useFourWayDirections) animator.SetFloat(DIRY_HASH, dirY);
        }

        /* ───────────────────── helper wrappers ─────────────────────── */
        public void SetSortingOrder(int order)
        {
            if (spriteRenderer) spriteRenderer.sortingOrder = order;
        }

        public void FlashHit()            => animator.SetTrigger("Hit");
        public void PlayDefeatAnimation() => animator.Play(defeatHash, 0, 0f);
        public void PlayReviveAnimation() => animator.Play(reviveHash,  0, 0f);
    }
}
