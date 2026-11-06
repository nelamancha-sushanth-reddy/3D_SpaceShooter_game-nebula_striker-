using System.Collections;
using UnityEngine;

namespace NebulaStriker
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    /// <summary>
    /// Central game controller (singleton). Builds the whole world procedurally at start-up,
    /// owns the game-state machine (Menu → Playing ⇄ Paused → GameOver) and the score.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool NewRecord { get; private set; }
        public int Wave => spawner != null ? spawner.CurrentWave : 0;
        public float Difficulty => 1f + Mathf.Max(0, Wave - 1) * 0.12f;

        public PlayerController Player { get; private set; }
        public Transform PlayerTransform => Player != null && Player.IsAlive ? Player.transform : null;
        public WaveSpawner Spawner => spawner;
        public UIManager UI => ui;

        WaveSpawner spawner;
        UIManager ui;
        NebulaController nebula;
        Camera cam;
        bool canRestart;

        const string HighScoreKey = "NebulaStriker_HighScore";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Reset statics (safe even when "Enter Play Mode Options" disables domain reload)
            EnemyBase.Active.Clear();
            Projectile.Active.Clear();
            PowerUp.Active.Clear();

            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            BuildWorld();
        }

        void BuildWorld()
        {
            MaterialLibrary.Init();
            Sfx.Init(gameObject);

            // --- Camera ---
            cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 2500f;
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(0f, 3.2f, -17f);
            if (cam.GetComponent<ChaseCamera>() == null) cam.gameObject.AddComponent<ChaseCamera>();

            // --- Lighting: switch off any default lights and add our own key light ---
            foreach (var l in FindAllLights()) l.enabled = false;
            var sunGO = new GameObject("Key Light");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.None;
            sunGO.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.1f, 0.2f);

            // --- Nebula sky & environment ---
            nebula = gameObject.AddComponent<NebulaController>();
            nebula.Init(sun, cam);
            SpaceEnvironment.Build();

            // --- Systems ---
            spawner = gameObject.AddComponent<WaveSpawner>();
            ui = gameObject.AddComponent<UIManager>();
            ui.Build();
        }

        static Light[] FindAllLights()
        {
#if UNITY_2022_2_OR_NEWER
            return FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<Light>();
#endif
        }

        void Update()
        {
            bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            bool esc = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);

            switch (State)
            {
                case GameState.Menu:
                    if (enter) StartGame();
                    break;
                case GameState.Playing:
                    if (esc) SetPaused(true);
                    break;
                case GameState.Paused:
                    if (esc) SetPaused(false);
                    else if (Input.GetKeyDown(KeyCode.Q)) ReturnToMenu();
                    break;
                case GameState.GameOver:
                    if (canRestart && enter) StartGame();
                    else if (canRestart && Input.GetKeyDown(KeyCode.Escape)) ReturnToMenu();
                    break;
            }
        }

        public void StartGame()
        {
            StopAllCoroutines();
            ClearField();
            Time.timeScale = 1f;
            Score = 0;
            NewRecord = false;
            Player = ShipFactory.CreatePlayer();
            State = GameState.Playing;
            nebula.SetPalette(0);
            spawner.BeginRun();
            Sfx.Play(SfxId.Ui, 0.7f);
        }

        public void SetPaused(bool paused)
        {
            if (paused && State == GameState.Playing) { State = GameState.Paused; Time.timeScale = 0f; }
            else if (!paused && State == GameState.Paused) { State = GameState.Playing; Time.timeScale = 1f; }
            Sfx.Play(SfxId.Ui, 0.5f);
        }

        public void ReturnToMenu()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            spawner.StopRun();
            ClearField();
            State = GameState.Menu;
            nebula.SetPalette(0);
        }

        void ClearField()
        {
            foreach (var e in EnemyBase.Active.ToArray()) if (e != null) Destroy(e.gameObject);
            foreach (var p in Projectile.Active.ToArray()) if (p != null) Destroy(p.gameObject);
            foreach (var p in PowerUp.Active.ToArray()) if (p != null) Destroy(p.gameObject);
            if (Player != null) Destroy(Player.gameObject);
            Player = null;
        }

        public void AddScore(int amount)
        {
            if (State != GameState.Playing) return;
            Score += amount;
            if (Score > HighScore)
            {
                HighScore = Score;
                NewRecord = true;
            }
        }

        public void OnWaveStarted(int wave)
        {
            if (wave % 5 == 0) ui.ShowBanner("WARNING\nDREADNOUGHT APPROACHING", new Color(1f, 0.35f, 0.3f), 3f);
            else ui.ShowBanner("WAVE " + wave, new Color(0.5f, 0.9f, 1f), 2.2f);
            nebula.SetPalette((wave - 1) / 5);
            Sfx.Play(SfxId.Ui, 0.6f, 0.8f);
        }

        public void OnBossDefeated()
        {
            ui.ShowBanner("DREADNOUGHT DESTROYED", new Color(1f, 0.85f, 0.4f), 2.5f);
        }

        public void OnPlayerHit() => ui.Flash(0.45f);

        public void OnPlayerDied()
        {
            StartCoroutine(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            canRestart = false;
            yield return new WaitForSeconds(2.2f);
            State = GameState.GameOver;
            spawner.StopRun();
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            yield return new WaitForSecondsRealtime(0.8f);
            canRestart = true;
        }

        public bool CanRestart => canRestart;

        void OnApplicationQuit()
        {
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }
    }
}
