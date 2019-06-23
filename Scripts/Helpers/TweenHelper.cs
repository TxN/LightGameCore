using UnityEngine;
using DG.Tweening;

public static class TweenHelper {
	public static Sequence ResetSequence(Sequence seq, bool complete = true) {
		if (seq != null) {
			seq.SetAutoKill(false);
			if (complete) {
				seq.Complete();
			}
			seq.Kill();
			seq = null;
		}
		return seq;
	}

	public static Sequence ReplaceSequence(Sequence seq, bool complete = true) {
		ResetSequence(seq, complete);
		return DOTween.Sequence();
	}

	public static void SendItemTo(Sequence seq, Transform moveItem, Transform endTrans, Transform midTrans, float speedScale = 1.0f) {
		var t = seq.Duration();
		if (midTrans) {
			var path = new Vector3[] { moveItem.position, midTrans.position, endTrans.position };
			seq.Append(moveItem.DOPath(path, 0.5f * speedScale, PathType.CatmullRom));
		} else {
			seq.Append(moveItem.DOMove(endTrans.position, 0.5f * speedScale));
		}
		seq.Insert(t, moveItem.DOScale(endTrans.localScale, 0.5f * speedScale).SetEase(Ease.OutElastic));
	}

	public static void SendItemToInsert(Sequence seq, Transform moveItem, Transform endTrans, Transform midTrans, float delay) {
		if (midTrans) {
			var path = new Vector3[] { moveItem.position, midTrans.position, endTrans.position };
			seq.Insert(delay, moveItem.DOPath(path, 0.5f, PathType.CatmullRom));
		} else {
			seq.Insert(delay, moveItem.DOMove(endTrans.position, 0.5f));
		}
	}

	public static void DoRewardEffect(GameObject effect, int reward, float height = 200f) {
		var ef_trans = effect.transform;
		var ef_group = effect.GetComponent<CanvasGroup>();

		ef_trans.localScale = Vector3.one;
		if (ef_group) {
			ef_group.alpha = 1.0f;
		}

		effect.SetActive(true);
		Vector3 startPos = ef_trans.localPosition;
		ef_trans.DOLocalMove(startPos + new Vector3(0, height, 0), 1.0f);

		var dot_seq = DOTween.Sequence();
		dot_seq.Append(ef_trans.DOShakeScale(1.0f, 0.2f));
		if (ef_group) {
			dot_seq.Append(ef_group.DOFade(0.0f, 1.0f));
		}
		dot_seq.AppendCallback(() => {
			effect.SetActive(false);
		});
	}

	public static void PlayClickAnimation(Transform t) {
		var seq = DOTween.Sequence();
		seq.Append(t.DOScale(1.1f, 0.2f));
		seq.Append(t.DOScale(0.9f, 0.2f));
		seq.Append(t.DOScale(1f, 0.2f));
	}
}