#pragma warning disable IDE1006 // Naming Styles

using System;
using HarmonyLib;
using Verse;

namespace DontTalkToMe
{
  [HarmonyPatch(typeof(LetterStack))]
	[HarmonyPatch("ReceiveLetter")]
	[HarmonyPatch(new Type[] { typeof(Letter), typeof(string) })]
	internal class LetterStack_ReceiveLetter
	{
		public static bool Prefix(Letter let)
		{
			return !ModMain.Mod.ShouldSuppressPopup(let.Label.RawText) || !let.CanDismissWithRightClick;
		}
	}

	[HarmonyPatch(typeof(Messages))]
	[HarmonyPatch("AcceptsMessage")]
	[HarmonyPatch(new Type[] { typeof(string), typeof(LookTargets) })]
	internal class Messages_AcceptsMessage
	{
		public static bool Prefix(string text)
		{
			return !ModMain.Mod.ShouldSuppressPopup(text);
		}
	}

	internal class TranslatorFormattedStringExtensions_Translate
	{
		public static void Postfix(ref TaggedString __result, string key)
		{
			ModMain.Mod.RerouteTranslation(key, ref __result);
		}
	}
}
