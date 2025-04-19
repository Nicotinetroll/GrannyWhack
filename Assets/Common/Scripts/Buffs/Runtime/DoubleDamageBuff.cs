using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class DoubleDamageBuff : RuntimeBuff
    {
        protected override void Apply()
        {
            PlayerBehavior.Player.PushDamageBuff(2f);   // x2 dmg
            Debug.Log("Double Damage Applied!");
        }

        protected override void Remove()
        {
            PlayerBehavior.Player.PopDamageBuff(2f);
            Debug.Log("Double Damage Removed!");
            Destroy(gameObject);
        }
    }
}