#if UNITY_2017_1_OR_NEWER
using System.Collections.Generic;

using UnityEngine;

namespace SMGCore {
	public sealed class SoundLibrary : ScriptableObject {
		public List<SoundItem> Sounds = new List<SoundItem>();
		public List<SoundItem> Music  = new List<SoundItem>();
	}

	[System.Serializable]
	public sealed class SoundItem {
		public string SoundName = string.Empty;
		public AudioClip Clip   = null;
	}
}
#endif