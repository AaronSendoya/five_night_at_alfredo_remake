namespace Alfredo
{
    /// <summary>Bongo (Bonnie con guitarra): el patrullero. Recorre las zonas y persigue en cuanto te ve.</summary>
    public class BongoBrain : AnimatronicBrain
    {
        protected override void Think(float dt)
        {
            if (HuntPlayer(ChaseSpeed, dt, 5f)) return;
            Patrol(dt, PatrolSpeed);
        }
    }
}
