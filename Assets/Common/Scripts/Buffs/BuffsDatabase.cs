using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Buffs
{
    [CreateAssetMenu(fileName = "BuffsDatabase", menuName = "October/Buffs/Buff Database")]
    public class BuffsDatabase : ScriptableObject
    {
        [SerializeField] private List<BuffData> buffs = new();

        public BuffData GetBuff(BuffType type)
        {
            return buffs.Find(buff => buff.BuffType == type);
        }

        public List<BuffData> GetAll() => buffs;
    }
}