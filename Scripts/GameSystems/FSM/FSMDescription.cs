namespace SMGCore.FSM {
	public abstract class FSMDescription<T> where T : System.Enum {
		public T InitialState;

		public FSMState<T> CreateInitialState() {
			return CreateStateFromType(InitialState);
		}

		public abstract FSMState<T> CreateStateFromType(T type);
	}
}
