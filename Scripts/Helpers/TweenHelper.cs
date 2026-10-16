#if DOTWEEN
using UnityEngine;

using DG.Tweening;

namespace SMGCore {
	public static class TweenHelper {
		static bool _isQuitting;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetQuittingState() {
			_isQuitting = false;
			Application.quitting -= OnApplicationQuitting;
			Application.quitting += OnApplicationQuitting;
		}

		static void OnApplicationQuitting() {
			_isQuitting = true;
		}

		public static Sequence ResetSequence(Sequence seq, bool complete = true, bool withCallbacks = false) {
			// On quit DOTween purges its counters but leaves tweens active; Complete() would then despawn them again.
			if ( seq != null && !_isQuitting ) {
				seq.SetAutoKill(false);
				if ( complete ) {
					seq.Complete(withCallbacks);
				}
				seq.Kill();
			}
			return null;
		}

		public static Sequence ReplaceSequence(Sequence seq, bool complete = true, bool withCallbacks = false) {
			ResetSequence(seq, complete, withCallbacks);
			return DOTween.Sequence();
		}

		public static void PlayClickAnimation(Transform t) {
			var seq = DOTween.Sequence();
			seq.Append(t.DOScale(1.1f, 0.2f));
			seq.Append(t.DOScale(0.9f, 0.2f));
			seq.Append(t.DOScale(1f, 0.2f));
		}
	}
}
#endif