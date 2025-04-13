using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class HasteBuff : RuntimeBuff
    {
        private float originalCooldown;

        protected override void Apply()
        {
            originalCooldown = PlayerBehavior.Player.CooldownMultiplier;
            PlayerBehavior.Player.RecalculateCooldownMuliplier(data.Value); // ✅ uses inherited field
            Debug.Log("Haste Applied!");
        }

        protected override void Remove()
        {
            PlayerBehavior.Player.RecalculateCooldownMuliplier(1f); // Back to normal
            Debug.Log("Haste Removed!");
            Destroy(this.gameObject);
        }
    }
}