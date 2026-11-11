using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo.EditorTools
{
    /// <summary>Escena del menú principal: fondo en pixel art con los cuatro animatrónicos y botones translúcidos.</summary>
    public static class MenuSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Menu.unity";
        static readonly Vector2 C = new Vector2(0.5f, 0.5f);

        /// <summary>Botón translúcido con borde fino: deja ver el fondo y se ilumina al tocarlo.</summary>
        static Button Styled(Button b, bool accent = false)
        {
            var img = b.GetComponent<Image>();
            img.color = Color.white;
            var colors = b.colors;
            colors.normalColor = accent ? new Color(0.55f, 0.06f, 0.06f, 0.62f) : new Color(0.06f, 0.06f, 0.08f, 0.5f);
            colors.highlightedColor = accent ? new Color(0.7f, 0.1f, 0.1f, 0.75f) : new Color(0.2f, 0.2f, 0.24f, 0.62f);
            colors.pressedColor = new Color(0.8f, 0.15f, 0.15f, 0.85f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.35f);
            colors.colorMultiplier = 1f;
            b.colors = colors;
            var o = b.gameObject.AddComponent<Outline>();
            o.effectColor = accent ? new Color(1f, 0.55f, 0.45f, 0.55f) : new Color(0.95f, 0.9f, 0.8f, 0.28f);
            o.effectDistance = new Vector2(2f, -2f);
            var label = UiFactory.Label(b);
            if (label != null) label.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return b;
        }

        const string HistoryText =
            "En los años 80, Alfredo's Pizza era la pizzería de los cumpleaños del barrio. Su gran atracción eran cuatro " +
            "animatrónicos que cantaban sobre el escenario: Alfredo el oso, Matilda la gallina, Bongo el conejo de la guitarra " +
            "y Rufus, el zorro pirata.\n\n" +
            "Después de una serie de «incidentes» que nunca se aclararon, el local cerró sus salas al público. Aun así, los dueños " +
            "pagan a un guardia nocturno: el edificio es viejo, los fusibles se queman y los animatrónicos no se pueden apagar, " +
            "o sus motores se traban.\n\n" +
            "De noche, su programación los hace pasear. Si ven a alguien sin traje, creen que es un endoesqueleto suelto " +
            "y tratan de «ayudarlo» a ponerse uno... con toda su fuerza de robot.\n\n" +
            "Nadie habla del cuarto que hay detrás del escenario. Los empleados antiguos dicen que allí guardaron los trajes viejos, " +
            "y que algo más se quedó con ellos. Tú solo tienes que sobrevivir hasta las 6:00 AM, noche tras noche. ¿Qué tan difícil puede ser?";

        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var white = BuilderUtil.GeneratedSprite("blanco", 4, false);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;

            var audioGo = new GameObject("Audio");
            var audio = audioGo.AddComponent<AudioManager>();
            AudioBuilder.Configure(audio, true);
            audioGo.AddComponent<MenuAmbience>();

            var canvas = UiFactory.MakeCanvas("Menu").transform;
            var bgRt = UiFactory.Stretch("Fondo", canvas);
            var bg = UiFactory.AddImage(bgRt, Color.white, false, BuilderUtil.First("sp_menu_pixel"));
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bg.sprite != null ? bg.sprite.rect.width / bg.sprite.rect.height : 16f / 9f;
            var shade = UiFactory.FullPanel("Sombra", canvas, new Color(0f, 0f, 0f, 0.1f));
            shade.raycastTarget = false;

            var menu = canvas.gameObject.AddComponent<MenuController>();

            // Principal
            var main = UiFactory.Stretch("Principal", canvas);
            var title = UiFactory.AddText("Titulo", main, "CINCO NOCHES\nCON ALFREDO", 96, new Vector2(0.2f, 1f), new Vector2(0f, -170f), new Vector2(760f, 240f), TextAnchor.MiddleCenter, new Color(0.96f, 0.9f, 0.8f), FontStyle.Bold);
            var ts = title.gameObject.AddComponent<Shadow>();
            ts.effectColor = new Color(0.45f, 0f, 0f, 0.9f);
            ts.effectDistance = new Vector2(5f, -5f);
            var progress = UiFactory.AddText("Progreso", main, "", 30, new Vector2(0.2f, 1f), new Vector2(0f, -310f), new Vector2(760f, 44f), TextAnchor.MiddleCenter, new Color(0.8f, 0.78f, 0.72f));
            var anchor = new Vector2(0.2f, 0.5f);
            var cont = Styled(UiFactory.AddButton("Continuar", main, "Continuar", anchor, new Vector2(0f, 120f), new Vector2(520f, 92f), menu.Continue, 40), true);
            Styled(UiFactory.AddButton("Elegir noche", main, "Elegir noche", anchor, new Vector2(0f, 12f), new Vector2(520f, 92f), menu.ShowNights));
            Styled(UiFactory.AddButton("Nueva partida", main, "Nueva partida", anchor, new Vector2(0f, -96f), new Vector2(520f, 92f), menu.NewGame));
            Styled(UiFactory.AddButton("Opciones", main, "Opciones", anchor, new Vector2(-133f, -204f), new Vector2(254f, 92f), menu.ShowOptions, 36));
            Styled(UiFactory.AddButton("Historia", main, "Historia", anchor, new Vector2(133f, -204f), new Vector2(254f, 92f), menu.ShowHistory, 36));
            Styled(UiFactory.AddButton("Creditos", main, "Créditos", anchor, new Vector2(-133f, -312f), new Vector2(254f, 92f), menu.ShowCredits, 36));
            Styled(UiFactory.AddButton("Salir", main, "Salir", anchor, new Vector2(133f, -312f), new Vector2(254f, 92f), menu.Quit, 36));

            // Noches
            var nights = UiFactory.FullPanel("Noches", canvas, new Color(0f, 0f, 0f, 0.8f));
            UiFactory.AddText("Titulo", nights.transform, "ELEGIR NOCHE", 80, C, new Vector2(0f, 330f), new Vector2(1200f, 110f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            var nightButtons = new Object[SaveSystem.MaxNight];
            var nightLabels = new Object[SaveSystem.MaxNight];
            for (int i = 0; i < SaveSystem.MaxNight; i++)
            {
                var b = Styled(UiFactory.AddButton("Noche " + (i + 1), nights.transform, "Noche " + (i + 1), C, new Vector2(0f, 180f - i * 120f), new Vector2(620f, 100f), null));
                UnityEventTools.AddIntPersistentListener(b.onClick, menu.PlayNight, i + 1);
                nightButtons[i] = b;
                nightLabels[i] = UiFactory.Label(b);
            }
            Styled(UiFactory.AddButton("Volver", nights.transform, "Volver", C, new Vector2(0f, -360f), new Vector2(400f, 90f), menu.ShowMain));

            // Opciones
            var options = UiFactory.FullPanel("Opciones", canvas, new Color(0f, 0f, 0f, 0.85f));
            UiFactory.AddText("Titulo", options.transform, "OPCIONES", 80, C, new Vector2(0f, 330f), new Vector2(1200f, 110f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            var sliders = new Slider[3];
            string[] names = { "Música de tensión", "Ambiente", "Efectos" };
            for (int i = 0; i < 3; i++)
            {
                UiFactory.AddText("Etiqueta " + i, options.transform, names[i], 40, C, new Vector2(-330f, 160f - i * 140f), new Vector2(520f, 60f), TextAnchor.MiddleLeft);
                sliders[i] = UiFactory.AddSlider("Volumen " + i, options.transform, C, new Vector2(220f, 160f - i * 140f), new Vector2(560f, 50f), white);
            }
            Styled(UiFactory.AddButton("Guardar", options.transform, "Guardar y volver", C, new Vector2(0f, -330f), new Vector2(520f, 100f), menu.SaveOptions, 40), true);

            // Créditos
            var credits = UiFactory.FullPanel("Creditos", canvas, new Color(0f, 0f, 0f, 0.88f));
            UiFactory.AddText("Titulo", credits.transform, "CRÉDITOS", 80, C, new Vector2(0f, 330f), new Vector2(1200f, 110f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            UiFactory.AddText("Texto", credits.transform,
                "Cinco Noches con Alfredo\nJuego fan-made (hecho por un fan) para Android\n\nDesarrollo: René Aarón Sendoya Baqueros\nUniversidad Privada del Valle (UNIVALLE)\n\nTodo el juego es una inspiración de Five Nights at Freddy's, creado por Scott Cawthon.\nProyecto académico sin fines de lucro, no oficial ni afiliado a Scott Cawthon.\n\nMúsica del menú: «Ambient Horror Track 01» de Cleyton Kauffman (OpenGameArt.org, CC0).\nVoces de los animatrónicos: síntesis con Piper, voz «Ryan» (CC BY-NC-SA 4.0), procesada.\nGritos de los jumpscares: «Ignited» (The Joy of Creation, de Nikson), versiones subidas a Voicemod.",
                32, C, new Vector2(0f, -10f), new Vector2(1700f, 600f));
            Styled(UiFactory.AddButton("Volver", credits.transform, "Volver", C, new Vector2(0f, -360f), new Vector2(400f, 90f), menu.ShowMain));

            // Historia (lore basado en el juego original: la pizzería, los endoesqueletos y el cuarto secreto)
            var history = UiFactory.FullPanel("Historia", canvas, new Color(0f, 0f, 0f, 0.9f));
            UiFactory.AddText("Titulo", history.transform, "ALFREDO'S PIZZA", 80, C, new Vector2(0f, 370f), new Vector2(1200f, 110f), TextAnchor.MiddleCenter, new Color(0.96f, 0.85f, 0.65f), FontStyle.Bold);
            UiFactory.AddText("Texto", history.transform, HistoryText, 33, C, new Vector2(0f, 10f), new Vector2(1640f, 620f), TextAnchor.MiddleCenter);
            Styled(UiFactory.AddButton("Volver", history.transform, "Volver", C, new Vector2(0f, -400f), new Vector2(400f, 90f), menu.ShowMain));

            BuilderUtil.SetRef(menu, "mainPanel", main.gameObject);
            BuilderUtil.SetRef(menu, "historyPanel", history.gameObject);
            BuilderUtil.SetRef(menu, "nightsPanel", nights.gameObject);
            BuilderUtil.SetRef(menu, "optionsPanel", options.gameObject);
            BuilderUtil.SetRef(menu, "creditsPanel", credits.gameObject);
            BuilderUtil.SetRef(menu, "continueLabel", UiFactory.Label(cont));
            BuilderUtil.SetRefs(menu, "nightButtons", nightButtons);
            BuilderUtil.SetRefs(menu, "nightLabels", nightLabels);
            BuilderUtil.SetRef(menu, "musicSlider", sliders[0]);
            BuilderUtil.SetRef(menu, "ambienceSlider", sliders[1]);
            BuilderUtil.SetRef(menu, "sfxSlider", sliders[2]);
            BuilderUtil.SetRef(menu, "progressText", progress);

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Alfredo] Menú creado: " + ScenePath);
        }
    }
}
