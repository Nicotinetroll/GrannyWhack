using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(menuName = "October/Enemies Database", fileName = "Enemies Database")]
    public class EnemiesDatabase : ScriptableObject
    {
        [SerializeField] List<EnemyData> enemies;

        public int EnemiesCount => enemies.Count;

        public EnemyData GetEnemyData(int index)
        {
            return enemies[index];
        }

        public EnemyData GetEnemyData(EnemyType type)
        {
            for(int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Type == type) return enemies[i];
            }

            return null;
        }
    }

    [System.Serializable]
    
    public class EnemyData
    {
        [SerializeField] EnemyType type;
        [SerializeField] GameObject prefab;
        [SerializeField] Sprite icon;
        [SerializeField] List<EnemyDropData> enemyDrop;

        [Header("Death FX")]
        [SerializeField] GameObject deathParticlePrefab;
        [SerializeField] string deathParticlePoolName;

        public EnemyType Type => type;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public List<EnemyDropData> EnemyDrop => enemyDrop;
        public GameObject DeathParticlePrefab => deathParticlePrefab;
        public string DeathParticlePoolName => deathParticlePoolName;
    }


    [System.Serializable]
    public class EnemyDropData
    {
        [SerializeField] DropType dropType;
        [SerializeField, Range(0, 100)] float chance;

        public DropType DropType => dropType;
        public float Chance => chance;
    }

    public enum EnemyType
    {
        Pumpkin = 0,
        Bat = 1,
        Slime = 2,
        Vampire = 3,
        Plant = 4,
        Jellyfish = 5,
        Bug = 8,
        Wasp = 9,
        Hand = 10,
        Eye = 11,
        FireSlime = 12,
        PurpleJellyfish = 13,
        StagBeetle = 14,
        Shade = 15,
        ShadeJellyfish = 16,
        ShadeBat = 17,
        ShadeVampire = 18,
        ZombieCop = 19,
        ZombieRandom = 20,
        GhostJar = 21,
        Skull = 22,
        
        
        Grave = 100,
    }
}