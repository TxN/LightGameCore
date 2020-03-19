using UnityEngine;

using System.Collections.Generic;


namespace SMGCore {
	public class InstancesHolder<T> : MonoBehaviour where T : InstancesHolder<T>  {
		public static HashSet<T> Instances = new HashSet<T>();
		
		protected virtual void OnEnable() {
			Instances.Add(this as T);
		}

		protected virtual void OnDisable() {
			Instances.Remove(this as T);
		}

		public static void FillListWithInstances(List<T> list) {
			list.Clear();
			foreach ( var item in Instances ) {
				list.Add(item);
			}
		}

		public static List<T> CreateInstancesList() {
			var list = new List<T>(Instances.Count);
			FillListWithInstances(list);
			return list;
		}

		public static int GetCount() {
			return Instances.Count;
		}
	}
}