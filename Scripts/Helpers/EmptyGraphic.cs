#if UNITY_2017_1_OR_NEWER
using UnityEngine.UI;

namespace SMGCore {
	public sealed class EmptyGraphic : Graphic {
		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}
	}
}

#endif