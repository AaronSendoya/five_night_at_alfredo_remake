using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo.EditorTools
{
    /// <summary>Referencias de la interfaz de juego que necesita el constructor de escena.</summary>
    public class GameUi
    {
        public ScreenPanels screens;
        public GameObject monitorPanel, signalLost, subtitlePanel;
        public GameObject[] mapHighlights;
        public Button[] camButtons;   // un botón táctil por sala del minimapa (CAM 01–07)
        public HudView hud;
        public ScreamerView screamer;
        public Text cameraName, recText, subtitleText;
        public RawImage staticOverlay;
        public Button camClose, skipCall;
        public Button victoryNext, victoryMenu, overRetry, overMenu, pauseButton, pauseResume, pauseMenu;
    }

    public static class GameUiBuilder
    {
        static readonly Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), TC = new Vector2(0.5f, 1f);
        static readonly Vector2 BL = new Vector2(0f, 0f), BR = new Vector2(1f, 0f), BC = new Vector2(0.5f, 0f), C = new Vector2(0.5f, 0.5f);

        public static GameUi Build(Sprite white, PlayerController player, PlayerInteractor interactor, Flashlight flashlight, NightClock clock, AnimatronicDirector director, ZoneCamera zoneCam)
        {
            var ui = new GameUi();
            var canvas = UiFactory.MakeCanvas("Interfaz").transform;
            var circle = BuilderUtil.GeneratedSprite("circulo", 128, true);

            // ---------- Monitor de cámaras (debajo del HUD) ----------
            var monitor = UiFactory.Stretch("Monitor de camaras", canvas);
            ui.monitorPanel = monitor.gameObject;
            var stat = UiFactory.Stretch("Estatica", monitor).gameObject.AddComponent<RawImage>();
            stat.color = new Color(1f, 1f, 1f, 0.18f);
            stat.raycastTarget = false;
            ui.staticOverlay = stat;
            ui.cameraName = UiFactory.AddText("Camara", monitor, "CAM 01", 44, TC, new Vector2(60f, -60f), new Vector2(760f, 64f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            ui.recText = UiFactory.AddText("REC", monitor, "REC", 40, TC, new Vector2(-380f, -60f), new Vector2(120f, 64f), TextAnchor.MiddleRight, new Color(0.9f, 0.15f, 0.15f), FontStyle.Bold);
            var lost = UiFactory.FullPanel("Senal perdida", monitor, Color.black);
            lost.raycastTarget = false;
            UiFactory.AddText("Texto", lost.transform, "SEÑAL PERDIDA", 70, C, Vector2.zero, new Vector2(900f, 120f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            ui.signalLost = lost.gameObject;
            ui.camClose = UiFactory.AddButton("Cerrar", monitor, "Cerrar monitor", BC, new Vector2(0f, 90f), new Vector2(380f, 110f), null, 40, UiFactory.Accent);
            ui.mapHighlights = BuildMinimap(monitor, white, out ui.camButtons);
            monitor.gameObject.SetActive(false);

            // ---------- HUD ----------
            var hudRt = UiFactory.Stretch("HUD", canvas);
            var hud = hudRt.gameObject.AddComponent<HudView>();
            var clockText = UiFactory.AddText("Reloj", hudRt, "12:00 AM", 64, TL, new Vector2(240f, -60f), new Vector2(420f, 80f), TextAnchor.MiddleLeft, null, FontStyle.Bold);
            var nightText = UiFactory.AddText("Noche", hudRt, "Noche 1", 34, TL, new Vector2(240f, -118f), new Vector2(420f, 44f), TextAnchor.MiddleLeft, UiFactory.MutedText);
            var livesText = UiFactory.AddText("Vidas", hudRt, "Vidas: 3", 32, TL, new Vector2(240f, -160f), new Vector2(420f, 40f), TextAnchor.MiddleLeft);
            var zoneText = UiFactory.AddText("Zona", hudRt, "", 28, TL, new Vector2(240f, -198f), new Vector2(420f, 36f), TextAnchor.MiddleLeft, UiFactory.MutedText);
            var stLabel = UiFactory.AddText("Resistencia", hudRt, "RESISTENCIA", 24, TL, new Vector2(240f, -244f), new Vector2(420f, 30f), TextAnchor.MiddleLeft, UiFactory.MutedText);
            var stFill = UiFactory.Bar("Barra resistencia", hudRt, TL, new Vector2(220f, -276f), new Vector2(380f, 22f), new Color(0.85f, 0.85f, 0.85f), white);
            var batGroup = UiFactory.Rect("Bateria", hudRt, TL, TL, new Vector2(220f, -330f), new Vector2(380f, 70f));
            UiFactory.AddText("Etiqueta", batGroup, "BATERÍA", 24, new Vector2(0.5f, 1f), new Vector2(10f, -15f), new Vector2(400f, 30f), TextAnchor.MiddleLeft, UiFactory.MutedText);
            var batFill = UiFactory.Bar("Barra", batGroup, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(380f, 22f), new Color(0.95f, 0.85f, 0.4f), white);

            var powerText = UiFactory.AddText("Luz", hudRt, "Luz: normal", 34, TR, new Vector2(-300f, -55f), new Vector2(560f, 50f), TextAnchor.MiddleRight);
            var fusesText = UiFactory.AddText("Fusibles", hudRt, "Fusibles", 30, TR, new Vector2(-300f, -100f), new Vector2(560f, 44f), TextAnchor.MiddleRight, UiFactory.MutedText);
            ui.pauseButton = RoundButton("Pausa", hudRt, TR, new Vector2(-75f, -175f), 96f, circle, BuilderUtil.UiSprite("icon_pausa"));

            var messageText = UiFactory.AddText("Aviso", hudRt, "", 42, TC, new Vector2(0f, -150f), new Vector2(1500f, 70f), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), FontStyle.Bold);
            var airGroup = UiFactory.Rect("Aire", hudRt, TC, TC, new Vector2(0f, -240f), new Vector2(520f, 70f));
            UiFactory.AddText("Etiqueta", airGroup, "AIRE", 28, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(520f, 32f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            var airFill = UiFactory.Bar("Barra", airGroup, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(520f, 24f), new Color(0.5f, 0.8f, 1f), white);

            // Controles táctiles
            var controls = UiFactory.Stretch("Controles", hudRt);
            var stickArea = UiFactory.Rect("Zona joystick", controls, Vector2.zero, new Vector2(0.45f, 0.7f), Vector2.zero, Vector2.zero);
            UiFactory.AddImage(stickArea, new Color(0f, 0f, 0f, 0f), true);
            var stickBg = UiFactory.Rect("Base", stickArea, BL, BL, new Vector2(260f, 260f), new Vector2(260f, 260f));
            UiFactory.AddImage(stickBg, new Color(1f, 1f, 1f, 0.12f), false, circle);
            var knob = UiFactory.Rect("Palanca", stickBg, C, C, Vector2.zero, new Vector2(120f, 120f));
            UiFactory.AddImage(knob, new Color(1f, 1f, 1f, 0.35f), false, circle);
            var joystick = stickArea.gameObject.AddComponent<VirtualJoystick>();
            BuilderUtil.SetRef(joystick, "background", stickBg);
            BuilderUtil.SetRef(joystick, "knob", knob);

            // Botones de acción redondos con íconos (abajo a la derecha, en arco alrededor de Usar).
            var ring = UiFactory.Rect("Progreso", controls, BR, BR, new Vector2(-185f, 195f), new Vector2(222f, 222f));
            var useProgress = UiFactory.AddImage(ring, new Color(0.85f, 0.2f, 0.2f, 0.95f), false, circle);
            useProgress.type = Image.Type.Filled;
            useProgress.fillMethod = Image.FillMethod.Radial360;
            useProgress.fillOrigin = (int)Image.Origin360.Top;
            useProgress.fillClockwise = true;
            useProgress.fillAmount = 0f;
            var useBtn = RoundTouch("Usar", controls, new Vector2(-185f, 195f), 192f, circle, BuilderUtil.UiSprite("icon_mano"), TouchAction.Interact, out var useBg, out var useIcon);
            useProgress.transform.SetAsFirstSibling();
            var hint = UiFactory.Rect("Pista", controls, BR, BR, new Vector2(-770f, 110f), new Vector2(440f, 64f));
            UiFactory.AddImage(hint, new Color(0f, 0f, 0f, 0.6f));
            var useLabel = UiFactory.AddText("Texto", hint, "", 30, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 60f));
            var run = RoundTouch("Correr", controls, new Vector2(-410f, 110f), 132f, circle, BuilderUtil.UiSprite("icon_correr"), TouchAction.Run, out var runBg, out var runIcon);
            var crank = RoundTouch("Manivela", controls, new Vector2(-395f, 305f), 120f, circle, BuilderUtil.UiSprite("icon_manivela"), TouchAction.Crank, out var crankBg, out var crankIcon);
            var flash = RoundTouch("Linterna", controls, new Vector2(-195f, 410f), 120f, circle, BuilderUtil.UiSprite("icon_linterna_encender"), TouchAction.Flashlight, out _, out var flashIcon);
            var useRt = (RectTransform)useBtn.transform;

            BuilderUtil.SetRef(hud, "interactIcon", useIcon);
            BuilderUtil.SetRef(hud, "interactHint", hint.gameObject);
            BuilderUtil.SetRefs(hud, "interactIcons", new Object[]
            {
                BuilderUtil.UiSprite("icon_mano"), BuilderUtil.UiSprite("icon_esconderse"), BuilderUtil.UiSprite("icon_salir"),
                BuilderUtil.UiSprite("icon_telefono"), BuilderUtil.UiSprite("icon_camaras"), BuilderUtil.UiSprite("icon_fusible"),
                BuilderUtil.UiSprite("icon_interruptor"), BuilderUtil.UiSprite("icon_ventilacion")
            });
            BuilderUtil.SetRef(hud, "flashlightIcon", flashIcon);
            BuilderUtil.SetRef(hud, "flashTurnOnIcon", BuilderUtil.UiSprite("icon_linterna_encender"));
            BuilderUtil.SetRef(hud, "flashTurnOffIcon", BuilderUtil.UiSprite("icon_linterna_apagar"));
            BuilderUtil.SetRef(hud, "runBackground", runBg);
            BuilderUtil.SetRef(hud, "runIcon", runIcon);
            BuilderUtil.SetRef(hud, "crankIcon", crankIcon);
            BuilderUtil.SetRef(hud, "crankBackground", crankBg);
            ui.hud = hud;

            BuilderUtil.SetRef(hud, "clock", clock);
            BuilderUtil.SetRef(hud, "player", player);
            BuilderUtil.SetRef(hud, "interactor", interactor);
            BuilderUtil.SetRef(hud, "flashlight", flashlight);
            BuilderUtil.SetRef(hud, "director", director);
            BuilderUtil.SetRef(hud, "clockText", clockText);
            BuilderUtil.SetRef(hud, "nightText", nightText);
            BuilderUtil.SetRef(hud, "livesText", livesText);
            BuilderUtil.SetRef(hud, "fusesText", fusesText);
            BuilderUtil.SetRef(hud, "powerText", powerText);
            BuilderUtil.SetRef(hud, "messageText", messageText);
            BuilderUtil.SetRef(hud, "zoneText", zoneText);
            BuilderUtil.SetRef(hud, "staminaFill", stFill);
            BuilderUtil.SetRef(hud, "staminaLabel", stLabel);
            BuilderUtil.SetRef(hud, "batteryFill", batFill);
            BuilderUtil.SetRef(hud, "batteryGroup", batGroup.gameObject);
            BuilderUtil.SetRef(hud, "airFill", airFill);
            BuilderUtil.SetRef(hud, "airGroup", airGroup.gameObject);
            BuilderUtil.SetRef(hud, "controlsRoot", controls.gameObject);
            BuilderUtil.SetRef(hud, "interactButton", useRt.gameObject);
            BuilderUtil.SetRef(hud, "interactLabel", useLabel);
            BuilderUtil.SetRef(hud, "interactProgress", useProgress);
            BuilderUtil.SetRef(hud, "interactBackground", useBg);
            BuilderUtil.SetRef(hud, "flashlightButton", flash);
            BuilderUtil.SetRef(hud, "crankButton", crank);

            // ---------- Consejos ----------
            var tipRt = UiFactory.Rect("Consejos", hudRt, BC, BC, new Vector2(0f, 215f), new Vector2(980f, 120f));
            var tipBg = UiFactory.AddImage(tipRt, new Color(0.05f, 0.05f, 0.07f, 0.82f));
            tipBg.raycastTarget = false;
            var tipGroup = tipRt.gameObject.AddComponent<CanvasGroup>();
            tipGroup.blocksRaycasts = false;
            tipGroup.interactable = false;
            UiFactory.AddText("Etiqueta", tipRt, "CONSEJO", 22, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(940f, 28f), TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.35f), FontStyle.Bold);
            var tipText = UiFactory.AddText("Texto", tipRt, "", 29, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(940f, 84f));
            tipText.raycastTarget = false;
            var tipView = tipRt.gameObject.AddComponent<TipView>();
            BuilderUtil.SetRef(tipView, "group", tipGroup);
            BuilderUtil.SetRef(tipView, "tipText", tipText);

            // ---------- Ventilaciones ----------
            var ventRoot = UiFactory.Rect("Ventilaciones", canvas, C, C, new Vector2(0f, 60f), new Vector2(900f, 640f));
            UiFactory.AddImage(ventRoot, new Color(0.04f, 0.08f, 0.12f, 0.92f), true);
            UiFactory.AddText("Titulo", ventRoot, "VENTILACIONES", 52, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(860f, 70f), TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f));
            UiFactory.AddText("Ayuda", ventRoot, "Elige por dónde salir (o sal aquí mismo). El aire se acaba.", 30, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(860f, 44f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            var vent = ventRoot.gameObject.AddComponent<VentView>();
            var ventButtons = new Object[4];
            var ventLabels = new Object[4];
            for (int i = 0; i < 4; i++)
            {
                var b = UiFactory.AddButton("Salida " + (i + 1), ventRoot, "Salida", new Vector2(0.5f, 1f), new Vector2(0f, -210f - i * 105f), new Vector2(760f, 90f), null, 34);
                UnityEventTools.AddIntPersistentListener(b.onClick, vent.Choose, i);
                ventButtons[i] = b;
                ventLabels[i] = UiFactory.Label(b);
            }
            BuilderUtil.SetRef(vent, "panel", ventRoot.gameObject);
            BuilderUtil.SetRefs(vent, "exitButtons", ventButtons);
            BuilderUtil.SetRefs(vent, "exitLabels", ventLabels);

            // ---------- Subtítulos de la llamada ----------
            var sub = UiFactory.Rect("Subtitulos", canvas, TC, TC, new Vector2(0f, -340f), new Vector2(1500f, 110f));
            UiFactory.AddImage(sub, new Color(0f, 0f, 0f, 0.7f));
            ui.subtitleText = UiFactory.AddText("Texto", sub, "", 36, C, Vector2.zero, new Vector2(1460f, 100f));
            ui.skipCall = UiFactory.AddButton("Saltar llamada", sub, "Saltar llamada", new Vector2(0.5f, 0f), new Vector2(0f, -50f), new Vector2(360f, 70f), null, 30);
            ui.subtitlePanel = sub.gameObject;

            // ---------- Pantallas ----------
            var screensRt = UiFactory.Stretch("Pantallas", canvas);
            ui.screens = screensRt.gameObject.AddComponent<ScreenPanels>();

            var intro = UiFactory.FullPanel("Inicio de noche", screensRt, Color.black);
            UiFactory.AddText("Hora", intro.transform, "12:00 AM", 120, C, new Vector2(0f, 50f), new Vector2(1200f, 160f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            var introNight = UiFactory.AddText("Noche", intro.transform, "Noche 1", 54, C, new Vector2(0f, -70f), new Vector2(900f, 80f), TextAnchor.MiddleCenter, UiFactory.MutedText);

            var tutorial = BuildTutorial(screensRt);

            var jump = UiFactory.FullPanel("Susto", screensRt, Color.black);
            var jumpImgRt = UiFactory.Rect("Animatronico", jump.transform, C, C, Vector2.zero, new Vector2(1000f, 1000f));
            var jumpImg = UiFactory.AddImage(jumpImgRt, Color.white);
            var flashImg = UiFactory.FullPanel("Destello", jump.transform, new Color(0.6f, 0f, 0f, 0f));
            flashImg.raycastTarget = false;

            var victory = UiFactory.FullPanel("Victoria", screensRt, new Color(0.03f, 0.03f, 0.04f, 0.96f));
            var confettiRt = UiFactory.Stretch("Confeti", victory.transform);
            var confetti = confettiRt.gameObject.AddComponent<ConfettiView>();
            UiFactory.AddText("Hora", victory.transform, "6:00 AM", 150, C, new Vector2(0f, 200f), new Vector2(1200f, 180f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            var victoryDetail = UiFactory.AddText("Detalle", victory.transform, "", 46, C, new Vector2(0f, 50f), new Vector2(1400f, 70f));
            ui.victoryNext = UiFactory.AddButton("Siguiente noche", victory.transform, "Siguiente noche", C, new Vector2(-240f, -150f), new Vector2(440f, 110f), null, 40, UiFactory.Accent);
            ui.victoryMenu = UiFactory.AddButton("Menu", victory.transform, "Menú", C, new Vector2(240f, -150f), new Vector2(440f, 110f), null);

            var over = UiFactory.FullPanel("Derrota", screensRt, new Color(0.03f, 0.03f, 0.04f, 0.97f));
            UiFactory.AddText("Titulo", over.transform, "FIN DEL TURNO", 110, C, new Vector2(0f, 190f), new Vector2(1400f, 150f), TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold);
            var overDetail = UiFactory.AddText("Detalle", over.transform, "", 44, C, new Vector2(0f, 90f), new Vector2(1500f, 70f));
            var overTip = UiFactory.AddText("Consejo", over.transform, "", 32, C, new Vector2(0f, -20f), new Vector2(1500f, 130f), TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.35f));
            ui.overRetry = UiFactory.AddButton("Reintentar", over.transform, "Reintentar la noche", C, new Vector2(-260f, -190f), new Vector2(480f, 110f), null, 40, UiFactory.Accent);
            ui.overMenu = UiFactory.AddButton("Menu", over.transform, "Menú", C, new Vector2(260f, -190f), new Vector2(480f, 110f), null);

            var pause = UiFactory.FullPanel("Pausa", screensRt, new Color(0f, 0f, 0f, 0.85f));
            UiFactory.AddText("Titulo", pause.transform, "PAUSA", 96, C, new Vector2(0f, 180f), new Vector2(900f, 130f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            ui.pauseResume = UiFactory.AddButton("Continuar", pause.transform, "Continuar", C, new Vector2(0f, 10f), new Vector2(480f, 110f), null, 40, UiFactory.Accent);
            ui.pauseMenu = UiFactory.AddButton("Menu", pause.transform, "Salir al menú", C, new Vector2(0f, -130f), new Vector2(480f, 110f), null);

            BuilderUtil.SetRef(ui.screens, "introPanel", intro.gameObject);
            BuilderUtil.SetRef(ui.screens, "introNightText", introNight);
            BuilderUtil.SetRef(ui.screens, "jumpscarePanel", jump.gameObject);
            BuilderUtil.SetRef(ui.screens, "jumpscareImage", jumpImg);
            BuilderUtil.SetRef(ui.screens, "jumpscareFlash", flashImg);
            BuilderUtil.SetRef(ui.screens, "victoryPanel", victory.gameObject);
            BuilderUtil.SetRef(ui.screens, "victoryDetail", victoryDetail);
            BuilderUtil.SetRef(ui.screens, "nextNightButton", ui.victoryNext.gameObject);
            BuilderUtil.SetRef(ui.screens, "confetti", confetti);
            BuilderUtil.SetRef(ui.screens, "gameOverPanel", over.gameObject);
            BuilderUtil.SetRef(ui.screens, "gameOverDetail", overDetail);
            BuilderUtil.SetRef(ui.screens, "gameOverTip", overTip);
            BuilderUtil.SetRef(ui.screens, "pausePanel", pause.gameObject);
            BuilderUtil.SetRef(ui.screens, "pauseButton", ui.pauseButton.gameObject);

            // ---------- Susto de Shadow Alfredo ----------
            var scr = UiFactory.Stretch("Shadow Alfredo", canvas);
            var scrTint = UiFactory.AddImage(scr, new Color(0.25f, 0f, 0.35f, 0f));
            scrTint.raycastTarget = false;
            var scrImgRt = UiFactory.Rect("Imagen", scr, C, C, Vector2.zero, new Vector2(900f, 900f));
            var scrImg = UiFactory.AddImage(scrImgRt, Color.white);
            scrImg.raycastTarget = false;
            var scrText = UiFactory.AddText("Frase", scr, "", 48, new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(1700f, 90f), TextAnchor.MiddleCenter, new Color(0.85f, 0.6f, 1f), FontStyle.Italic);
            scrText.raycastTarget = false;
            ui.screamer = scr.gameObject.AddComponent<ScreamerView>();
            BuilderUtil.SetRef(ui.screamer, "image", scrImg);
            BuilderUtil.SetRef(ui.screamer, "tint", scrTint);
            BuilderUtil.SetRef(ui.screamer, "phrase", scrText);

            // ---------- Fundido (siempre encima) ----------
            var fader = UiFactory.FullPanel("Fundido", canvas, Color.black);
            fader.gameObject.AddComponent<CanvasGroup>();
            fader.gameObject.AddComponent<ScreenFader>();
            return ui;
        }

        static GameObject BuildTutorial(Transform parent)
        {
            var panel = UiFactory.FullPanel("Tutorial", parent, new Color(0.02f, 0.02f, 0.03f, 1f));
            var picRt = UiFactory.Rect("Imagen", panel.transform, C, C, new Vector2(0f, 230f), new Vector2(760f, 320f));
            var pic = UiFactory.AddImage(picRt, Color.white);
            var body = UiFactory.AddText("Texto", panel.transform, "", 42, C, new Vector2(0f, -110f), new Vector2(1600f, 340f));
            var step = UiFactory.AddText("Paso", panel.transform, "1 / 10", 30, BC, new Vector2(0f, 190f), new Vector2(300f, 44f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            var view = panel.gameObject.AddComponent<TutorialView>();
            var next = UiFactory.AddButton("Siguiente", panel.transform, "Siguiente", BC, new Vector2(300f, 90f), new Vector2(420f, 110f), view.Next, 40, UiFactory.Accent);
            UiFactory.AddButton("Saltar", panel.transform, "Saltar tutorial", BC, new Vector2(-300f, 90f), new Vector2(420f, 110f), view.Skip, 36);
            BuilderUtil.SetRef(view, "panel", panel.gameObject);
            BuilderUtil.SetRef(view, "bodyText", body);
            BuilderUtil.SetRef(view, "stepText", step);
            BuilderUtil.SetRef(view, "picture", pic);
            BuilderUtil.SetRefs(view, "pictures", new Object[]
            {
                null, null, BuilderUtil.First("sp_tutorial_stamina"), BuilderUtil.First("sp_fusible_0"),
                BuilderUtil.First("sp_fusible_1"), BuilderUtil.First("sp_tutorial_locker"), BuilderUtil.First("sp_tutorial_camara"),
                BuilderUtil.First("sp_interruptor_puerta"), null, null
            });
            return panel.gameObject;
        }

        static readonly Color RoundIdle = new Color(0.1f, 0.1f, 0.11f, 0.8f);

        /// <summary>Botón táctil redondo con ícono.</summary>
        static GameObject RoundTouch(string name, Transform parent, Vector2 pos, float size, Sprite circle, Sprite icon, TouchAction action, out Image background, out Image iconImage)
        {
            var rt = UiFactory.Rect(name, parent, BR, BR, pos, new Vector2(size, size));
            UiFactory.AddImage(rt, new Color(0.93f, 0.9f, 0.85f, 0.3f), true, circle);
            var fill = UiFactory.Rect("Fondo", rt, C, C, Vector2.zero, new Vector2(size - 6f, size - 6f));
            background = UiFactory.AddImage(fill, RoundIdle, false, circle);
            var iconRt = UiFactory.Rect("Icono", rt, C, C, Vector2.zero, new Vector2(size * 0.58f, size * 0.58f));
            iconImage = UiFactory.AddImage(iconRt, Color.white, false, icon);
            iconImage.preserveAspect = true;
            SetAction(rt.gameObject.AddComponent<TouchButton>(), action);
            return rt.gameObject;
        }

        /// <summary>Botón normal (clic) redondo con ícono, como Pausa.</summary>
        static Button RoundButton(string name, Transform parent, Vector2 anchor, Vector2 pos, float size, Sprite circle, Sprite icon)
        {
            var rt = UiFactory.Rect(name, parent, anchor, anchor, pos, new Vector2(size, size));
            var bg = UiFactory.AddImage(rt, RoundIdle, true, circle);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var iconRt = UiFactory.Rect("Icono", rt, C, C, Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
            UiFactory.AddImage(iconRt, Color.white, false, icon).preserveAspect = true;
            return b;
        }

        // Debe coincidir con el script que dibujó minimapa.png.
        const float MapMinX = -140f, MapMinY = -130f, MapScale = 0.065f, MapUi = 0.85f;
        static readonly (string zone, string label)[] CameraRooms =
        {
            ("Comedor", "CAM 01"), ("Mantenimiento", "CAM 02"), ("Cocina", "CAM 03"), ("Arcade", "CAM 04"),
            ("Almacen", "CAM 05"), ("Escenario", "CAM 06"), ("Cuarto secreto", "CAM 07")
        };

        /// <summary>Minimapa del monitor: salas, nombres, resaltado de la cámara activa y una zona táctil por sala para cambiar de cámara.</summary>
        static GameObject[] BuildMinimap(Transform monitor, Sprite white, out Button[] camButtons)
        {
            var mapSprite = BuilderUtil.UiSprite("minimapa");
            Vector2 size = mapSprite != null ? new Vector2(mapSprite.rect.width, mapSprite.rect.height) * MapUi : new Vector2(343f, 372f);
            var root = UiFactory.Rect("Minimapa", monitor, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f - size.x / 2f, 20f), size);
            UiFactory.AddText("Titulo", root, "MAPA", 26, new Vector2(0.5f, 1f), new Vector2(0f, 22f), new Vector2(300f, 36f), TextAnchor.MiddleCenter, UiFactory.MutedText, FontStyle.Bold);
            var content = UiFactory.Rect("Contenido", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(size.x / 2f, -size.y / 2f), size);
            var highlightsRoot = UiFactory.Rect("Resaltados", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddImage(content, Color.white, false, mapSprite);

            var map = BuilderUtil.LoadMap();
            var highlights = new GameObject[CameraRooms.Length];
            camButtons = new Button[CameraRooms.Length];
            foreach (var z in map.zones)
            {
                float x0 = (z.x - MapMinX) * MapScale * MapUi, y0 = (z.y - MapMinY) * MapScale * MapUi;
                float w = z.w * MapScale * MapUi, h = z.h * MapScale * MapUi;
                Vector2 center = new Vector2(x0 + w / 2f, -(y0 + h / 2f));
                int cam = System.Array.FindIndex(CameraRooms, c => c.zone == z.name);
                string label = z.name == "Almacen" ? "Almacén" : z.name;
                if (z.name == "Oficina") label = "Oficina (tú)";
                UiFactory.AddText("Sala " + z.name, content, label, 21, new Vector2(0f, 1f), center + new Vector2(0f, 9f), new Vector2(w, 30f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
                if (cam >= 0)
                {
                    UiFactory.AddText("Cam " + z.name, content, CameraRooms[cam].label, 17, new Vector2(0f, 1f), center + new Vector2(0f, -15f), new Vector2(w, 26f), TextAnchor.MiddleCenter, UiFactory.MutedText);
                    var hl = UiFactory.Rect("Resaltado " + z.name, highlightsRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), center, new Vector2(w - 14f, h - 14f));
                    UiFactory.AddImage(hl, new Color(0.85f, 0.15f, 0.15f, 0.35f), false, white);
                    hl.gameObject.SetActive(cam == 0);
                    highlights[cam] = hl.gameObject;

                    // Zona táctil: toda la sala del minimapa; el destello blanco al pulsar confirma el toque.
                    var hit = UiFactory.Rect("Toque " + z.name, content, new Vector2(0f, 1f), new Vector2(0f, 1f), center, new Vector2(w, h));
                    var hitImg = UiFactory.AddImage(hit, Color.white, true, white);
                    hitImg.canvasRenderer.cullTransparentMesh = false;   // si no, un gráfico transparente no recibe toques
                    var btn = hit.gameObject.AddComponent<Button>();
                    btn.targetGraphic = hitImg;
                    btn.transition = Selectable.Transition.ColorTint;
                    var cb = btn.colors;
                    cb.normalColor = new Color(1f, 1f, 1f, 0f);
                    cb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
                    cb.pressedColor = new Color(1f, 1f, 1f, 0.28f);
                    cb.selectedColor = new Color(1f, 1f, 1f, 0f);
                    cb.disabledColor = new Color(1f, 1f, 1f, 0f);
                    cb.fadeDuration = 0.05f;
                    btn.colors = cb;
                    var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
                    camButtons[cam] = btn;
                }
            }
            return highlights;
        }

        static GameObject TouchRect(string name, Transform parent, Vector2 pos, string label, TouchAction action)
        {
            var rt = UiFactory.Rect(name, parent, BR, BR, pos, new Vector2(260f, 130f));
            UiFactory.AddImage(rt, UiFactory.ButtonColor, true);
            UiFactory.AddText("Texto", rt, label, 36, C, Vector2.zero, new Vector2(240f, 120f));
            SetAction(rt.gameObject.AddComponent<TouchButton>(), action);
            return rt.gameObject;
        }

        static void SetAction(TouchButton b, TouchAction action) =>
            BuilderUtil.SetValue(b, "action", p => p.enumValueIndex = (int)action);

        public static void WireMonitorButtons(GameUi ui, CameraMonitor monitor)
        {
            for (int i = 0; i < ui.camButtons.Length; i++)
                if (ui.camButtons[i] != null) UnityEventTools.AddIntPersistentListener(ui.camButtons[i].onClick, monitor.SelectCamera, i);
            UnityEventTools.AddPersistentListener(ui.camClose.onClick, monitor.Close);
        }

        public static void WirePhone(GameUi ui, PhoneCall phone) =>
            UnityEventTools.AddPersistentListener(ui.skipCall.onClick, phone.SkipCall);

        public static void WireGameButtons(GameUi ui, GameManager gm)
        {
            UnityEventTools.AddPersistentListener(ui.victoryNext.onClick, gm.NextNight);
            UnityEventTools.AddPersistentListener(ui.victoryMenu.onClick, gm.GoToMenu);
            UnityEventTools.AddPersistentListener(ui.overRetry.onClick, gm.RetryNight);
            UnityEventTools.AddPersistentListener(ui.overMenu.onClick, gm.GoToMenu);
            UnityEventTools.AddPersistentListener(ui.pauseButton.onClick, gm.Pause);
            UnityEventTools.AddPersistentListener(ui.pauseResume.onClick, gm.Resume);
            UnityEventTools.AddPersistentListener(ui.pauseMenu.onClick, gm.GoToMenu);
        }
    }
}
