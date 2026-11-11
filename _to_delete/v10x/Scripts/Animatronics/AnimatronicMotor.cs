using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Movimiento de un animatrónico sobre la cuadrícula de rutas. No decide nada: solo va adonde
    /// le indica su cerebro, anima el sprite según la dirección y cruza los pasos entre zonas.
    /// </summary>
    public class AnimatronicMotor : MonoBehaviour
    {
        [SerializeField] SpriteFrameAnimator anim;
        [SerializeField] DirectionalSprites sprites;
        [SerializeField] float warpPauseSeconds = 0.5f;
        [SerializeField] float warpCooldownSeconds = 2f;

        readonly List<Vector2> path = new List<Vector2>(64);
        int pathIndex;
        float pauseTimer;
        float warpCooldown;
        float stuckTimer;
        Vector2 lastPos;

        public float Speed { get; set; } = 3f;
        public Vector2 Position => transform.position;
        public Facing Facing { get; private set; } = Facing.Down;
        public Vector2 FacingDir => Facing.ToVector();
        public bool HasPath => pathIndex < path.Count;
        public bool IsMoving => HasPath && pauseTimer <= 0f;
        public Vector2 Destination => path.Count > 0 ? path[path.Count - 1] : Position;
        public SpriteFrameAnimator Animator => anim;

        public event Action<Warp> Warped;

        // Pulido visual: balanceo al caminar y postura de ataque al estar por atrapar al jugador.
        Transform visual;
        Vector3 visualBasePos;
        Vector3 visualBaseScale;
        float bobPhase;
        float attack01;
        bool attacking;
        Vector2 attackDir;

        void Awake()
        {
            if (anim != null)
            {
                visual = anim.transform;
                visualBasePos = visual.localPosition;
                visualBaseScale = visual.localScale;
            }
        }

        /// <summary>Lo llama el cerebro: muy cerca del jugador se estira hacia él (postura de ataque).</summary>
        public void SetAttack(bool on, Vector2 towards)
        {
            attacking = on;
            if (towards.sqrMagnitude > 0.0001f) attackDir = towards.normalized;
        }

        void LateUpdate()
        {
            if (visual == null) return;
            attack01 = Mathf.MoveTowards(attack01, attacking ? 1f : 0f, Time.deltaTime * 6f);
            if (motion == null) motion = visual.GetComponent<SpriteMotion>();
            if (motion != null) motion.SetAttack(attack01, attackDir);
        }

        SpriteMotion motion;

        public bool SetDestination(Vector2 target)
        {
            pathIndex = 0;
            if (NavGrid.Instance == null || !NavGrid.Instance.FindPath(Position, target, path))
            {
                path.Clear();
                return false;
            }
            stuckTimer = 0f;
            return true;
        }

        public void Stop()
        {
            path.Clear();
            pathIndex = 0;
        }

        public void Pause(float seconds) => pauseTimer = Mathf.Max(pauseTimer, seconds);

        public void Face(Facing f)
        {
            Facing = f;
            if (anim != null) anim.SetFrames(sprites.Get(f));
        }

        public void TeleportTo(Vector2 p)
        {
            Stop();
            transform.position = p;
            lastPos = p;
        }

        /// <summary>Avanza un paso. Lo llama el cerebro solo cuando el animatrónico está activo.</summary>
        public void Tick(float dt)
        {
            if (warpCooldown > 0f) warpCooldown -= dt;
            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                if (anim != null) anim.Play(false);
                return;
            }
            if (!HasPath)
            {
                if (anim != null) anim.Play(false);
                return;
            }

            Vector2 pos = Position;
            Vector2 target = path[pathIndex];
            Vector2 delta = target - pos;
            float step = Speed * dt;
            if (delta.magnitude <= step)
            {
                pos = target;
                pathIndex++;
            }
            else pos += delta.normalized * step;

            if (delta.sqrMagnitude > 0.0001f) Face(FacingUtil.FromVector(delta, Facing));
            transform.position = pos;
            if (anim != null)
            {
                anim.fps = Mathf.Clamp(Speed * 1.1f, 6f, 14f);
                anim.Play(true);
            }

            stuckTimer = (pos - lastPos).sqrMagnitude < 0.0001f ? stuckTimer + dt : 0f;
            lastPos = pos;
            if (stuckTimer > 1f) Stop();

            if (warpCooldown <= 0f)
            {
                var w = Warp.At(pos);
                if (w != null) DoWarp(w);
            }
        }

        void DoWarp(Warp w)
        {
            TeleportTo(w.destination);
            Face(w.arrivalFacing);
            pauseTimer = warpPauseSeconds;
            warpCooldown = warpCooldownSeconds;
            Warped?.Invoke(w);
        }

        public void Freeze()
        {
            if (anim != null) anim.Play(false);
        }
    }
}
