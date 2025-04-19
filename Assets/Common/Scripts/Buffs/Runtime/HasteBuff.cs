using UnityEngine;

namespace OctoberStudio.Buffs
{
    public class HasteBuff : RuntimeBuff
    {
        protected override void Apply()
        {
            // “data.Value” is the factor stored in the ScriptableObject, e.g. 0.4
            PlayerBehavior.Player.PushCooldownBuff(data.Value);
            Debug.Log("Haste Applied!");
        }

        protected override void Remove()
        {
            PlayerBehavior.Player.PopCooldownBuff(data.Value);
            Debug.Log("Haste Removed!");
            Destroy(gameObject);
        }
    }
}