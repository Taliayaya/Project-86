using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Armament.Shared
{
	public static class NewMiscHelper
	{
		public static int Abs(this int value) => Mathf.Abs(value);
		public static float Abs(this float value) => Mathf.Abs(value);


		public static IEnumerable<TSource> ExceptBy<TSource, TKey>(
			this IEnumerable<TSource> first,
			IEnumerable<TSource> second,
			Func<TSource, TKey> keySelector)
		{
			return first.ExceptBy<TSource, TKey>(second, keySelector, (IEqualityComparer<TKey>)null);
		}

		public static IEnumerable<TSource> ExceptBy<TSource, TKey>(
			this IEnumerable<TSource> first,
			IEnumerable<TSource> second,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey>? keyComparer)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));
			if (second == null)
				throw new ArgumentNullException(nameof(second));
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			return Impl();

			IEnumerable<
#nullable disable
				TSource> Impl()
			{
				HashSet<TKey> keys = new HashSet<TKey>(second.Select<TSource, TKey>(keySelector), keyComparer);
				foreach (TSource source in first)
				{
					TKey key = keySelector(source);
					if (!keys.Contains(key))
					{
						yield return source;
						keys.Add(key);
						key = default(TKey);
					}
				}
			}
		}

		public static T Random<T>(this T[] array) =>
			array is { Length: > 0 } ? array[UnityEngine.Random.Range(0, array.Length)] : default;

		public static T Random<T>(this T[] array, T except)
		{
			if (except == null) return array.Random();
			IEnumerable<T> exc = array.Except(new[]
			{
				except
			});
			return exc.Random();
		}

		public static T Random<T>(this List<T> array) =>
			array is { Count: > 0 } ? array[UnityEngine.Random.Range(0, array.Count)] : default;

		public static T Random<T>(this IEnumerable<T> array) => array != null && array.Count() > 0
			? array.ElementAt(UnityEngine.Random.Range(0, array.Count()))
			: default;


		public static Vector3 SetX(this Vector3 vector3, float x) => vector3.SetAllVector(x: x);
		public static Vector3 SetY(this Vector3 vector3, float y) => vector3.SetAllVector(y: y);
		public static Vector3 SetZ(this Vector3 vector3, float z) => vector3.SetAllVector(z: z);

		public static Vector2 SetX(this Vector2 vector2, float x) => new Vector2(x, vector2.y);
		public static Vector2 SetY(this Vector2 vector2, float y) => new Vector2(vector2.x, y);

		private static Vector3 SetAllVector(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
		{
			if (x != null) vector3.x = (float)x;
			if (y != null) vector3.y = (float)y;
			if (z != null) vector3.z = (float)z;

			return vector3;
		}


		public static Vector2Int RotatePoint(this Vector2Int pt, float angle)
		{
			float fi = Mathf.Atan2(pt.y, pt.x) * Mathf.Rad2Deg;
			float fi2 = fi + angle;

			float magnitude = pt.magnitude;
			pt.y = Mathf.RoundToInt(magnitude * Mathf.Sin(fi2 * Mathf.Deg2Rad));
			pt.x = Mathf.RoundToInt(magnitude * Mathf.Cos(fi2 * Mathf.Deg2Rad));

			return pt;
		}

		public static void FadeAlpha(this Image image, float alpha)
		{
			image.color = image.color.FadeAlpha(alpha);
		}

		public static void FadeAlpha(this TextMeshProUGUI text, float alpha)
		{
			text.color = text.color.FadeAlpha(alpha);
		}

		public static void FadeAlpha(this SpriteRenderer image, float alpha)
		{
			image.color = image.color.FadeAlpha(alpha);
		}

		public static void FadeAlpha(this CanvasGroup image, float alpha)
		{
			image.alpha = alpha;
		}

		public static Color FadeAlpha(this Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}

		public static T[] ToArray<T>(this T target) where T : Enum => (T[])Enum.GetValues(target.GetType());

		public static T[] ToArray<T>() where T : Enum => (T[])Enum.GetValues(typeof(T));

		public static int Clamp(this int value, int min, int max) => Math.Clamp(value, min, max);
		public static float Clamp(this float value, float min, float max) => Mathf.Clamp(value, min, max);

		public static float Cap(this float number, float min, float max)
		{
			if (number > max) number = max;
			if (number < min) number = min;

			return number;
		}

		public static int Cap(this int number, int min, int max)
		{
			if (number > max) number = max;
			if (number < min) number = min;

			return number;
		}


		public static int IndexOf<T>(this T target, T[] array)
		{
			return Array.IndexOf(array, target);
		}

		public static int IndexOfType<T>(this T target, T[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (target.GetType() == array[i].GetType())
					return i;
			}

			return 0;
		}

		public static int IndexOf<T>(this T target, List<T> array)
		{
			return Array.IndexOf(array.ToArray(), target);
		}

		public static void CopySharedArrayTo<T>(this T[] from, T[] to, int length)
		{
			Array.Copy(from, to, length);
		}

		public static Vector2[] GetCircleRandomPoints(Vector2 center, float radius, int count)
		{
			List<Vector2> points = new List<Vector2>();

			for (var i = 0; i < count; i++)
			{
				float theta = UnityEngine.Random.Range(0f, 2 * Mathf.PI); // angle

				float randomRadius = Mathf.Sqrt(UnityEngine.Random.value) * radius;

				float x = center.x + randomRadius * Mathf.Cos(theta);
				float y = center.y + randomRadius * Mathf.Sin(theta);
				points.Add(new Vector2(x, y));
			}

			return points.ToArray();
		}
	}
}