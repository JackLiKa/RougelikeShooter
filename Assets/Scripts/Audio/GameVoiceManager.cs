using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameVoiceManager : MonoBehaviour
{
    private enum AudioSceneScope
    {
        None = 0,
        MainMenu = 1,
        CharacterPanel = 2,
        Game = 3
    }

    private const string AudioResourceFolder = "AudioVoices/";
    private const string MainMenuLoopKey = "MainMenuLoop";
    private const string CharacterLoopKey = "InCharacterInfoLoop";
    private const string InGameLoopKey = "InGameLoop";
    private const string GameStartKey = "GameStart";
    private const string Enemy1DieKey = "Enemy1Die";
    private const string DireEnemy1DieKey = "DireEnemy1Die";
    private const string UpPlayerKey = "UpPlayer";
    private const string FirstInWaterKey = "FirstInWater";
    private const string MainMenuSceneName = "MainMenuScene";
    private const string CharacterPanelSceneName = "CharacterPanelScene";
    private const string GameSceneName = "GameScene";

    private static GameVoiceManager instance;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    private AudioSource loopSource;
    private AudioSource oneShotSource;
    private string currentLoopKey = string.Empty;
    private AudioSceneScope currentScope = AudioSceneScope.None;
    private int currentSceneHandle = int.MinValue;

    public static GameVoiceManager EnsureExists(GameObject host = null)
    {
        if (instance != null)
        {
            instance.SyncToScene(SceneManager.GetActiveScene(), false);
            return instance;
        }

        GameVoiceManager existing = FindAnyObjectByType<GameVoiceManager>();
        if (existing != null)
        {
            instance = existing;
            instance.SyncToScene(SceneManager.GetActiveScene(), false);
            return existing;
        }

        GameObject root = new GameObject("GameVoiceManager");
        if (host != null)
        {
            root.transform.position = host.transform.position;
        }

        instance = root.AddComponent<GameVoiceManager>();
        return instance;
    }

    public static void PlayEnemyDeath(string enemyKey)
    {
        if (string.IsNullOrWhiteSpace(enemyKey))
        {
            return;
        }

        if (!IsCurrentScope(AudioSceneScope.Game))
        {
            return;
        }

        EnsureExists().PlayOneShot(enemyKey.Equals("DireEnemy1", System.StringComparison.OrdinalIgnoreCase) ? DireEnemy1DieKey : Enemy1DieKey, 0.92f);
    }

    public static void PlayUpgradePlayer()
    {
        if (!IsCurrentScope(AudioSceneScope.CharacterPanel))
        {
            return;
        }

        EnsureExists().PlayOneShot(UpPlayerKey, 0.96f);
    }

    public static void PlayFirstInWater()
    {
        if (!IsCurrentScope(AudioSceneScope.Game))
        {
            return;
        }

        EnsureExists().PlayOneShot(FirstInWaterKey, 1f);
    }

    public static void EnterMainMenuScene()
    {
        EnsureExists().EnterSceneScope(AudioSceneScope.MainMenu, MainMenuSceneName);
    }

    public static void EnterCharacterPanelScene()
    {
        EnsureExists().EnterSceneScope(AudioSceneScope.CharacterPanel, CharacterPanelSceneName);
    }

    public static void EnterGameScene()
    {
        EnsureExists().EnterSceneScope(AudioSceneScope.Game, GameSceneName);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;

        PreloadClips();
        SyncToScene(SceneManager.GetActiveScene(), true);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void PreloadClips()
    {
        LoadClip(MainMenuLoopKey);
        LoadClip(CharacterLoopKey);
        LoadClip(InGameLoopKey);
        LoadClip(GameStartKey);
        LoadClip(Enemy1DieKey);
        LoadClip(DireEnemy1DieKey);
        LoadClip(UpPlayerKey);
        LoadClip(FirstInWaterKey);
    }

    private void EnterSceneScope(AudioSceneScope expectedScope, string expectedSceneName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !string.Equals(activeScene.name, expectedSceneName, StringComparison.Ordinal))
        {
            SyncToScene(activeScene, false);
            return;
        }

        SyncToScene(activeScene, expectedScope == AudioSceneScope.Game);
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        SyncToScene(nextScene, true);
    }

    private void SyncToScene(Scene scene, bool playSceneEnterOneShot)
    {
        RefreshAudioListeners(scene);
        SwitchToScope(ResolveScope(scene.name), scene, playSceneEnterOneShot);
    }

    private void SwitchToScope(AudioSceneScope nextScope, Scene scene, bool playSceneEnterOneShot)
    {
        int nextSceneHandle = scene.IsValid() ? scene.handle : int.MinValue;
        bool scopeChanged = currentScope != nextScope;
        bool sceneChanged = currentSceneHandle != nextSceneHandle;
        if (!scopeChanged && !sceneChanged)
        {
            return;
        }

        StopOneShot();
        currentScope = nextScope;
        currentSceneHandle = nextSceneHandle;
        switch (nextScope)
        {
            case AudioSceneScope.MainMenu:
                PlayLoop(MainMenuLoopKey, 0.48f);
                break;
            case AudioSceneScope.CharacterPanel:
                PlayLoop(CharacterLoopKey, 0.48f);
                break;
            case AudioSceneScope.Game:
                PlayLoop(InGameLoopKey, 0.2f);
                if (playSceneEnterOneShot)
                {
                    PlayOneShot(GameStartKey, 0.72f);
                }
                break;
            default:
                StopLoop();
                break;
        }
    }

    private AudioClip LoadClip(string clipKey)
    {
        if (clips.TryGetValue(clipKey, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(AudioResourceFolder + clipKey);
        clips[clipKey] = clip;
        return clip;
    }

    private void PlayLoop(string clipKey, float volume)
    {
        AudioClip clip = LoadClip(clipKey);
        if (clip == null)
        {
            StopLoop();
            return;
        }

        loopSource.volume = Mathf.Clamp01(volume);
        if (currentLoopKey == clipKey && loopSource.isPlaying)
        {
            return;
        }

        currentLoopKey = clipKey;
        loopSource.clip = clip;
        loopSource.Play();
    }

    private void StopLoop()
    {
        currentLoopKey = string.Empty;
        loopSource.Stop();
        loopSource.clip = null;
    }

    private void StopOneShot()
    {
        if (oneShotSource == null)
        {
            return;
        }

        oneShotSource.Stop();
        oneShotSource.clip = null;
    }

    private void PlayOneShot(string clipKey, float volume)
    {
        AudioClip clip = LoadClip(clipKey);
        if (clip == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void RefreshAudioListeners(Scene activeScene)
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasEnabledListener = false;
        for (int index = 0; index < listeners.Length; index++)
        {
            AudioListener listener = listeners[index];
            bool shouldEnable =
                listener != null &&
                listener.gameObject.scene == activeScene &&
                listener.gameObject.activeInHierarchy &&
                !hasEnabledListener;

            if (listener != null && listener.enabled != shouldEnable)
            {
                listener.enabled = shouldEnable;
            }

            if (shouldEnable)
            {
                hasEnabledListener = true;
            }
        }
    }

    private static AudioSceneScope ResolveScope(string sceneName)
    {
        if (string.Equals(sceneName, MainMenuSceneName, StringComparison.Ordinal))
        {
            return AudioSceneScope.MainMenu;
        }

        if (string.Equals(sceneName, CharacterPanelSceneName, StringComparison.Ordinal))
        {
            return AudioSceneScope.CharacterPanel;
        }

        if (string.Equals(sceneName, GameSceneName, StringComparison.Ordinal))
        {
            return AudioSceneScope.Game;
        }

        return AudioSceneScope.None;
    }

    private static bool IsCurrentScope(AudioSceneScope scope)
    {
        if (instance == null)
        {
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() &&
               ResolveScope(activeScene.name) == scope &&
               instance.currentScope == scope &&
               instance.currentSceneHandle == activeScene.handle;
    }
}
