#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using System.Collections.Generic;

namespace SMGCore {
	public class ShakeInstance {
		public string ID;
		public ShakeProfile Profile;
		public float MaxAmount;
		public float CurrentAmount;
		public float Duration;
		public float TimeRemaining;
		public Vector3 NoiseSeed;
		public bool IsDecaying = true;

		public ShakeInstance(string id, ShakeProfile profile, float amount, float duration, bool decaying = true) {
			ID = id;
			Profile = profile;
			MaxAmount = amount;
			CurrentAmount = amount;
			Duration = duration;
			TimeRemaining = duration;
			NoiseSeed = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);
			IsDecaying = decaying;
		}
	}

	public sealed class CameraShake : MonoBehaviour {
		public float MaxAmount = 5f;
		public float MaxDuration = 2f;
		public bool IsSuppressed = false; //подавляем тряску, но продолжаем "проигрывать" эффект

		public ShakeProfileCollection ProfileCollection;
		
		// API for external scripts to query legacy shake values
		public float shakeAmount => GetShake(LegacyShakeId)?.CurrentAmount ?? 0f;
		public float shakeDuration => GetShake(LegacyShakeId)?.TimeRemaining ?? 0f;

		public bool  smooth;
		public float smoothAmount = 5f;

		private List<ShakeProfile> _profiles = new List<ShakeProfile>();
		private readonly List<ShakeInstance> _activeInstances = new List<ShakeInstance>();
		private Vector3 _initialLocalPosition;
		private Quaternion _initialLocalRotation;
		private bool _initialized = false;
		private bool _wasShaking = false;
		
		private const string LegacyShakeId = "__LEGACY_SHAKE__";
		private static readonly ShakeProfile LegacyProfile = new ShakeProfile {
			Name = "Legacy",
			NoiseType = ShakeNoiseType.Random,
			RotationMultiplier = Vector3.one,
			PositionMultiplier = Vector3.zero,
			FalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f) 
		};

		void Awake() {
			Init();
		}

		void OnEnable() {
			Init();
		}

		void Init() {
			if (!_initialized) {
				if ( ProfileCollection == null ) {
					ProfileCollection = Resources.Load<ShakeProfileCollection>("DefaultShakeProfiles");
				}
				if ( ProfileCollection != null ) {
					_profiles = ProfileCollection.Profiles;
				}

				_initialLocalPosition = transform.localPosition;
				_initialLocalRotation = transform.localRotation;
				_initialized = true;
			}
		}
		
		void OnDisable() {
			StopAll();
			transform.localPosition = _initialLocalPosition;
			transform.localRotation = _initialLocalRotation;
		}

		public void AddDuration(float duration, string id = LegacyShakeId) {
			var instance = GetShake(id);
			if (instance != null) {
				var newDuration = Mathf.Clamp(instance.TimeRemaining + duration, 0, MaxDuration);
				instance.Duration = newDuration;
				instance.TimeRemaining = newDuration;
				instance.IsDecaying = true;
			}
		}

		public void ShakeCamera(float amount, float duration) {
			var clampedAmount = Mathf.Clamp(amount, 0, MaxAmount);
			var clampedDuration = Mathf.Clamp(duration, 0, MaxDuration);
			Shake(LegacyShakeId, clampedAmount, clampedDuration);
		}
		

#if ODIN_INSPECTOR
	[Sirenix.OdinInspector.Button]
#endif
		public void Shake(string profileName, float amount, float duration, bool decay = true, string id = LegacyShakeId) {
			var profile = _profiles.Find(p => p.Name == profileName);
			if ( profile == null ) {
				profile = LegacyProfile;
			}
			Shake(profile, amount, duration, decay, id);
		}

		public void Shake(ShakeProfile profile, float amount, float duration, bool decay = true, string id = LegacyShakeId) {
			if ( profile == null ) {
				profile = LegacyProfile;
			}
			var existing = _activeInstances.Find(x => x.ID == id);
			if ( existing != null ) {
				existing.MaxAmount = amount;
				existing.Duration = duration;
				existing.TimeRemaining = duration;
				existing.Profile = profile;
				existing.IsDecaying = decay;
			} else {
				_activeInstances.Add(new ShakeInstance(id, profile, amount, duration, decay));
			}
		}

		public ShakeInstance GetShake(string id = LegacyShakeId) {
			return _activeInstances.Find(x => x.ID == id);
		}

		public void StopShake(string id = LegacyShakeId) {
			var item = _activeInstances.Find(x => x.ID == id);
			if (item != null) {
				_activeInstances.Remove(item);
			} 
		}

		public void StopAll() {
			_activeInstances.Clear();			
			transform.localRotation = _initialLocalRotation;
			transform.localPosition = _initialLocalPosition;
		}

		public void Decay() {
			var instance = GetShake(LegacyShakeId);
			if (instance != null) {
				instance.IsDecaying = true;
			}
		}

		void Update() {
			if ( Time.timeScale < 0.01f ) {
				return;
			}

			Vector3 totalRotation = Vector3.zero;
			Vector3 totalPosition = Vector3.zero;

			for (int i = _activeInstances.Count - 1; i >= 0; i--) {
				var inst = _activeInstances[i];
				
				bool isConstant = inst.Duration <= 0f;
				bool preventDecay = !inst.IsDecaying && !isConstant;

				if (!isConstant && !preventDecay) {
					inst.TimeRemaining -= Time.deltaTime;
				}

				if (inst.TimeRemaining <= 0 && !isConstant) {
					_activeInstances.RemoveAt(i);
					continue;
				}

				float damp;
				if (isConstant || preventDecay) {
					damp = 1f;
				} else {
					float progress = Mathf.Clamp01(1f - (inst.TimeRemaining / inst.Duration));
					if (inst.Profile.FalloffCurve != null && inst.Profile.FalloffCurve.length > 0) {
						damp = inst.Profile.FalloffCurve.Evaluate(progress);
					} else {
						damp = 1f - progress;
					}
				}
				
				inst.CurrentAmount = inst.MaxAmount * damp;

				Vector3 noiseVec = Vector3.zero;
				var profile = inst.Profile ?? LegacyProfile;
				if (profile.NoiseType == ShakeNoiseType.Perlin) {
					float seed = Time.time * profile.Frequency;
					noiseVec.x = (Mathf.PerlinNoise(seed, inst.NoiseSeed.x) - 0.5f) * 2f;
					noiseVec.y = (Mathf.PerlinNoise(seed, inst.NoiseSeed.y) - 0.5f) * 2f;
					noiseVec.z = (Mathf.PerlinNoise(seed, inst.NoiseSeed.z) - 0.5f) * 2f;
				} else {
					noiseVec = Random.insideUnitSphere;
					noiseVec.z = 0;
				}

				totalRotation += Vector3.Scale(noiseVec * inst.CurrentAmount, profile.RotationMultiplier);
				totalPosition += Vector3.Scale(noiseVec * inst.CurrentAmount, profile.PositionMultiplier);
			}
			var isShakingThisFrame = _activeInstances.Count > 0;

			if ( isShakingThisFrame ) {
				if (!IsSuppressed) {
					Vector3 targetRotationEuler = totalRotation;
					Vector3 targetPosition = _initialLocalPosition + totalPosition;

					float currentSmoothAmount = smooth ? this.smoothAmount : LegacyProfile.Smoothness;
					var firstProfile = _activeInstances[0]?.Profile;
					if (firstProfile != null) currentSmoothAmount = firstProfile.Smoothness;


					if (smooth) {
						transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetRotationEuler), Time.deltaTime * currentSmoothAmount);
						transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * currentSmoothAmount);
					}
					else {
						transform.localRotation = Quaternion.Euler(targetRotationEuler);
						transform.localPosition = targetPosition;
					}
				}
			} else if ( _wasShaking ) {
				if ( !IsSuppressed ) {
					transform.localRotation = _initialLocalRotation;
					transform.localPosition = _initialLocalPosition;
				}
			}
			_wasShaking = isShakingThisFrame;
		}
	}
}
#endif
