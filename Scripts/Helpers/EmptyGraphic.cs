using UnityEngine.UI;

namespace CookingCapital.Utils {
	public sealed class EmptyGraphic : Graphic {
		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}
	}
}

