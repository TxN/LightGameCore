using System.Collections.Generic;

namespace Utils {
	public abstract class HandlerBase {
		public static bool LogsEnabled => false;
		public static bool AllFireLogs => LogsEnabled;

		public List<object> Watchers { get; } = new List<object>(100);

		public virtual void CleanUp() {}

		// ReSharper disable once UnusedMethodReturnValue.Global
		public virtual bool FixWatchers() => false;
	}
}