using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndGame : MonoBehaviour {
	[Header("Game Result Effects Holder")]
	public GameObject WinGameHolder  = null;
	public GameObject LoseGameHolder = null;
	[Header("Titles")]
	public GameObject EndTitles = null;
	[Header("Utilities")]
	public FadeScreen Fader = null;

	bool _closing = false;

	void Start() {
		var pData = ScenePersistence.Instance.Data;
		WinGameHolder.SetActive ( pData.IsWin);
		LoseGameHolder.SetActive(!pData.IsWin);

		Fader.FadeToWhite(1f);
	}

	void Update() {
		if ( Input.GetKeyDown(KeyCode.Escape) ) {
			GoToStart();
		}
	}

	public void GoToStart() {
		if ( _closing ) {
			return;
		}
		_closing = true;
		Fader.FadeToBlack(1f);
		Fader.OnFadeToBlackFinished.AddListener(LoadStartScene);
	}

	public void FastRestart() {
		if ( _closing ) {
			return;
		}
		_closing = true;
		Fader.FadeToBlack(1f);
		ScenePersistence.Instance.Data.FastRestart = true;
		Fader.OnFadeToBlackFinished.AddListener(LoadLevel);
	}

	void LoadStartScene() {
		SceneManager.LoadScene("MainMenu");
	}

	void LoadLevel() {
		SceneManager.LoadScene("Gameplay");
	}
}
