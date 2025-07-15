using UnityEngine;

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
        private Vector3 _noiseOffset;
        private float _currentDistance;

        Vector3 _targetPointOffset;

        private bool _isAdjustingOffset = false;

        private float _smoothX;
        private float _smoothY;
        private float _velocityX;
        private float _velocityY;

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
                _currentDistance = Distance;
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
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                if (Input.GetMouseButtonDown(1)) {
                    _isAdjustingOffset = true;
                    _isRotating = false;
                }
            }
            
            if (Input.GetMouseButtonUp(1)) {
                _isAdjustingOffset = false;
            }

            if (_isAdjustingOffset) {
                // Get the right and up vectors in world space
                Vector3 right = transform.right;
                Vector3 up = transform.up;

                // Calculate offset based on mouse movement
                float horizontalInput = Input.GetAxis("Mouse X") * LookSensitivity;
                float verticalInput = Input.GetAxis("Mouse Y") * LookSensitivity;

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
            _targetVelocityMagnitude = Vector3.Distance(_lastTargetPosition, Target.position) / Time.unscaledDeltaTime;
            _lastTargetPosition = Target.position;

            // Handle rotation with right mouse button
            if (!_isAdjustingOffset) {
                if (Input.GetMouseButtonDown(1)) {
                    _isRotating = true;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else if (Input.GetMouseButtonUp(1)) {
                    _isRotating = false;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                if (_isRotating) {
                    _currentX += Input.GetAxis("Mouse X") * LookSensitivity;
                    _currentY -= Input.GetAxis("Mouse Y") * LookSensitivity;
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
            float scroll = Input.GetAxis("Mouse ScrollWheel");
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
                position += GetPerlinNoiseOffset(Time.time);
                rotation *= GetPerlinNoiseRotation(Time.time);
            }

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
