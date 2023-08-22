using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DontTalkToMe
{
	public class ModMain : Mod
	{
		public static ModMain Mod = null;

		private readonly string _canary;

		private readonly Harmony _harmony = null;

		private readonly Settings _settings;

		public ModMain(ModContentPack content)
			: base(content)
		{
			this._settings = this.GetSettings<Settings>();
			this._canary = $"@@{this.Content.PackageIdPlayerFacing}@@";
			Mod = this;

			this._harmony = new Harmony(this.Content.PackageIdPlayerFacing);
			this._harmony.PatchAll();

			var postfix = new HarmonyMethod(typeof(TranslatorFormattedStringExtensions_Translate), nameof(TranslatorFormattedStringExtensions_Translate.Postfix));
			Action<IEnumerable<Type>> patch = (p) => {
				var original = AccessTools.Method(
					type: typeof(TranslatorFormattedStringExtensions),
					name: "Translate",
					parameters: p.ToArray());
				this._harmony.Patch(original, postfix: postfix);
			};

			patch(new Type[] { typeof(string), typeof(NamedArgument[]) });
			var parameters = new List<Type>() { typeof(string) };
			for (int i = 0; i < 8; ++i) {
				parameters.Add(typeof(NamedArgument));
				patch(parameters);
			}

			this._harmony.Patch(
				AccessTools.Method(
					type: typeof(Translator),
					name: "Translate",
					parameters: new Type[] { typeof(string) }),
				postfix: postfix);
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			this._settings.DoWindowContents(inRect);
		}

		public void RerouteTranslation(string key, ref TaggedString result)
		{
			if (this._settings.ShouldBlockKey(key)) {
				result = new TaggedString($"{this._canary} {result.RawText}");
			}
		}

		public override string SettingsCategory()
		{
			return this.Content.Name;
		}

		public bool ShouldSuppressPopup(string text)
		{
			return text.Contains(this._canary);
		}
	}
}
