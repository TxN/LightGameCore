using System.Collections.Generic;

using UnityEngine;

public sealed class SoundLibrary : ScriptableObject {
    public List<SoundItem> Sounds = new List<SoundItem>();
    public List<SoundItem> Music  = new List<SoundItem>();
}

[System.Serializable]
public sealed class SoundItem {
    public string    SoundName = string.Empty;
    public AudioClip Clip      = null;
}
