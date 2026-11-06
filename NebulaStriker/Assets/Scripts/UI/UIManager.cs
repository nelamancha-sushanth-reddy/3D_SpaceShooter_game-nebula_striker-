using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaStriker
{
    /// <summary>
    /// Builds the complete uGUI interface from code: title screen, HUD (score, wave, hull bar,
    /// bombs, power-up timers), boss health bar, wave banners, pickup toasts, damage flash,
    /// pause and game-over screens.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);
        static readonly Color Violet = new Color(0.8f, 0.6f, 1f);

        Font font;
        GameObject hudRoot, menuRoot, pauseRoot, gameOverRoot, bossRoot;
        Text scoreText, waveText, bestText, bombText, statusText, bannerText, toastText;
        Text menuPrompt, menuBest, goScore, goBest, goRecord, goPrompt;
        RectTransform hpFill, bossFill;
        Image hpFillImg, damageFlash;
        float bannerTimer, bannerDuration = 1f, toastTimer, flashTimer;
        Color flashColor = Color.red;
        readonly StringBuilder sb = new StringBuilder();

        public void Build()
        {
            font = LoadFont();

            var canvasGO = new GameObject("UI Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            Transform root = canvasGO.transform;

            damageFlash = MakeImage(root, "DamageFlash", new Color(1f, 0.1f, 0.1f, 0f));
            Stretch(damageFlash.rectTransform);

            // ---------------- HUD ----------------
            hudRoot = Group(root, "HUD");
            var hud = hudRoot.transform;
            scoreText = MakeText(hud, "Score", 44, TextAnchor.UpperLeft, Color.white, true);
            Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(700f, 60f));
            waveText = MakeText(hud, "Wave", 40, TextAnchor.UpperCenter, Cyan, true);
            Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(500f, 60f));
            bestText = MakeText(hud, "Best", 32, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.7f), false);
            Place(bestText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -34f), new Vector2(600f, 50f));

            var hpLabel = MakeText(hud, "HullLabel", 26, TextAnchor.LowerLeft, Cyan, true);
            hpLabel.text = "HULL INTEGRITY";
            Place(hpLabel.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 76f), new Vector2(400f, 36f));
            hpFill = MakeBar(hud, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(440f, 26f), Cyan, out hpFillImg);

            bombText = MakeText(hud, "Bombs", 28, TextAnchor.LowerLeft, new Color(1f, 0.5f, 0.95f), true);
            Place(bombText.rectTransform, new Vector2(0f, 0f), new Vector2(510f, 38f), new Vector2(400f, 40f));

            statusText = MakeText(hud, "Status", 28, TextAnchor.LowerRight, new Color(1f, 0.9f, 0.5f), true);
            Place(statusText.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 38f), new Vector2(800f, 140f));

            // Boss bar
            bossRoot = Group(root, "BossBar");
            var bossName = MakeText(bossRoot.transform, "BossName", 30, TextAnchor.LowerCenter, new Color(1f, 0.45f, 0.4f), true);
            bossName.text = BossEnemy.DisplayName;
            Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(800f, 40f));
            Image bossImg;
            bossFill = MakeBar(bossRoot.transform, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(900f, 20f), new Color(1f, 0.3f, 0.25f), out bossImg);
            bossRoot.SetActive(false);

            bannerText = MakeText(root, "Banner", 84, TextAnchor.MiddleCenter, Cyan, true);
            Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1600f, 240f));
            bannerText.color = new Color(1f, 1f, 1f, 0f);

            toastText = MakeText(root, "Toast", 40, TextAnchor.MiddleCenter, Color.white, true);
            Place(toastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(900f, 60f));
            toastText.color = new Color(1f, 1f, 1f, 0f);

            // ---------------- Menu ----------------
            menuRoot = Group(root, "Menu");
            var m = menuRoot.transform;
            var shade = MakeImage(m, "Shade", new Color(0f, 0f, 0.02f, 0.35f));
            Stretch(shade.rectTransform);
            var title = MakeText(m, "Title", 140, TextAnchor.MiddleCenter, Cyan, true);
            title.text = "NEBULA STRIKER";
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1700f, 180f));
            var sub = MakeText(m, "Subtitle", 34, TextAnchor.MiddleCenter, Violet, false);
            sub.text = "DEFEND THE SECTOR  ·  SURVIVE THE WAVES  ·  DESTROY THE DREADNOUGHT";
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1700f, 50f));
            menuPrompt = MakeText(m, "Prompt", 50, TextAnchor.MiddleCenter, Color.white, true);
            menuPrompt.text = "PRESS  ENTER  TO  LAUNCH";
            Place(menuPrompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1200f, 70f));
            var controls = MakeText(m, "Controls", 28, TextAnchor.UpperCenter, new Color(0.85f, 0.9f, 1f, 0.9f), false);
            controls.text =
                "WASD / ARROWS   —   Fly\n" +
                "SPACE / LEFT MOUSE   —   Fire twin lasers\n" +
                "B / RIGHT MOUSE   —   Nova Bomb (clears the screen)\n" +
                "SHIFT   —   Barrel roll (dodge, invulnerable)\n" +
                "ESC / P   —   Pause";
            controls.lineSpacing = 1.25f;
            Place(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 260f));
            menuBest = MakeText(m, "Best", 32, TextAnchor.LowerCenter, new Color(1f, 0.85f, 0.4f), true);
            Place(menuBest.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(800f, 50f));
            var credit = MakeText(m, "Credit", 22, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.45f), false);
            credit.text = "GMDD Course Project  ·  Built with Unity";
            Place(credit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(800f, 36f));

            // ---------------- Pause ----------------
            pauseRoot = Group(root, "Pause");
            var ps = MakeImage(pauseRoot.transform, "Shade", new Color(0f, 0f, 0.03f, 0.6f));
            Stretch(ps.rectTransform);
            var pt = MakeText(pauseRoot.transform, "Title", 110, TextAnchor.MiddleCenter, Cyan, true);
            pt.text = "PAUSED";
            Place(pt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 150f));
            var ph = MakeText(pauseRoot.transform, "Hint", 34, TextAnchor.MiddleCenter, Color.white, false);
            ph.text = "ESC  —  Resume          Q  —  Quit to menu";
            Place(ph.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1200f, 60f));
            pauseRoot.SetActive(false);

            // ---------------- Game Over ----------------
            gameOverRoot = Group(root, "GameOver");
            var g = gameOverRoot.transform;
            var gs = MakeImage(g, "Shade", new Color(0.05f, 0f, 0.02f, 0.55f));
            Stretch(gs.rectTransform);
            var gt = MakeText(g, "Title", 130, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f), true);
            gt.text = "GAME OVER";
            Place(gt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1400f, 170f));
            goScore = MakeText(g, "Score", 54, TextAnchor.MiddleCenter, Color.white, true);
            Place(goScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 70f));
            goBest = MakeText(g, "Best", 36, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f), false);
            Place(goBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1200f, 50f));
            goRecord = MakeText(g, "Record", 44, TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.6f), true);
            goRecord.text = "- NEW RECORD -";
            Place(goRecord.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1200f, 60f));
            goPrompt = MakeText(g, "Prompt", 36, TextAnchor.MiddleCenter, Color.white, false);
            goPrompt.text = "ENTER  —  Fly again          ESC  —  Main menu";
            Place(goPrompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(1400f, 60f));
            gameOverRoot.SetActive(false);
        }

        // ------------------------------------------------------------------ public API
        public void ShowBanner(string text, Color color, float duration)
        {
            bannerText.text = text;
            bannerText.color = color;
            bannerDuration = duration;
            bannerTimer = duration;
        }

        public void Toast(string text, Color color)
        {
            toastText.text = text;
            toastText.color = color;
            toastTimer = 1.6f;
        }

        public void Flash(float strength) => Flash(strength, new Color(1f, 0.1f, 0.1f));

        public void Flash(float strength, Color color)
        {
            flashColor = color;
            flashTimer = Mathf.Max(flashTimer, strength);
        }

        // ------------------------------------------------------------------ per-frame refresh
        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || hudRoot == null) return;
            float dt = Time.unscaledDeltaTime;
            var st = gm.State;

            menuRoot.SetActive(st == GameState.Menu);
            hudRoot.SetActive(st == GameState.Playing || st == GameState.Paused);
            pauseRoot.SetActive(st == GameState.Paused);
            gameOverRoot.SetActive(st == GameState.GameOver);

            if (st == GameState.Menu)
            {
                var c = menuPrompt.color;
                c.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
                menuPrompt.color = c;
                menuBest.text = "BEST SCORE   " + gm.HighScore.ToString("N0");
            }

            if (st == GameState.Playing || st == GameState.Paused)
            {
                scoreText.text = "SCORE   " + gm.Score.ToString("000000");
                waveText.text = "WAVE " + Mathf.Max(1, gm.Wave);
                bestText.text = "BEST  " + gm.HighScore.ToString("N0");
                var p = gm.Player;
                if (p != null)
                {
                    float pct = p.Health.Percent;
                    SetBar(hpFill, pct);
                    hpFillImg.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), Cyan, Mathf.InverseLerp(0.2f, 0.7f, pct));
                    bombText.text = "NOVA BOMBS  x" + p.Weapons.Bombs;

                    sb.Length = 0;
                    if (p.Weapons.RapidTime > 0f) sb.Append("RAPID FIRE  ").Append(Mathf.CeilToInt(p.Weapons.RapidTime)).Append("s\n");
                    if (p.ShieldTimeLeft > 0f) sb.Append("SHIELD  ").Append(Mathf.CeilToInt(p.ShieldTimeLeft)).Append("s\n");
                    sb.Append(p.RollReady ? "ROLL READY" : "ROLL ...");
                    statusText.text = sb.ToString();
                }
            }

            var boss = gm.Spawner != null ? gm.Spawner.Boss : null;
            bool showBoss = boss != null && (st == GameState.Playing || st == GameState.Paused);
            if (bossRoot.activeSelf != showBoss) bossRoot.SetActive(showBoss);
            if (showBoss) SetBar(bossFill, boss.Health.Percent);

            if (goRecord != null && st == GameState.GameOver)
            {
                goScore.text = "SCORE   " + gm.Score.ToString("N0") + "      WAVE  " + gm.Wave;
                goBest.text = "BEST   " + gm.HighScore.ToString("N0");
                goRecord.gameObject.SetActive(gm.NewRecord);
                var c = goPrompt.color;
                c.a = gm.CanRestart ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)) : 0f;
                goPrompt.color = c;
            }

            // Banner fade in/out
            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float elapsed = bannerDuration - bannerTimer;
                float a = Mathf.Clamp01(elapsed / 0.3f) * Mathf.Clamp01(bannerTimer / 0.5f);
                var c = bannerText.color; c.a = a; bannerText.color = c;
                bannerText.rectTransform.localScale = Vector3.one * (1f + 0.08f * (1f - Mathf.Clamp01(elapsed / 0.3f)));
            }
            else if (bannerText.color.a > 0f) { var c = bannerText.color; c.a = 0f; bannerText.color = c; }

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                var c = toastText.color; c.a = Mathf.Clamp01(toastTimer / 0.4f); toastText.color = c;
                toastText.rectTransform.anchoredPosition = new Vector2(0f, -170f + (1.6f - toastTimer) * 30f);
            }

            if (flashTimer > 0f) flashTimer = Mathf.Max(0f, flashTimer - dt * 1.6f);
            var fc = flashColor; fc.a = flashTimer * 0.6f; damageFlash.color = fc;
        }

        // ------------------------------------------------------------------ builders
        static Font LoadFont()
        {
            Font f = null;
#if UNITY_2022_2_OR_NEWER
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return f;
        }

        static GameObject Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        Text MakeText(Transform parent, string name, int size, TextAnchor anchor, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.75f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static RectTransform MakeBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor, out Image fillImg)
        {
            var bg = MakeImage(parent, "BarBG", new Color(0f, 0f, 0f, 0.55f));
            Place(bg.rectTransform, anchor, pos, size);
            var frame = bg.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(fillColor.r, fillColor.g, fillColor.b, 0.6f);
            frame.effectDistance = new Vector2(2f, 2f);
            fillImg = MakeImage(bg.transform, "Fill", fillColor);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(3f, 3f);
            rt.offsetMax = new Vector2(-3f, -3f);
            return rt;
        }

        static void SetBar(RectTransform fill, float pct)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
        }
    }
}
