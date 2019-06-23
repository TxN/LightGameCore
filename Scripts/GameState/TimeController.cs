using System.Collections.Generic;
using UnityEngine;
using EventSys;

public sealed class TimeController {
	HashSet<Object> _pauseHolders = new HashSet<Object>();

	public TimeController() {
		TimeScale = 1f;
	}

	public float TimeScale { get; set; } = 1f;

    public float CurrentTime { get; private set; } = 0f;

	public bool IsPause {
		get {
			return _pauseHolders.Count > 0;
		}
	}

	public void AddPause(Object holder) {
		var lastPause = IsPause;
		_pauseHolders.Add(holder);

		if ( lastPause != IsPause ) {
			EventManager.Fire(new Event_PauseChange(IsPause));
		}
	}

	public void RemovePause(Object holder) {
		var lastPause = IsPause;
		_pauseHolders.Remove(holder);

		if ( lastPause != IsPause ) {
			EventManager.Fire(new Event_PauseChange(IsPause));
		}
	}

	public bool AddOrRemovePause (Object holder) { //for toggling, returns pause state
		if ( _pauseHolders.Contains(holder) ) {
			RemovePause(holder);
		} else {
			AddPause(holder);
		}
		return IsPause;
	}

	public void Update(float deltaTime) {
		if ( !IsPause ) {
			CurrentTime += deltaTime * TimeScale; 
		}
	}

}

public struct Event_PauseChange {
	public bool Pause;

	public Event_PauseChange(bool flag) {
		Pause = flag;
	}
}
