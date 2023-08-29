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

	internal static class StringExt
	{
		public static bool ContainsKMP(this string self, string word)
		{
			return self.IndexOfKMP(word) >= 0;
		}

		public static int IndexOfKMP(this string self, string word)
		{
			if (self.Length == 0) {
				return word.Length == 0 ? 0 : -1;
			} else if (word.Length == 0) {
				return -1;
			} else if (self.Length < word.Length) {
				return -1;
			}

			int j = 0;
			int k = 0;
			int[] table = KMPTable(word);

			while (j < self.Length) {
				if (word[k] == self[j]) {
					++j;
					++k;
					if (k == word.Length) {
						return j - k;
					}
				} else {
					k = table[k];
					if (k < 0) {
						++j;
						++k;
					}
				}
			}

			return -1;
		}

		private static int[] KMPTable(string word)
		{
			int pos = 1;
			int cnd = 0;
			int[] table = new int[word.Length + 1];

			table[0] = -1;

			while (pos < word.Length) {
				if (word[pos] == word[cnd]) {
					table[pos] = table[cnd];
				} else {
					table[pos] = cnd;
					while (cnd >= 0 && word[pos] != word[cnd]) {
						cnd = table[cnd];
					}
				}
				++pos;
				++cnd;
			}

			table[pos] = cnd;
			return table;
		}
	}
}
