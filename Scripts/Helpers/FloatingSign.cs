#if DOTWEEN
using UnityEngine;

using DG.Tweening;

namespace SMGCore {
	[RequireComponent(typeof(RectTransform))]
	public class FloatingSign : MonoBehaviour {
		public Vector2 Movement      = new Vector2(10, 0);
		public float   Time          = 1f;
		public float   MaxStartDelay = 0.1f;

		Vector2 _initPos = Vector2.zero;
		Sequence _seq = null;

		void Start() {
			var rt = GetComponent<RectTransform>();
			_initPos = rt.anchoredPosition;
			_seq = TweenHelper.ReplaceSequence(_seq);
			_seq.AppendInterval(Random.Range(0, MaxStartDelay));
			_seq.Append(rt.DOAnchorPos(rt.anchoredPosition + Movement, Time));
			_seq.Append(rt.DOAnchorPos(_initPos, Time));
			_seq.SetLoops(-1);
		}

		void OnDestroy() {
			_seq = TweenHelper.ResetSequence(_seq);	
		}
	}
}
#endif
