#nullable enable

using System;
using UnityEngine;
using Verse;

namespace DontTalkToMe
{
	internal class CheckboxWidget
	{
		public bool Disabled = false;

		public Action? OnChanged = null;

		public bool Paintable = false;

		private bool _selected;

		public CheckboxWidget(bool selected)
		{
			this._selected = selected;
		}

		public bool Selected {
			get => this._selected;
			set {
				if (this._selected != value) {
					this._selected = value;
					this.OnChanged?.Invoke();
				}
			}
		}

		public void Draw(Rect canvas)
		{
			bool state = this.Selected;
			Widgets.Checkbox(canvas.position, ref state, size: canvas.height, disabled: this.Disabled, this.Paintable);
			this.Selected = state;
		}
	}
}
