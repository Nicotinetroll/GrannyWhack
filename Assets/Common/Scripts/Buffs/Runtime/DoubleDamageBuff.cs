using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class DoubleDamageBuff : RuntimeBuff
    {
        private float originalDamage;

        protected override void Apply()
        {
            originalDamage = PlayerBehavior.Player.Damage;
            PlayerBehavior.Player.RecalculateDamage(2f); // x2
            Debug.Log("Double Damage Applied!");
        }

        protected override void Remove()
        {
            PlayerBehavior.Player.RecalculateDamage(1f); // back to normal
            Debug.Log("Double Damage Removed!");
            Destroy(this.gameObject);
        }
    }
}