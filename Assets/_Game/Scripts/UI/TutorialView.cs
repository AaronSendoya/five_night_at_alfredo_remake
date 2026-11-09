using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Tutorial de la primera noche (obj_tutorial_controller), adaptado a controles táctiles.</summary>
    public class TutorialView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text bodyText;
        [SerializeField] Text stepText;
        [SerializeField] Image picture;
        [SerializeField] Sprite[] pictures = new Sprite[14];

        public static readonly string[] Steps =
        {
            "BIENVENIDO A ALFREDO'S PIZZA\n\nEres el nuevo guardia nocturno de una pizzería cerrada al público.\nSus cuatro animatrónicos siguen encendidos... y de noche se pasean.",
            "MOVERTE\n\nArrastra el pulgar sobre el joystick de la izquierda.\nCuanto más lo alejes del centro, más rápido caminas.",
            "CORRER\n\nMantén pulsado el botón Correr (abajo a la derecha).\nGasta la barra de RESISTENCIA: si se vacía, quedas agotado unos segundos.",
            "BOTÓN USAR\n\nCerca de algo útil aparece el botón grande con un ícono (mano, cámara, fusible...).\nTócalo para usarlo. Algunos, como las ventilaciones, hay que mantenerlos.",
            "LA LUZ Y LOS FUSIBLES\n\nArriba a la derecha ves la luz y los fusibles del panel.\nCada cierto tiempo se quema uno: con uno menos la luz falla, con dos o más hay apagón.",
            "REPARAR FUSIBLES\n\nBusca fusibles de repuesto (brillan en el suelo) y recógelos con Usar.\nLlévalos al panel de Mantenimiento y mantén Usar para instalarlos.",
            "ESCONDERSE\n\nCasilleros, mesas y la piscina de pelotas sirven de escondite: acércate y toca Usar.\nNo te verán, pero el aire se acaba. Toca Salir antes de quedarte sin aire.",
            "VENTILACIONES\n\nMantén Usar junto a una rejilla para entrar. Verás el mapa:\ntoca una sala con rejilla para salir allí, o el botón de salida para volver.",
            "CÁMARAS\n\nEn el escritorio de la oficina está el monitor.\nToca cualquier sala del mapa para ver su cámara. Sin luz, no funciona.",
            "LINTERNA Y MANIVELA\n\nRecoge la linterna en la oficina y enciéndela con su botón.\nSi se queda sin batería, mantén el botón de la manivela para recargarla.",
            "PUERTAS DE LA OFICINA\n\nJunto a cada puerta hay una palanca: úsala para cerrar o abrir la cortina.\nCerradas frenan a los animatrónicos, pero gastan más luz y queman antes los fusibles.",
            "LA MÁQUINA DEL ARCADE\n\nEn el Arcade hay una máquina que todavía funciona. Atrapa 10 pizzas y ganas un fusible.\nMientras juegas, los animatrónicos no pueden verte ni atraparte.",
            "CONOCE A TUS COMPAÑEROS\n\nAlfredo embiste riendo. Matilda sabotea los fusibles. Bongo patrulla sin descanso.\nRufus caza a oscuras y huye de la linterna. Si corres, te oyen.",
            "OBJETIVO\n\nSobrevive de las 12:00 AM a las 6:00 AM.\nNo dejes que te atrapen y mantén la luz encendida.\n\n¡Buena suerte!"
        };

        int step;

        void Start()
        {
            GameManager.Instance.StateChanged += OnState;
            OnState(GameManager.Instance.State);
        }

        void OnState(GameState s)
        {
            bool show = s == GameState.Tutorial;
            panel.SetActive(show);
            if (show) { step = 0; Refresh(); }
        }

        public void Next()
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            step++;
            if (step >= Steps.Length) { GameManager.Instance.FinishTutorial(); return; }
            Refresh();
        }

        public void Skip()
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            GameManager.Instance.FinishTutorial();
        }

        void Refresh()
        {
            bodyText.text = Steps[step];
            stepText.text = $"{step + 1} / {Steps.Length}";
            Sprite s = step < pictures.Length ? pictures[step] : null;
            picture.gameObject.SetActive(s != null);
            picture.sprite = s;
            picture.preserveAspect = true;
        }
    }
}
