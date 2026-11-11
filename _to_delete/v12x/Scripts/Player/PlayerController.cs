using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Movimiento del jugador, carrera con resistencia, esconderse y paso entre zonas.
    /// Valores del original (obj_jugador): caminar 6 px/cuadro, correr x1.8, resistencia 220,
    /// gasto 1.3 y recuperación 0.7 por cuadro, 2 s de agotamiento.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] SpriteFrameAnimator anim;
        [SerializeField] DirectionalSprites sprites;
        [SerializeField] DirectionalSprites runSprites;
        [Tooltip("Los mismos cuadros, pero con la linterna en la mano (se usan cuando ya la recogió)")]
        [SerializeField] DirectionalSprites lightSprites;
        [SerializeField] DirectionalSprites lightRunSprites;
        [SerializeField] Flashlight flashlight;
        [SerializeField] SpriteMotion motion;
        [SerializeField] Sprite[] hideFrames;
        [SerializeField] float hideAnimSeconds = 0.3f;

        [Header("Movimiento (píxeles por cuadro del original)")]
        [SerializeField] float walkSpeedGm = 6f;
        [SerializeField] float runMultiplier = 1.8f;

        [Header("Resistencia")]
        [SerializeField] float staminaMax = 280f;        // ~5,5 s corriendo (antes ~2,8 s)
        [SerializeField] float drainPerFrame = 0.85f;
        [SerializeField] float recoverPerFrame = 0.8f;
        [SerializeField] float exhaustedSeconds = 2f;

        [Header("Cuerpo")]
        [Tooltip("Altura del centro del cuerpo sobre los pies (la colisión solo cubre los pies)")]
        [SerializeField] float bodyCenterHeight = 2.8f;

        [Header("Paso entre zonas")]
        [SerializeField] float warpFadeSeconds = 0.07f;
        [SerializeField] float warpFreezeSeconds = 0f;
        [Tooltip("Segundos sin poder ser atrapado justo después de cruzar una puerta")]
        [SerializeField] float warpInvulnerableSeconds = 0.5f;

        Rigidbody2D rb;
        BoxCollider2D body;
        Vector2 moveInput;
        float stamina;
        float exhaustedTimer;
        float warpCooldown;
        bool warping;
        float hideAnim;
        float stepTimer;
        readonly HashSet<object> locks = new HashSet<object>();

        public Facing Facing { get; private set; } = Facing.Down;
        public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;
        /// <summary>Centro del cuerpo (los pies están en Position).</summary>
        public Vector2 BodyCenter => Position + Vector2.up * bodyCenterHeight;
        /// <summary>Huella de los pies (la caja de colisión): con ella se entra a los pasos y se choca con los muebles.</summary>
        public Rect FootRect => body != null ? new Rect(Position + body.offset - body.size * 0.5f, body.size) : new Rect(Position - new Vector2(0.9f, 0f), new Vector2(1.8f, 0.9f));
        public bool IsRunning { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsExhausted => exhaustedTimer > 0f;
        public float Stamina01 => stamina / staminaMax;
        public bool IsHidden { get; private set; }
        /// <summary>Jugando en la máquina del arcade: los animatrónicos no lo ven ni pueden atraparlo.</summary>
        public bool InArcade { get; private set; }
        /// <summary>Invisible e intocable para los animatrónicos (escondido o en la máquina del arcade).</summary>
        public bool IsUndetectable => IsHidden || InArcade;
        public HidingSpot CurrentHidingSpot { get; private set; }
        public Inventory Inventory { get; private set; }
        public ZoneInfo CurrentZone => MapZones.Instance != null ? MapZones.Instance.NearestZone(Position) : null;

        public event Action<bool> HiddenChanged;
        public event Action<ZoneInfo> Warped;

        public bool CanMove
        {
            get
            {
                var gm = GameManager.Instance;
                return locks.Count == 0 && !IsHidden && !warping && gm != null && gm.IsPlaying;
            }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<BoxCollider2D>();
            Inventory = GetComponent<Inventory>();
            stamina = staminaMax;
        }

        public void Lock(object owner) => locks.Add(owner);
        public void Unlock(object owner) => locks.Remove(owner);

        float invulnerableUntil = -1f;
        ZoneInfo timerZone;

        /// <summary>Recién cruzó una puerta: ningún animatrónico puede atraparlo (0,5 s).</summary>
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public bool IsWarping => warping;
        public float WarpCooldown => warpCooldown;

        /// <summary>Segundos seguidos en la misma sala (sin contar el tiempo escondido). Lo usa el Rastreo de Alfredo.</summary>
        public float ZoneSeconds { get; private set; }
        public void ResetZoneTimer() => ZoneSeconds = 0f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (warpCooldown > 0f) warpCooldown -= dt;

            var gmNow = GameManager.Instance;
            if (gmNow != null && gmNow.IsPlaying)
            {
                var zone = CurrentZone;
                if (zone != timerZone) { timerZone = zone; ZoneSeconds = 0f; }
                else if (!IsUndetectable) ZoneSeconds += dt;
            }

            moveInput = CanMove ? TouchInput.Move : Vector2.zero;
            IsMoving = moveInput.sqrMagnitude > 0.01f;

            bool wantsRun = IsMoving && TouchInput.RunHeld && stamina > 0f && exhaustedTimer <= 0f;
            if (wantsRun)
            {
                stamina -= drainPerFrame * GameUnits.GmFps * dt;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhaustedTimer = exhaustedSeconds;
                    wantsRun = false;
                    AudioManager.PlaySfx(Sfx.Gasp);
                    AudioManager.PlaySfx(Sfx.TiredBreath);   // respiración cansada mientras se recupera
                }
            }
            else if (exhaustedTimer <= 0f)
            {
                stamina = Mathf.Min(staminaMax, stamina + recoverPerFrame * GameUnits.GmFps * dt);
            }
            if (exhaustedTimer > 0f) exhaustedTimer -= dt;
            IsRunning = wantsRun;

            Facing = FacingUtil.FromVector(moveInput, Facing);
            UpdateFootsteps(dt);
            if (hideAnim > 0f)
            {
                hideAnim -= dt;
                if (hideAnim <= 0f && IsHidden) SetVisible(false);
            }
            else if (anim != null && !IsHidden)
            {
                anim.SetFrames(FramesFor(Facing, IsRunning));
                anim.fps = IsRunning ? 12f : 8f;
                anim.Play(IsMoving);
            }

            if (!warping && warpCooldown <= 0f && !IsHidden)
            {
                var warp = Warp.Overlapping(FootRect);
                if (warp != null) StartCoroutine(WarpRoutine(warp));
            }
        }

        /// <summary>Un paso cada cierto tiempo mientras camina (más rápido y más fuerte al correr).</summary>
        void UpdateFootsteps(float dt)
        {
            if (!IsMoving || IsHidden || warping || !CanMove)
            {
                stepTimer = 0f;
                return;
            }
            stepTimer -= dt;
            if (stepTimer > 0f) return;
            stepTimer = IsRunning ? 0.2f : 0.34f;
            AudioManager.PlayStep(IsRunning ? 1f : 0.65f);
        }

        Sprite[] FramesFor(Facing f, bool running)
        {
            bool lit = flashlight != null && flashlight.HasFlashlight;
            var walk = lit && lightSprites != null && lightSprites.Get(f) != null && lightSprites.Get(f).Length > 0 ? lightSprites : sprites;
            var run = lit && lightRunSprites != null && lightRunSprites.Get(f) != null && lightRunSprites.Get(f).Length > 0 ? lightRunSprites : runSprites;
            if (running && run != null)
            {
                var r = run.Get(f);
                if (r != null && r.Length > 0) return r;
            }
            return walk.Get(f);
        }

        void FixedUpdate()
        {
            float speed = GameUnits.Speed(walkSpeedGm) * (IsRunning ? runMultiplier : 1f);
            Vector2 dir = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            rb.linearVelocity = CanMove ? dir * speed : Vector2.zero;
        }

        IEnumerator WarpRoutine(Warp warp)
        {
            warping = true;
            rb.linearVelocity = Vector2.zero;
            AudioManager.PlaySfx(Sfx.PassDoor);
            var fader = ScreenFader.Instance;
            if (fader != null && warpFadeSeconds > 0f) yield return fader.FadeOut(warpFadeSeconds);
            Teleport(warp.destination, warp.arrivalFacing);
            invulnerableUntil = Time.time + warpInvulnerableSeconds;
            Warped?.Invoke(CurrentZone);
            if (fader != null && warpFadeSeconds > 0f) yield return fader.FadeIn(warpFadeSeconds);
            if (warpFreezeSeconds > 0f) yield return new WaitForSeconds(warpFreezeSeconds);
            warping = false;
            warpCooldown = 0.75f;
        }

        public void Teleport(Vector2 position, Facing facing)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            Facing = facing;
            if (anim != null) { anim.SetFrames(FramesFor(facing, false)); anim.Play(false); }
            Physics2D.SyncTransforms();
        }

        public void Hide(HidingSpot spot)
        {
            IsHidden = true;
            CurrentHidingSpot = spot;
            rb.linearVelocity = Vector2.zero;
            if (motion != null && spot != null) motion.Vanish(spot.transform.position, spot.IsVent);
            if (anim != null && hideFrames != null && hideFrames.Length > 0)
            {
                // Animación breve de agacharse antes de desaparecer en el escondite.
                anim.Play(false);
                anim.SetFrames(hideFrames);
                anim.fps = hideFrames.Length / hideAnimSeconds;
                anim.Play(true);
                hideAnim = hideAnimSeconds;
            }
            else SetVisible(false);
            HiddenChanged?.Invoke(true);
        }

        public void Unhide(Vector2 exitPosition)
        {
            if (!IsHidden) return;
            bool wasVent = CurrentHidingSpot != null && CurrentHidingSpot.IsVent;
            IsHidden = false;
            CurrentHidingSpot = null;
            if (motion != null) motion.Appear(wasVent);
            hideAnim = 0f;
            Teleport(exitPosition, Facing);
            SetVisible(true);
            HiddenChanged?.Invoke(false);
        }

        public void SetInArcade(bool on)
        {
            InArcade = on;
            rb.linearVelocity = Vector2.zero;
            if (on) Facing = Facing.Up;
            if (anim != null) { anim.SetFrames(FramesFor(Facing, false)); anim.Play(false); }
        }

        /// <summary>Tras una captura: sale de cualquier escondite y recupera la resistencia.</summary>
        public void ResetAfterCatch()
        {
            if (CurrentHidingSpot != null) CurrentHidingSpot.ForceRelease();
            IsHidden = false;
            InArcade = false;
            CurrentHidingSpot = null;
            SetVisible(true);
            stamina = staminaMax;
            exhaustedTimer = 0f;
            hideAnim = 0f;
            locks.Clear();
            StopAllCoroutines();
            warping = false;
            ZoneSeconds = 0f;
            invulnerableUntil = -1f;
        }

        void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = visible;
        }
    }
}
