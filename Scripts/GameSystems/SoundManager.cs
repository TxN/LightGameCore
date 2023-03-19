using System.Collections.Generic;

using UnityEngine;

using JetBrains.Annotations;

namespace SMGCore {
	public sealed class SoundManager : MonoSingleton<SoundManager> {
		const string RunnedPrefsPar  = "SoundManager_RunnedPreviously";
		const string SoundPrefsPar   = "SoundManager_SoundEnabled";
		const string MusicPrefsPar   = "SoundManager_MusicEnabled";
		const string SoundLibPath    = "Sounds/SoundLibrary";
		const int    MaxSoundSources = 8;

		bool                          _inited       = false;
		SoundLibrary                  _library      = null;
		AudioSource                   _musicSource  = null;
		List<AudioSource>             _soundSources = null;
		Dictionary<string, AudioClip> _soundDict    = null;
		Dictionary<string, AudioClip> _musicDict    = null;

		public bool SoundEnabled {
			get {
				return PlayerPrefs.GetFloat(SoundPrefsPar) > 0.01f;
			}
			set {
				var val = value ? 1f : 0f;
				AudioListener.volume = val;
				PlayerPrefs.SetFloat(SoundPrefsPar, val);
			}
		}

		public bool MusicEnabled {
			get {
				return PlayerPrefs.GetFloat(MusicPrefsPar) > 0.01f;
			}
			set {
				var val = value ? 1f : 0f;
				if ( _musicSource ) {
					_musicSource.volume = val;
				}				
				PlayerPrefs.SetFloat(MusicPrefsPar, val);
			}
		}

		protected override void Awake() {
			base.Awake();
			if ( PlayerPrefs.GetInt(RunnedPrefsPar) != 1) {
				SoundEnabled = true;
				MusicEnabled = true;
				PlayerPrefs.SetInt(RunnedPrefsPar, 1);
			}
			if ( !SoundEnabled ) {
				AudioListener.volume = 0f;
			}
			if ( !MusicEnabled && _musicSource ) {
				_musicSource.volume = 0f;
			}
			if ( !transform.parent ) {
				DontDestroyOnLoad(gameObject);
			}
			
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
			_musicSource.volume = MusicEnabled ? 1f : 0f;
			_musicSource.loop = true;
			_musicSource.Stop();
			_musicSource.clip = musicClip;
			_musicSource.Play();
		}

		public AudioSource PlaySound(string soundName, float volume = 1f, float pitch = 1f, bool loop = false) {
			TryInit();
			var soundClip = GetSoundClip(soundName);
			if ( soundClip == null ) {
				return null;
			}
			var source = GetFreeSoundSource();
			source.Stop();
			source.spatialBlend = 0f;
			source.volume = volume;
			source.pitch = pitch;
			source.clip = soundClip;
			source.loop = loop;
			source.Play();
			return source;
		}

		public AudioSource Play3DSound(string soundName, Vector3 position, float volume = 1f, float pitch = 1f, bool loop = false) {
			var s = PlaySound(soundName, volume, pitch, loop);
			if ( !s ) {
				return null;
			}
			s.transform.position = position;
			s.spatialBlend = 1f;
			return s;
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
		public void StopSound(string soundName, bool instant = true) {
			TryInit();
			var clip = GetSoundClip(soundName);
			if ( clip != null ) {
				foreach ( var source in _soundSources ) {
					if ( source.clip == clip ) {
						if ( instant ) {
							source.Stop();
						} else {
							source.loop = false;
						}
					}
				} 
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
				Destroy(child.gameObject);
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
