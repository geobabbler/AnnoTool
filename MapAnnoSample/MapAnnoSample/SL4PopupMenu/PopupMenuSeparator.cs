using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SL4PopupMenu
{
	public class PopupMenuSeparator : PopupMenuItem
	{
		public Rectangle HorizontalSeparator { get; set; }

		public Brush Brush
		{
			get { return HorizontalSeparator.Fill; }
			set { HorizontalSeparator.Fill = value; }
		}

		public PopupMenuSeparator()
			: base(null, null, true)
		{
			CreateHorizontalSeparator(null, 2, null);
		}

		public PopupMenuSeparator(string tag)
			: base(null, null, true)
		{
			CreateHorizontalSeparator(tag, 2, null);
		}

		public PopupMenuSeparator(string tag, double? leftMarginMinWidth)
			: base(null, null, true)
		{
			CreateHorizontalSeparator(tag, 2, leftMarginMinWidth);
		}

		private Rectangle CreateHorizontalSeparator(string tag, double height, double? leftMarginMinWidth)
		{
			CloseOnClick = false;

			if (leftMarginMinWidth.HasValue)
				ImageLeftMinWidth = leftMarginMinWidth.Value;

			Color endColor = SeparatorEndColor;
			if (endColor.A + 10 <= 255)
				endColor.A += 10;

			HorizontalSeparator = new Rectangle
			{
				Height = height,
				Margin = new Thickness(-3, 0, -3, 0),
				Fill = PopupMenuUtils.MakeColorGradient(SeparatorStartColor, endColor, 90)
			};

			DockPanel.SetDock(HorizontalSeparator, Dock.Top);

			if (tag != null)
				HorizontalSeparator.Tag = tag;

			this.Header = HorizontalSeparator;
			DockPanel.SetDock(HorizontalSeparator, Dock.Top);
			return HorizontalSeparator;
		}
	}

}
