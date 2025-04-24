using UnityEngine;

namespace OctoberStudio
{
    public class CharacterBehavior : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] Animator animator;

        [Tooltip("If enabled, animator gets DirY (-1 down / 0 side / +1 up) " +
                 "so you can blend 4-way sprites. Leave OFF for enemies.")]
        [SerializeField] bool useFourWayDirections = false;

        /* hashed params */
        static readonly int SPEED_HASH = Animator.StringToHash("Speed");
        static readonly int DIRY_HASH  = Animator.StringToHash("DirY");

        Vector3 lastPos;
        float   smoothSpeed;

        /* called every frame by PlayerBehavior or AI -------------------- */
        public void SetSpeed(float inputMagnitude)
        {
            Vector3 delta = transform.parent.position - lastPos;
            lastPos = transform.parent.position;

            float dirY = 0f;
            if (useFourWayDirections)
            {
                if (delta.sqrMagnitude > 0.0001f)
                    dirY = Mathf.Clamp(delta.y, -1f, 1f);    // up=+1, down=-1
            }

            smoothSpeed = Mathf.Lerp(smoothSpeed, inputMagnitude, 15f * Time.deltaTime);

            animator.SetFloat(SPEED_HASH, smoothSpeed);

            if (useFourWayDirections)
                animator.SetFloat(DIRY_HASH, dirY);
        }

        /* utility helpers kept unchanged -------------------------------- */
        public void SetSortingOrder(int order)
            => GetComponent<SpriteRenderer>().sortingOrder = order;

        public void FlashHit()            => animator.SetTrigger("Hit");
        public void PlayDefeatAnimation() => animator.Play("Ninja Defeat", 0, 0);
        public void PlayReviveAnimation() => animator.Play("Ninja Revive", 0, 0);
    }
}