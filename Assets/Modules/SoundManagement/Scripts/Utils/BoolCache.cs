using System;

namespace SoundManagement.Utils
{
	[Serializable]
	public class BoolCache<T>
	{
		public string Name;
		public T Value;
		public bool HasValue;
		private Func<T> _initialization;

		private ReadonlyBoolCache<T> _readonlyBoolCache;
		public ReadonlyBoolCache<T> Readonly => _readonlyBoolCache;
		
		public BoolCache()
		{
			HasValue = false;
			Name = "Empty";
			_readonlyBoolCache = new ReadonlyBoolCache<T>(this);
		}

		public BoolCache(Func<T> initialization)
		{
			HasValue = false;
			_initialization = initialization;
			Name = "Empty";
			_readonlyBoolCache = new ReadonlyBoolCache<T>(this);
		}
		
		
		public static implicit operator bool(BoolCache<T> t)
		{
			return t.HasValue;
		}

		public static implicit operator T(BoolCache<T> t)
		{
			return t.Value;
		}

		public void Reset()
		{
			HasValue = false;
			Value = default;
		}

		public void Init(T value)
		{
			Value = value;
			HasValue = true;
		}

		public void InitWithValidation(T value)
		{
			Value = value;
			HasValue = value != null;
		}

		public void InitWithValidation(T value, Func<T, bool> isValid)
		{
			Value = value;
			HasValue = isValid(value);
		}

		public void SetInitialization(Func<T> initialization)
		{
			_initialization = initialization;
		}
		
		public void TryInitialize()
		{
			if (HasValue) return;
			if (_initialization == null) return;
			Init(_initialization());
		}
	}

	public class ReadonlyBoolCache<T>
	{
		private BoolCache<T> _target;


		public string Name => _target.Name;
		public bool HasValue => _target.HasValue;
		public T Value => _target.Value;
		
		public ReadonlyBoolCache(BoolCache<T> target)
		{
			_target = target;
		}
	}
}