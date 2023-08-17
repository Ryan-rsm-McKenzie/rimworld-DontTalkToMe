using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DontTalkToMe
{
  internal class SearchWidget
	{
		public QuickSearchFilter Filter = new QuickSearchFilter();

		public bool NoMatches = false;

		public Action OnChanged = null;

		private static ulong s_instanceCounter = 0;

		private readonly string _controlName = "DontTalkToMe_SearchWidget";

		public SearchWidget()
		{
			this._controlName = $"DontTalkToMe_SearchWidget{s_instanceCounter++}";
		}

		public void Draw(Rect canvas)
		{
			const float Padding = 4f;

			if (this.HasFocus() && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
				this.KillFocus();
				Event.current.Use();
			}

			if (OriginalEventUtility.EventType == EventType.MouseDown && !canvas.Contains(Event.current.mousePosition)) {
				this.KillFocus();
			}

			GUI.color = Color.white;

			canvas.SplitVerticallyWithMargin(out var icon, out var right, out _, compressibleMargin: Padding, leftWidth: canvas.height);
			icon.ShrinkVertically(0.10f * icon.height);
			GUI.DrawTexture(icon, TexButton.Search);

			right.SplitVerticallyWithMargin(out var textfield, out var clear, out _, compressibleMargin: Padding, rightWidth: canvas.height);
			GUI.SetNextControlName(this._controlName);
			if (this.NoMatches && this.Filter.Active) {
				GUI.color = ColorLibrary.RedReadable;
			} else if (!this.Filter.Active && !this.HasFocus()) {
				GUI.color = ColorLibrary.Grey;
			}
			this.SetText(Widgets.TextField(textfield, this.Filter.Text));

			if (this.Filter.Active) {
				clear.ShrinkVertically(0.10f * clear.height);
				if (Widgets.ButtonImage(clear, TexButton.CloseXSmall, true)) {
					this.SetText("");
					SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
				}
			}
		}

		public bool HasFocus()
		{
			return GUI.GetNameOfFocusedControl() == this._controlName;
		}

		public void KillFocus()
		{
			if (this.HasFocus()) {
				UI.UnfocusCurrentControl();
			}
		}

		public void SetFocus()
		{
			GUI.FocusControl(this._controlName);
		}

		private void SetText(string text)
		{
			if (text != this.Filter.Text) {
				this.Filter.Text = text;
				this.OnChanged?.Invoke();
			}
		}
	}
}
