using UnityEngine.UI;

namespace SMGCore {
	public sealed class EmptyGraphic : Graphic {
		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}
	}
}

