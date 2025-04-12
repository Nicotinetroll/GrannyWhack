using UnityEngine;
using OctoberStudio.Buffs; // ✅ THIS IS WHAT YOU NEED

namespace OctoberStudio.Drop
{
    public class DoubleDamageDropBehavior : DropBehavior
    {
        public override void OnPickedUp()
        {
            base.OnPickedUp(); // plays VFX/SFX
            
            BuffManager.Instance.ApplyBuff(BuffType.DoubleDamage); // ✅ use Buffs system
            
            gameObject.SetActive(false);
        }
    }
}