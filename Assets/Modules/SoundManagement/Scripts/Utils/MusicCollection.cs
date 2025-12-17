using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;

namespace SoundManagement.Utils
{
	public class MusicCollection : IEnumerable<BoolCache<EventInstance>>
	{
		public BoolCache<EventInstance>[] Collection;

		public MusicCollection(params BoolCache<EventInstance>[] array)
		{
			Collection = array;
		}
		
		public IEnumerator<BoolCache<EventInstance>> GetEnumerator()
		{
			return Collection.AsEnumerable().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}