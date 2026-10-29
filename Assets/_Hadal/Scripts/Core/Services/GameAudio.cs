using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Services
{
    /// <summary>Music loops; each crossfades into the next.</summary>
    public enum MusicTrack
    {
        None,
        Menu,
        Base,
        World,
        Battle
    }

    /// <summary>One-shot effects; the clip is Resources/Audio/Sfx/&lt;snake_case name&gt;.</summary>
    public enum Sfx
    {
        UiTap,
        UiToggle,
        PanelOpen,
        PanelClose,
        Reward,
        ChestOpen,
        Collect,
        BuildStart,
        BuildComplete,
        ResearchComplete,
        MarchLaunch,
        SubTravel,
        SonarPing,
        Teleport,
        BattleHit,
        Explosion,
        LevelUp,
        VipClaim,
        Error,
        Speedup
    }

    /// <summary>
    /// The game's music and sound effects (art/audio makes the clips). Music crossfades between the base,
    /// world map, battle and title loops and remembers where each one was; victory and defeat stings duck it.
    /// Effects come from a small pool of sources. Volumes and mutes follow <see cref="GameSettings"/>.
    /// </summary>
    public static class GameAudio
    {
        public const string MusicFolder = "Audio/Music/";
        public const string SfxFolder = "Audio/Sfx/";

        private static GameAudioRunner _runner;

        public static MusicTrack CurrentMusic => _runner != null ? _runner.Current : MusicTrack.None;

        public static void SetMusic(MusicTrack track) => Runner()?.SetMusic(track);

        public static void Play(Sfx sfx, float volume = 1f) => Runner()?.Play(sfx, volume);

        /// <summary>Victory or defeat fanfare over the music.</summary>
        public static void Sting(bool victory) => Runner()?.Sting(victory ? "victory" : "defeat");

        public static string MusicClip(MusicTrack track) => track switch
        {
            MusicTrack.Menu => "menu",
            MusicTrack.Base => "base",
            MusicTrack.World => "world",
            MusicTrack.Battle => "battle",
            _ => null
        };

        /// <summary>UiTap -> "ui_tap".</summary>
        public static string SfxClip(Sfx sfx)
        {
            var name = sfx.ToString();
            var chars = new System.Text.StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                    chars.Append('_');
                chars.Append(char.ToLowerInvariant(name[i]));
            }

            return chars.ToString();
        }

        private static GameAudioRunner Runner()
        {
            if (_runner != null)
                return _runner;
            if (!Application.isPlaying)
                return null;

            var go = new GameObject("GameAudio");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<GameAudioRunner>();
            return _runner;
        }
    }

    internal sealed class GameAudioRunner : MonoBehaviour
    {
        private const int Voices = 10;
        private const float FadeSeconds = 1.6f;
        private const float BattleFadeSeconds = 0.7f;

        private sealed class Deck
        {
            public AudioSource Source;
            public MusicTrack Track;
            public float Level;
            public float Target;
        }

        private readonly Deck[] _decks = new Deck[2];
        private readonly Dictionary<MusicTrack, float> _resumeAt = new();
        private readonly Dictionary<Sfx, AudioClip> _clips = new();
        private readonly Dictionary<Sfx, float> _lastPlayed = new();
        private readonly List<AudioSource> _voices = new();
        private AudioSource _sting;
        private float _duck = 1f;
        private float _duckUntil;
        private float _fade = FadeSeconds;
        private float _nextListenerCheck;

        public MusicTrack Current { get; private set; }

        private void Awake()
        {
            for (var i = 0; i < _decks.Length; i++)
                _decks[i] = new Deck { Source = NewSource("Music" + i, true) };
            _sting = NewSource("Sting", false);
            for (var i = 0; i < Voices; i++)
                _voices.Add(NewSource("Sfx" + i, false));
        }

        private AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = loop ? 0 : 128;
            return source;
        }

        public void SetMusic(MusicTrack track)
        {
            if (track == Current)
                return;
            Current = track;
            Debug.Log($"[Audio] music {track}");
            _fade = track == MusicTrack.Battle ? BattleFadeSeconds : FadeSeconds;

            // The outgoing deck fades away and remembers where it was, so the base theme picks up where it left off.
            foreach (var deck in _decks)
            {
                if (deck.Track != MusicTrack.None && deck.Source.isPlaying)
                    _resumeAt[deck.Track] = deck.Source.time;
                deck.Target = 0f;
            }

            if (track == MusicTrack.None)
                return;

            var clip = Resources.Load<AudioClip>(GameAudio.MusicFolder + GameAudio.MusicClip(track));
            if (clip == null)
                return;

            var next = _decks[0].Level <= _decks[1].Level ? _decks[0] : _decks[1];
            next.Source.Stop();
            next.Source.clip = clip;
            next.Track = track;
            next.Level = 0f;
            next.Target = 1f;
            next.Source.volume = 0f;
            next.Source.Play();
            // Battle always starts from its top; the calm loops resume.
            if (track != MusicTrack.Battle && _resumeAt.TryGetValue(track, out var at) && at < clip.length - 1f)
                next.Source.time = at;
        }

        public void Play(Sfx sfx, float volume)
        {
            var level = GameSettings.EffectiveSoundVolume * volume;
            if (level <= 0.001f)
                return;

            // The same effect twice within a few frames (a tap that also opens a panel, a burst of results) plays once.
            var now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sfx, out var last) && now - last < 0.07f)
                return;
            _lastPlayed[sfx] = now;

            if (!_clips.TryGetValue(sfx, out var clip))
            {
                clip = Resources.Load<AudioClip>(GameAudio.SfxFolder + GameAudio.SfxClip(sfx));
                _clips[sfx] = clip;
                Debug.Log($"[Audio] sfx {sfx} first play, clip {(clip != null ? "loaded" : "missing")}");
            }

            if (clip == null)
                return;

            var voice = FreeVoice();
            voice.clip = clip;
            voice.volume = level;
            voice.pitch = sfx is Sfx.UiTap or Sfx.BattleHit or Sfx.Collect ? Random.Range(0.96f, 1.04f) : 1f;
            voice.Play();
        }

        public void Sting(string name)
        {
            var clip = Resources.Load<AudioClip>(GameAudio.MusicFolder + name);
            if (clip == null)
                return;
            Debug.Log($"[Audio] sting {name}");
            _sting.Stop();
            _sting.clip = clip;
            _sting.volume = GameSettings.EffectiveMusicVolume;
            _sting.Play();
            _duckUntil = Time.unscaledTime + clip.length * 0.85f;
        }

        private AudioSource FreeVoice()
        {
            AudioSource oldest = _voices[0];
            foreach (var voice in _voices)
            {
                if (!voice.isPlaying)
                    return voice;
                if (voice.time > oldest.time)
                    oldest = voice;
            }

            return oldest;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            var duckTarget = Time.unscaledTime < _duckUntil ? 0.2f : 1f;
            _duck = Mathf.MoveTowards(_duck, duckTarget, dt * (duckTarget < _duck ? 4f : 0.8f));
            var music = GameSettings.EffectiveMusicVolume;
            foreach (var deck in _decks)
            {
                deck.Level = Mathf.MoveTowards(deck.Level, deck.Target, dt / _fade);
                // Equal-power curve so the crossfade does not dip in the middle.
                deck.Source.volume = Mathf.Sin(deck.Level * Mathf.PI * 0.5f) * music * _duck;
                if (deck.Level <= 0f && deck.Target <= 0f && deck.Source.isPlaying)
                    deck.Source.Stop();
            }

            if (_sting.isPlaying)
                _sting.volume = music;

            // Everything here is 2D, but Unity plays nothing without an enabled listener (the world map swaps cameras).
            if (Time.unscaledTime >= _nextListenerCheck)
            {
                _nextListenerCheck = Time.unscaledTime + 1f;
                EnsureListener();
            }
        }

        private void EnsureListener()
        {
            var own = GetComponent<AudioListener>();
            var others = 0;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != own && listener.isActiveAndEnabled)
                    others++;
            }

            if (others == 0 && own == null)
                gameObject.AddComponent<AudioListener>();
            else if (own != null)
                own.enabled = others == 0;
        }
    }
}
