using System.Collections.Generic;

namespace SMGCore.FSM {
	public abstract class FSMState<T> where T : System.Enum {
		protected T Type;
		protected float                 TimeToExit = -1f; //время нахождения в этом состоянии. -1 - бесконечно
		protected float                 InTransitionTime = 0f;// время входа в состояние. Если оно больше 0, то в основной цикл состояния автомат перейдет только через это время
		protected float                 OutTransitionTime = 0f;// время выхода из состояния.Если оно больше 0, то при покидании состояния автомат перейдет в новое состояние только через это время
		protected List<T>               AvailableTransitions;//список доступных выходных состояний. Из этого состояния автомат может перейти только в перечисленные состояния
		protected T                     ExitState;//выходное состояние по умолчанию. При TimeToExit > 0 автомат перейдет в это состояние спустя указанное время.
		protected T                     EmptyState;//пустое состояние, в которое нельзя перейти
		protected float                 EnterTimestamp;//время создания состояния.
		protected StateStatus           CurrentStatus = StateStatus.None;
		protected FiniteStateMachine<T> Owner;

		protected float       _exitStatusEnterTime = 0f;
		protected float       _currentTime = 0f;
		protected FSMState<T> _nextState = null;
		protected bool        _skipMainStatus = false;

		public T StateType {
			get { return Type; }
		}

		public StateStatus Status {
			get { return CurrentStatus; }
		}

		public FSMState(FiniteStateMachine<T> owner, float timestamp) {
			Owner = owner;
			EnterTimestamp = timestamp;
		}

		public void UpdateState(float timeDelta) {
			_currentTime += timeDelta;
			ProcessState();
		}

		protected virtual void ProcessState() {
			switch ( CurrentStatus ) {
				case StateStatus.None:
					break;
				case StateStatus.Enter:
					ProcessEnterStatus();
					if ( InTransitionTime <= 0 || (_currentTime > InTransitionTime) ) {
						PostprocessEnterStatus();
						ChangeStatus(StateStatus.Active);
					}
					break;
				case StateStatus.Active:
					if ( TimeToExit >= 0f && !ExitState.Equals(EmptyState) ) {
						if ( _currentTime > TimeToExit + InTransitionTime ) {
							PreprocessExitStatus();
							ChangeStatus(StateStatus.Exit);
							break;
						}
					}
					ProcessActiveStatus();
					break;
				case StateStatus.Exit:
					ProcessExitStatus();
					if ( OutTransitionTime <= 0 || ( _currentTime > _exitStatusEnterTime ) ) {
						PostprocessExitStatus();
						if ( _nextState != null ) {
							Owner.OnNeedSwitchState(_nextState);							
						} else if ( !ExitState.Equals(EmptyState) ) {
							Owner.OnNeedSwitchState(ExitState);
						} else {
							ChangeStatus(StateStatus.None);
						}
					}
					break;
				default:
					break;
			}
		}

		void ChangeStatus(StateStatus newStatus) {
			CurrentStatus = newStatus;
			if ( newStatus == StateStatus.Exit ) {
				_exitStatusEnterTime = _currentTime;
			}
			ProcessState();
		}

		public bool CanChangeState(T newState) {
			foreach ( var state in AvailableTransitions ) {
				if ( state.Equals(newState) ) {
					return true;
				}
			}
			return false;
		}

		public bool SwitchToState(FSMState<T> newState) {
			if ( !CanChangeState(newState.Type) ) {
				return false;
			}

			_skipMainStatus = true;
			_nextState = newState;

			if ( Status == StateStatus.None ) {
				Owner.OnNeedSwitchState(_nextState);
			}

			return true;
		}

		public bool IsLoop() {
			return TimeToExit < 0;
		}

		public bool SetNextState(FSMState<T> newState) {
			if ( !CanChangeState(newState.Type) ) {
				return false;
			}

			_nextState = newState;
			return true;
		}

		public abstract void ActivateState();

		protected abstract void ProcessEnterStatus();

		protected abstract void ProcessActiveStatus();

		protected abstract void ProcessExitStatus();

		protected virtual void PostprocessExitStatus() { }
		protected virtual void PreprocessExitStatus() { }
		protected virtual void PostprocessEnterStatus() { }

	}
}
