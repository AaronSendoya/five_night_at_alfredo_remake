using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Base de los animatrónicos. Esta clase NO decide qué hace cada uno: solo resuelve lo común
    /// (si está activo, la espera inicial con su espectáculo en el escenario y la captura) y ofrece
    /// herramientas (patrullar, perseguir, navegar entre zonas). Cada animatrónico decide en Think().
    /// Así se evita el problema del original, donde la lógica heredada pisaba las mecánicas propias.
    /// </summary>
    public abstract class AnimatronicBrain : MonoBehaviour
    {
        [SerializeField] protected AnimatronicId id;
        [SerializeField] protected string displayName;
        [SerializeField] protected AnimatronicMotor motor;
        [SerializeField] protected AnimatronicSenses senses;
        [SerializeField] protected Transform visual;
        [SerializeField] Sprite jumpscareSprite;
        [SerializeField] protected float catchRadius = 1.8f;

        protected PlayerController player;
        protected AnimatronicDirector director;
        protected Vector2 home;
        protected float bootRemaining;

        // Patrulla
        readonly Vector2[] breadcrumbs = new Vector2[10];
        int crumbIndex;
        float idleTimer;
        float patience;
        bool hadPath;
        ZoneInfo previousZone;

        // Navegación
        float repathTimer;
        Vector2 lastGoal;

        // Espectáculo
        float showTimer;
        bool showPoseLeft;

        public AnimatronicId Id => id;
        public string DisplayName => displayName;
        public Sprite JumpscareSprite => jumpscareSprite;
        public int AiLevel { get; private set; }
        public bool IsActive => AiLevel > 0 && bootRemaining <= 0f;
        public bool IsChasing { get; protected set; }
        public AnimatronicMotor Motor => motor;

        protected float PatrolSpeed => GameUnits.Speed(2f + AiLevel * 0.15f);
        protected float ChaseSpeed => GameUnits.Speed(2f + AiLevel * 0.15f + 1.5f);
        protected virtual bool CanCatch => true;
        protected float PatienceSeconds => Mathf.Max(5f, 14f - AiLevel * 0.4f);

        protected virtual void Awake()
        {
            home = transform.position;
            for (int i = 0; i < breadcrumbs.Length; i++) breadcrumbs[i] = home;
            motor.Warped += OnWarped;
        }

        public void Configure(PlayerController p, AnimatronicDirector d, NightConfig night)
        {
            player = p;
            director = d;
            AiLevel = night.AiLevel(id);
            bootRemaining = night.BootSeconds(id);
            patience = PatienceSeconds;
            OnConfigured(night);
        }

        protected virtual void OnConfigured(NightConfig night) { }

        /// <summary>Vuelve al escenario tras una captura; espera al menos 'grace' segundos.</summary>
        public void ResetToHome(float grace)
        {
            motor.TeleportTo(home);
            motor.Face(Facing.Down);
            senses.Forget();
            bootRemaining = Mathf.Max(bootRemaining, grace);
            EndChase();
            idleTimer = 0f; hadPath = false; patience = PatienceSeconds;
            OnResetToHome();
        }

        protected virtual void OnResetToHome() { }

        /// <summary>Solo pruebas: se activa ya, sin esperar.</summary>
        public void SkipBoot() => bootRemaining = 0f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) { motor.Freeze(); return; }
            float dt = Time.deltaTime;

            if (AiLevel <= 0 || bootRemaining > 0f)
            {
                if (bootRemaining > 0f) bootRemaining -= dt;
                Perform(dt);
                if (IsActive) EndPerform();
                return;
            }

            senses.Tick(player, dt);
            Think(dt);
            motor.Tick(dt);

            if (CanCatch && !player.IsHidden && Vector2.Distance(motor.Position, player.Position) <= catchRadius)
                gm.PlayerCaught(this);
        }

        /// <summary>Comportamiento propio de cada animatrónico.</summary>
        protected abstract void Think(float dt);

        // ---------- Espectáculo (el "baile" del original mientras están inactivos) ----------
        void Perform(float dt)
        {
            motor.Stop();
            motor.Freeze();
            if (visual == null) return;
            showTimer -= dt;
            if (showTimer > 0f) return;
            showTimer = 0.3f;
            showPoseLeft = !showPoseLeft;
            visual.localRotation = Quaternion.Euler(0f, 0f, showPoseLeft ? 5f : -5f);
        }

        void EndPerform()
        {
            if (visual != null) visual.localRotation = Quaternion.identity;
        }

        // ---------- Herramientas ----------

        /// <summary>Patrulla: puntos de la zona que no ha visitado hace poco; cuando se aburre, cambia de zona.</summary>
        protected void Patrol(float dt, float speed)
        {
            motor.Speed = speed;
            if (motor.HasPath) { hadPath = true; patience -= dt; return; }
            if (hadPath)
            {
                hadPath = false;
                RememberCrumb(motor.Position);
                idleTimer = Random.Range(GameUnits.Seconds(Mathf.Max(5f, 30f - AiLevel)), GameUnits.Seconds(Mathf.Max(20f, 75f - AiLevel * 2.5f)));
            }
            if (idleTimer > 0f) { idleTimer -= dt; return; }

            if (patience <= 0f && TryLeaveZone()) { patience = PatienceSeconds; return; }

            Vector2 best = motor.Position;
            float bestScore = -1f;
            for (int i = 0; i < 10; i++)
            {
                if (!NavGrid.Instance.RandomPointNear(motor.Position, 6f, 20f, out Vector2 p, 4)) continue;
                float score = 0f;
                foreach (var c in breadcrumbs) score += Vector2.Distance(p, c);
                if (score > bestScore) { bestScore = score; best = p; }
            }
            if (bestScore < 0f || !motor.SetDestination(best)) idleTimer = 0.3f;
        }

        static readonly System.Collections.Generic.List<Warp> exitCandidates = new System.Collections.Generic.List<Warp>();

        /// <summary>Elige un paso de su zona, evitando volver a la zona de la que viene si hay otra opción.</summary>
        bool TryLeaveZone()
        {
            var here = MapZones.Instance.NearestZone(motor.Position);
            exitCandidates.Clear();
            foreach (var w in Warp.All)
                if (w.FromZone == here && w.ToZone != previousZone) exitCandidates.Add(w);
            if (exitCandidates.Count == 0)
                foreach (var w in Warp.All)
                    if (w.FromZone == here) exitCandidates.Add(w);
            if (exitCandidates.Count == 0) return false;
            var choice = exitCandidates[Random.Range(0, exitCandidates.Count)];
            return motor.SetDestination(choice.area.center);
        }

        void RememberCrumb(Vector2 p)
        {
            breadcrumbs[crumbIndex] = p;
            crumbIndex = (crumbIndex + 1) % breadcrumbs.Length;
        }

        void OnWarped(Warp w)
        {
            previousZone = w.FromZone;
            hadPath = false;
            idleTimer = 0f;
            repathTimer = 0f;
            for (int i = 0; i < breadcrumbs.Length; i++) breadcrumbs[i] = w.destination;
        }

        /// <summary>Va hacia un punto aunque esté en otra zona, cruzando los pasos necesarios.</summary>
        protected bool NavigateTo(Vector2 target, float speed, float dt, float repathEvery = 0.25f)
        {
            motor.Speed = speed;
            var zones = MapZones.Instance;
            var from = zones.NearestZone(motor.Position);
            var to = zones.NearestZone(target);
            Vector2 goal = target;
            if (from != to)
            {
                var w = ZoneRouter.NextWarp(from, to);
                if (w == null) return false;
                goal = w.area.center;
            }
            repathTimer -= dt;
            if (repathTimer <= 0f || !motor.HasPath || (goal - lastGoal).sqrMagnitude > 1f)
            {
                repathTimer = repathEvery;
                lastGoal = goal;
                if (!motor.SetDestination(goal)) return false;
            }
            return true;
        }

        protected bool Arrived(Vector2 target, float tolerance = 1f) => Vector2.Distance(motor.Position, target) <= tolerance;

        /// <summary>
        /// Persigue al jugador mientras lo ve y, si lo pierde, va a revisar dónde lo vio por última vez.
        /// Devuelve false cuando no hay nada que perseguir (o el director no le da turno de persecución).
        /// </summary>
        protected bool HuntPlayer(float speed, float dt, float searchSeconds = 4f)
        {
            if (senses.CanSeePlayer)
            {
                if (!IsChasing && !director.RequestChase(this)) return false;
                IsChasing = true;
                NavigateTo(player.Position, speed, dt);
                return true;
            }
            if (IsChasing && senses.TimeSinceSeen < searchSeconds && !Arrived(senses.LastSeenPosition))
            {
                NavigateTo(senses.LastSeenPosition, speed, dt);
                return true;
            }
            EndChase();
            return false;
        }

        protected void EndChase()
        {
            if (!IsChasing) return;
            IsChasing = false;
            director.ReleaseChase(this);
            motor.Stop();
        }
    }
}
