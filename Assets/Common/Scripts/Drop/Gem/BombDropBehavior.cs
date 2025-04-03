using System.Collections;
using UnityEngine;

namespace OctoberStudio.Drop
{
    public class BombDropBehavior : DropBehavior
    {
        [SerializeField] float damageMultiplier = 100f;
        [SerializeField] float slowMoDuration = 1f;
        [SerializeField] float timeScaleDuringSlowMo = 0.2f;
        [SerializeField] GameObject explosionParticlePrefab;

        private static readonly int EXPLOSION_SOUND_HASH = "Bomb Explode".GetHashCode(); // Matches AudioDatabase "Bomb Explode"

        public override void OnPickedUp()
        {
            base.OnPickedUp(); // This already plays Bomb Pick Up sound

            StartCoroutine(HandleBombExplosion());
        }

        private IEnumerator HandleBombExplosion()
        {
            // ⏳ Start slow-motion
            Time.timeScale = timeScaleDuringSlowMo;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return new WaitForSecondsRealtime(slowMoDuration);

            // ⏱ Reset time
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // 💥 Play explosion sound
            GameController.AudioManager.PlaySound(EXPLOSION_SOUND_HASH);

            // 💨 Spawn explosion FX
            if (explosionParticlePrefab != null)
            {
                var explosion = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
                explosion.SetActive(true);
                Destroy(explosion, 2f);
            }

            // ☠️ Damage all enemies
            StageController.EnemiesSpawner.DealDamageToAllEnemies(PlayerBehavior.Player.Damage * damageMultiplier);

            gameObject.SetActive(false);
        }
    }
}