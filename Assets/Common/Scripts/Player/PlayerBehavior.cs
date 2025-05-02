using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.UI;
using OctoberStudio.Upgrades;
using OctoberStudio.Save;                 // ← NEW
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio
{
    /// <summary>Handles movement, stats, damage, buffs, death & revive logic.</summary>
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

        CharactersSave   charactersSave;
        float baseDamageOverride = -1f;   // ← Dev‑popup overrides
        int   baseHPOverride     = -1;

        public CharacterData     Data { get; private set; }
        CharacterBehavior Character { get; set; }

        /* ─────────────────── Awake ─────────────────── */
        void Awake()
        {
            instance = this;

            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            Data           = charactersDatabase.GetCharacterData(charactersSave.SelectedCharacterId);

            /* --- prefab --- */
            Character = Instantiate(Data.Prefab).GetComponent<CharacterBehavior>();
            Character.transform.SetParent(transform);
            Character.transform.ResetLocal();
            baseCharacterScale = Character.transform.localScale;

            // ------- OVERLAY FROM DEV SAVE ------- 
            if (charactersSave.CharacterDamage > 0f)
                baseDamageOverride = charactersSave.CharacterDamage;

            if (charactersSave.CharacterHealth > 0f)
                baseHPOverride = Mathf.RoundToInt(charactersSave.CharacterHealth);

            /* --- Healthbar --- */
            int initHP = baseHPOverride >= 0
                ? baseHPOverride
                : Mathf.RoundToInt(Data.BaseHP);   // <—  explicitný cast z float na int
            healthbar.Init(initHP);
            healthbar.Init(initHP);
            healthbar.SetAutoHideWhenMax(true);
            healthbar.SetAutoShowOnChanged(true);

            /* --- recalcs --- */
            RecalculateMagnetRadius(1f);
            RecalculateMoveSpeed(1f);
            UpdateDamage();                 // uses override if present
            RecalculateMaxHP(1f);           // uses override if present
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
        float lastDirY = 0f;

        void Update()
        {
            if (healthbar.IsZero) return;

            /* --- dotka nepriateľa vo vnútri --- */
            for (int i = enemiesInside.Count - 1; i >= 0; i--)
            {
                var e = enemiesInside[i];
                if (Time.time - e.LastTimeDamagedPlayer > enemyInsideDamageInterval)
                {
                    TakeDamage(e.GetDamage());
                    e.LastTimeDamagedPlayer = Time.time;
                }
            }

            if (!IsMovingAlowed) return;

            var input = GameController.InputManager.MovementValue;
            float power = input.magnitude;
            Character.SetSpeed(power);

            if (power > 0.01f)
            {
                Character.SetDirection(Mathf.Abs(input.y) >= 0.90f ? Mathf.Sign(input.y) : 0f);
            }

            if (power > 0.01f && Time.timeScale > 0f)
            {
                Vector3 move = (Vector3)input * Time.deltaTime * Speed;

                if (StageController.FieldManager.ValidatePosition(transform.position + Vector3.right * move.x, fenceOffset))
                    transform.position += Vector3.right * move.x;
                if (StageController.FieldManager.ValidatePosition(transform.position + Vector3.up * move.y, fenceOffset))
                    transform.position += Vector3.up * move.y;

                collisionHelper.transform.localPosition = Vector3.zero;

                float scaleX = Mathf.Abs(baseCharacterScale.x) * SizeMultiplier;
                Character.transform.localScale = new Vector3(input.x >= 0 ? scaleX : -scaleX,
                                                             baseCharacterScale.y * SizeMultiplier,
                                                             baseCharacterScale.z);

                LookDirection = input.normalized;
            }
            else if (power <= 0.01f && LookDirection == Vector2.zero)
            {
                LookDirection = Vector2.right;
            }

            if (healthbar != null)
            {
                healthbar.transform.localScale = Vector3.one;
                healthbar.transform.rotation   = Quaternion.identity;
            }
        }

        /* ─────────── recalculation helpers ─────────── */
        public void RecalculateMagnetRadius(float m)
            => MagnetRadiusSqr = Mathf.Pow(defaultMagnetRadius * m, 2);

        public void RecalculateMoveSpeed(float m)
            => Speed = speed * m;

        public void RecalculateDamage(float permMul)
        {
            permanentDamageMultiplier = permMul;
            UpdateDamage();
        }
        public void PushDamageBuff(float f) { buffDamageMultiplier *= f; UpdateDamage(); }
        public void PopDamageBuff (float f) { buffDamageMultiplier /= f; UpdateDamage(); }

        void UpdateDamage()
        {
            float raw = baseDamageOverride >= 0f
                ? baseDamageOverride
                : (Data.BaseDamage + CharacterLevelSystem.GetDamageBonus(Data));

            float perm = raw * permanentDamageMultiplier;
            float buff = perm * buffDamageMultiplier;

            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Damage))
                buff *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Damage);

            Damage = buff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInsideMagnetRadius(Transform t)
            => (transform.position - t.position).sqrMagnitude <= MagnetRadiusSqr;

        public void RecalculateCooldownMuliplier(float permMul)
        {
            permanentCooldownMultiplier = permMul;
            CooldownMultiplier = cooldownMultiplier
                               * permanentCooldownMultiplier
                               * buffCooldownMultiplier;
        }
        public void PushCooldownBuff(float f) { buffCooldownMultiplier *= f; RecalculateCooldownMuliplier(permanentCooldownMultiplier); }
        public void PopCooldownBuff (float f) { buffCooldownMultiplier /= f; RecalculateCooldownMuliplier(permanentCooldownMultiplier); }

        public void RecalculateMaxHP(float mul)
        {
            int rawBase = baseHPOverride >= 0
                ? baseHPOverride
                : Mathf.RoundToInt(Data.BaseHP);          // explicitný cast

            int upgrade = Mathf.RoundToInt(GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Health));

            healthbar.ChangeMaxHP((rawBase + upgrade) * mul);
        }

        public void RecalculateXPMuliplier(float m) => XPMultiplier = xpMultiplier * m;

        public void RecalculateDamageReduction(float percent)
        {
            DamageReductionMultiplier = (100f - initialDamageReductionPercent - percent) / 100f;
            if (GameController.UpgradesManager.IsUpgradeAquired(UpgradeType.Armor))
                DamageReductionMultiplier *= GameController.UpgradesManager.GetUpgadeValue(UpgradeType.Armor);
        }

        public void RecalculateProjectileSpeedMultiplier(float m)
            => ProjectileSpeedMultiplier = initialProjectileSpeedMultiplier * m;

        public void RecalculateSizeMultiplier(float m)
        {
            SizeMultiplier = initialSizeMultiplier * m;
            var s = baseCharacterScale * SizeMultiplier;
            Character.transform.localScale =
                new Vector3(Mathf.Abs(s.x), s.y, s.z);   // X sign flips v Update()
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
