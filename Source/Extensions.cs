using System.Collections.Generic;
using UnityEngine;

namespace DontTalkToMe
{
  internal static class ListExt
	{
		public static List<T> Clone<T>(this List<T> list)
		{
			return new List<T>(list);
		}
	}

	internal static class RectExt
	{
		public static void ShrinkBottomEdge(this ref Rect rect, float value)
		{
			rect.height -= value;
		}

		public static void ShrinkHorizontally(this ref Rect rect, float value)
		{
			rect.x += value / 2;
			rect.width -= value;
		}

		public static void ShrinkLeftEdge(this ref Rect rect, float value)
		{
			rect.x += value;
			rect.width -= value;
		}

		public static void ShrinkRightEdge(this ref Rect rect, float value)
		{
			rect.width -= value;
		}

		public static void ShrinkTopEdge(this ref Rect rect, float value)
		{
			rect.y += value;
			rect.height -= value;
		}

		public static void ShrinkVertically(this ref Rect rect, float value)
		{
			rect.y += value / 2;
			rect.height -= value;
		}
	}
}
