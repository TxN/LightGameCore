namespace Utils {
	public sealed class RectArrayWrapper<T> {
		public T[] Data {
			get {
				return _data;
			}
			set {
				_data = value;
			}
		}

		public int SizeX {
			get { return _x; }
			set { _x = value; }
		}

		public int SizeY {
			get { return _y; }
			set { _y = value; }
		}

		public int Length => _data.Length;

		T[] _data = null;
		int _x = 0;
		int _y = 0;

		public RectArrayWrapper() {
		}

		public RectArrayWrapper(int sizeX, int sizeY) {
			_x = sizeX;
			_y = sizeY;
			Data = new T[sizeX * sizeY];
		}

		public T this[int x, int y] {
			get {
				return Data[_x * y + x];
			}
			set {
				//встроенная защита от записи за границу массива
				if ( x >= _x || y >= _y || x < 0 || y < 0 ) {
					return;
				}

				Data[_x * y + x] = value;
			}
		}
	}
}
