using UnityEngine;
using UnityEngine.InputSystem;

namespace SMGCore {
    public class OrbitCamera : MonoBehaviour {
        public Transform Target;
        public float Distance = 10f;
        public Vector2 DistanceLimits = new Vector2(2f, 50f);
        public float MaxOffsetDistance = 5f;

        public bool UseCameraMovementNoise = false;
        public float CameraMovementNoiseScale = 0.01f;
        public float CameraNoiseSpeedMultiplier = 1f;
        public float CameraNoiseBySpeedScale = 1f;

        public float BaseCameraNoise = 0.1f;
        public float NoiseVelocitySmoothFactor = 15f;

        public bool CheckObstructions = false;
        public LayerMask ObstructionLayerMask = 0;

        /// <summary>
        /// How fast the camera will adjust to the target position.
        /// </summary>
        public float FollowSmoothFactor = 0.5f;

		/// <summary>
		/// Sensitivity for look.
		/// </summary>
		public float LookSensitivity = 3f;

		/// <summary>
		/// Amount to zoom the camera when using the mouse wheel.
		/// </summary>
        public float ZoomSensitivity = 10f;

        /// <summary>
        /// Enable rotation smoothing
        /// </summary>
        public bool UseRotationSmoothing = true;

        /// <summary>
        /// How fast the camera rotation will be smoothed
        /// </summary>
        public float RotationSmoothTime = 0.12f;

        private float _currentX = 0f;
        private float _currentY = 0f;
        private bool _isRotating = false;
        private Vector3 _targetPosition;
        private Vector3 _currentVelocity;
        private Vector3 _lastTargetPosition;
        private float _targetVelocityMagnitude;
        private float _smoothedNoiseAmplitude;
        private float _noiseTime;
        private Vector3 _noiseOffset;
        private float _currentDistance;

        Vector3 _targetPointOffset;

        private bool _isAdjustingOffset = false;

        private float _smoothX;
        private float _smoothY;
        private float _velocityX;
        private float _velocityY;

        Transform _initialTarget;

        void Start() {
            if (Target != null) {
                transform.LookAt(Target);
                var angles = transform.eulerAngles;
                _currentX = angles.y;
                _currentY = angles.x;
                _smoothX = _currentX;
                _smoothY = _currentY;
                _targetPosition = Target.position;
                _lastTargetPosition = Target.position;
                _noiseOffset = new Vector3(Random.Range(0f, 1000f), Random.Range(0f, 1000f), Random.Range(0f, 1000f));
                _noiseTime = 0f;
                _smoothedNoiseAmplitude = CameraMovementNoiseScale * BaseCameraNoise;
                _currentDistance = Distance;
                _initialTarget = Target;
            }
        }

        public void SetTarget(Transform target) {
            if ( Target ) {
                OnDisable();
            }
            Target = target;
            OnEnable();
        }

        void OnEnable() {
            if ( !Target ) {
                return;
            }
            //move to target position instantly
            var rotation = Quaternion.Euler(_currentY, _currentX, 0);
            var position = rotation * new Vector3(0.0f, 0.0f, -Distance) + Target.position;
            transform.position = position;
            _targetPosition = Target.position;
            var rb = Target.GetComponent<Rigidbody>();
            if ( rb ) {
                //enable interpolation to get rid of jitter. Do not forget to disable it when disabling the camera because enabled 
                //interpolation disables the rigidbody ability to instantly change position through code.
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        void OnDisable() {
            if ( !Target ) {
                return;
            }
            var rb = Target.GetComponent<Rigidbody>();
            if ( rb ) {
                rb.interpolation = RigidbodyInterpolation.None;
            }
        }

        private Vector3 GetPerlinNoiseOffset(float time, float scale) {
            float speed = time * CameraNoiseSpeedMultiplier;
            return new Vector3(
                (Mathf.PerlinNoise(_noiseOffset.x + speed, 2.3f) - 0.5f) * scale,
                (Mathf.PerlinNoise(_noiseOffset.y + speed, 5.7f) - 0.5f) * scale,
                (Mathf.PerlinNoise(_noiseOffset.z + speed, 8.1f) - 0.5f) * scale
            );
        }

        private Quaternion GetPerlinNoiseRotation(float time, float scale) {
            float rotScale = scale * 15f;
            float speed = time * CameraNoiseSpeedMultiplier;
            return Quaternion.Euler(
                (Mathf.PerlinNoise(_noiseOffset.x + speed + 25.3f, 5.35f) - 0.5f) * rotScale,
                (Mathf.PerlinNoise(_noiseOffset.y + speed + 12.7f, -0.7f) - 0.5f) * rotScale,
                (Mathf.PerlinNoise(_noiseOffset.z + speed + 3.1f,   1.1f) - 0.5f) * rotScale
            );
        }

        private float CheckForObstructions(Vector3 targetPos, Vector3 desiredCameraPos) {
            if (!CheckObstructions) return Distance;

            RaycastHit hit;
            Vector3 direction = (desiredCameraPos - targetPos).normalized;
            float distance = Vector3.Distance(targetPos, desiredCameraPos);

            if (Physics.Raycast(targetPos, direction, out hit, distance, ObstructionLayerMask)) {
                // Add small offset to prevent camera from being exactly at the collision point
                return Mathf.Max(hit.distance - 0.5f, DistanceLimits.x);
            }

            return distance;
        }

        private void UpdateTargetOffset() {
            if (Keyboard.current[Key.LeftShift].isPressed || Keyboard.current[Key.RightShift].isPressed) {
                if (Mouse.current.rightButton.wasPressedThisFrame) {
                    _isAdjustingOffset = true;
                    _isRotating = false;
                }
            }
            
            if (Mouse.current.rightButton.wasReleasedThisFrame) {
                _isAdjustingOffset = false;
            }

            if (_isAdjustingOffset) {
                // Get the right and up vectors in world space
                Vector3 right = transform.right;
                Vector3 up = transform.up;

                // Calculate offset based on mouse movement
                float horizontalInput = Mouse.current.delta.x.ReadValue() * LookSensitivity * Time.unscaledDeltaTime * 2f;
                float verticalInput = Mouse.current.delta.y.ReadValue() * LookSensitivity * Time.unscaledDeltaTime * 2f;

                // Apply the offset
                _targetPointOffset += (right * horizontalInput + up * verticalInput) * Time.unscaledDeltaTime * 5f;

                // Clamp the total offset magnitude
                if (_targetPointOffset.magnitude > MaxOffsetDistance) {
                    _targetPointOffset = _targetPointOffset.normalized * MaxOffsetDistance;
                }
            }
        }

        void LateUpdate() {
            if (Target == null) {
                return;
            }

            // Calculate target velocity
            var instantaneousVelocity = Vector3.Distance(_lastTargetPosition, Target.position) / Time.unscaledDeltaTime;
            _targetVelocityMagnitude = Mathf.Lerp(_targetVelocityMagnitude, instantaneousVelocity, Time.unscaledDeltaTime * NoiseVelocitySmoothFactor);
            _lastTargetPosition = Target.position;

            // Handle rotation with right mouse button
            if (!_isAdjustingOffset) {
                if (Mouse.current.rightButton.wasPressedThisFrame) {
                    _isRotating = true;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                } else if (Mouse.current.rightButton.wasReleasedThisFrame) {
                    _isRotating = false;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                if (_isRotating) {
                    _currentX += Mouse.current.delta.x.ReadValue() * LookSensitivity * Time.unscaledDeltaTime * 2f;
                    _currentY -= Mouse.current.delta.y.ReadValue() * LookSensitivity * Time.unscaledDeltaTime * 2f;
                    _currentY = Mathf.Clamp(_currentY, -85f, 85f);
                }
            }

            // Apply rotation smoothing
            if (UseRotationSmoothing) {
                _smoothX = Mathf.SmoothDamp(_smoothX, _currentX, ref _velocityX, RotationSmoothTime);
                _smoothY = Mathf.SmoothDamp(_smoothY, _currentY, ref _velocityY, RotationSmoothTime);
            } else {
                _smoothX = _currentX;
                _smoothY = _currentY;
            }

            // Update target point offset
            UpdateTargetOffset();

            // Handle zoom with mouse wheel
            float scroll = Mouse.current.scroll.y.ReadValue();
            Distance -= scroll * ZoomSensitivity;
            Distance = Mathf.Clamp(Distance, DistanceLimits.x, DistanceLimits.y);

            // Calculate base camera position and rotation
            Vector3 targetPos = Target.position + _targetPointOffset;
            _targetPosition = Vector3.SmoothDamp(_targetPosition, targetPos, ref _currentVelocity, FollowSmoothFactor);

            var rotation = Quaternion.Euler(_smoothY, _smoothX, 0);
            var desiredPosition = rotation * new Vector3(0.0f, 0.0f, -Distance) + _targetPosition;

            // Check for obstructions and adjust distance
            float targetDistance = CheckForObstructions(_targetPosition, desiredPosition);
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.unscaledDeltaTime * 5f);
            
            var position = rotation * new Vector3(0.0f, 0.0f, -_currentDistance) + _targetPosition;

            // Apply noise if enabled
            if (UseCameraMovementNoise) {
                _noiseTime += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float targetAmplitude = CameraMovementNoiseScale * (BaseCameraNoise + _targetVelocityMagnitude * CameraNoiseBySpeedScale);
                _smoothedNoiseAmplitude = Mathf.Lerp(_smoothedNoiseAmplitude, targetAmplitude, Time.unscaledDeltaTime * NoiseVelocitySmoothFactor);
                position += GetPerlinNoiseOffset(_noiseTime, _smoothedNoiseAmplitude);
                rotation *= GetPerlinNoiseRotation(_noiseTime, _smoothedNoiseAmplitude);
            }

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
