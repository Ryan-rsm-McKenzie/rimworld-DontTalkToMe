﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DontTalkToMe
{
	internal static class FilterMethodExt
	{
		public static string ToLabel(this Settings.Window.FilterMethod method)
		{
			return method switch {
				Settings.Window.FilterMethod.Key => "DontTalkToMe.KeyLabel".Translate(),
				Settings.Window.FilterMethod.Source => "DontTalkToMe.SourceLabel".Translate(),
				Settings.Window.FilterMethod.Reset => "Reset".Translate(),
				_ => "DontTalkToMe.ValueLabel".Translate(),
			};
		}

		public static string ToTooltip(this Settings.Window.FilterMethod method)
		{
			return method switch {
				Settings.Window.FilterMethod.Key => "DontTalkToMe.KeyTooltip".Translate(),
				Settings.Window.FilterMethod.Source => "DontTalkToMe.SourceTooltip".Translate(),
				Settings.Window.FilterMethod.Reset => "DontTalkToMe.ResetTooltip".Translate(),
				_ => "DontTalkToMe.ValueTooltip".Translate(),
			};
		}
	}

	internal class Settings : ModSettings
	{
		private readonly HashSet<string> _blockedKeys = new();

		private Window? _window = null;

		public void DoWindowContents(Rect canvas)
		{
			this._window ??= new Window(this._blockedKeys); // settings menu opened
			this._window.Draw(canvas);
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving && this._window is not null) {
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

		internal class Window
		{
			public readonly List<ReplacementConfig> Config;

			private readonly List<Widgets.DropdownMenuElement<FilterMethod>> _dropdownOptions = new();

			private readonly List<ReplacementConfig> _filteredConfig = new();

			private readonly SearchWidget _searcher = new();

			private readonly TextStyle _style = new();

			private readonly Dictionary<string, string> _truncationCache = new();

			private FilterMethod _filterMethod = FilterMethod.Value;

			private CheckboxWidget? _reseter = null;

			private ScrollPosition _scrollPos = default;

			public Window(ICollection<string> blockedKeys)
			{
				var all = from val in LanguageDatabase.activeLanguage.keyedReplacements.Values
									orderby val.key
									select new ReplacementConfig(val, !blockedKeys.Contains(val.key));
				this.Config = all.ToList();
				this._searcher.OnChanged = () => this.OnFilterChanged();
				this._dropdownOptions.AddRange(
					Enum.GetValues(typeof(FilterMethod))
							.Cast<FilterMethod>()
							.Select((e) => {
								return new Widgets.DropdownMenuElement<FilterMethod>() {
									option = new FloatMenuOption(
										label: e.ToLabel(),
										action: () => { this._filterMethod = e; this.OnFilterChanged(); },
										mouseoverGuiAction: (rect) => TooltipHandler.TipRegion(rect, () => e.ToTooltip(), (int)e)
									),
									payload = e,
								};
							}));
				this.OnFilterChanged();
			}

			internal enum FilterMethod
			{
				Value,

				Key,

				Source,

				Reset,
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
				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				var canvas = manager.Allocate();

				var fullScrollRect = new Rect(0, 0, canvas.width - GenUI.ScrollBarWidth, this._style.Height * this._filteredConfig.Count);
				float maxScrollable = fullScrollRect.height - canvas.height;
				this._scrollPos.Absolute.y = Math.Max(0f, this._scrollPos.Relative * maxScrollable);
				Widgets.BeginScrollView(canvas, ref this._scrollPos.Absolute, fullScrollRect, true);
				this._scrollPos.Relative = this._scrollPos.Absolute.y / maxScrollable;

				var visibleScrollRect = new Rect(this._scrollPos.Absolute, new Vector2(fullScrollRect.width, canvas.height));
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
					inner.SplitVerticallyWithMargin(out var left, out var right, out _, compressibleMargin: GenUI.GapTiny, rightWidth: inner.height);
					var config = this._filteredConfig[i];

					Widgets.DrawHighlightIfMouseover(left);
					TooltipHandler.TipRegion(
						left,
						() => "DontTalkToMe.ListItemTooltip".Translate(config.Replacement.fileSource, config.Replacement.key, config.Value),
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
				const float TooltipMarginTop = 2;
				var canvas = manager.Allocate(Text.LineHeightOf(GameFont.Small) + Text.LineHeightOf(GameFont.Tiny) + TooltipMarginTop);

				canvas.SplitVerticallyWithMargin(out var left, out var dropdown, out _, compressibleMargin: GenUI.GapTiny, rightWidth: 2 * canvas.height);
				left.SplitHorizontallyWithMargin(out var searchbox, out var tooltip, out _, topHeight: Text.LineHeightOf(GameFont.Small));

				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				Widgets.Dropdown(
					dropdown,
					this,
					(_) => this._filterMethod,
					(_) => this._dropdownOptions,
					this._filterMethod.ToLabel());

				if (this._filterMethod == FilterMethod.Reset) {
					Text.Font = GameFont.Small;
					GUI.color = Color.white;

					string fullText = "DontTalkToMe.ResetConfirm".Translate();
					string sizedText;
					float leftWidth = Text.CalcSize(fullText).x;
					if ((leftWidth + GenUI.GapTiny + left.height) > left.width) {
						leftWidth = left.width - GenUI.GapTiny - left.height;
						sizedText = fullText.Truncate(leftWidth, this._truncationCache);
					} else {
						sizedText = fullText;
					}

					searchbox.SplitVerticallyWithMargin(out var label, out var checkbox, out _, compressibleMargin: GenUI.GapTiny, leftWidth: leftWidth);
					Widgets.DrawHighlightIfMouseover(label);
					Widgets.Label(label, sizedText);
					TooltipHandler.TipRegion(label, () => fullText, 0);
					this._reseter!.Draw(checkbox);
				} else {
					Text.Font = GameFont.Small;
					GUI.color = Color.white;
					this._searcher.Draw(searchbox);

					Text.Font = GameFont.Tiny;
					GUI.color = new Color(1f, 1f, 1f, 0.6f);
					tooltip.ShrinkTopEdge(TooltipMarginTop);
					Widgets.Label(tooltip, "DontTalkToMe.SearchbarTooltip".Translate());
				}
			}

			private void OnFilterChanged()
			{
				Func<ReplacementConfig, bool> filter = this._filterMethod switch {
					FilterMethod.Key => (config) => this._searcher.Filter.Matches(config.Replacement.key),
					FilterMethod.Source => (config) => this._searcher.Filter.Matches(config.Replacement.fileSource),
					FilterMethod.Reset => (_) => false,
					_ => (config) => this._searcher.Filter.Matches(config.Value),
				};

				if (this._filterMethod == FilterMethod.Reset) {
					if (this._reseter == null) {
						this._reseter = new CheckboxWidget(false);
						this._reseter.OnChanged = () => {
							if (this._reseter.Selected) {
								foreach (var config in this.Config) {
									config.Allowed = true;
								}
								Messages.Message("DontTalkToMe.ResetComplete".Translate(), MessageTypeDefOf.TaskCompletion, false);
							}
						};
					}
				} else {
					this._reseter = null;
				}

				this._filteredConfig.Clear();
				this._filteredConfig.AddRange(this.Config.Where(filter));
				this._searcher.NoMatches = this._filteredConfig.Count == 0;
			}

			private struct ScrollPosition
			{
				public Vector2 Absolute;

				public float Relative;
			}

			internal class ReplacementConfig
			{
				public readonly LoadedLanguage.KeyedReplacement Replacement;

				public bool Allowed;

				private string? _value = null;

				public ReplacementConfig(LoadedLanguage.KeyedReplacement replacement, bool allowed)
				{
					this.Replacement = replacement;
					this.Allowed = allowed;
				}

				public string Value {
					get {
						this._value ??= this.Replacement.value.StripTags();
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
