using UnityEngine;
using UnityEngine.Events;

using DG.Tweening;

public sealed class FadeScreen : MonoBehaviour {
	public bool        AutoRemoveCallbacks   = false;
	public CanvasGroup CanvasGroup           = null;
	public UnityEvent  OnFadeToBlackFinished = null;
	public UnityEvent  OnFadeToWhiteFinished = null;
	public UnityEvent  OnAnyFadeFinished     = null;

	Sequence _seq = null;

	void Start() {
		CheckCanvasGroup();
	}

	void OnDestroy() {
		_seq = TweenHelper.ResetSequence(_seq, false);
    }

    public void FadeToBlack(float time, bool disableRaycasts = true) {
		CheckCanvasGroup();
		gameObject.SetActive(true);
		CanvasGroup.alpha = 0f;
		CanvasGroup.blocksRaycasts = disableRaycasts;
		_seq = TweenHelper.ReplaceSequence(_seq, false);
		_seq.Append(CanvasGroup.DOFade(1, time));
		_seq.AppendCallback(AfterFadeToBlack);
	}

    public void FadeToWhite(float time, bool disableRaycasts = false) {
		CheckCanvasGroup();
		gameObject.SetActive(true);
		CanvasGroup.alpha = 1f;
		CanvasGroup.blocksRaycasts = disableRaycasts;
		_seq = TweenHelper.ReplaceSequence(_seq, false);
		_seq.Append(CanvasGroup.DOFade(0, time));
		_seq.AppendCallback(AfterFadeToWhite);
    }

	void AfterFadeToWhite() {
		gameObject.SetActive(false);
		CanvasGroup.interactable = false;
		TriggerCallbacks(OnFadeToWhiteFinished);
		TriggerCallbacks(OnAnyFadeFinished);
	}

	void AfterFadeToBlack() {
		TriggerCallbacks(OnFadeToBlackFinished);
		TriggerCallbacks(OnAnyFadeFinished);
	}

	void TriggerCallbacks(UnityEvent eventHolder) {
		if ( eventHolder != null ) {
			eventHolder.Invoke();
		}
		if ( AutoRemoveCallbacks ) {
			eventHolder.RemoveAllListeners();
		}
	}

	void CheckCanvasGroup() {
		if ( !CanvasGroup ) {
			CanvasGroup = GetComponent<CanvasGroup>();
		}
	}
}
