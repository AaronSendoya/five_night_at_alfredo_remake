namespace Alfredo
{
    /// <summary>Bongo (Bonnie con guitarra): el patrullero. Recorre las zonas y persigue en cuanto te ve.</summary>
    public class BongoBrain : AnimatronicBrain
    {
        /// <summary>Pasiva: oído fino. Oye el ruido del jugador hasta 2 salas de distancia (los demás, 1).</summary>
        public override int NoiseHearRooms => 2;

        protected override void Think(float dt)
        {
            if (HuntPlayer(ChaseSpeed, dt, 5f)) return;
            Patrol(dt, PatrolSpeed);
        }
    }
}
