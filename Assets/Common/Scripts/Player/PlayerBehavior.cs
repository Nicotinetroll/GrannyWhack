using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.UI;
using OctoberStudio.Upgrades;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    /// <summary>
    /// Handles movement, stats, damage, buffs, death & revive logic.
    /// </summary>
    public class PlayerBehavior : MonoBehaviour
    {
        /* ───────── constants & singletons ───────── */
        static readonly int DEATH_HASH            = "Death".GetHashCode();
        static readonly int REVIVE_HASH           = "Revive".GetHashCode();
        static readonly int RECEIVING_DAMAGE_HASH = "Receiving Damage".GetHashCode();

        static PlayerBehavior instance;
        public  static PlayerBehavior Player => instance;

        /* ───────── inspector fields ───────── */
        [Header("Databases")]
        [SerializeField] CharactersDatabase charactersDatabase;

        [Header("Base stats")]
        [SerializeField, Min(0.01f)] float speed = 2f;
        [SerializeField, Min(0.10f)] float defaultMagnetRadius = 0.75f;
        [SerializeField, Min(1f)] float xpMultiplier = 1f;
        [SerializeField, Range(0.1f, 1f)] float cooldownMultiplier = 1f;
        [SerializeField, Range(0, 100)] int   initialDamageReductionPercent = 0;
        [SerializeField, Min(1f)] float initialProjectileSpeedMultiplier = 1f;
        [SerializeField, Min(1f)] float initialSizeMultiplier = 1f;
        [SerializeField, Min(1f)] float initialDurationMultiplier = 1f;
        [SerializeField, Min(1f)] float initialGoldMultiplier = 1f;

        [Header("Rerolls")]
        [SerializeField] int rerollUnlockLevel = 2;
        [SerializeField] int maxRerollCharges  = 3;
        public int RerollUnlockLevel => rerollUnlockLevel;
        public int MaxRerollCharges  => maxRerollCharges;

        [Header("Scene references")]
        [SerializeField] HealthbarBehavior          healthbar;
        [SerializeField] Transform                  centerPoint;
        [SerializeField] PlayerEnemyCollisionHelper collisionHelper;

        public static Transform CenterTransform => instance.centerPoint;
        public static Vector2   CenterPosition  => instance.centerPoint.position;
        public  HealthbarBehavior Healthbar     => healthbar;

        [Header("Death & revive FX")]
        [SerializeField] ParticleSystem reviveParticle;
        [Header("Immune FX")]
        [SerializeField] ParticleSystem immuneVFX;

        [Header("Revive tint sprites")]
        [SerializeField] SpriteRenderer reviveBackgroundSpriteRenderer;
        [SerializeField, Range(0, 1)] float reviveBackgroundAlpha     = .7f;
        [SerializeField, Range(0, 1)] float reviveBackgroundSpawnDelay = .0f;
        [SerializeField, Range(0, 1)] float reviveBackgroundHideDelay  = .4f;

        [SerializeField] SpriteRenderer reviveBottomSpriteRenderer;
        [SerializeField, Range(0, 1)] float reviveBottomAlpha     = .7f;
        [SerializeField, Range(0, 1)] float reviveBottomSpawnDelay = .0f;
        [SerializeField, Range(0, 1)] float reviveBottomHideDelay  = .4f;

        [Header("Misc")]
        [SerializeField] Vector2 fenceOffset;
        [SerializeField] Color   hitColor = Color.white;
        [SerializeField] float   enemyInsideDamageInterval = 2f;

        /* ───────── private fields ───────── */
        float permanentCooldownMultiplier = 1f;
        float permanentDamageMultiplier   = 1f;
        float buffCooldownMultiplier      = 1f;
        float buffDamageMultiplier        = 1f;

        Vector3 baseCharacterScale;

        public  event UnityAction onPlayerDied;

        public float Damage                    { get; private set; }
        public float MagnetRadiusSqr           { get; private set; }
        public float Speed                     { get; private set; }
        public float XPMultiplier              { get; private set; }
        public float CooldownMultiplier        { get; private set; }
        public float DamageReductionMultiplier { get; private set; }
        public float ProjectileSpeedMultiplier { get; private set; }
        public float SizeMultiplier            { get; private set; }
        public float DurationMultiplier        { get; private set; }
        public float GoldMultiplier            { get; private set; }
        public Vector2 LookDirection           { get; private set; }
        public bool    IsMovingAlowed          { get; set; }

        bool invincible = false;
        readonly List<EnemyBehavior> enemiesInside = new();

        CharactersSave    charactersSave;
        public  CharacterData     Data { get; private set; }
        CharacterBehavior Character { get; set; }

        /* ─────────────────── Awake ─────────────────── */
        void Awake()
        {
            instance = this;

            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            Data           = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);

            Character = Instantiate(Data.Prefab).GetComponent<CharacterBehavior>();
            Character.transform.SetParent(transform);
            Character.transform.ResetLocal();

            baseCharacterScale = Character.transform.localScale;

            healthbar.Init(Data.BaseHP);
            healthbar.SetAutoHideWhenMax(true);
            healthbar.SetAutoShowOnChanged(true);

            RecalculateMagnetRadius(1f);
            RecalculateMoveSpeed(1f);
            RecalculateDamage(1f);
            RecalculateMaxHP(1f);
            RecalculateXPMuliplier(1f);
            RecalculateCooldownMuliplier(1f);
            RecalculateDamageReduction(0f);
            RecalculateProjectileSpeedMultiplier(1f);
            RecalculateSizeMultiplier(1f);
            RecalculateDurationMultiplier(1f);
            RecalculateGoldMultiplier(1f);

            LookDirection  = Vector2.right;
            IsMovingAlowed = true;
        }

        /* ─────────────────── Update ─────────────────── */
        float lastDirY = 0f;    // remembers last vertical heading (+1 / 0 / -1)

        void Update()
        {
            if (healthbar.IsZero) return;

            /* periodic damage from enemies standing inside player */
            for (int i = enemiesInside.Count - 1; i >= 0; i--)
            {
                EnemyBehavior enemy = enemiesInside[i];
                if (Time.time - enemy.LastTimeDamagedPlayer > enemyInsideDamageInterval)
                {
                    TakeDamage(enemy.GetDamage());
                    enemy.LastTimeDamagedPlayer = Time.time;
                }
            }

            if (!IsMovingAlowed) return;

            /* -------- movement & direction -------- */
            Vector2 input = GameController.InputManager.MovementValue;
            float   power = input.magnitude;

            /* remember last significant vertical direction */
            if (power > 0.01f)
            {
                if (Mathf.Abs(input.y) > 0.1f)                    // moving up / down
                    lastDirY = Mathf.Sign(input.y);
                else if (Mathf.Abs(input.x) > 0.1f)               // moving sideways
                    lastDirY = 0f;                                // ← keeps Idle-side
            }

            /* feed animator every frame (moving OR idle) */
            Character.SetDirection(lastDirY);

            Character.SetSpeed(power);             // actual “Speed” param

            if (power > 0.01f && Time.timeScale > 0f)
            {
                Vector3 move = (Vector3)input * Time.deltaTime * Speed;

                if (StageController.FieldManager
                    .ValidatePosition(transform.position + Vector3.right * move.x, fenceOffset))
                    transform.position += Vector3.right * move.x;

                if (StageController.FieldManager
                    .ValidatePosition(transform.position + Vector3.up * move.y, fenceOffset))
                    transform.position += Vector3.up * move.y;

                collisionHelper.transform.localPosition = Vector3.zero;

                float magX = Mathf.Abs(transform.localScale.x);
                bool facingRight = input.x >= 0;

                transform.localScale = new Vector3(facingRight ? magX : -magX,
                    transform.localScale.y,
                    transform.localScale.z);

// Fix: cancel horizontal flip on healthbar
                var hbScale = healthbar.transform.localScale;
                hbScale.x = Mathf.Abs(hbScale.x); // always keep positive
                healthbar.transform.localScale = hbScale;
                
                if (healthbar != null)
                {
                    var scale = healthbar.transform.localScale;
                    scale.x = Mathf.Abs(scale.x); // always positive
                    healthbar.transform.localScale = scale;

                    var angles = healthbar.transform.localEulerAngles;
                    angles.y = 0f;
                    healthbar.transform.localEulerAngles = angles;
                }
                healthbar.transform.SetParent(null); // unparent completely
            }
        }


        /* ─────────── recalculation helpers ─────────── */
        public void RecalculateMagnetRadius(float magMul)
            => MagnetRadiusSqr = Mathf.Pow(defaultMagnetRadius * magMul, 2);

        public void RecalculateMoveSpeed(float moveMul)
            => Speed = speed * moveMul;

        public void RecalculateDamage(float newPermanentMultiplier)
        {
            permanentDamageMultiplier = newPermanentMultiplier;
            UpdateDamage();
        }

        public void PushDamageBuff(float factor)
        {
            buffDamageMultiplier *= factor;
            UpdateDamage();
        }
        public void PopDamageBuff(float factor)
        {
            buffDamageMultiplier /= factor;
            UpdateDamage();
        }

        void UpdateDamage()
        {
            float raw  = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);
            float perm = raw * permanentDamageMultiplier;
            float buff = perm * buffDamageMultiplier;
            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Damage))
                buff *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);
            Damage = buff;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInsideMagnetRadius(Transform target)
            => (transform.position - target.position).sqrMagnitude <= MagnetRadiusSqr;

        public void RecalculateCooldownMuliplier(float newPermanentMultiplier)
        {
            permanentCooldownMultiplier = newPermanentMultiplier;
            UpdateCooldownMultiplierValue();
        }
        public void PushCooldownBuff(float factor)
        {
            buffCooldownMultiplier *= factor;
            UpdateCooldownMultiplierValue();
        }
        public void PopCooldownBuff(float factor)
        {
            buffCooldownMultiplier /= factor;
            UpdateCooldownMultiplierValue();
        }
        void UpdateCooldownMultiplierValue()
        {
            CooldownMultiplier = cooldownMultiplier
                                 * permanentCooldownMultiplier
                                 * buffCooldownMultiplier;
        }

        public void RecalculateMaxHP(float maxHPMul)
        {
            var upgrade = GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Health);
            healthbar.ChangeMaxHP((Data.BaseHP + upgrade) * maxHPMul);
        }
        public void RecalculateXPMuliplier(float m) => XPMultiplier = xpMultiplier * m;

        public void RecalculateDamageReduction(float percent)
        {
            DamageReductionMultiplier =
                (100f - initialDamageReductionPercent - percent) / 100f;
            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Armor))
                DamageReductionMultiplier *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Armor);
        }

        public void RecalculateProjectileSpeedMultiplier(float m)
            => ProjectileSpeedMultiplier = initialProjectileSpeedMultiplier * m;

        public void RecalculateSizeMultiplier(float m)      // ← updated
        {
            SizeMultiplier = initialSizeMultiplier * m;
            var s = baseCharacterScale * SizeMultiplier;
            Character.transform.localScale = new Vector3(
                Mathf.Abs(s.x),  // positive; Update() flips X sign as needed
                s.y,
                s.z);
        }

        public void RecalculateDurationMultiplier(float m)
            => DurationMultiplier = initialDurationMultiplier * m;

        public void RecalculateGoldMultiplier(float m)
            => GoldMultiplier = initialGoldMultiplier * m;

        public void RestoreHP(float hpPercent)
            => healthbar.AddPercentage(hpPercent);

        public void Heal(float hp)
            => healthbar.AddHP(hp + GameController.UpgradesManager
                                            .GetUpgadeValue(UpgradeType.Healing));

        public void Revive()
        {
            Character.PlayReviveAnimation();
            reviveParticle.Play();
            invincible      = true;
            IsMovingAlowed  = false;
            healthbar.ResetHP(1f);
            Character.SetSortingOrder(102);

            reviveBackgroundSpriteRenderer
                .DoAlpha(0f, 0.3f, reviveBackgroundHideDelay)
                .SetUnscaledTime(true)
                .SetOnFinish(() => reviveBackgroundSpriteRenderer
                                       .gameObject.SetActive(false));

            reviveBottomSpriteRenderer
                .DoAlpha(0f, 0.3f, reviveBottomHideDelay)
                .SetUnscaledTime(true)
                .SetOnFinish(() => reviveBottomSpriteRenderer
                                       .gameObject.SetActive(false));

            GameController.AudioManager.PlaySound(REVIVE_HASH);

            EasingManager.DoAfter(1f, () =>
            {
                IsMovingAlowed = true;
                Character.SetSortingOrder(0);
            });

            EasingManager.DoAfter(3f, () => invincible = false);
        }

        public void CheckTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == 7)
            {
                if (invincible) return;
                var enemy = collision.GetComponent<EnemyBehavior>();
                if (enemy != null)
                {
                    enemiesInside.Add(enemy);
                    enemy.LastTimeDamagedPlayer = Time.time;
                    enemy.onEnemyDied += OnEnemyDied;
                    TakeDamage(enemy.GetDamage());
                }
            }
            else
            {
                if (invincible) return;
                var projectile = collision.GetComponent<SimpleEnemyProjectileBehavior>();
                if (projectile != null)
                    TakeDamage(projectile.Damage);
            }
        }

        public void CheckTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.layer == 7)
            {
                if (invincible) return;
                var enemy = collision.GetComponent<EnemyBehavior>();
                if (enemy != null)
                {
                    enemiesInside.Remove(enemy);
                    enemy.onEnemyDied -= OnEnemyDied;
                }
            }
        }

        private void OnEnemyDied(EnemyBehavior enemy)
        {
            enemy.onEnemyDied -= OnEnemyDied;
            enemiesInside.Remove(enemy);
        }

        private float lastTimeVibrated = 0f;
        public void TakeDamage(float damage)
        {
            if (invincible || healthbar.IsZero) return;
            healthbar.Subtract(damage * DamageReductionMultiplier);
            Character.FlashHit();

            if (healthbar.IsZero)
            {
                Character.PlayDefeatAnimation();
                Character.SetSortingOrder(102);

                reviveBackgroundSpriteRenderer
                    .gameObject.SetActive(true);
                reviveBackgroundSpriteRenderer
                    .DoAlpha(reviveBackgroundAlpha, 0.3f, reviveBackgroundSpawnDelay)
                    .SetUnscaledTime(true);
                reviveBackgroundSpriteRenderer.transform
                    .position = transform.position
                                .SetZ(reviveBackgroundSpriteRenderer
                                      .transform.position.z);

                reviveBottomSpriteRenderer
                    .gameObject.SetActive(true);
                reviveBottomSpriteRenderer
                    .DoAlpha(reviveBottomAlpha, 0.3f, reviveBottomSpawnDelay)
                    .SetUnscaledTime(true);

                GameController.AudioManager.PlaySound(DEATH_HASH);

                EasingManager.DoAfter(0.5f, () => onPlayerDied?.Invoke())
                             .SetUnscaledTime(true);

                GameController.VibrationManager.StrongVibration();
            }
            else
            {
                if (Time.time - lastTimeVibrated > 0.05f)
                {
                    GameController.VibrationManager.LightVibration();
                    lastTimeVibrated = Time.time;
                }
                GameController.AudioManager.PlaySound(RECEIVING_DAMAGE_HASH);
            }
        }

        public void StartInvincibility(float duration)
        {
            if (invincible) return;
            invincible = true;

            int lvl = StageController.ExperienceManager.Level;
            if (lvl > 0 && immuneVFX != null)
            {
                immuneVFX.gameObject.SetActive(false);
                immuneVFX.gameObject.SetActive(true);
                immuneVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                immuneVFX.Clear(true);
                immuneVFX.Play(true);
            }

            Debug.Log("invincible START");
            StartCoroutine(InvincibilityCoroutine(duration));
        }

        private IEnumerator InvincibilityCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            invincible = false;
            if (immuneVFX != null)
                immuneVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Debug.Log("invincible END");
        }
    }
}
