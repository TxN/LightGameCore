namespace SMGCore.FSM {
	public class FiniteStateMachine<T> where T : System.Enum {
		FSMDescription<T> _description = null;
		FSMState<T> _currentState = null;

		public T CurrentStateType {
			get {
				if ( _currentState == null ) {
					return default(T);
				}
				return _currentState.StateType;
			}
		}

		public FiniteStateMachine(FSMDescription<T> description) {
			_description = description;
		}

		public FSMState<T> Initialize() {
			_currentState = _description.CreateInitialState();
			return _currentState;
		}

		public void ActivateState() {
			if ( _currentState != null && _currentState.Status == StateStatus.None ) {
				_currentState.ActivateState();
			}
		}

		public bool TryChangeState(T newStateType) {
			if ( _currentState == null ) {
				return false;
			}

			var s = _description.CreateStateFromType(newStateType);
			return TryChangeState(s);
		}

		public bool TryChangeState(FSMState<T> newState) {
			if ( _currentState == null ) {
				return false;
			}
			return _currentState.SwitchToState(newState);
		}

		public bool TrySetNextState(T nextStateType) {
			if ( _currentState == null ) {
				return false;
			}
			var s = _description.CreateStateFromType(nextStateType);
			return TrySetNextState(s);
		}

		public bool TrySetNextState(FSMState<T> nextState) {
			if ( _currentState == null ) {
				return false;
			}
			return _currentState.SetNextState(nextState);
		}


		public void OnNeedSwitchState(FSMState<T> nextState) {
			if ( nextState == null ) {
				return;
			}

			_currentState = nextState;
			ActivateState();
		}


		public void OnNeedSwitchState(T newState) {
			var s = _description.CreateStateFromType(newState);
			if ( s != null ) {
				OnNeedSwitchState(s);
			}
		}


		public void Update(float timeDelta) {
			if ( _currentState != null && _currentState.Status != StateStatus.None ) {
				_currentState.UpdateState(timeDelta);
			}
		}
	}
}

