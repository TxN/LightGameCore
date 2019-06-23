using System.Collections.Generic;

using UnityEngine;

using JetBrains.Annotations;

namespace SMGCore {
	public sealed class SoundManager : MonoSingleton<SoundManager> {
		const string SoundLibPath    = "Sounds/SoundLibrary";
		const int    MaxSoundSources = 8;

		bool                          _inited       = false;
		SoundLibrary                  _library      = null;
		AudioSource                   _musicSource  = null;
		List<AudioSource>             _soundSources = null;
		Dictionary<string, AudioClip> _soundDict    = null;
		Dictionary<string, AudioClip> _musicDict    = null;

		protected override void Awake() {
			base.Awake();
			DontDestroyOnLoad(gameObject);
			TryInit();
		}

		public void PlayMusic(string clipName) {
			TryInit();
			var musicClip = GetMusicClip(clipName);
			if ( musicClip == null ) {
				return;
			}
			if ( _musicSource.clip == musicClip ) {
				return;
			}
			_musicSource.loop = true;
			_musicSource.Stop();
			_musicSource.clip = musicClip;
			_musicSource.Play();
		}

		public void PlaySound(string soundName, float volume = 1f, float pitch = 1f) {
			TryInit();
			var soundClip = GetSoundClip(soundName);
			if ( soundClip == null ) {
				return;
			}
			var source = GetFreeSoundSource();
			source.Stop();
			source.volume = volume;
			source.pitch = pitch;
			source.clip = soundClip;
			source.Play();
		}

		public void StopMusic() {
			TryInit();
			_musicSource.Stop();
		}

		public void StopAllSounds() {
			TryInit();
			foreach ( var source in _soundSources ) {
				source.Stop();
			}
		}

		[CanBeNull]
		public AudioClip GetSoundClip(string clipName) {
			TryInit();
			if ( !_soundDict.TryGetValue(clipName, out AudioClip result) ) {
				Debug.LogWarningFormat("Cannot find sound with name '{0}'", clipName);
			}
			return result;
		}

		[CanBeNull]
		public AudioClip GetMusicClip(string clipName) {
			TryInit();
			if ( !_musicDict.TryGetValue(clipName, out AudioClip result) ) {
				Debug.LogWarningFormat("Cannot find music with name '{0}'", clipName);
			}
			return result;
		}

		void TryInit() {
			if ( !_inited ) {
				Init();
			}
		}

		void Init() {
			_library = Resources.Load<SoundLibrary>(SoundLibPath);
			Debug.Assert(_library != null, "Sound library is null");
			_soundDict = FillDict(_library.Sounds);
			_musicDict = FillDict(_library.Music);

			foreach ( Transform child in transform ) {
				Destroy(child);
			}

			var musicHolder = new GameObject("Music");
			musicHolder.transform.SetParent(transform, false);
			_musicSource = musicHolder.AddComponent<AudioSource>();
			var soundHolder = new GameObject("Sounds");
			soundHolder.transform.SetParent(transform, false);
			_soundSources = new List<AudioSource>();
			for ( int i = 0; i < MaxSoundSources; i++ ) {
				_soundSources.Add(soundHolder.AddComponent<AudioSource>());
			}
			_inited = true;
		}

		Dictionary<string, AudioClip> FillDict(List<SoundItem> sounds) {
			var dict = new Dictionary<string, AudioClip>();
			foreach ( var sound in sounds ) {
				dict.Add(sound.SoundName, sound.Clip);
			}
			return dict;
		}

		AudioSource GetFreeSoundSource() {
			foreach ( var source in _soundSources ) {
				if ( !source.isPlaying ) {
					return source;
				}
			}
			return _soundSources[Random.Range(0, _soundSources.Count)];
		}
	}
}