using UnityEngine;
using System.Collections;

namespace SMGCore {
	public sealed class CameraShake : MonoBehaviour {
		public float MaxAmount = 5f;
		public float MaxDuration = 2f;
		public bool IsSuppressed = false; //подавляем тряску, но продолжаем "проигрывать" эффект
		public bool debugMode = false; //Calls ShakeCamera() on start

		public float shakeAmount;  //The amount to shake this frame.
		public float shakeDuration;//The duration this frame.

		float shakePercentage;//A percentage (0-1) representing the amount of shake to be applied when setting rotation.
		float startAmount;    //The initial shake amount (to determine percentage), set when ShakeCamera is called.
		float startDuration;  //The initial shake duration, set when ShakeCamera is called.

		bool isRunning = false; //Is the coroutine running right now?
		bool decay = false;

		public bool  smooth;
		public float smoothAmount = 5f;

		void Start() {
			if ( debugMode ) {
				ShakeCamera();
			}
		}

		[ContextMenu("Shake test")]
		void ShakeCamera() {
			startAmount = shakeAmount;
			startDuration = shakeDuration;

			if ( !isRunning ) {
				StartCoroutine(Shake());
			}
		}

		public void ShakeCamera(float amount, float duration) {
			decay = false;
			shakeAmount = Mathf.Clamp(shakeAmount + amount,0, MaxAmount);
			startAmount = shakeAmount;
			shakeDuration = Mathf.Clamp(shakeDuration + duration, 0 , MaxDuration);
			startDuration = shakeDuration;

			if ( !isRunning ) {
				StartCoroutine(Shake());
			}
		}

		public void Stop() {
			isRunning = false;
			StopAllCoroutines();
		}

		public void Decay() {
			transform.localRotation = Quaternion.identity;//Set the local rotation to 0 when done, just to get rid of any fudging stuff.
			decay = true;
		}

		IEnumerator Shake() {
			isRunning = true;

			while ( shakeDuration > 0.01f ) {
				if ( Time.timeScale < 0.01f ) {
					yield return null;
				}
				if ( decay ) {
					shakeDuration = Mathf.Clamp(shakeDuration - Time.deltaTime, 0, shakeDuration);
				}
				Vector3 rotationAmount = Random.insideUnitSphere * shakeAmount;//A Vector3 to add to the Local Rotation
				rotationAmount.z = 0;//Don't change the Z; it looks funny.

				shakePercentage = shakeDuration / startDuration;//Used to set the amount of shake (% * startAmount).

				shakeAmount = startAmount * shakePercentage;//Set the amount of shake (% * startAmount).
															//   shakeDuration = Mathf.Lerp(shakeDuration, 0, Time.deltaTime);//Lerp the time, so it is less and tapers off towards the end.

				if ( !IsSuppressed ) {
					if ( smooth )
						transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(rotationAmount), Time.deltaTime * smoothAmount);
					else
						transform.localRotation = Quaternion.Euler(rotationAmount);//Set the local rotation the be the rotation amount.
				}
				yield return null;
			}
			transform.localRotation = Quaternion.identity;//Set the local rotation to 0 when done, just to get rid of any fudging stuff.
			isRunning = false;
		}
	}
}
