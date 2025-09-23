using System;
namespace Armament.Shared
{
	[Serializable]
	public abstract class OneTypeBase
	{
		public abstract string TypeName { get; }
		public abstract string ValueName { get; }

	}
}