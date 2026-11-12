using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Recorte de periódico antes de la noche 1 (después del tutorial): entra girando con sonido de papel,
    /// cae con un golpe, suena una musiquita antigua y, al tocar la pantalla (o a los 12 s), funde a negro
    /// y empieza la noche. Se construye en tiempo de ejecución; la imagen y los audios están en Assets/Resources.
    /// </summary>
    public static class NewspaperIntro
    {
        const string ImageName = "imagen de inicio";
        const float AutoContinueSeconds = 12f;

        public static IEnumerator Play()
        {
            var tex = Resources.Load<Texture2D>(ImageName);
            if (tex == null) yield break;
            var music = Resources.Load<AudioClip>("periodico_musica");
            var paper = Resources.Load<AudioClip>("periodico_papel");
            var thud = Resources.Load<AudioClip>("periodico_golpe");

            var root = new GameObject("Periodico", typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = NewRect("Fondo", root.transform);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = Color.black;

            var paperRt = NewRect("Recorte", root.transform);
            paperRt.anchorMin = Vector2.zero; paperRt.anchorMax = Vector2.one; paperRt.offsetMin = paperRt.offsetMax = Vector2.zero;
            var raw = paperRt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            var fit = paperRt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)tex.width / tex.height;

            var hintRt = NewRect("Toca", root.transform);
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 40f);
            hintRt.sizeDelta = new Vector2(900f, 50f);
            var hint = hintRt.gameObject.AddComponent<Text>();
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 30; hint.alignment = TextAnchor.MiddleCenter; hint.fontStyle = FontStyle.Bold;
            hint.text = "Toca para continuar";
            hint.color = new Color(1f, 1f, 1f, 0f);
            hintRt.gameObject.AddComponent<Shadow>().effectColor = Color.black;

            var fadeRt = NewRect("Fundido", root.transform);
            fadeRt.anchorMin = Vector2.zero; fadeRt.anchorMax = Vector2.one; fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
            var fade = fadeRt.gameObject.AddComponent<Image>();
            fade.color = new Color(0f, 0f, 0f, 0f);
            fade.raycastTarget = false;

            var sfx = root.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            var mus = root.AddComponent<AudioSource>();
            mus.playOnAwake = false; mus.loop = true; mus.clip = music;
            float musicVol = 0.75f * SaveSystem.MusicVolume;

            // 1) Entra girando (como en las películas antiguas) con el sonido del papel.
            if (paper != null) sfx.PlayOneShot(paper, 0.9f * SaveSystem.SfxVolume);
            if (mus.clip != null) { mus.volume = 0f; mus.Play(); }
            const float spin = 1.25f;
            for (float t = 0f; t < spin; t += Time.unscaledDeltaTime)
            {
                float k = t / spin;
                float e = 1f - Mathf.Pow(1f - k, 3f);
                paperRt.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, e);
                paperRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(720f, 0f, e));
                mus.volume = musicVol * k;
                yield return null;
            }
            paperRt.localScale = Vector3.one;
            paperRt.localRotation = Quaternion.identity;
            mus.volume = musicVol;
            if (thud != null) sfx.PlayOneShot(thud, SaveSystem.SfxVolume);

            // 2) Pequeño rebote al caer y lento acercamiento mientras se lee.
            float hold = 0f;
            bool go = false;
            while (!go)
            {
                hold += Time.unscaledDeltaTime;
                float bounce = hold < 0.25f ? 1f + 0.035f * Mathf.Sin(hold / 0.25f * Mathf.PI) : 1f;
                paperRt.localScale = Vector3.one * bounce * (1f + 0.06f * Mathf.Clamp01(hold / AutoContinueSeconds));
                if (hold > 1.8f) hint.color = new Color(1f, 1f, 1f, 0.45f + 0.4f * Mathf.Abs(Mathf.Sin(hold * 2.5f)));
                if (hold > 1f && (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) go = true;
                if (hold >= AutoContinueSeconds) go = true;
                yield return null;
            }

            // 3) Transición: papel, el recorte se aleja y todo funde a negro mientras baja la música.
            if (paper != null) sfx.PlayOneShot(paper, 0.6f * SaveSystem.SfxVolume);
            const float outTime = 1.2f;
            Vector3 from = paperRt.localScale;
            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                float k = t / outTime;
                paperRt.localScale = from * Mathf.Lerp(1f, 1.35f, k * k);
                fade.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, k));
                hint.color = new Color(1f, 1f, 1f, 0f);
                mus.volume = musicVol * (1f - k);
                yield return null;
            }
            mus.Stop();
            yield return new WaitForSecondsRealtime(0.3f);
            Object.Destroy(root);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
