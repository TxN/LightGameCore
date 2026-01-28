using UnityEngine;
using System.Collections.Generic;

namespace SMGCore {
    public enum ShakeNoiseType {
        Random,
        Perlin
    }

    [System.Serializable]
    public class ShakeProfile {
        public string Name;
        public ShakeNoiseType NoiseType = ShakeNoiseType.Random;
        public float Frequency = 10f;
        public Vector3 RotationMultiplier = Vector3.one;
        public Vector3 PositionMultiplier = Vector3.zero;
        public float Smoothness = 5f;
        public AnimationCurve FalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }

    [CreateAssetMenu(fileName = "ShakeProfileCollection", menuName = "SMGCore/Shake Profile Collection", order = 0)]
    public class ShakeProfileCollection : ScriptableObject {
        public AnimationCurve DefaultDistanceFalloffCurve = AnimationCurve.Linear(0f, 1f, 65f, 0f);
        public List<ShakeProfile> Profiles = new List<ShakeProfile>();
    }
}
