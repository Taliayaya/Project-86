using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;
namespace Armament.Shared
{
	public class TempList<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable
	{
		private List<T> _list;

		// ReSharper disable once ConvertConstructorToMemberInitializers
		public TempList()
		{
			_list = ListPool<T>.Get();
			_list.Clear();
		}

		public void Add(T item) => _list.Add(item);

		int IList.Add(object value)
		{
			if (value is not T t) return -1;

			_list.Add(t);
			return _list.Count - 1;
		}

		public void Clear() => _list.Clear();

		public bool Contains(object value)
		{
			if (value is not T t) return false;
			return _list.Contains(t);
		}

		public int IndexOf(object value)
		{
			if (value is not T t) return -1;
			return _list.IndexOf(t);
		}

		public void Insert(int index, object value)
		{
			if (value is not T t) return;
			_list.Insert(index, t);
		}

		public void Remove(object value)
		{
			if (value is not T t) return;
			_list.Remove(t);
		}

		public bool Contains(T item) => _list.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		public bool Remove(T item) => _list.Remove(item);

		public void CopyTo(Array array, int index)
		{
			if (array is T[] t) _list.CopyTo(t, index);
		}

		public int Count => _list.Count;
		public bool IsSynchronized => false;
		public object SyncRoot => this;
		public bool IsReadOnly => false;

		object IList.this[int index]
		{
			get => this[index];
			set
			{
				if (value is T t) this[index] = t;
			}
		}

		public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

		public int IndexOf(T item) => _list.IndexOf(item);

		public void Insert(int index, T item) => _list.Insert(index, item);

		public void RemoveAt(int index) => _list.RemoveAt(index);
		public bool IsFixedSize => false;

		public T this[int index]
		{
			get => _list[index];
			set => _list[index] = value;
		}

		public void Dispose()
		{
			_list.Clear();
			ListPool<T>.Release(_list);
			_list = null;
		}

		public void AddRange(IEnumerable<T> where)
		{
			_list.AddRange(where);
		}

		public void AddToList(List<T> cells)
		{
			cells.AddRange(_list);
		}

		public void ReferenceList(Action<List<T>> toReference)
		{
			toReference?.Invoke(_list);
		}

		public T[] ToArray() => _list.ToArray();
	}

	public static class TempListHelper
	{
		public static TempList<T> Create<T>(this IEnumerable<T> from)
		{
			TempList<T> list = new TempList<T>();
			if (from.Any())
				list.AddRange(from);
			return list;
		}


		public static int IndexOf<T>(this T target, TempList<T> array)
		{
			return array.IndexOf(target);
		}
	}
}