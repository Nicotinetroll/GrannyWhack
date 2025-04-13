using UnityEngine;
using OctoberStudio.Buffs; // ✅ Required for buff access

namespace OctoberStudio.Drop
{
    public class HasteDropBehavior : DropBehavior
    {
        public override void OnPickedUp()
        {
            base.OnPickedUp(); // ✅ Plays VFX, SFX, etc.

            BuffManager.Instance.ApplyBuff(BuffType.Haste); // ✅ Apply the haste buff

            gameObject.SetActive(false); // ✅ Disable after pickup
        }
    }
}