namespace SMGCore.FSM {
	public abstract class FSMDescription<T> where T : System.Enum {
		public T InitialState;

		public FSMState<T> CreateInitialState(FiniteStateMachine<T> owner) {
			return CreateStateFromType(InitialState, owner);
		}

		public abstract FSMState<T> CreateStateFromType(T type, FiniteStateMachine<T> owner);
	}
}
