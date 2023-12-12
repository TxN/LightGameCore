#if DOTWEEN
using UnityEngine;

using DG.Tweening;

namespace SMGCore {
	public static class TweenHelper {
		public static Sequence ResetSequence(Sequence seq, bool complete = true, bool withCallbacks = false) {
			if ( seq != null ) {
				seq.SetAutoKill(false);
				if ( complete ) {
					seq.Complete(withCallbacks);
				}
				seq.Kill();
				seq = null;
			}
			return seq;
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