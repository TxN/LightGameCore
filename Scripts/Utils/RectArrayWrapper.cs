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
				return _data[_x * y + x];
			}
			set {
				//встроенная защита от записи за границу массива
				if ( x >= _x || x < 0 || y >= _y  || y < 0 ) {
					return;
				}

				_data[_x * y + x] = value;
			}
		}

		/// <summary>
		/// Без проверки на выход за границы массива
		/// Фактически, экономия на спичках, так как не выполняющаяся проверка почти ничего не ест всё равно
		/// </summary>
		public void SetUnsafe(int x, int y, T value) {
			_data[_x * y + x] = value;
		}

		public void SetAll(byte value) {
			//UnsafeMethods.UnsafeMethods.InitBlock(&_data[0], value, (uint)_data.Length);
		}
	}
}
