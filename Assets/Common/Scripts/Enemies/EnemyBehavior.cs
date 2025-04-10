using OctoberStudio.Easing;
using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using OctoberStudio.Timeline;
using System.Collections.Generic;
using OctoberStudio.Pool;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using CartoonFX;
using OctoberStudio.UI;

namespace OctoberStudio
{
    public class EnemyBehavior : MonoBehaviour
    {
        protected static readonly int _Overlay = Shader.PropertyToID("_Overlay");
        protected static readonly int _Disolve = Shader.PropertyToID("_Disolve");

        private static readonly int HIT_HASH = "Hit".GetHashCode();

        [Header("Settings")]
        [SerializeField] protected float speed;
        public float Speed { get; protected set; }

        [SerializeField] float damage = 1f;
        [SerializeField] float hp;
        [FormerlySerializedAs("canBekickedBack")]
        [SerializeField] bool canBeKickedBack = true;
        [SerializeField] bool shouldFadeIn;

        [Header("References")]
        [SerializeField] Rigidbody2D rb;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] DissolveSettings dissolveSettings;
        [SerializeField] SpriteRenderer shadowSprite;
        [SerializeField] Collider2D enemyCollider;

        public Vector2 Center => enemyCollider.bounds.center;

        [Header("Hit")]
        [SerializeField] float hitScaleAmount = 0.2f;
        [SerializeField] Color hitColor = Color.white;
        [SerializeField] GameObject hitParticlePrefab;

        [Header("UI")]
        [SerializeField] private EnemyHealthbarBehavior eliteHealthbar; // ✅ NEW

        public EnemyData Data { get; private set; }
        public WaveOverride WaveOverride { get; protected set; }

        public bool IsVisible => spriteRenderer.isVisible;
        public bool IsAlive => HP > 0;
        public bool IsInvulnerable { get; protected set; }

        public float HP { get; private set; }
        public float MaxHP { get; private set; }
        public bool ShouldSpawnChestOnDeath { get; set; }

        IEasingCoroutine fallBackCoroutine;

        private Dictionary<EffectType, List<Effect>> appliedEffects = new();
        protected bool IsMoving { get; set; }
        public bool IsMovingToCustomPoint { get; protected set; }
        public Vector2 CustomPoint { get; protected set; }

        public float LastTimeDamagedPlayer { get; set; }

        private Material sharedMaterial;
        private Material effectsMaterial;
        private float shadowAlpha;

        public event UnityAction<EnemyBehavior> onEnemyDied;
        public event UnityAction<float, float> onHealthChanged;

        private float lastTimeSwitchedDirection = 0;
        IEasingCoroutine damageCoroutine;
        protected IEasingCoroutine scaleCoroutine;
        IEasingCoroutine fadeInCoroutine;

        private float damageTextValue;
        private float lastTimeDamageText;
        private static int lastFrameHitSound;
        private float lastTimeHitSound;

        protected virtual void Awake()
        {
            sharedMaterial = spriteRenderer.sharedMaterial;
            effectsMaterial = Instantiate(sharedMaterial);

            shadowAlpha = shadowSprite.color.a;

            // ✅ NEW - Auto-find elite healthbar if not assigned
            if (eliteHealthbar == null)
                eliteHealthbar = GetComponentInChildren<EnemyHealthbarBehavior>(true);

            if (eliteHealthbar != null)
                eliteHealthbar.gameObject.SetActive(false);
        }

        public void SetData(EnemyData data) => Data = data;
        public void SetWaveOverride(WaveOverride waveOverride) => WaveOverride = waveOverride;

        public virtual void Play()
        {
            MaxHP = StageController.Stage.EnemyHP * hp;
            Speed = speed;

            if (WaveOverride != null)
            {
                MaxHP = WaveOverride.ApplyHPOverride(MaxHP);
                Speed = WaveOverride.ApplySpeedOverride(Speed);
            }

            HP = MaxHP;
            IsMoving = true;

            shadowSprite.SetAlpha(shadowAlpha);
            enemyCollider.enabled = true;

            if (shouldFadeIn)
            {
                spriteRenderer.SetAlpha(0);
                fadeInCoroutine = spriteRenderer.DoAlpha(1, 0.2f);
            }

            // ✅ NEW - Init and show elite healthbar if assigned
            if (eliteHealthbar != null)
            {
                eliteHealthbar.Init(MaxHP);
                eliteHealthbar.Show();
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive || !IsMoving || PlayerBehavior.Player == null) return;

            Vector3 target = IsMovingToCustomPoint ? CustomPoint : PlayerBehavior.Player.transform.position;
            Vector3 direction = (target - transform.position).normalized;

            float moveSpeed = Speed;

            if (appliedEffects.TryGetValue(EffectType.Speed, out var speedEffects))
                foreach (var effect in speedEffects)
                    moveSpeed *= effect.Modifier;

            transform.position += direction * Time.deltaTime * moveSpeed;

            if (!scaleCoroutine.ExistsAndActive())
            {
                var scale = transform.localScale;
                if ((direction.x > 0 && scale.x < 0) || (direction.x < 0 && scale.x > 0))
                {
                    if (Time.unscaledTime - lastTimeSwitchedDirection > 0.1f)
                    {
                        scale.x *= -1;
                        transform.localScale = scale;
                        lastTimeSwitchedDirection = Time.unscaledTime;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProjectileBehavior projectile = other.GetComponent<ProjectileBehavior>();
            if (projectile == null) return;

            TakeDamage(PlayerBehavior.Player.Damage * projectile.DamageMultiplier);

            if (HP > 0)
            {
                if (projectile.KickBack && canBeKickedBack)
                    KickBack(PlayerBehavior.CenterPosition);

                if (projectile.Effects?.Count > 0)
                    AddEffects(projectile.Effects);
            }
        }

        public float GetDamage()
        {
            float baseDmg = StageController.Stage.EnemyDamage * damage;
            if (WaveOverride != null) baseDmg = WaveOverride.ApplyDamageOverride(damage) * StageController.Stage.EnemyDamage;

            if (appliedEffects.TryGetValue(EffectType.Damage, out var effects))
                foreach (var e in effects)
                    baseDmg *= e.Modifier;

            return baseDmg;
        }

        public List<EnemyDropData> GetDropData() =>
            WaveOverride != null ? WaveOverride.ApplyDropOverride(Data.EnemyDrop) : Data.EnemyDrop;

        public void TakeDamage(float damage)
{
    if (!IsAlive || IsInvulnerable) return;

    PlayerStatsManager.Instance?.AddDamage(damage);

    // Spawn hit particle
    if (hitParticlePrefab != null && poolsManager != null)
    {
        var pooled = poolsManager.GetEntity("ParticlePrefab");
        if (pooled != null)
        {
            pooled.transform.position = Center;
            pooled.transform.rotation = Quaternion.identity;
            pooled.SetActive(true);
            pooled.GetComponent<ParticleSystem>()?.Play();
        }
    }

    HP -= damage;
    HP = Mathf.Max(0, HP); // Prevent negatives

    onHealthChanged?.Invoke(HP, MaxHP);

    // Update healthbar
    eliteHealthbar?.Subtract(damage);

    // Damage Text (rounded and clamped)
    damageTextValue += damage;
    if (Time.unscaledTime - lastTimeDamageText > 0.2f && damageTextValue >= 0.5f)
    {
        int rounded = Mathf.RoundToInt(damageTextValue);
        if (rounded > 0) // ✅ Don't show 0 damage
        {
            Vector3 pos = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.05f, 0.35f));
            StageController.WorldSpaceTextManager.SpawnText(pos, rounded.ToString());
        }

        damageTextValue = 0;
        lastTimeDamageText = Time.unscaledTime;
    }

    // Hit sound
    if (Time.frameCount != lastFrameHitSound && Time.unscaledTime - lastTimeHitSound > 0.2f)
    {
        GameController.AudioManager.PlaySound(HIT_HASH);
        lastFrameHitSound = Time.frameCount;
        lastTimeHitSound = Time.unscaledTime;
    }

    // Death or feedback
    if (HP <= 0)
    {
        Die(true);
    }
    else
    {
        // Flash hit
        if (!damageCoroutine.ExistsAndActive())
        {
            FlashHit(true);
        }

        // Scale on hit
        if (!scaleCoroutine.ExistsAndActive())
        {
            var x = transform.localScale.x;
            scaleCoroutine = transform.DoLocalScale(new Vector3(x * (1 - hitScaleAmount), (1 + hitScaleAmount), 1), 0.07f)
                .SetEasing(EasingType.SineOut)
                .SetOnFinish(() =>
                    scaleCoroutine = transform.DoLocalScale(new Vector3(x, 1, 1), 0.07f).SetEasing(EasingType.SineInOut)
                );
        }
    }
}



        private void FlashHit(bool resetMaterial, UnityAction onFinish = null)
        {
            spriteRenderer.material = effectsMaterial;
            var transparentColor = hitColor;
            transparentColor.a = 0;
            effectsMaterial.SetColor(_Overlay, transparentColor);

            damageCoroutine = effectsMaterial.DoColor(_Overlay, hitColor, 0.05f).SetOnFinish(() =>
                damageCoroutine = effectsMaterial.DoColor(_Overlay, transparentColor, 0.05f).SetOnFinish(() =>
                {
                    if (resetMaterial) spriteRenderer.material = sharedMaterial;
                    onFinish?.Invoke();
                }));
        }

        public void Kill()
        {
            HP = 0;
            Die(false);
        }

        protected virtual void Die(bool flash)
        {
            enemyCollider.enabled = false;
            damageCoroutine.StopIfExists();
            onEnemyDied?.Invoke(this);
            fallBackCoroutine.StopIfExists();
            rb.simulated = true;
            fadeInCoroutine.StopIfExists();

            // ✅ NEW
            if (eliteHealthbar != null)
                eliteHealthbar.Hide();

            if (Data != null && !string.IsNullOrEmpty(Data.DeathParticlePoolName) && poolsManager != null)
            {
                var fx = poolsManager.GetEntity(Data.DeathParticlePoolName);
                if (fx != null)
                {
                    fx.transform.position = transform.position;
                    fx.transform.rotation = Quaternion.identity;
                    fx.SetActive(true);
                    fx.GetComponent<ParticleSystem>()?.Play();
                    fx.GetComponent<CFXR_Effect>()?.Initialize();
                }
            }

            spriteRenderer.material = effectsMaterial;

            if (flash)
                FlashHit(false, () => effectsMaterial.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration - 0.1f));
            else
                effectsMaterial.DoColor(_Overlay, dissolveSettings.DissolveColor, dissolveSettings.Duration);

            effectsMaterial.SetFloat(_Disolve, 0);
            effectsMaterial.DoFloat(_Disolve, 1, dissolveSettings.Duration + 0.02f)
                .SetEasingCurve(dissolveSettings.DissolveCurve)
                .SetOnFinish(() =>
                {
                    effectsMaterial.SetColor(_Overlay, Color.clear);
                    effectsMaterial.SetFloat(_Disolve, 0);
                    gameObject.SetActive(false);
                    spriteRenderer.material = sharedMaterial;
                });

            shadowSprite.DoAlpha(0, dissolveSettings.Duration);
            appliedEffects.Clear();
            WaveOverride = null;
        }

        public void KickBack(Vector3 position)
        {
            var dir = (transform.position - position).normalized;
            rb.simulated = false;
            fallBackCoroutine.StopIfExists();
            fallBackCoroutine = transform.DoPosition(transform.position + dir * 0.6f, 0.15f)
                .SetEasing(EasingType.ExpoOut)
                .SetOnFinish(() => rb.simulated = true);
        }

        public void AddEffects(List<Effect> effects)
        {
            foreach (var e in effects) AddEffect(e);
        }

        public void AddEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType))
                appliedEffects[effect.EffectType] = new List<Effect>();

            if (!appliedEffects[effect.EffectType].Contains(effect))
                appliedEffects[effect.EffectType].Add(effect);
        }

        public void RemoveEffect(Effect effect)
        {
            if (!appliedEffects.ContainsKey(effect.EffectType)) return;
            appliedEffects[effect.EffectType].Remove(effect);
        }

        // Pooling
        public void SetHitParticle(GameObject prefab) => hitParticlePrefab = prefab;
        public void SetPoolsManager(PoolsManager manager) => poolsManager = manager;
        private PoolsManager poolsManager;
    }
}
