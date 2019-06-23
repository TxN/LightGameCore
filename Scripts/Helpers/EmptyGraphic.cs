using UnityEngine.UI;

namespace CookingCapital.Utils {
	public class EmptyGraphic : Graphic {
		protected override void OnPopulateMesh(VertexHelper vh) {
			vh.Clear();
		}
	}
}

