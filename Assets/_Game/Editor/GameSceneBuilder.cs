using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo.EditorTools
{
    /// <summary>Arma la escena de juego a partir de los datos del mapa original (rm_oficina_camaras).</summary>
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Game.unity";

        static Dictionary<string, SpriteMeta> meta;

        // Escalas de los personajes. El jugador (sprite de 52 px) mide unos 215 px del mapa y los animatrónicos
        // (sprites de 47-48 px) unos 230 px: siempre algo más grandes que él, en proporción con los muebles.
        const float PlayerScale = 4.3f;
        const float AnimatronicScale = 4.9f;
        const float ShadowScale = 7.6f;
        const float PlayerFootWidth = 1.8f, PlayerFootHeight = 0.9f;
        static Vector2 Gm(float x, float y) => GameUnits.FromGm(x, y);

        public static void Build(NightConfig[] nights)
        {
            meta = new Dictionary<string, SpriteMeta>();
            foreach (var s in JsonUtility.FromJson<SpriteMetaList>(File.ReadAllText(BuilderUtil.GameRoot + "Data/sprites.json")).sprites) meta[s.name] = s;
            var map = BuilderUtil.LoadMap();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var white = BuilderUtil.GeneratedSprite("blanco", 4, false);

            // ---------- Cámara y luz ----------
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 24f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);
            var zoneCam = camGo.AddComponent<ZoneCamera>();

            var globalLight = NewLight("Luz global", null, Light2D.LightType.Global, 0.75f, Color.white);

            // ---------- Sistemas ----------
            var systems = new GameObject("Sistemas");
            var zones = systems.AddComponent<MapZones>();
            var nav = systems.AddComponent<NavGrid>();
            var clock = systems.AddComponent<NightClock>();
            var power = systems.AddComponent<PowerSystem>();
            var lighting = systems.AddComponent<LightingController>();
            var director = systems.AddComponent<AnimatronicDirector>();
            var spawner = systems.AddComponent<FuseSpawner>();
            var audio = systems.AddComponent<AudioManager>();
            var gm = systems.AddComponent<GameManager>();
            var debugTools = systems.AddComponent<DebugTools>();
            int blockMask = (1 << BuilderUtil.WallsLayer) | (1 << BuilderUtil.ObstaclesLayer);
            int sightMask = 1 << BuilderUtil.WallsLayer;
            BuilderUtil.SetValue(nav, "blockingLayers", p => p.intValue = blockMask);
            // La Despensa está debajo de la Cocina en el mapa: la cuadrícula llega hasta y = 8384 px.
            BuilderUtil.SetValue(nav, "heightCells", p => p.intValue = 262);
            BuilderUtil.SetRef(lighting, "globalLight", globalLight);
            AudioBuilder.Configure(audio);

            // ---------- Mapa ----------
            var mapRoot = new GameObject("Mapa").transform;
            var zonesRoot = BuilderUtil.Child("Zonas", mapRoot).transform;
            var zoneList = new List<ZoneInfo>();
            foreach (var z in map.zones)
            {
                var m = meta[z.sprite];
                var go = BuilderUtil.Child("Zona " + z.name, zonesRoot);
                go.transform.position = Gm(z.x + m.ox * z.sx, z.y + m.oy * z.sy);
                go.transform.localScale = new Vector3(z.sx, z.sy, 1f);
                var sr = BuilderUtil.AddSprite(go, BuilderUtil.First(z.sprite));
                sr.sortingOrder = -30000;
                go.isStatic = true;
                zoneList.Add(new ZoneInfo { name = z.name, rect = new Rect(z.x / 32f, -(z.y + z.h) / 32f, z.w / 32f, z.h / 32f) });
            }
            BuilderUtil.SetValue(zones, "zones", p =>
            {
                p.arraySize = zoneList.Count;
                for (int i = 0; i < zoneList.Count; i++)
                {
                    var e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("name").stringValue = zoneList[i].name;
                    e.FindPropertyRelative("rect").rectValue = zoneList[i].rect;
                }
            });

            var walls = BuilderUtil.Child("Paredes", mapRoot).transform;
            var obstacles = BuilderUtil.Child("Obstaculos", mapRoot).transform;
            var warps = BuilderUtil.Child("Pasos", mapRoot).transform;
            var hides = BuilderUtil.Child("Escondites", mapRoot).transform;
            var vents = BuilderUtil.Child("Ventilaciones", mapRoot).transform;
            var props = BuilderUtil.Child("Objetos", mapRoot).transform;
            var lights = BuilderUtil.Child("Luces", mapRoot).transform;
            var pickups = BuilderUtil.Child("Fusibles", mapRoot).transform;

            // ---------- Jugador ----------
            var playerData = map.instances.Find(i => i.obj == "obj_jugador");
            var playerGo = new GameObject("Jugador");
            playerGo.transform.position = Gm(playerData.x, playerData.y);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // Colisión en los pies (vista cenital 3/4): el cuerpo puede tapar la pared o un mueble que está
            // detrás, pero los pies nunca pisan un mueble ni la pared.
            var box = playerGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(PlayerFootWidth, PlayerFootHeight);
            box.offset = new Vector2(0f, PlayerFootHeight * 0.5f);
            playerGo.AddComponent<Inventory>();
            var playerVisual = BuilderUtil.Child("Visual", playerGo.transform);
            playerVisual.transform.localScale = new Vector3(PlayerScale, PlayerScale, 1f);
            var psr = BuilderUtil.AddSprite(playerVisual, BuilderUtil.First("sp_player_caminar_frente"));
            var panim = playerVisual.AddComponent<SpriteFrameAnimator>();
            playerGo.AddComponent<YSort>();
            var player = playerGo.AddComponent<PlayerController>();
            playerGo.AddComponent<DoorDangerSense>();
            playerGo.AddComponent<NoiseMeter>();   // ruido al correr (atrae a los animatrónicos cercanos)   // pasos apagados + borde rojo si hay alguien tras la puerta
            BuilderUtil.SetRef(player, "anim", panim);
            // Sin linterna hasta recogerla; luego, los cuadros con la linterna en la mano.
            BuilderUtil.SetDirectional(player, "sprites", BuilderUtil.Directional("sp_player", "_sinlinterna"));
            BuilderUtil.SetDirectional(player, "runSprites", new DirectionalSprites
            {
                down = BuilderUtil.Frames("sp_player_correr_frente_sinlinterna"), up = BuilderUtil.Frames("sp_player_correr_atras_sinlinterna"),
                right = BuilderUtil.Frames("sp_player_correr_derecha_sinlinterna"), left = BuilderUtil.Frames("sp_player_correr_izquierda_sinlinterna")
            });
            BuilderUtil.SetDirectional(player, "lightSprites", BuilderUtil.Directional("sp_player"));
            BuilderUtil.SetDirectional(player, "lightRunSprites", new DirectionalSprites
            {
                down = BuilderUtil.Frames("sp_player_correr_frente"), up = BuilderUtil.Frames("sp_player_correr_atras"),
                right = BuilderUtil.Frames("sp_player_correr_derecha"), left = BuilderUtil.Frames("sp_player_correr_izquierda")
            });
            psr.sprite = BuilderUtil.First("sp_player_caminar_frente_sinlinterna");
            var pmotion = playerVisual.AddComponent<SpriteMotion>();
            BuilderUtil.SetRef(player, "motion", pmotion);
            BuilderUtil.SetRefs(player, "hideFrames", BuilderUtil.Frames("sp_player_esconderse"));
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            var spawn = new GameObject("Inicio del jugador").transform;
            spawn.position = playerGo.transform.position;

            var flashGo = new GameObject("Linterna");
            var flashlight = flashGo.AddComponent<Flashlight>();
            BuilderUtil.SetRef(player, "flashlight", flashlight);
            var beam = NewLight("Haz", flashGo.transform, Light2D.LightType.Point, 1.1f, new Color(1f, 0.95f, 0.8f));
            // Haz más largo y ancho; Flashlight.Illuminates usa estos mismos valores (lo que ves es lo que alumbra a Rufus).
            beam.pointLightOuterRadius = 38f;
            beam.pointLightInnerRadius = 6f;
            beam.pointLightOuterAngle = 52f;
            beam.pointLightInnerAngle = 28f;
            var halo = NewLight("Halo", flashGo.transform, Light2D.LightType.Point, 0.6f, new Color(1f, 0.95f, 0.8f));
            halo.pointLightOuterRadius = 3.5f;
            halo.pointLightInnerRadius = 0.5f;
            BuilderUtil.SetRef(flashlight, "player", player);
            BuilderUtil.SetValue(flashlight, "lightHeight", p => p.floatValue = 1.1f * PlayerScale / 3.6f);
            BuilderUtil.SetValue(flashlight, "spritePixel", p => p.floatValue = PlayerScale / 32f);
            BuilderUtil.SetRef(flashlight, "beam", beam);
            BuilderUtil.SetRef(flashlight, "halo", halo);
            BuilderUtil.SetValue(flashlight, "sightBlockers", p => p.intValue = sightMask);

            // ---------- Objetos del mapa ----------
            int hideCount = 0;
            var warpByGm = new Dictionary<Vector2, Warp>();
            FusePanel panel = null;
            var poweredLights = new List<Light2D>();
            CameraMonitor monitor = null;
            ArcadeMachine arcadeMachine = null;
            PhoneCall phone = null;
            foreach (var it in map.instances)
            {
                Vector2 at = Gm(it.x, it.y);
                switch (it.obj)
                {
                    case "obj_pared":
                    case "obj_pared_2":
                    {
                        bool wall = it.obj == "obj_pared";
                        var go = BuilderUtil.Child(wall ? "Pared" : "Obstaculo", wall ? walls : obstacles);
                        go.transform.position = at;
                        go.transform.rotation = Quaternion.Euler(0f, 0f, it.rot);
                        go.layer = wall ? BuilderUtil.WallsLayer : BuilderUtil.ObstaclesLayer;
                        go.isStatic = true;
                        var c = go.AddComponent<BoxCollider2D>();
                        c.size = new Vector2(Mathf.Abs(it.sx), Mathf.Abs(it.sy));
                        c.offset = new Vector2(it.sx * 0.5f, -it.sy * 0.5f);
                        break;
                    }
                    case "obj_warp":
                    {
                        var go = BuilderUtil.Child("Paso", warps);
                        go.transform.position = at;
                        var w = go.AddComponent<Warp>();
                        float wpx = it.sx, hpx = it.sy;
                        w.area = new Rect(it.x / 32f, -(it.y / 32f + hpx), wpx, hpx);
                        // El paso del original es una franja de 1 casilla pegada al muro: con la perspectiva
                        // 3/4 el jugador ya está dentro del marco y aún no cruza. Se alarga hacia el piso
                        // (abajo en puertas superiores, arriba en las inferiores) para cruzar al pisar el umbral.
                        var wz = map.zones.Find(z => z.x <= it.x + 1f && it.x <= z.x + z.w && z.y <= it.y + 1f && it.y <= z.y + z.h);
                        bool topDoor = wz == null || it.y < wz.y + wz.h * 0.5f;
                        if (topDoor) w.area.yMin -= 1.0f; else w.area.yMax += 1.2f;
                        w.destination = Gm(it.tx, it.ty);
                        w.arrivalFacing = it.arrival != null && it.arrival.Contains("atras") ? Facing.Up : Facing.Down;
                        warpByGm[new Vector2(it.x, it.y)] = w;
                        break;
                    }
                    case "obj_escondite":
                    {
                        var go = BuilderUtil.Child("Escondite " + (++hideCount), hides);
                        go.transform.position = Gm(it.x + 16f, it.y + 16f);
                        var h = go.AddComponent<HidingSpot>();
                        string zoneName = ZoneName(zoneList, go.transform.position);
                        BuilderUtil.SetValue(h, "displayName", p => p.stringValue = "Escondite (" + zoneName + ")");
                        BuilderUtil.SetValue(h, "radius", p => p.floatValue = 3.4f);
                        break;
                    }
                    case "obj_ventilation":
                    {
                        var go = BuilderUtil.Child("Ventilacion", vents);
                        go.transform.position = at;
                        var sr = BuilderUtil.AddSprite(go, BuilderUtil.First("sp_ventilation"));
                        sr.sortingOrder = -20000;
                        var h = go.AddComponent<HidingSpot>();
                        string zoneName = ZoneName(zoneList, at);
                        BuilderUtil.SetValue(h, "isVent", p => p.boolValue = true);
                        BuilderUtil.SetValue(h, "displayName", p => p.stringValue = zoneName);
                        BuilderUtil.SetValue(h, "airSeconds", p => p.floatValue = 5f); // 4 s del original + 1 s extra
                        BuilderUtil.SetValue(h, "cooldownSeconds", p => p.floatValue = 9.3f);
                        BuilderUtil.SetValue(h, "enterHoldSeconds", p => p.floatValue = 1f);
                        BuilderUtil.SetValue(h, "exitOffset", p => p.vector2Value = new Vector2(0f, -150f / 32f));
                        BuilderUtil.SetValue(h, "radius", p => p.floatValue = 4.5f);
                        break;
                    }
                    case "obj_panel_fusibles":
                    {
                        var go = BuilderUtil.Child("Panel de fusibles", props);
                        go.transform.position = Gm(it.x, it.y - 100f);
                        var body = BuilderUtil.Child("Panel", go.transform, new Vector3(0f, -100f / 32f, 0f));
                        BuilderUtil.AddSprite(body, BuilderUtil.First("sp_panel")).sortingOrder = -20000;
                        var slots = new Object[PowerSystem.FuseCount];
                        for (int i = 0; i < slots.Length; i++)
                        {
                            var f = BuilderUtil.Child("Fusible " + (i + 1), go.transform, new Vector3((-60f + i * 40f) / 32f, 0f, 0f));
                            var fsr = BuilderUtil.AddSprite(f, BuilderUtil.First("sp_fusible_1"));
                            fsr.sortingOrder = -19990;
                            f.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                            slots[i] = fsr;
                        }
                        panel = go.AddComponent<FusePanel>();
                        BuilderUtil.SetRefs(panel, "fuseSlots", slots);
                        BuilderUtil.SetRef(panel, "fuseOk", BuilderUtil.First("sp_fusible_1"));
                        BuilderUtil.SetRef(panel, "fuseBroken", BuilderUtil.First("sp_fusible_0"));
                        BuilderUtil.SetValue(panel, "radius", p => p.floatValue = 6f);
                        BuilderUtil.SetValue(panel, "workSpotOffset", p => p.vector2Value = new Vector2(33f / 32f, -148f / 32f));
                        break;
                    }
                    case "obj_switch_general":
                    {
                        var go = BuilderUtil.Child("Interruptor general", props);
                        go.transform.position = Gm(it.x, it.y - 68f);
                        var body = BuilderUtil.Child("Sprite", go.transform, new Vector3(0f, -68f / 32f, 0f));
                        var frames = BuilderUtil.Frames("sp_interruptor");
                        var sr = BuilderUtil.AddSprite(body, frames.Length > 1 ? frames[1] : null);
                        sr.sortingOrder = -20000;
                        var sw = go.AddComponent<PowerSwitch>();
                        BuilderUtil.SetRef(sw, "spriteRenderer", sr);
                        if (frames.Length > 1) { BuilderUtil.SetRef(sw, "offSprite", frames[0]); BuilderUtil.SetRef(sw, "onSprite", frames[1]); }
                        BuilderUtil.SetValue(sw, "radius", p => p.floatValue = 3.5f);
                        break;
                    }
                    case "obj_telefono":
                    {
                        var go = BuilderUtil.Child("Telefono", props);
                        go.transform.position = at;
                        var body = BuilderUtil.Child("Sprite", go.transform);
                        body.transform.localScale = new Vector3(it.sx, it.sy, 1f);
                        BuilderUtil.AddSprite(body, BuilderUtil.First("sp_telefono")).sortingOrder = -20000;
                        phone = go.AddComponent<PhoneCall>();
                        BuilderUtil.SetRef(phone, "visual", body.transform);
                        BuilderUtil.SetValue(phone, "radius", p => p.floatValue = 14f);
                        break;
                    }
                    case "obj_pc_trigger":
                    {
                        var go = BuilderUtil.Child("Monitor de camaras", props);
                        go.transform.position = at;
                        monitor = go.AddComponent<CameraMonitor>();
                        BuilderUtil.SetRef(monitor, "zoneCamera", zoneCam);
                        BuilderUtil.SetValue(monitor, "radius", p => p.floatValue = 14f);
                        break;
                    }
                    case "obj_linterna_pickup":
                    {
                        var go = BuilderUtil.Child("Linterna (recoger)", props);
                        go.transform.position = at;
                        BuilderUtil.AddSprite(go, BuilderUtil.First("spr_linterna_item")).sortingOrder = -19000;
                        var lp = go.AddComponent<FlashlightPickup>();
                        BuilderUtil.SetRef(lp, "flashlight", flashlight);
                        BuilderUtil.SetValue(lp, "radius", p => p.floatValue = 3.4f);
                        break;
                    }
                    case "obj_arcade_trigger":
                    {
                        var go = BuilderUtil.Child("Maquina del arcade", props);
                        go.transform.position = at;
                        arcadeMachine = go.AddComponent<ArcadeMachine>();
                        BuilderUtil.SetValue(arcadeMachine, "radius", p => p.floatValue = 4.5f);
                        var glow = NewLight("Pantalla encendida", go.transform, Light2D.LightType.Point, 0.9f, new Color(0.55f, 0.75f, 1f));
                        glow.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                        glow.pointLightOuterRadius = 4f;
                        glow.pointLightInnerRadius = 0.4f;
                        BuilderUtil.SetRef(arcadeMachine, "screenGlow", glow.transform);
                        break;
                    }
                    case "obj_fusible_pickup":
                        CreateFusePickup("Fusible", pickups, at);
                        break;
                    case "obj_luz_disco":
                    {
                        var l = NewLight("Foco de colores", lights, Light2D.LightType.Point, 1.2f, Color.red);
                        l.transform.position = at;
                        l.pointLightOuterRadius = 5f;
                        l.pointLightInnerRadius = 0.5f;
                        l.gameObject.AddComponent<DiscoLight>();
                        BuilderUtil.SetRef(l.GetComponent<DiscoLight>(), "light2D", l);
                        poweredLights.Add(l);
                        break;
                    }
                    case "obj_luz_mantenimiento":
                    {
                        var zone = zoneList.Find(z => z.name == "Mantenimiento");
                        var go = BuilderUtil.Child("Falla de luz (Mantenimiento)", lights);
                        go.transform.position = zone.rect.center;
                        go.transform.localScale = new Vector3(zone.rect.width, zone.rect.height, 1f);
                        var sr = BuilderUtil.AddSprite(go, white, false);
                        sr.color = new Color(0f, 0f, 0f, 0f);
                        sr.sortingOrder = 32000;
                        go.AddComponent<FlickerOverlay>();
                        break;
                    }
                }
            }
            BuilderUtil.SetRefs(lighting, "poweredLights", poweredLights.ToArray());

            // ---------- Puertas de la oficina (cortina + placa) ----------
            var doorsRoot = BuilderUtil.Child("Puertas", mapRoot).transform;
            // Cortina: centro y ancho medidos sobre el hueco real del fondo de la oficina (px del mapa original).
            // Orientación: ambas puertas están arriba en la oficina, así que llevan a la parte de ABAJO
            // de la sala siguiente (izquierda -> Arcade, derecha -> Comedor, como en el minimapa).
            BuildOfficeDoor("Puerta al arcade", doorsRoot, warpByGm, new Vector2(800f, 416f), new Vector2(1280f, 3168f), 941.7f, 240f, 1151.5f);
            BuildOfficeDoor("Puerta al comedor", doorsRoot, warpByGm, new Vector2(2336f, 416f), new Vector2(4354f, 1472f), 2470.5f, 240f, 2680f);

            // ---------- Puerta nueva: Comedor <-> Escenario (sin carteles) ----------
            BuildSideDoor("Puerta Comedor-Escenario", doorsRoot, warps, new Vector2(3086f, 1250f),
                new Rect(3070f / 32f, -1330f / 32f, 65f / 32f, 160f / 32f), Gm(2700f, 4480f), Facing.Left);
            BuildSideDoor("Puerta Escenario-Comedor", doorsRoot, warps, new Vector2(2814f, 4470f),
                new Rect(2770f / 32f, -4560f / 32f, 60f / 32f, 180f / 32f), Gm(3200f, 1260f), Facing.Right);

            // Plantilla de fusible para el generador (desactivada).
            var template = CreateFusePickup("Plantilla de fusible", pickups, Vector2.zero);
            template.gameObject.SetActive(false);
            BuilderUtil.SetRef(spawner, "template", template);
            BuilderUtil.SetRef(spawner, "container", pickups);

            // ---------- Animatrónicos ----------
            var animRoot = new GameObject("Animatronicos").transform;
            var brains = new List<Object>();
            brains.Add(Animatronic<BongoBrain>(map, "obj_bonnie_guitarra", "Bongo", AnimatronicId.Bongo, "sp_bonnie", "_guitarra", animRoot, sightMask, 1100f, 70f, "mov_bongo.wav"));
            var matilda = Animatronic<MatildaBrain>(map, "obj_chica", "Matilda", AnimatronicId.Matilda, "sp_chica", "", animRoot, sightMask, 1100f, 70f, "mov_matilda.wav");
            BuilderUtil.SetRef(matilda, "panel", panel);
            brains.Add(matilda);
            brains.Add(Animatronic<AlfredoBrain>(map, "obj_freddy", "Alfredo", AnimatronicId.Alfredo, "sp_freddy", "", animRoot, sightMask, 650f, 45f, "mov_alfredo.wav"));
            var rufus = Animatronic<RufusBrain>(map, "obj_foxy", "Rufus", AnimatronicId.Rufus, "sp_foxy", "", animRoot, sightMask, 1100f, 70f, "mov_rufus.wav");
            BuilderUtil.SetRef(rufus, "flashlight", flashlight);
            brains.Add(rufus);
            BuilderUtil.SetRefs(director, "animatronics", brains.ToArray());

            // ---------- Shadow Alfredo (cuarto secreto, nunca se mueve) ----------
            var shadowGo = new GameObject("Shadow Alfredo");
            shadowGo.transform.SetParent(animRoot, false);
            shadowGo.transform.position = Gm(5216f, 6096f);
            var shadowVisual = BuilderUtil.Child("Visual", shadowGo.transform);
            shadowVisual.transform.localScale = new Vector3(ShadowScale, ShadowScale, 1f);
            var shadowSr = BuilderUtil.AddSprite(shadowVisual, BuilderUtil.First("sp_shadow_caminar_frente"));
            shadowGo.AddComponent<YSort>();
            var shadowEyes = NewLight("Ojos", shadowGo.transform, Light2D.LightType.Point, 0f, new Color(0.7f, 0.3f, 1f));
            shadowEyes.transform.localPosition = new Vector3(0f, 3.2f * ShadowScale / 4.2f, 0f);
            shadowEyes.pointLightOuterRadius = 4f;
            shadowEyes.pointLightInnerRadius = 0.3f;
            var shadow = shadowGo.AddComponent<ShadowFreddy>();
            BuilderUtil.SetRef(shadow, "body", shadowSr);
            BuilderUtil.SetRef(shadow, "eyes", shadowEyes);
            BuilderUtil.SetRefs(shadow, "screamers", new Object[] { BuilderUtil.UiSprite("screamer_shadow_1"), BuilderUtil.UiSprite("screamer_shadow_2"), BuilderUtil.UiSprite("screamer_shadow_3") });
            var voices = new Object[4];
            for (int i = 0; i < 4; i++) voices[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Game/Audio/voz_shadow_{i + 1}.wav");
            BuilderUtil.SetRefs(shadow, "voices", voices);
            BuilderUtil.SetValue(shadow, "lines", p =>
            {
                string[] l = { "You will not be spared. You will not be saved.", "I am your wickedness made flesh.", "I am here to claim what is left of you.", "I will vomit you back to relive your horror." };
                p.arraySize = l.Length;
                for (int i = 0; i < l.Length; i++) p.GetArrayElementAtIndex(i).stringValue = l[i];
            });
            BuilderUtil.SetRef(shadow, "screamSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/screamer_shadow.wav"));

            // ---------- Interfaz ----------
            var ui = GameUiBuilder.Build(white, player, interactor, flashlight, clock, director, zoneCam);
            BuilderUtil.SetRef(monitor, "monitorPanel", ui.monitorPanel);
            BuilderUtil.SetRef(monitor, "cameraName", ui.cameraName);
            BuilderUtil.SetRef(monitor, "staticOverlay", ui.staticOverlay);
            BuilderUtil.SetRef(monitor, "signalLost", ui.signalLost);
            BuilderUtil.SetRef(monitor, "hudGroup", ui.hudGroup);
            BuilderUtil.SetRef(monitor, "recText", ui.recText);
            GameUiBuilder.WireMonitorButtons(ui, monitor);
            BuilderUtil.SetRefs(monitor, "mapHighlights", ui.mapHighlights);
            BuilderUtil.SetRef(ui.hud, "monitor", monitor);
            if (arcadeMachine != null) BuilderUtil.SetRef(arcadeMachine, "game", ui.arcade);
            BuilderUtil.SetRef(shadow, "view", ui.screamer);
            systems.AddComponent<AmbientEvents>();
            var advisor = systems.AddComponent<TipAdvisor>();
            BuilderUtil.SetRef(advisor, "player", player);
            BuilderUtil.SetRef(advisor, "flashlight", flashlight);
            BuilderUtil.SetRef(advisor, "director", director);
            BuilderUtil.SetRef(phone, "subtitleText", ui.subtitleText);
            BuilderUtil.SetRef(phone, "subtitlePanel", ui.subtitlePanel);
            GameUiBuilder.WirePhone(ui, phone);

            // ---------- GameManager ----------
            // NewScene descarga los objetos en memoria: se cargan de nuevo desde disco.
            var nightAssets = new Object[nights.Length];
            for (int i = 0; i < nights.Length; i++)
                nightAssets[i] = AssetDatabase.LoadAssetAtPath<NightConfig>($"Assets/_Game/Data/Noches/Noche{i + 1}.asset");
            BuilderUtil.SetRefs(gm, "nights", nightAssets);
            BuilderUtil.SetRef(gm, "clock", clock);
            BuilderUtil.SetRef(gm, "player", player);
            BuilderUtil.SetRef(gm, "director", director);
            BuilderUtil.SetRef(gm, "power", power);
            BuilderUtil.SetRef(gm, "fuseSpawner", spawner);
            BuilderUtil.SetRef(gm, "screens", ui.screens);
            BuilderUtil.SetRef(gm, "zoneCamera", zoneCam);
            BuilderUtil.SetRef(gm, "playerSpawn", spawn);
            BuilderUtil.SetRef(zoneCam, "player", player);
            BuilderUtil.SetRef(debugTools, "clock", clock);
            BuilderUtil.SetRef(debugTools, "director", director);
            BuilderUtil.SetRef(debugTools, "player", player);
            GameUiBuilder.WireGameButtons(ui, gm);

            Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Alfredo] Escena de juego creada: " + ScenePath);
        }

        // Hueco de la puerta en el fondo de la oficina (px del mapa): parte alta y alto de la cortina.
        const float ShutterTopGm = 144.7f;
        const float ShutterHeightGm = 380f;
        // Centro (y) de la placa en la pared: alineada con la caja de interruptor dibujada en el fondo.
        const float PlateCenterGm = 324f;

        /// <summary>Cortina metálica que rellena el hueco de la puerta y placa en la pared, junto a ella.</summary>
        static void BuildOfficeDoor(string name, Transform parent, Dictionary<Vector2, Warp> warpByGm, Vector2 insideGm, Vector2 outsideGm, float centerXGm, float widthGm, float plateXGm)
        {
            var root = BuilderUtil.Child(name, parent);
            var pivot = BuilderUtil.Child("Cortina", root.transform);
            pivot.transform.position = Gm(centerXGm, ShutterTopGm);
            // El sprite mide 96x118 px (32 px = 1 unidad): la escala lo estira hasta el hueco exacto.
            var sprite = BuilderUtil.Child("Sprite", pivot.transform, new Vector3(0f, -ShutterHeightGm / 32f / 2f, 0f));
            sprite.transform.localScale = new Vector3(widthGm / 96f, ShutterHeightGm / 118f, 1f);
            BuilderUtil.AddSprite(sprite, BuilderUtil.First("sp_puerta_oficina")).sortingOrder = -25000;

            var plateGo = BuilderUtil.Child("Placa", root.transform);
            plateGo.transform.position = Gm(plateXGm, 430f);
            var plateSprite = BuilderUtil.Child("Sprite", plateGo.transform, new Vector3(0f, (430f - PlateCenterGm) / 32f, 0f));
            plateSprite.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var plateFrames = BuilderUtil.Frames("sp_interruptor_puerta");
            var psr = BuilderUtil.AddSprite(plateSprite, plateFrames.Length > 1 ? plateFrames[1] : null);
            psr.sortingOrder = -20000;

            var door = root.AddComponent<OfficeDoor>();
            BuilderUtil.SetValue(door, "doorName", p => p.stringValue = name);
            warpByGm.TryGetValue(insideGm, out var inside);
            warpByGm.TryGetValue(outsideGm, out var outside);
            if (inside == null || outside == null) Debug.LogError("[Alfredo] No se encontraron los pasos de " + name);
            BuilderUtil.SetRef(door, "insideWarp", inside);
            BuilderUtil.SetRef(door, "outsideWarp", outside);
            BuilderUtil.SetRef(door, "shutter", pivot.transform);
            BuilderUtil.SetRef(door, "plateRenderer", psr);
            if (plateFrames.Length > 1) { BuilderUtil.SetRef(door, "plateClosed", plateFrames[0]); BuilderUtil.SetRef(door, "plateOpen", plateFrames[1]); }

            var plate = plateGo.AddComponent<DoorPlate>();
            BuilderUtil.SetRef(plate, "door", door);
            BuilderUtil.SetValue(plate, "radius", p => p.floatValue = 4f);
        }

        /// <summary>Puerta de madera en una pared lateral (sprite 44x160 px a escala 1:1) y su paso hacia la otra sala.</summary>
        static void BuildSideDoor(string name, Transform parent, Transform warps, Vector2 doorGm, Rect area, Vector2 destination, Facing facing)
        {
            // El vano, el marco y la hoja abierta están pintados en el fondo de cada sala (encajan con la pared);
            // aquí solo queda el paso.
            var root = BuilderUtil.Child(name, parent);

            var go = BuilderUtil.Child("Paso", warps);
            go.transform.position = area.center;
            var w = go.AddComponent<Warp>();
            w.area = area;
            w.destination = destination;
            w.arrivalFacing = facing;
        }

        /// <summary>
        /// Grito del jumpscare: si en Audio/ hay un archivo "ignited-[freddy|chica|bonnie|foxy]-jumpscare..." (cualquier
        /// formato) se usa ese; si no, el generado susto_[nombre].wav.
        /// </summary>
        static AudioClip FindScream(string who)
        {
            string gm = who == "alfredo" ? "freddy" : who == "matilda" ? "chica" : who == "bongo" ? "bonnie" : "foxy";
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).ToLowerInvariant().StartsWith("ignited-" + gm + "-jumpscare"))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/susto_" + who + ".wav");
        }

        static string ZoneName(List<ZoneInfo> zones, Vector2 p)
        {
            foreach (var z in zones) if (z.rect.Contains(p)) return z.name;
            return "?";
        }

        static FusePickup CreateFusePickup(string name, Transform parent, Vector2 at)
        {
            var go = BuilderUtil.Child(name, parent);
            go.transform.position = at;
            var visual = BuilderUtil.Child("Sprite", go.transform, new Vector3(0f, 0.6f, 0f));
            BuilderUtil.AddSprite(visual, BuilderUtil.First("sp_fusible_1"));
            go.AddComponent<YSort>();
            var light = NewLight("Brillo", go.transform, Light2D.LightType.Point, 0.6f, new Color(1f, 0.85f, 0.4f));
            light.pointLightOuterRadius = 2.2f;
            light.pointLightInnerRadius = 0.2f;
            var pickup = go.AddComponent<FusePickup>();
            BuilderUtil.SetRef(pickup, "visual", visual.transform);
            BuilderUtil.SetValue(pickup, "radius", p => p.floatValue = 3.4f);
            return pickup;
        }

        static T Animatronic<T>(MapData map, string obj, string displayName, AnimatronicId id, string prefix, string suffix, Transform parent, int sightMask, float viewPx, float halfAngle, string moveSound) where T : AnimatronicBrain
        {
            var data = map.instances.Find(i => i.obj == obj);
            var go = new GameObject(displayName);
            go.transform.SetParent(parent, false);
            go.transform.position = GameUnits.FromGm(data.x, data.y);
            var visual = BuilderUtil.Child("Visual", go.transform);
            visual.transform.localScale = new Vector3(AnimatronicScale, AnimatronicScale, 1f);
            var sprites = BuilderUtil.Directional(prefix, suffix);
            BuilderUtil.AddSprite(visual, sprites.down.Length > 0 ? sprites.down[0] : null);
            var anim = visual.AddComponent<SpriteFrameAnimator>();
            var amotion = visual.AddComponent<SpriteMotion>();
            BuilderUtil.SetValue(amotion, "leanDegrees", p => p.floatValue = 4f);
            BuilderUtil.SetValue(amotion, "shadowWidth", p => p.floatValue = 1.8f);
            go.AddComponent<YSort>();

            var motor = go.AddComponent<AnimatronicMotor>();
            BuilderUtil.SetRef(motor, "anim", anim);
            BuilderUtil.SetDirectional(motor, "sprites", sprites);
            var senses = go.AddComponent<AnimatronicSenses>();
            BuilderUtil.SetRef(senses, "motor", motor);
            BuilderUtil.SetValue(senses, "sightBlockers", p => p.intValue = sightMask);
            BuilderUtil.SetValue(senses, "viewDistancePx", p => p.floatValue = viewPx);
            BuilderUtil.SetValue(senses, "halfAngle", p => p.floatValue = halfAngle);

            var brain = go.AddComponent<T>();
            BuilderUtil.SetValue(brain, "id", p => p.enumValueIndex = (int)id);
            BuilderUtil.SetValue(brain, "displayName", p => p.stringValue = displayName);
            BuilderUtil.SetRef(brain, "motor", motor);
            BuilderUtil.SetRef(brain, "senses", senses);
            BuilderUtil.SetRef(brain, "visual", visual.transform);
            BuilderUtil.SetRef(brain, "jumpscareSprite", sprites.down.Length > 0 ? sprites.down[0] : null);
            BuilderUtil.SetValue(brain, "catchRadius", p => p.floatValue = 2.3f);
            var voice = go.AddComponent<AnimatronicVoice>();
            BuilderUtil.SetRef(voice, "brain", brain);
            BuilderUtil.SetRef(voice, "clip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/" + moveSound));
            BuilderUtil.SetRef(voice, "huntClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/" + moveSound.Replace("mov_", "caza_")));
            var stepClips = new List<Object>();
            for (int i = 1; i <= 4; i++)
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Game/Audio/paso_animatronico_{i}.wav");
                if (c != null) stepClips.Add(c);
            }
            BuilderUtil.SetRefs(voice, "steps", stepClips.ToArray());
            string whoStep = moveSound.Replace("mov_", "").Replace(".wav", "");
            float pitch = whoStep == "alfredo" ? 0.82f : whoStep == "matilda" ? 1.1f : whoStep == "rufus" ? 1.15f : 0.95f;
            BuilderUtil.SetValue(voice, "stepPitch", sp => sp.floatValue = pitch);
            string who = moveSound.Replace("mov_", "").Replace(".wav", "");
            BuilderUtil.SetRef(brain, "scream", FindScream(who));
            var lines = new System.Collections.Generic.List<Object>();
            for (int i = 1; i <= 3; i++)
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Game/Audio/frase_{who}_{i}.wav");
                if (c != null) lines.Add(c);
            }
            BuilderUtil.SetRefs(brain, "deathLines", lines.ToArray());
            if (brain is RufusBrain) BuilderUtil.SetRef(brain, "songClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/rufus_cancion.wav"));
            return brain;
        }

        static Light2D NewLight(string name, Transform parent, Light2D.LightType type, float intensity, Color color)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light2D>();
            l.intensity = intensity;
            l.color = color;
            var so = new SerializedObject(l);
            var typeProp = so.FindProperty("m_LightType");
            if (typeProp != null) typeProp.intValue = (int)type;
            var layers = so.FindProperty("m_ApplyToSortingLayers");
            if (layers != null)
            {
                var all = SortingLayer.layers;
                layers.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++) layers.GetArrayElementAtIndex(i).intValue = all[i].id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return l;
        }
    }
}
