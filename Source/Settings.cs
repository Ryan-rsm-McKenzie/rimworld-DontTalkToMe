using System;
using System.Collections.Generic;
using System.Linq;
using KTrie;
using RimWorld;
using UnityEngine;
using Verse;

namespace DontTalkToMe
{
	internal class Settings : ModSettings
	{
		private StringTrieSet _blockedKeys = new StringTrieSet();

		private Window _window = null;

		public void DoWindowContents(Rect canvas)
		{
			if (this._window == null) {
				// settings menu opened
				this._window = new Window(this._blockedKeys);
			}
			this._window.Draw(canvas);
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving) {
				// settings menu closed
				this._blockedKeys.Clear();
				var query = from config in this._window.Config
										where !config.Allowed
										select config.Replacement.key;
				this._blockedKeys.AddRange(query);
				this._window = null;
			}

			var blockedKeys = new List<string>(this._blockedKeys);
			this._blockedKeys.Clear();
			Scribe_Collections.Look(ref blockedKeys, "blockedKeys", LookMode.Value);
			if (blockedKeys != null) {
				this._blockedKeys.AddRange(blockedKeys);
			}
		}

		public bool ShouldBlockKey(string key)
		{
			return this._blockedKeys.Contains(key);
		}

		private class Window
		{
			public readonly List<ReplacementConfig> Config;

			private readonly TextStyle _style = new TextStyle();

			private List<ReplacementConfig> _filteredConfig;

			private Vector2 _scrollPos = default;

			private SearchWidget _searcher = new SearchWidget();

			private Dictionary<string, string> _truncationCache = new Dictionary<string, string>();

			public Window(ICollection<string> blockedKeys)
			{
				var all = from val in LanguageDatabase.activeLanguage.keyedReplacements.Values
									orderby val.key
									select new ReplacementConfig(val, !blockedKeys.Contains(val.key));
				this.Config = all.ToList();
				this._filteredConfig = this.Config.Clone();
				this._searcher.OnChanged = () => {
					this._filteredConfig.Clear();
					var filtered = from config in this.Config
												 where this._searcher.Filter.Matches(config.Value)
												 select config;
					this._filteredConfig.AddRange(filtered);
					this._searcher.NoMatches = this._filteredConfig.Count == 0;
				};
			}

			public void Draw(Rect inRect)
			{
				Text.Anchor = TextAnchor.UpperLeft;

				var manager = new CanvasManager(inRect);
				this.DrawTopBar(manager);
				manager.Pad(2);
				this.DrawSearchResults(manager);
			}

			private static (int offset, int count) GetVisibleRange(Rect rect, float itemSize)
			{
				int first = (int)(rect.y / itemSize);
				int last = (int)Math.Ceiling((rect.y + rect.height) / itemSize);

				return (first, last - first);
			}

			private void DrawSearchResults(CanvasManager manager)
			{
				const float MarginBorder = 16f;

				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				var canvas = manager.Allocate();

				var fullScrollRect = new Rect(0, 0, canvas.width - MarginBorder, this._style.Height * this._filteredConfig.Count);
				Widgets.BeginScrollView(canvas, ref this._scrollPos, fullScrollRect, true);

				var visibleScrollRect = new Rect(this._scrollPos, new Vector2(fullScrollRect.width, canvas.height));
				var (offset, count) = GetVisibleRange(visibleScrollRect, this._style.Height);
				count = Math.Min(this._filteredConfig.Count, count);

				for (int i = offset; count > 0; ++i, --count) {
					var outer = new Rect(
						x: 0,
						y: this._style.Height * i,
						width: visibleScrollRect.width,
						height: this._style.Height);

					if (i % 2 == 0) {
						Widgets.DrawHighlight(outer);
					}

					Widgets.BeginGroup(outer);

					var inner = new Rect(
						x: this._style.MarginLeft,
						y: this._style.MarginTop,
						width: outer.width - this._style.MarginHorizontal,
						height: this._style.ContentHeight);
					inner.SplitVerticallyWithMargin(out var left, out var right, out _, compressibleMargin: 4, rightWidth: inner.height);
					var config = this._filteredConfig[i];

					Widgets.DrawHighlightIfMouseover(left);
					TooltipHandler.TipRegion(
						left,
						() => $"Source: {config.Replacement.fileSource}\nKey: {config.Replacement.key}\nValue: {config.Value}",
						i);

					string label = config.Value.Truncate(left.width, this._truncationCache);
					Widgets.Label(left, label);
					Widgets.Checkbox(right.position, ref config.Allowed, size: right.height, paintable: true);

					Widgets.EndGroup();
				}

				Widgets.EndScrollView();
			}

			private void DrawTopBar(CanvasManager manager)
			{
				const int TooltipMarginTop = 2;
				var canvas = manager.Allocate(Text.LineHeightOf(GameFont.Small) + Text.LineHeightOf(GameFont.Tiny) + TooltipMarginTop);

				canvas.SplitVerticallyWithMargin(out var left, out var reset, out _, compressibleMargin: 4, rightWidth: 2 * canvas.height);
				left.SplitHorizontallyWithMargin(out var searchbox, out var tooltip, out _, topHeight: Text.LineHeightOf(GameFont.Small));

				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				Widgets.BeginGroup(searchbox);
				this._searcher.Draw(new Rect(default, searchbox.size));
				Widgets.EndGroup();

				Text.Font = GameFont.Tiny;
				GUI.color = new Color(1f, 1f, 1f, 0.6f);
				tooltip.ShrinkTopEdge(TooltipMarginTop);
				Widgets.BeginGroup(tooltip);
				Widgets.Label(new Rect(default, tooltip.size), "Use the search bar to filter messages");
				Widgets.EndGroup();

				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				Widgets.BeginGroup(reset);
				if (Widgets.ButtonText(new Rect(default, reset.size), "Reset".Translate())) {
					foreach (var config in this.Config) {
						config.Allowed = true;
					}
					Messages.Message("Reset mod specific settings to their defaults", MessageTypeDefOf.TaskCompletion, false);
				}
				Widgets.EndGroup();
			}

			internal class ReplacementConfig
			{
				public readonly LoadedLanguage.KeyedReplacement Replacement;

				public bool Allowed;

				private string _value = null;

				public ReplacementConfig(LoadedLanguage.KeyedReplacement replacement, bool allowed)
				{
					this.Replacement = replacement;
					this.Allowed = allowed;
				}

				public string Value {
					get {
						if (this._value == null) {
							this._value = this.Replacement.value.StripTags();
						}
						return this._value;
					}
				}
			}

			private class CanvasManager
			{
				public readonly Rect Canvas;

				private float _pos;

				public CanvasManager(Rect canvas)
				{
					this.Canvas = canvas;
					this._pos = this.Canvas.y;
				}

				public Rect Allocate()
				{
					return this.Allocate(this.Canvas.height - this._pos);
				}

				public Rect Allocate(float height)
				{
#if DEBUG
					if (this._pos + height > this.Canvas.height) {
						Log.Error("Attempted to draw past end of canvas. Fixing...");
						height = this.Canvas.height - this._pos;
					}
#endif
					float y = this._pos;
					this._pos += height;
					return new Rect(0, y, this.Canvas.width, height);
				}

				public void Pad(float height)
				{
#if DEBUG
					if (this._pos + height > this.Canvas.height) {
						Log.Error("Attempted to pad past end of canvas. Fixing...");
						height = this.Canvas.height - this._pos;
					}
#endif
					this._pos += height;
				}
			}

			private class TextStyle
			{
				public readonly float ContentHeight;

				public readonly float Height;

				public readonly float MarginHorizontal = 4f;

				public readonly float MarginVertical;

				public TextStyle()
				{
					this.ContentHeight = Text.LineHeightOf(GameFont.Small);
					this.MarginVertical = this.ContentHeight / 4;
					this.Height = this.ContentHeight + this.MarginVertical;
				}

				public float MarginBottom => this.MarginVertical / 2;

				public float MarginLeft => this.MarginHorizontal / 2;

				public float MarginRight => this.MarginHorizontal / 2;

				public float MarginTop => this.MarginVertical / 2;
			}
		}
	}
}
