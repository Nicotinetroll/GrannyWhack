using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.UI;
using OctoberStudio.Upgrades;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

namespace OctoberStudio
{
    public class PlayerBehavior : MonoBehaviour
    {
        private static readonly int DEATH_HASH            = "Death".GetHashCode();
        private static readonly int REVIVE_HASH           = "Revive".GetHashCode();
        private static readonly int RECEIVING_DAMAGE_HASH = "Receiving Damage".GetHashCode();

        private static PlayerBehavior instance;
        public  static PlayerBehavior Player => instance;

        [SerializeField] private CharactersDatabase charactersDatabase;

        [Header("Stats")]
        [SerializeField, Min(0.01f)] float speed                   = 2f;
        [SerializeField, Min(0.1f )] float defaultMagnetRadius    = 0.75f;
        [SerializeField, Min(1f   )] float xpMultiplier           = 1f;
        [SerializeField, Range(0.1f,1f)] float cooldownMultiplier = 1f;
        [SerializeField, Range(0,100  )] int   initialDamageReductionPercent = 0;
        [SerializeField, Min(1f    )] float initialProjectileSpeedMultiplier = 1f;
        [SerializeField, Min(1f    )] float initialSizeMultiplier            = 1f;
        [SerializeField, Min(1f    )] float initialDurationMultiplier        = 1f;
        [SerializeField, Min(1f    )] float initialGoldMultiplier            = 1f;

        [Header("Reroll Settings")]
        [SerializeField] private int rerollUnlockLevel   = 2;
        [SerializeField] private int maxRerollCharges    = 3;
        public  int RerollUnlockLevel => rerollUnlockLevel;
        public  int MaxRerollCharges  => maxRerollCharges;

        [Header("References")]
        [SerializeField] private HealthbarBehavior healthbar;
        [SerializeField] private Transform centerPoint;
        [SerializeField] private PlayerEnemyCollisionHelper collisionHelper;

        public static Transform CenterTransform => instance.centerPoint;
        public static Vector2  CenterPosition  => instance.centerPoint.position;
        public HealthbarBehavior Healthbar => healthbar;   // restored for TimelineDebugDisplay

        [Header("Death and Revive")]
        [SerializeField] private ParticleSystem reviveParticle;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem immuneVFX;

        [Space]
        [SerializeField] private SpriteRenderer reviveBackgroundSpriteRenderer;
        [SerializeField, Range(0,1)] float reviveBackgroundAlpha;
        [SerializeField, Range(0,1)] float reviveBackgroundSpawnDelay;
        [SerializeField, Range(0,1)] float reviveBackgroundHideDelay;

        [Space]
        [SerializeField] private SpriteRenderer reviveBottomSpriteRenderer;
        [SerializeField, Range(0,1)] float reviveBottomAlpha;
        [SerializeField, Range(0,1)] float reviveBottomSpawnDelay;
        [SerializeField, Range(0,1)] float reviveBottomHideDelay;

        [Header("Other")]
        [SerializeField] private Vector2 fenceOffset;
        [SerializeField] private Color   hitColor;
        [SerializeField] private float   enemyInsideDamageInterval = 2f;

        // ───────── Buff & upgrade tracking ────────────────────────────────────────
        private float permanentCooldownMultiplier = 1f;
        private float permanentDamageMultiplier   = 1f;
        private float buffCooldownMultiplier      = 1f;
        private float buffDamageMultiplier        = 1f;

        public event UnityAction onPlayerDied;

        public float Damage                    { get; private set; }
        public float MagnetRadiusSqr          { get; private set; }
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

        private bool invincible = false;
        private readonly List<EnemyBehavior> enemiesInside = new();

        private CharactersSave    charactersSave;
        public  CharacterData     Data { get; set; }
        private CharacterBehavior Character { get; set; }

        private void Awake()
        {
            instance = this;

            charactersSave = GameController.SaveManager
                                .GetSave<CharactersSave>("Characters");
            Data = charactersDatabase
                     .GetCharacterData(charactersSave.SelectedCharacterId);

            Character = Instantiate(Data.Prefab)
                         .GetComponent<CharacterBehavior>();
            Character.transform.SetParent(transform);
            Character.transform.ResetLocal();

            healthbar.Init(Data.BaseHP);
            healthbar.SetAutoHideWhenMax(true);
            healthbar.SetAutoShowOnChanged(true);

            // initialize all
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

            LookDirection   = Vector2.right;
            IsMovingAlowed  = true;
        }

        private void Update()
        {
            if (healthbar.IsZero) return;

            for (int i = enemiesInside.Count - 1; i >= 0; i--)
            {
                var enemy = enemiesInside[i];
                if (Time.time - enemy.LastTimeDamagedPlayer > enemyInsideDamageInterval)
                {
                    TakeDamage(enemy.GetDamage());
                    enemy.LastTimeDamagedPlayer = Time.time;
                }
            }

            if (!IsMovingAlowed) return;

            var input = GameController.InputManager.MovementValue;
            float power = input.magnitude;
            Character.SetSpeed(power);

            if (!Mathf.Approximately(power, 0f) && Time.timeScale > 0f)
            {
                Vector3 move = (Vector3)input * Time.deltaTime * Speed;
                if (StageController.FieldManager
                      .ValidatePosition(transform.position + Vector3.right * move.x,
                                        fenceOffset))
                    transform.position += Vector3.right * move.x;

                if (StageController.FieldManager
                      .ValidatePosition(transform.position + Vector3.up * move.y,
                                        fenceOffset))
                    transform.position += Vector3.up * move.y;

                collisionHelper.transform.localPosition = Vector3.zero;
                Character.SetLocalScale(new Vector3(input.x > 0 ? 1 : -1, 1, 1));
                LookDirection = input.normalized;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInsideMagnetRadius(Transform target)
            => (transform.position - target.position).sqrMagnitude
               <= MagnetRadiusSqr;

        // ───────── Pipelines & Buffs ─────────────────────────────────────────────
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

        private void UpdateDamage()
        {
            float raw    = Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data);
            float perm   = raw * permanentDamageMultiplier;
            float buffed = perm * buffDamageMultiplier;
            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Damage))
                buffed *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);
            Damage = buffed;
        }

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

        private void UpdateCooldownMultiplierValue()
        {
            CooldownMultiplier = cooldownMultiplier
                                 * permanentCooldownMultiplier
                                 * buffCooldownMultiplier;
        }

        public void RecalculateMaxHP(float maxHPMul)
        {
            var upgrade = GameController.UpgradesManager
                            .GetUpgadeValue(UpgradeType.Health);
            healthbar.ChangeMaxHP((Data.BaseHP + upgrade) * maxHPMul);
        }

        public void RecalculateXPMuliplier(float xpMul)
            => XPMultiplier = xpMultiplier * xpMul;

        public void RecalculateDamageReduction(float dmgRedPercent)
        {
            DamageReductionMultiplier =
                (100f - initialDamageReductionPercent - dmgRedPercent) / 100f;
            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Armor))
                DamageReductionMultiplier *= GameController.UpgradesManager
                                                .GetUpgadeValue(UpgradeType.Armor);
        }

        public void RecalculateProjectileSpeedMultiplier(float projSpeedMul)
            => ProjectileSpeedMultiplier = initialProjectileSpeedMultiplier
                                          * projSpeedMul;

        public void RecalculateSizeMultiplier(float sizeMul)
            => SizeMultiplier = initialSizeMultiplier * sizeMul;

        public void RecalculateDurationMultiplier(float durMul)
            => DurationMultiplier = initialDurationMultiplier * durMul;

        public void RecalculateGoldMultiplier(float goldMul)
            => GoldMultiplier = initialGoldMultiplier * goldMul;

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
