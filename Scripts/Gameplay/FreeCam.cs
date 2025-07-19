#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	/// <summary>
	/// A simple free camera to be added to a Unity game object.
	/// 
	/// Keys:
	///	wasd / arrows	- movement
	///	q/e 			- up/down (local space)
	///	r/f 			- up/down (world space)
	///	pageup/pagedown	- up/down (world space)
	///	hold shift		- enable fast movement mode
	///	right mouse  	- enable free look
	///	mouse			- free look / rotation
	///     
	/// </summary>
	public class FreeCam : MonoBehaviour {
		/// <summary>
		/// Normal speed of camera movement.
		/// </summary>
		public float movementSpeed = 10f;

		/// <summary>
		/// Speed of camera movement when shift is held down,
		/// </summary>
		public float fastMovementSpeed = 100f;

		/// <summary>
		/// Sensitivity for free look.
		/// </summary>
		public float freeLookSensitivity = 3f;

		/// <summary>
		/// Amount to zoom the camera when using the mouse wheel.
		/// </summary>
		public float zoomSensitivity = 10f;

		/// <summary>
		/// Amount to zoom the camera when using the mouse wheel (fast mode).
		/// </summary>
		public float fastZoomSensitivity = 50f;

		/// <summary>
		/// Enable smoothing of camera movement. Can be toggled at runtime.
		/// </summary>
		[Tooltip("Enable smooth camera movement")]
		public bool SmoothMovement = false;

		/// <summary>
		/// Interpolation factor for movement used when smoothing is enabled.
		/// </summary>
		[Range(0f, 1f)]
		[Tooltip("Smoothing factor for movement (0–slow / 1–instant)")]
		public float SmoothMovementFactor = 0.5f;

		/// <summary>
		/// Enable smoothing of camera rotation. Can be toggled at runtime.
		/// </summary>
		[Tooltip("Enable smooth camera rotation")]
		public bool SmoothRotation = false;

		/// <summary>
		/// Interpolation factor for rotation used when smoothing is enabled.
		/// </summary>
		[Range(0f, 1f)]
		[Tooltip("Smoothing factor for rotation (0–slow / 1–instant)")]
		public float SmoothRotationFactor = 0.5f;

		[Header("Noise")]
		public bool UseCameraMovementNoise = false;
		public float CameraMovementNoiseScale = 0.01f;
		public float CameraNoiseSpeedMultiplier = 1f;
		public float CameraNoiseBySpeedScale = 1f;
		public float BaseCameraNoise = 0.1f;
		public float NoiseVelocitySmoothFactor = 15f;

		/// <summary>
		/// Set to true when free looking (on right mouse button).
		/// </summary>
		private bool looking = false;

		// Target values used for smoothing
		private Vector3 _targetPosition;
		private Quaternion _targetRotation;
		private float _yaw;
		private float _pitch;
		// внутренние сглаженные значения, чтобы исключить крен
		private float _smoothYaw;
		private float _smoothPitch;

		private float _targetVelocityMagnitude;
		private Vector3 _noiseOffset;
		private Vector3 _currentPosition;

		bool _velocityMatchMode = false;
		Transform _targetTransform;
		Vector3 _lastTargetTransformPosition;

		public bool VelocityMatchEnabled => _velocityMatchMode && _targetTransform != null;
		public Transform VelocityMatchTarget => _targetTransform;

		[ContextMenu("Toggle Velocity Match Mode")]
		public void ToggleVelocityMatchMode() {
			_velocityMatchMode = !_velocityMatchMode;
			if ( _velocityMatchMode ) {
				var raycastHit = Physics.Raycast(transform.position, transform.forward, out var hit, 100f);
				if ( raycastHit ) {
					_targetTransform = hit.transform;
					_lastTargetTransformPosition = _targetTransform.position;
				} else {
					_velocityMatchMode = false;
				}
			}
			if (!_velocityMatchMode ) {
				_targetTransform = null;
				_lastTargetTransformPosition = Vector3.zero;
			}			
		}

		public void ForcePositionAndRotation(Vector3 position, Quaternion rotation) {
			_targetPosition = position;
			_targetRotation = rotation;
			_yaw = rotation.eulerAngles.y;
			_pitch = rotation.eulerAngles.x;
			_smoothYaw = _yaw;
			_currentPosition = position;
		}

		void OnEnable() {
			ForcePositionAndRotation(transform.position, transform.rotation);
			_noiseOffset = new Vector3(Random.Range(0f, 1000f), Random.Range(0f, 1000f), Random.Range(0f, 1000f));
		}

		void Update() {
			var prevPosition = _currentPosition;
			Vector3 targetDisplacement = Vector3.zero;
			if ( _velocityMatchMode && _targetTransform ) {
				var targetPosition = _targetTransform.position;
				targetDisplacement = targetPosition - _lastTargetTransformPosition;
				_lastTargetTransformPosition = targetPosition;
			}
			
			var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

			// ---
			// HANDLE TRANSLATION
			// ---
			var pos = _targetPosition; // begin with last frame's target

			if ( Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ) {
				pos += -transform.right * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ) {
				pos += transform.right * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ) {
				pos += transform.forward * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ) {
				pos += -transform.forward * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.Q) ) {
				pos += transform.up * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.E) ) {
				pos += -transform.up * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp) ) {
				pos += Vector3.up * movementSpeed * Time.unscaledDeltaTime;
			}
			if ( Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown) ) {
				pos += -Vector3.up * movementSpeed * Time.unscaledDeltaTime;
			}

			float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
			if (scrollAxis != 0) {
				var zSens = fastMode ? fastZoomSensitivity : zoomSensitivity;
				pos += transform.forward * scrollAxis * zSens;
			}

			_targetPosition = pos;

			// ---
			// HANDLE ROTATION
			// ---
			if ( looking ) {
				_yaw += Input.GetAxis("Mouse X") * freeLookSensitivity;
				_pitch -= Input.GetAxis("Mouse Y") * freeLookSensitivity;
			}
			_targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);

			if ( Input.GetKeyDown(KeyCode.Mouse1) ) {
				StartLooking();
			}
			else if ( Input.GetKeyUp(KeyCode.Mouse1) ) {
				StopLooking();
			}

			// ---
			// APPLY TO TRANSFORM WITH OPTIONAL SMOOTHING
			// ---
			if ( SmoothMovement ) {
				float t = Mathf.Clamp01(SmoothMovementFactor);
				_currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, t);
			} else {
				_currentPosition = _targetPosition;
			}
			
			if ( _velocityMatchMode && _targetTransform != null ) {
				_currentPosition += targetDisplacement;
				_targetPosition += targetDisplacement;
			}

			transform.position = _currentPosition;

			if ( SmoothRotation ) {
				float t = Mathf.Clamp01(SmoothRotationFactor);
				_smoothPitch = Mathf.LerpAngle(_smoothPitch, _pitch, t);
				_smoothYaw   = Mathf.LerpAngle(_smoothYaw,   _yaw,   t);
				transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);
			} else {
				_smoothPitch = _pitch;
				_smoothYaw = _yaw;
				transform.rotation = _targetRotation;
			}

			if (UseCameraMovementNoise) {
				var instantaneousVelocity = Vector3.Distance(prevPosition, _currentPosition) / Time.unscaledDeltaTime;
				_targetVelocityMagnitude = Mathf.Lerp(_targetVelocityMagnitude, instantaneousVelocity, Time.unscaledDeltaTime * NoiseVelocitySmoothFactor);
				transform.position += GetPerlinNoiseOffset(Time.time);
				transform.rotation *= GetPerlinNoiseRotation(Time.time);
			}
		}

		void OnDisable() {
			StopLooking();
		}

		/// <summary>
		/// Enable free looking.
		/// </summary>
		public void StartLooking() {
			looking = true;
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		/// <summary>
		/// Disable free looking.
		/// </summary>
		public void StopLooking() {
			looking = false;
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
		}

		private Vector3 GetPerlinNoiseOffset(float time) {
			float scale = CameraMovementNoiseScale * (BaseCameraNoise + _targetVelocityMagnitude * CameraNoiseBySpeedScale);
			float speed = time * CameraNoiseSpeedMultiplier;
            
			return new Vector3(
				(Mathf.PerlinNoise(_noiseOffset.x + speed, 0f) - 0.5f) * scale,
				(Mathf.PerlinNoise(_noiseOffset.y + speed, 0f) - 0.5f) * scale,
				(Mathf.PerlinNoise(_noiseOffset.z + speed, 0f) - 0.5f) * scale
			);
		}

		private Quaternion GetPerlinNoiseRotation(float time) {
			float scale = CameraMovementNoiseScale * 15f * (BaseCameraNoise + _targetVelocityMagnitude * CameraNoiseBySpeedScale);
			float speed = time * CameraNoiseSpeedMultiplier;
            
			return Quaternion.Euler(
				(Mathf.PerlinNoise(_noiseOffset.x + speed + 100f, 0f) - 0.5f) * scale,
				(Mathf.PerlinNoise(_noiseOffset.y + speed + 100f, 0f) - 0.5f) * scale,
				(Mathf.PerlinNoise(_noiseOffset.z + speed + 100f, 0f) - 0.5f) * scale
			);
		}
	}
}
#endif