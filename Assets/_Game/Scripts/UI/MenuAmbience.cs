using UnityEngine;

namespace Alfredo
{
    /// <summary>Música de fondo del menú principal. Solo existe en la escena del menú: el juego no la usa.</summary>
    public class MenuAmbience : MonoBehaviour
    {
        void Start()
        {
            // La música sustituye al zumbido y al ambiente de la oficina, que en el menú se mezclaban con ella.
            AudioManager.SetAmbience(false);
            AudioManager.SetTension(0f);
            AudioManager.PlayMenuMusic();
        }

        void OnDestroy() => AudioManager.StopMenuMusic();
    }
}
