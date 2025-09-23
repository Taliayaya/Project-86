using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Armament.Shared
{
	public class TempArray<T> : IReadOnlyList<T>, ICloneable, IDisposable, IList
	{
		private T[] _array;
		private int _size;

		public TempArray()
		{
			_array = null;
			_size = 0;
		}

		public TempArray(int size)
		{
			_size = size;
			if (_size > 0)
			{
				_array = ArrayPool<T>.Shared.Rent(size);
				for (var i = 0; i < _array.Length; i++)
				{
					_array[i] = default;
				}
			}
			else
			{
				_array = Array.Empty<T>();
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _array.Take(_size).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Contains(T item)
		{
			return _array.Contains(item);
		}

		public void CopyTo(Array array, int index)
		{
			_array.CopyTo(array, index);
		}

		public int Count => _size;
		public bool IsSynchronized => false;
		public object SyncRoot => this;
		public int Length => _size;

		int IList.Add(object value)
		{
			return -1;
		}

		public void Clear()
		{
			for (var i = 0; i < _array.Length; i++)
			{
				_array[i] = default;
			}
		}

		public bool Contains(object value)
		{
			if (value is T value1)
				return _array.Contains(value1);
			else
				return false;
		}

		public int IndexOf(object value)
		{
			if (value is T value1)
				return value1.IndexOf(_array);
			else
				return -1;
		}

		void IList.Insert(int index, object value)
		{
		}

		void IList.Remove(object value)
		{
		}

		void IList.RemoveAt(int index)
		{
		}

		public bool IsFixedSize => true;
		public bool IsReadOnly => false;
		public T[] Raw => _array;

		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (T)value;
		}

		public T this[int index]
		{
			get => _array[index];
			set => _array[index] = value;
		}

		public object Clone()
		{
			TempArray<T> array = new TempArray<T>(_size);
			_array.CopyTo(array._array, 0);
			return array;
		}

		public void Dispose()
		{
			if (_size > 0)
				ArrayPool<T>.Shared.Return(_array);
			_array = null;
		}

		public void NestedDispose()
		{
			if (ImplementsIDisposable())
			{
				for (var i = 0; i < Length; i++)
				{
					if (_array[i] is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
			}

			Dispose();
		}

		public bool ImplementsIDisposable()
		{
			return typeof(IDisposable).IsAssignableFrom(typeof(T));
		}

		public void Swap(int i, int j)
		{
			(_array[i], _array[j]) = (_array[j], _array[i]);
		}
	}
}