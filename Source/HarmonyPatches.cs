﻿#pragma warning disable IDE1006 // Naming Styles
#nullable enable

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
			return !ModMain.Mod!.ShouldSuppressPopup(let.Label.RawText) || !let.CanDismissWithRightClick;
		}
	}

	[HarmonyPatch(typeof(Messages))]
	[HarmonyPatch("AcceptsMessage")]
	[HarmonyPatch(new Type[] { typeof(string), typeof(LookTargets) })]
	internal class Messages_AcceptsMessage
	{
		public static bool Prefix(string? text)
		{
			return text is null || !ModMain.Mod!.ShouldSuppressPopup(text);
		}
	}

	internal class TranslatorFormattedStringExtensions_Translate
	{
		public static void Postfix(ref TaggedString __result, string? key)
		{
			if (key is not null) {
				ModMain.Mod!.RerouteTranslation(key, ref __result);
			}
		}
	}
}
