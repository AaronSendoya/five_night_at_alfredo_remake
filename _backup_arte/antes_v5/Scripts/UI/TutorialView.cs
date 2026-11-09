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
        [SerializeField] Sprite[] pictures = new Sprite[10];

        static readonly string[] Steps =
        {
            "BIENVENIDO A CINCO NOCHES CON ALFREDO\n\nTu turno de noche empieza ahora.",
            "MOVERTE\n\nArrastra el pulgar en la mitad izquierda de la pantalla.",
            "CORRER\n\nMantén el botón Correr.\nCorrer gasta resistencia; si se agota, quedas agotado unos segundos.",
            "SISTEMA ELÉCTRICO\n\nCada cierto tiempo puede quemarse un fusible.\nCon un fusible menos la luz falla; con dos o más, apagón.",
            "REPARAR FUSIBLES\n\n1. Busca fusibles de repuesto por el mapa.\n2. Recógelos con Usar.\n3. Ve al panel en Mantenimiento.\n4. Mantén Usar para instalarlos.",
            "ESCONDERSE\n\nAcércate a un escondite y pulsa Usar.\nLos animatrónicos no te verán, pero el aire se acaba.\nEn las ventilaciones puedes salir por otra salida.",
            "CÁMARAS Y LINTERNA\n\nEn la oficina hay un monitor de cámaras y una linterna.\nSi la linterna se queda sin batería, mantén Manivela.",
            "PUERTAS DE LA OFICINA\n\nJunto a cada puerta de la oficina hay una placa: úsala para cerrarla o abrirla.\nCerradas frenan a los animatrónicos, pero gastan más luz y los fusibles se queman antes.\nSi se va la luz, se abren solas.",
            "ESCUCHA\n\nCada animatrónico hace un ruido propio al moverse: si suena fuerte, está cerca.\nRufus solo caza a oscuras y huye de la linterna.\nCon 2 fusibles o menos, algo despierta en el cuarto secreto.",
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
