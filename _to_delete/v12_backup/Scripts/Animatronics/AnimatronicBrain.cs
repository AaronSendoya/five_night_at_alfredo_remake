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
        [Tooltip("Grito propio al atraparte")]
        [SerializeField] AudioClip scream;
        [Tooltip("Frases (en inglés) que dice tras atraparte: se elige una al azar, sin repetir la anterior")]
        [SerializeField] AudioClip[] deathLines;
        int lastLine = -1;
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
        float zoneTimer;
        ZoneInfo roamTarget;
        Vector2 roamGoal;
        float roamStuck;
        int failedPicks;

        // Navegación
        float repathTimer;
        Vector2 lastGoal;

        // Caza: persecución -> rastreo -> búsqueda (en vez de volver a patrullar de golpe)
        enum HuntPhase { None, Chase, Track, Search }
        HuntPhase phase;
        float trackTimer;
        float searchTimer;
        bool searchReached;
        Vector2 searchCenter;
        float lookTimer;
        [SerializeField] float trackSeconds = 1.8f;

        // Espectáculo
        float showTimer;
        bool showPoseLeft;

        public AnimatronicId Id => id;
        public string DisplayName => displayName;
        public Sprite JumpscareSprite => jumpscareSprite;
        public AudioClip Scream => scream;
        public AudioClip NextDeathLine()
        {
            if (deathLines == null || deathLines.Length == 0) return null;
            int i = Random.Range(0, deathLines.Length);
            if (deathLines.Length > 1 && i == lastLine) i = (i + 1) % deathLines.Length;
            lastLine = i;
            return deathLines[i];
        }
        public int AiLevel { get; private set; }
        public bool IsActive => AiLevel > 0 && bootRemaining <= 0f;
        /// <summary>Solo pruebas (F8): congela a todos los animatrónicos.</summary>
        public static bool DebugFreeze;
        public bool IsChasing { get; protected set; }
        /// <summary>Activo esta noche o actuando igualmente (p. ej. Alfredo en modo Rastreo).</summary>
        public bool IsAwake => IsActive || ForceAwake;
        public AnimatronicMotor Motor => motor;

        /// <summary>
        /// Multiplicador de velocidad por noche: la noche 1 es la base y cada noche siguiente
        /// es un 10 % más rápida (noche 4 = x1,30). Lo usan todos los animatrónicos.
        /// </summary>
        protected float NightSpeed { get; private set; } = 1f;
        /// <summary>Ajuste global: +10 % al patrullar y +12 % al perseguir respecto al original.
        /// Noche 1: persiguen a ~4,8 px/f (el jugador camina a 6 y corre a 10,8); noche 4 (x1,30): ~6,2, hay que correr.</summary>
        const float MoveBoost = 1.1f, ChaseBoost = 1.12f;
        protected float PatrolSpeed => GameUnits.Speed(2.75f) * NightSpeed * MoveBoost;
        protected float ChaseSpeed => GameUnits.Speed(4.25f) * NightSpeed * ChaseBoost;
        /// <summary>Convierte píxeles por fotograma del original a unidades/s, ya con el ajuste de la noche (al moverse).</summary>
        protected float Px(float pixelsPerFrame) => GameUnits.Speed(pixelsPerFrame) * NightSpeed * MoveBoost;
        /// <summary>Igual que Px pero para persecuciones/embestidas.</summary>
        protected float PxChase(float pixelsPerFrame) => GameUnits.Speed(pixelsPerFrame) * NightSpeed * ChaseBoost;
        protected virtual bool CanCatch => true;
        /// <summary>Permite actuar aunque esté dormido esta noche (IA 0) o aún no haya despertado (Rastreo de Alfredo).</summary>
        protected virtual bool ForceAwake => false;
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
            NightSpeed = 1f + 0.10f * Mathf.Max(0, night.night - 1);
            patience = PatienceSeconds;
            zoneTimer = 0f; roamTarget = null;
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
            EndHunt();
            idleTimer = 0f; hadPath = false; patience = PatienceSeconds;
            zoneTimer = 0f; roamTarget = null; director?.ClearRoam(this);
            OnResetToHome();
        }

        protected virtual void OnResetToHome() { }

        /// <summary>Solo pruebas: se activa ya, sin esperar.</summary>
        public void SkipBoot() => bootRemaining = 0f;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || DebugFreeze) { motor.Freeze(); return; }
            float dt = Time.deltaTime;

            if ((AiLevel <= 0 || bootRemaining > 0f) && !ForceAwake)
            {
                if (bootRemaining > 0f) bootRemaining -= dt;
                Perform(dt);
                if (IsActive) EndPerform();
                return;
            }
            if (!IsActive) EndPerform();

            senses.Tick(player, dt);
            if (IsHeldOff)
            {
                // Zona segura al cruzar una puerta: se aparta del jugador y no ataca.
                motor.Tick(dt);
                motor.SetAttack(false, Vector2.zero);
                return;
            }
            Think(dt);
            motor.Tick(dt);

            Vector2 toPlayer = player.Position - motor.Position;
            motor.SetAttack(CanCatch && IsHunting && !player.IsUndetectable && toPlayer.magnitude <= catchRadius * 2.2f, toPlayer);

            if (CanCatch && !player.IsUndetectable && !player.IsInvulnerable && Vector2.Distance(motor.Position, player.Position) <= catchRadius)
                gm.PlayerCaught(this);
        }

        float holdOffUntil;
        public bool IsHeldOff => Time.time < holdOffUntil;

        /// <summary>
        /// El jugador acaba de entrar por una puerta cerca de este animatrónico: durante 'seconds' no ataca
        /// y retrocede unos pasos alejándose de 'from' (el punto de llegada), para no quedarse pegado a la puerta.
        /// </summary>
        public void HoldOff(Vector2 from, float seconds)
        {
            holdOffUntil = Time.time + seconds;
            Vector2 away = motor.Position - from;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitCircle;
            away.Normalize();
            motor.Speed = PatrolSpeed;
            bool moved = false;
            foreach (float ang in new[] { 0f, 35f, -35f, 70f, -70f, 110f, -110f })
            {
                Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, ang) * away);
                for (float dist = 6f; dist >= 3f && !moved; dist -= 1.5f)
                    moved = motor.SetDestination(motor.Position + dir * dist);
                if (moved) break;
            }
            if (!moved) motor.Stop();
        }

        /// <summary>Comportamiento propio de cada animatrónico.</summary>
        protected abstract void Think(float dt);

        // ---------- Espectáculo (el "baile" del original mientras están inactivos) ----------
        void Perform(float dt)
        {
            motor.SetAttack(false, Vector2.zero);
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

        /// <summary>
        /// Patrulla: recorre puntos de su sala que no visitó hace poco, lejos de otros animatrónicos.
        /// No se queda a vivir en una sala: tras un rato (antes si comparte la sala con otro) elige otra
        /// sala del mapa con el director, para que no "campeen" todos juntos en el mismo sitio.
        /// </summary>
        protected void Patrol(float dt, float speed)
        {
            motor.Speed = speed;
            var here = MapZones.Instance.NearestZone(motor.Position);
            bool crowded = director.Occupancy(here, this) > 0 && roamTarget == null;
            zoneTimer += dt;

            // Viaje a otra sala en curso.
            if (roamTarget != null)
            {
                if (here == roamTarget)
                {
                    roamTarget = null;
                    director.ClearRoam(this);
                    zoneTimer = 0f;
                    patience = PatienceSeconds;
                    hadPath = false;
                    idleTimer = 0f;
                }
                else
                {
                    roamStuck += dt;
                    if (roamStuck > 25f || !NavigateTo(roamGoal, speed, dt, 0.6f))
                    {
                        // La sala no es alcanzable ahora (puerta cerrada, por ejemplo): elige otra.
                        roamTarget = null;
                        director.ClearRoam(this);
                        if (!StartRoam(here)) TryLeaveZone();
                    }
                    return;
                }
            }

            if (motor.HasPath) { hadPath = true; patience -= dt * (crowded ? 3f : 1f); return; }
            if (hadPath)
            {
                hadPath = false;
                RememberCrumb(motor.Position);
                idleTimer = Random.Range(GameUnits.Seconds(Mathf.Max(5f, 30f - AiLevel)), GameUnits.Seconds(Mathf.Max(20f, 75f - AiLevel * 2.5f)));
                if (crowded) idleTimer *= 0.4f;
            }
            if (idleTimer > 0f)
            {
                idleTimer -= dt;
                if (crowded) patience -= dt * 2f;
                // Quieto no es ciego: gira la cabeza de vez en cuando.
                lookTimer -= dt;
                if (lookTimer <= 0f)
                {
                    lookTimer = Random.Range(1.1f, 2.2f);
                    motor.Face((Facing)Random.Range(0, 4));
                }
                return;
            }

            if (patience <= 0f || zoneTimer > MaxZoneSeconds)
            {
                patience = PatienceSeconds;
                if (StartRoam(here) || TryLeaveZone()) return;
            }

            Vector2 best = motor.Position;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < 10; i++)
            {
                if (!NavGrid.Instance.RandomPointNear(motor.Position, 6f, 20f, out Vector2 p, 4)) continue;
                float score = 0f;
                foreach (var c in breadcrumbs) score += Vector2.Distance(p, c);
                // Lejos de los demás: si dos patrullan la misma sala, cada uno toma un lado.
                score += Mathf.Min(director.NearestOther(this, p), 20f) * 4f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            if (float.IsNegativeInfinity(bestScore) || !motor.SetDestination(best))
            {
                idleTimer = 0.3f;
                if (++failedPicks >= 4) { failedPicks = 0; if (!StartRoam(here)) TryLeaveZone(); }
            }
            else failedPicks = 0;
        }

        /// <summary>Segundos máximos patrullando una misma sala antes de cambiar (menos con más IA).</summary>
        float MaxZoneSeconds => Mathf.Lerp(30f, 16f, Mathf.Clamp01(AiLevel / 20f));

        bool StartRoam(ZoneInfo here)
        {
            var target = director.PickRoamZone(this, here);
            if (target == null) return false;
            if (!NavGrid.Instance.RandomPointInZone(target, out roamGoal)) { director.ClearRoam(this); return false; }
            roamTarget = target;
            roamStuck = 0f;
            if (!NavigateTo(roamGoal, motor.Speed, 0f, 0.6f))
            {
                roamTarget = null;
                director.ClearRoam(this);
                return false;
            }
            return true;
        }

        static readonly System.Collections.Generic.List<Warp> exitCandidates = new System.Collections.Generic.List<Warp>();

        /// <summary>Plan B: un paso cualquiera de su zona, evitando volver a la zona de la que viene.</summary>
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
            zoneTimer = 0f;
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

        public bool IsHunting => phase != HuntPhase.None;

        /// <summary>
        /// Caza en tres fases para que no "olvide" al jugador de golpe:
        /// 1) Persecución mientras lo ve (ve en 360° y algo más lejos mientras está alerta).
        /// 2) Rastreo: al perderlo de vista sigue su rastro real un momento (cruza puertas detrás de él).
        /// 3) Búsqueda: va al último punto conocido y revisa los alrededores unos segundos.
        /// Si el director no le da turno de persecución, igual se acerca, pero al paso.
        /// Escondido o jugando en la máquina del arcade, el jugador no deja rastro: solo busca donde lo vio.
        /// Devuelve false cuando no hay nada que cazar.
        /// </summary>
        protected bool HuntPlayer(float speed, float dt, float searchSeconds = 5f)
        {
            bool hidden = player.IsUndetectable;
            if (senses.CanSeePlayer)
            {
                bool granted = IsChasing || director.RequestChase(this);
                if (granted) IsChasing = true;
                phase = HuntPhase.Chase;
                senses.Alerted = true;
                NavigateTo(player.Position, granted ? speed : Mathf.Lerp(PatrolSpeed, speed, 0.35f), dt, 0.2f);
                return true;
            }

            if (phase == HuntPhase.None)
            {
                // Lo oye correr (o caminar muy cerca): va a investigar sin perseguir todavía.
                if (!senses.HeardPlayer) return false;
                StartSearch(senses.LastKnownPosition, searchSeconds * 0.8f);
            }

            if (phase == HuntPhase.Chase)
            {
                phase = HuntPhase.Track;
                trackTimer = hidden ? 0f : trackSeconds;
            }

            if (phase == HuntPhase.Track)
            {
                trackTimer -= dt;
                if (!hidden && trackTimer > 0f && NavigateTo(player.Position, speed, dt, 0.2f)) return true;
                StartSearch(hidden ? senses.LastSeenPosition : player.Position, searchSeconds);
            }

            // Búsqueda
            searchTimer -= dt;
            if (senses.HeardPlayer)
            {
                searchCenter = senses.LastKnownPosition;
                searchReached = false;
                searchTimer = Mathf.Max(searchTimer, 2.5f);
            }
            if (searchTimer <= 0f) { EndHunt(); return false; }
            if (!searchReached)
            {
                if (Arrived(searchCenter, 1.2f) || !NavigateTo(searchCenter, speed * 0.85f, dt, 0.4f))
                {
                    searchReached = true;
                    motor.Stop();
                }
                return true;
            }
            motor.Speed = PatrolSpeed * 1.15f;
            if (!motor.HasPath)
            {
                lookTimer -= dt;
                if (lookTimer > 0f) return true;
                lookTimer = Random.Range(0.4f, 0.9f);
                if (!NavGrid.Instance.RandomPointNear(searchCenter, 2f, 7f, out Vector2 p, 4) || !motor.SetDestination(p))
                    motor.Face((Facing)Random.Range(0, 4));
            }
            return true;
        }

        /// <summary>Para persecuciones propias (embestida de Alfredo, caza de Rufus): si lo pierde, rastrea y busca.</summary>
        protected void MarkChasing()
        {
            IsChasing = true;
            phase = HuntPhase.Chase;
            senses.Alerted = true;
        }

        void StartSearch(Vector2 center, float seconds)
        {
            // La búsqueda no ocupa turno de persecución: otro animatrónico puede perseguir mientras tanto.
            if (IsChasing) { IsChasing = false; director.ReleaseChase(this); }
            phase = HuntPhase.Search;
            searchCenter = center;
            searchReached = false;
            searchTimer = seconds;
            senses.Alerted = true;
        }

        /// <summary>Termina cualquier fase de caza y vuelve a la rutina.</summary>
        protected void EndHunt()
        {
            phase = HuntPhase.None;
            senses.Alerted = false;
            EndChase();
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
