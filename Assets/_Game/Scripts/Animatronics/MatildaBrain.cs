using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Matilda (Chica): la saboteadora. Patrulla como los demás, pero cada cierto tiempo va al panel
    /// y quema un fusible. Durante la misión ignora al jugador. Corrige el original, donde el tiempo
    /// de sabotaje solo corría cuando estaba quieta.
    /// </summary>
    public class MatildaBrain : AnimatronicBrain
    {
        enum Mode { Normal, Travel, Work }

        [SerializeField] FusePanel panel;
        [SerializeField] float firstSabotageSeconds = 30f;
        [SerializeField] float sabotageCooldownSeconds = 60f;
        [SerializeField] float workSeconds = 2f;
        [SerializeField] float missionSpeedPx = 3.5f;

        Mode mode;
        float sabotageTimer;
        float workTimer;

        protected override void OnConfigured(NightConfig night)
        {
            mode = Mode.Normal;
            sabotageTimer = firstSabotageSeconds;
            // Antes saboteaba cada 15 s y los fusibles no duraban: ahora de 70 s (IA baja) a 40 s (IA 20).
            sabotageCooldownSeconds = Mathf.Lerp(70f, 40f, Mathf.Clamp01(night.AiLevel(id) / 20f));
        }

        protected override void OnResetToHome()
        {
            mode = Mode.Normal;
            sabotageTimer = Mathf.Max(sabotageTimer, firstSabotageSeconds);
        }

        protected override void Think(float dt)
        {
            switch (mode)
            {
                case Mode.Normal:
                    sabotageTimer -= dt;
                    if (sabotageTimer <= 0f && StartMission()) return;
                    if (HuntPlayer(ChaseSpeed, dt)) return;
                    Patrol(dt, PatrolSpeed);
                    break;

                case Mode.Travel:
                    if (Arrived(panel.WorkSpot, 1.2f))
                    {
                        mode = Mode.Work;
                        workTimer = workSeconds;
                        motor.Stop();
                        motor.Face(Facing.Up);
                        break;
                    }
                    if (!NavigateTo(panel.WorkSpot, Px(missionSpeedPx), dt, 1f))
                    {
                        mode = Mode.Normal;
                        sabotageTimer = 2f;
                    }
                    break;

                case Mode.Work:
                    motor.Face(Facing.Up);
                    workTimer -= dt;
                    if (workTimer > 0f) break;
                    PowerSystem.Instance.BurnRandomFuse();
                    mode = Mode.Normal;
                    sabotageTimer = sabotageCooldownSeconds;
                    break;
            }
        }

        bool StartMission()
        {
            if (panel == null || PowerSystem.Instance.ActiveFuses == 0)
            {
                sabotageTimer = sabotageCooldownSeconds;
                return false;
            }
            EndHunt();
            mode = Mode.Travel;
            return true;
        }
    }
}
