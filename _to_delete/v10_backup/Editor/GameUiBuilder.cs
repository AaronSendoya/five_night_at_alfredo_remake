using System.Collections.Generic;
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
        public ArcadeMinigame arcade;
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

            // ---------- Ventilaciones: el mapa del monitor; solo se tocan las salas con rejilla ----------
            var ventRoot = UiFactory.Stretch("Ventilaciones", canvas);
            UiFactory.AddImage(ventRoot, new Color(0.02f, 0.05f, 0.08f, 0.86f), true);
            UiFactory.AddText("Titulo", ventRoot, "VENTILACIONES", 58, TC, new Vector2(0f, -70f), new Vector2(1000f, 76f), TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f), FontStyle.Bold);
            UiFactory.AddText("Ayuda", ventRoot, "Toca una sala con rejilla para salir por ahí", 32, TC, new Vector2(0f, -130f), new Vector2(1300f, 44f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            var vent = ventRoot.gameObject.AddComponent<VentView>();
            BuildVentMap(ventRoot, white, vent, out var ventNames, out var ventButtons, out var ventMarkers);
            // aire (izquierda)
            UiFactory.AddText("Aire", ventRoot, "AIRE", 34, C, new Vector2(-640f, 70f), new Vector2(300f, 50f), TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f), FontStyle.Bold);
            var ventAir = UiFactory.Bar("Barra de aire", ventRoot, C, new Vector2(-640f, 20f), new Vector2(300f, 30f), new Color(0.55f, 0.85f, 1f), white);
            UiFactory.AddText("Aviso", ventRoot, "Si se acaba, sales\nsin aliento", 28, C, new Vector2(-640f, -50f), new Vector2(320f, 90f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            // salir por la misma rejilla (derecha)
            var exitHere = RoundButton("Salir aqui", ventRoot, C, new Vector2(640f, 30f), 170f, circle, BuilderUtil.UiSprite("icon_salir"));
            UnityEventTools.AddPersistentListener(exitHere.onClick, vent.ExitHere);
            UiFactory.AddText("Texto", exitHere.transform, "Salir aquí", 32, new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(300f, 44f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            BuilderUtil.SetRef(vent, "panel", ventRoot.gameObject);
            BuilderUtil.SetValue(vent, "roomNames", p =>
            {
                p.arraySize = ventNames.Length;
                for (int i = 0; i < ventNames.Length; i++) p.GetArrayElementAtIndex(i).stringValue = ventNames[i];
            });
            BuilderUtil.SetRefs(vent, "roomButtons", ventButtons);
            BuilderUtil.SetRefs(vent, "hereMarkers", ventMarkers);
            BuilderUtil.SetRef(vent, "airFill", ventAir);
            // VentView se oculta solo en su Awake (necesita estar activo al cargar para suscribirse).

            // ---------- Minijuego de la máquina del arcade ----------
            ui.arcade = BuildArcade(canvas, white);

            // ---------- Subtítulos de la llamada ----------
            var sub = UiFactory.Rect("Subtitulos", canvas, TC, TC, new Vector2(0f, -340f), new Vector2(1500f, 110f));
            UiFactory.AddImage(sub, new Color(0f, 0f, 0f, 0.7f));
            ui.subtitleText = UiFactory.AddText("Texto", sub, "", 36, C, Vector2.zero, new Vector2(1460f, 100f));
            ui.skipCall = UiFactory.AddButton("Saltar llamada", sub, "Colgar", new Vector2(0.5f, 0f), new Vector2(0f, -50f), new Vector2(360f, 70f), null, 30);
            ui.subtitlePanel = sub.gameObject;

            // ---------- Pantallas ----------
            var screensRt = UiFactory.Stretch("Pantallas", canvas);
            ui.screens = screensRt.gameObject.AddComponent<ScreenPanels>();

            var intro = UiFactory.FullPanel("Inicio de noche", screensRt, Color.black);
            UiFactory.AddText("Hora", intro.transform, "12:00 AM", 120, C, new Vector2(0f, 50f), new Vector2(1200f, 160f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
            var introNight = UiFactory.AddText("Noche", intro.transform, "Noche 1", 54, C, new Vector2(0f, -70f), new Vector2(900f, 80f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            var introLore = UiFactory.AddText("Historia", intro.transform, "", 34, C, new Vector2(0f, -170f), new Vector2(1500f, 100f), TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.6f), FontStyle.Italic);

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
            BuilderUtil.SetRef(ui.screens, "introLoreText", introLore);
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
            var white = BuilderUtil.GeneratedSprite("blanco", 4, false);
            var circle = BuilderUtil.GeneratedSprite("circulo", 128, true);
            var T = new Vector2(0.5f, 1f);
            var panel = UiFactory.FullPanel("Tutorial", parent, new Color(0.02f, 0.02f, 0.03f, 1f));
            var view = panel.gameObject.AddComponent<TutorialView>();
            var title = UiFactory.AddText("Titulo", panel.transform, "", 56, T, new Vector2(0f, -75f), new Vector2(1600f, 80f), TextAnchor.MiddleCenter, new Color(0.95f, 0.75f, 0.35f), FontStyle.Bold);

            // Escenario animado (recortado por la máscara)
            var frameRt = UiFactory.Rect("Marco", panel.transform, C, C, new Vector2(-330f, 20f), new Vector2(1012f, 572f));
            UiFactory.AddImage(frameRt, new Color(0.95f, 0.9f, 0.8f, 0.22f));
            var stage = UiFactory.Rect("Escena", frameRt, C, C, Vector2.zero, new Vector2(1000f, 560f));
            UiFactory.AddImage(stage, Color.black);
            stage.gameObject.AddComponent<RectMask2D>();

            // Columna derecha: explicación, botón de práctica y pista
            var body = UiFactory.AddText("Texto", panel.transform, "", 33, C, new Vector2(560f, 140f), new Vector2(660f, 380f), TextAnchor.UpperLeft);
            body.supportRichText = true;
            body.lineSpacing = 1.1f;
            var ringRt = UiFactory.Rect("Practica", panel.transform, C, C, new Vector2(560f, -170f), new Vector2(200f, 200f));
            var ringBack = UiFactory.AddImage(ringRt, new Color(1f, 1f, 1f, 0.12f), false, circle);
            var ringFill = UiFactory.Rect("Progreso", ringRt, C, C, Vector2.zero, new Vector2(200f, 200f));
            var ring = UiFactory.AddImage(ringFill, new Color(0.95f, 0.75f, 0.35f, 1f), false, circle);
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = (int)Image.Origin360.Top; ring.fillAmount = 0f;
            var btnRt = UiFactory.Rect("Boton", ringRt, C, C, Vector2.zero, new Vector2(168f, 168f));
            var btnImg = UiFactory.AddImage(btnRt, new Color(0.13f, 0.13f, 0.15f, 0.95f), true, circle);
            var iconRt = UiFactory.Rect("Icono", btnRt, C, C, Vector2.zero, new Vector2(100f, 100f));
            var icon = UiFactory.AddImage(iconRt, Color.white, false, null);
            icon.preserveAspect = true;
            var pressComp = btnRt.gameObject.AddComponent<TutorialPress>();
            var hint = UiFactory.AddText("Pista", panel.transform, "", 28, C, new Vector2(560f, -305f), new Vector2(660f, 70f), TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.6f));

            // Navegación
            var step = UiFactory.AddText("Paso", panel.transform, "1 / 8", 30, BC, new Vector2(80f, 95f), new Vector2(200f, 44f), TextAnchor.MiddleCenter, UiFactory.MutedText);
            UiFactory.AddButton("Saltar", panel.transform, "Saltar tutorial", BC, new Vector2(-640f, 90f), new Vector2(360f, 100f), view.Skip, 32);
            UiFactory.AddButton("Atras", panel.transform, "Atrás", BC, new Vector2(-230f, 90f), new Vector2(260f, 100f), view.Back, 34);
            var next = UiFactory.AddButton("Siguiente", panel.transform, "Siguiente", BC, new Vector2(470f, 90f), new Vector2(420f, 110f), view.Next, 40, UiFactory.Accent);

            BuilderUtil.SetRef(view, "panel", panel.gameObject);
            BuilderUtil.SetRef(view, "titleText", title);
            BuilderUtil.SetRef(view, "bodyText", body);
            BuilderUtil.SetRef(view, "stepText", step);
            BuilderUtil.SetRef(view, "hintText", hint);
            BuilderUtil.SetRef(view, "nextLabel", UiFactory.Label(next));
            BuilderUtil.SetRef(view, "stage", stage);
            BuilderUtil.SetRef(view, "actionButton", ringRt.gameObject);
            BuilderUtil.SetRef(view, "actionIcon", icon);
            BuilderUtil.SetRef(view, "actionRing", ring);
            BuilderUtil.SetRef(view, "press", pressComp);

            BuilderUtil.SetRef(view, "white", white);
            BuilderUtil.SetRef(view, "bgOffice", BuilderUtil.First("sp_fondo_camaras"));
            BuilderUtil.SetRef(view, "bgDeposito", BuilderUtil.First("sp_fondo_deposito"));
            BuilderUtil.SetRef(view, "bgArcade", BuilderUtil.First("sp_fondo_arcade"));
            BuilderUtil.SetRef(view, "bgComedor", BuilderUtil.First("sp_pizzeria_comedor"));
            BuilderUtil.SetRef(view, "bgAlmacen", BuilderUtil.First("sp_fondo_almacen"));
            BuilderUtil.SetRef(view, "bgEscenario", BuilderUtil.First("sp_escenario_principal"));
            BuilderUtil.SetDirectional(view, "playerSprites", BuilderUtil.Directional("sp_player"));
            BuilderUtil.SetRefs(view, "alfredo", BuilderUtil.Frames("sp_freddy_caminar_frente"));
            BuilderUtil.SetRefs(view, "matilda", BuilderUtil.Frames("sp_chica_caminar_frente"));
            BuilderUtil.SetRefs(view, "bongo", BuilderUtil.Frames("sp_bonnie_caminar_frente_guitarra"));
            BuilderUtil.SetRefs(view, "rufus", BuilderUtil.Frames("sp_foxy_caminar_frente"));
            BuilderUtil.SetRefs(view, "rufusUp", BuilderUtil.Frames("sp_foxy_caminar_atras"));
            BuilderUtil.SetRef(view, "shutter", BuilderUtil.First("sp_puerta_oficina"));
            var plate = BuilderUtil.Frames("sp_interruptor_puerta");
            if (plate.Length > 1) { BuilderUtil.SetRef(view, "plateClosed", plate[0]); BuilderUtil.SetRef(view, "plateOpen", plate[1]); }
            BuilderUtil.SetRef(view, "fuseOk", BuilderUtil.First("sp_fusible_1"));
            BuilderUtil.SetRef(view, "fuseBroken", BuilderUtil.First("sp_fusible_0"));
            BuilderUtil.SetRef(view, "panelSprite", BuilderUtil.First("sp_panel"));
            BuilderUtil.SetRef(view, "ventSprite", BuilderUtil.First("sp_ventilation"));
            BuilderUtil.SetRef(view, "minimap", BuilderUtil.UiSprite("minimapa"));
            BuilderUtil.SetRef(view, "iconHand", BuilderUtil.UiSprite("icon_mano"));
            BuilderUtil.SetRef(view, "iconFuse", BuilderUtil.UiSprite("icon_fusible"));
            BuilderUtil.SetRef(view, "iconFlashlight", BuilderUtil.UiSprite("icon_linterna_encender"));
            BuilderUtil.SetRef(view, "iconCrank", BuilderUtil.UiSprite("icon_manivela"));
            BuilderUtil.SetRef(view, "iconHide", BuilderUtil.UiSprite("icon_esconderse"));
            BuilderUtil.SetRef(view, "iconExit", BuilderUtil.UiSprite("icon_salir"));
            BuilderUtil.SetRef(view, "iconVent", BuilderUtil.UiSprite("icon_ventilacion"));
            BuilderUtil.SetRef(view, "iconCamera", BuilderUtil.UiSprite("icon_camaras"));
            return panel.gameObject;
        }

        /// <summary>Pantalla del minijuego de pizzas (rm_minijuego_pizza): fondo del original, el jugador abajo y un botón Salir.</summary>
        static ArcadeMinigame BuildArcade(Transform canvas, Sprite white)
        {
            var root = UiFactory.FullPanel("Minijuego arcade", canvas, new Color(0.01f, 0.01f, 0.02f, 0.96f));
            var game = root.gameObject.AddComponent<ArcadeMinigame>();
            var screen = UiFactory.Rect("Pantalla", root.transform, C, C, new Vector2(0f, -20f), new Vector2(1240f, 930f));
            UiFactory.AddImage(screen, new Color(0.9f, 0.3f, 0.6f, 0.6f));   // marco de neón
            var inner = UiFactory.Rect("Juego", screen, C, C, Vector2.zero, new Vector2(1224f, 914f));
            inner.gameObject.AddComponent<RectMask2D>(); // las pizzas no se dibujan fuera de la pantalla
            var bg = UiFactory.AddImage(inner, Color.white, false, BuilderUtil.First("sp_fondo_minijuego"));
            bg.raycastTarget = false;
            var catcher = UiFactory.Rect("Jugador", inner, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(150f, 244f));
            var catcherImg = UiFactory.AddImage(catcher, Color.white, false, BuilderUtil.First("sp_player_caminar_atras"));
            catcherImg.preserveAspect = true;
            var score = UiFactory.AddText("Puntos", inner, "PUNTOS: 0 / 100", 40, TL, new Vector2(250f, -45f), new Vector2(480f, 60f), TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            score.gameObject.AddComponent<Shadow>().effectColor = Color.black;
            var lives = UiFactory.AddText("Fallos", inner, "FALLOS: ---", 40, TR, new Vector2(-230f, -45f), new Vector2(420f, 60f), TextAnchor.MiddleRight, new Color(1f, 0.55f, 0.5f), FontStyle.Bold);
            lives.gameObject.AddComponent<Shadow>().effectColor = Color.black;
            var center = UiFactory.AddText("Aviso", inner, "", 54, C, new Vector2(0f, 60f), new Vector2(1100f, 200f), TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f), FontStyle.Bold);
            center.gameObject.AddComponent<Shadow>().effectColor = Color.black;
            var exit = UiFactory.AddButton("Salir", root.transform, "Salir", TR, new Vector2(-130f, -70f), new Vector2(200f, 90f), game.Close, 38, UiFactory.Accent);
            UiFactory.AddText("Titulo", root.transform, "PIZZA CATCH", 44, TL, new Vector2(210f, -60f), new Vector2(380f, 60f), TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.85f), FontStyle.Bold);
            BuilderUtil.SetRef(game, "panel", root.gameObject);
            BuilderUtil.SetRef(game, "playArea", inner);
            BuilderUtil.SetRef(game, "catcher", catcher);
            BuilderUtil.SetRef(game, "catcherImage", catcherImg);
            BuilderUtil.SetRefs(game, "idleFrames", BuilderUtil.Frames("sp_player_caminar_atras"));
            BuilderUtil.SetRefs(game, "leftFrames", BuilderUtil.Frames("sp_player_correr_izquierda"));
            BuilderUtil.SetRefs(game, "rightFrames", BuilderUtil.Frames("sp_player_correr_derecha"));
            BuilderUtil.SetRef(game, "pizzaSprite", BuilderUtil.First("sp_minipizza"));
            BuilderUtil.SetRef(game, "scoreText", score);
            BuilderUtil.SetRef(game, "livesText", lives);
            BuilderUtil.SetRef(game, "centerText", center);
            root.gameObject.SetActive(false);
            return game;
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
        const float MapUi = 0.85f;
        // Posición de cada sala en minimapa.png (px, origen arriba a la izquierda). El mapa es lógico, no a
        // escala: una puerta de ARRIBA lleva a la sala dibujada encima. Debe coincidir con el script que lo dibujó.
        static readonly Dictionary<string, Rect> MapLayout = new Dictionary<string, Rect>
        {
            { "Despensa", Rect.MinMaxRect(10, 10, 135, 90) },
            { "Cocina", Rect.MinMaxRect(10, 106, 135, 186) },
            { "Cuarto secreto", Rect.MinMaxRect(147, 106, 272, 186) },
            { "Almacen", Rect.MinMaxRect(284, 106, 409, 186) },
            { "Mantenimiento", Rect.MinMaxRect(421, 106, 586, 186) },
            { "Escenario", Rect.MinMaxRect(10, 202, 409, 282) },
            { "Comedor", Rect.MinMaxRect(421, 202, 586, 378) },
            { "Arcade", Rect.MinMaxRect(10, 298, 409, 378) },
            { "Oficina", Rect.MinMaxRect(10, 394, 586, 474) },
        };

        static string MapLabel(string zone) =>
            zone == "Almacen" ? "Almacén" : zone == "Cuarto secreto" ? "Cuarto\nsecreto" : zone == "Mantenimiento" ? "Manteni-\nmiento" : zone;

        static bool MapCell(ZoneJson z, float scale, out Vector2 center, out float w, out float h)
        {
            center = Vector2.zero; w = h = 0f;
            if (!MapLayout.TryGetValue(z.name, out var r)) return false;
            w = r.width * scale; h = r.height * scale;
            center = new Vector2(r.center.x * scale, -r.center.y * scale);
            return true;
        }
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
                if (!MapCell(z, MapUi, out var center, out float w, out float h)) continue;
                int cam = System.Array.FindIndex(CameraRooms, c => c.zone == z.name);
                string label = MapLabel(z.name);
                if (z.name == "Oficina") label = "Oficina (tú)";
                bool twoLines = label.Contains("\n");
                float sub = twoLines ? -22f : -14f;
                if (z.name == "Despensa") UiFactory.AddText("Sin camara " + z.name, content, "SIN CÁMARA", 14, new Vector2(0f, 1f), center + new Vector2(0f, sub), new Vector2(w, 22f), TextAnchor.MiddleCenter, UiFactory.MutedText);
                UiFactory.AddText("Sala " + z.name, content, label, 18, new Vector2(0f, 1f), center + new Vector2(0f, twoLines ? 8f : 8f), new Vector2(w - 6f, twoLines ? 42f : 26f), TextAnchor.MiddleCenter, null, FontStyle.Bold);
                if (cam >= 0)
                {
                    UiFactory.AddText("Cam " + z.name, content, CameraRooms[cam].label, 15, new Vector2(0f, 1f), center + new Vector2(0f, sub), new Vector2(w, 22f), TextAnchor.MiddleCenter, UiFactory.MutedText);
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

        /// <summary>
        /// Mapa de ventilaciones: el mismo dibujo del minimapa, más grande. Las salas con rejilla son botones;
        /// las demás quedan oscurecidas. Devuelve los nombres de sala (iguales a HidingSpot.DisplayName).
        /// </summary>
        static void BuildVentMap(Transform parent, Sprite white, VentView vent, out string[] names, out Object[] buttons, out Object[] markers)
        {
            const float ui = 1.22f;
            var mapSprite = BuilderUtil.UiSprite("minimapa");
            Vector2 size = new Vector2(mapSprite.rect.width, mapSprite.rect.height) * ui;
            var root = UiFactory.Rect("Mapa", parent, C, C, new Vector2(0f, -40f), size);
            UiFactory.AddImage(root, Color.white, false, mapSprite);
            var map = BuilderUtil.LoadMap();
            var ventZones = new System.Collections.Generic.List<string>();
            foreach (var it in map.instances)
            {
                if (it.obj != "obj_ventilation") continue;
                foreach (var z in map.zones)
                    if (it.x >= z.x && it.x <= z.x + z.w && it.y >= z.y && it.y <= z.y + z.h && !ventZones.Contains(z.name)) ventZones.Add(z.name);
            }
            var nameList = new System.Collections.Generic.List<string>();
            var btnList = new System.Collections.Generic.List<Object>();
            var markList = new System.Collections.Generic.List<Object>();
            var ventIcon = BuilderUtil.UiSprite("icon_ventilacion");
            foreach (var z in map.zones)
            {
                if (!MapCell(z, ui, out var center, out float w, out float h)) continue;
                string label = z.name == "Almacen" ? "Almacén" : z.name;
                bool hasVent = ventZones.Contains(z.name);
                var box = UiFactory.Rect("Sala " + z.name, root, new Vector2(0f, 1f), new Vector2(0f, 1f), center, new Vector2(w - 16f, h - 16f));
                if (!hasVent)
                {
                    UiFactory.AddImage(box, new Color(0f, 0f, 0f, 0.6f), false, white);
                    UiFactory.AddText("Nombre", box, label, 22, C, Vector2.zero, new Vector2(w, 30f), TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.35f));
                    continue;
                }
                var here = UiFactory.Rect("Estas aqui", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UiFactory.AddImage(here, new Color(0.85f, 0.15f, 0.15f, 0.55f), false, white);
                UiFactory.AddText("Texto", here, "ESTÁS AQUÍ", 17, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(w, 24f), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                here.gameObject.SetActive(false);
                var img = UiFactory.AddImage(box, Color.white, true, white);
                img.canvasRenderer.cullTransparentMesh = false;
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var cb = btn.colors;
                cb.normalColor = new Color(0.35f, 0.75f, 1f, 0.22f);
                cb.highlightedColor = new Color(0.35f, 0.75f, 1f, 0.35f);
                cb.pressedColor = new Color(0.6f, 0.9f, 1f, 0.6f);
                cb.selectedColor = cb.normalColor;
                cb.disabledColor = new Color(0f, 0f, 0f, 0f);
                btn.colors = cb;
                var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
                UiFactory.AddText("Nombre", box, label, 24, C, new Vector2(0f, 6f), new Vector2(w, 32f), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                var iconRt = UiFactory.Rect("Rejilla", box, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(36f, 36f));
                UiFactory.AddImage(iconRt, Color.white, false, ventIcon).preserveAspect = true;
                UnityEventTools.AddIntPersistentListener(btn.onClick, vent.Choose, nameList.Count);
                here.SetAsFirstSibling(); // debajo del nombre de la sala
                nameList.Add(z.name);
                btnList.Add(btn);
                markList.Add(here.gameObject);
            }
            names = nameList.ToArray();
            buttons = btnList.ToArray();
            markers = markList.ToArray();
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
