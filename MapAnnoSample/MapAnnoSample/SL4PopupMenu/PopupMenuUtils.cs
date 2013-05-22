// Copyright (c) 2009 Ziad Jeeroburkhan. All Rights Reserved.
// GNU General Public License version 2 (GPLv2) 
// (http://sl4popupmenu.codeplex.com/license)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace SL4PopupMenu
{
	public class PopupMenuUtils
	{
		public static void SetContent(Panel parentGrid, FrameworkElement childElement, bool addDefaultShadowEffect)
		{
			if (childElement.Parent != null)
			{
				if (childElement.Parent is ContentControl) // If the control originates from a ContentControl or the current PopupMenu content
					(childElement.Parent as ContentControl).Content = null; // dissociate it from the current PopupMenu content
				else if (childElement.Parent is Panel) // If the control originates from a Panel
					(childElement.Parent as Panel).Children.Remove(childElement); // dissociate it from the panel
				else
					throw new Exception("The content element must be placed in a container that inherits from the Panel or ContentControl class. "
									  + "The actual parent type is " + childElement.Parent.GetType());
			}
			parentGrid.Children.Clear();
			parentGrid.Children.Add(childElement);

			// Add the default shadow effect if the child element doesn't have any
			if (childElement.Effect == null && addDefaultShadowEffect)
				childElement.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 4, Opacity = 0.5, ShadowDepth = 2 };
		}

		public static FrameworkElement FindApplicationElementByName(string elementName, string elementQualifierForErrorMsg)
		{
			object obj = (Application.Current.RootVisual as FrameworkElement).FindName(elementName.Trim());
			if (obj == null) // Object not found. Use the more thorough but also more costly method.
				obj = Application.Current.RootVisual.GetVisualDescendants().OfType<FrameworkElement>()
																		   .Where(elem => elem.Name == elementName).FirstOrDefault();
			if (obj != null)
			{
				if (obj is UIElement)
				{
					return obj as FrameworkElement;
				}
				else
				{
					if (elementQualifierForErrorMsg != null && !DesignerProperties.IsInDesignTool) // Error messages are disabled at design time
						throw new ArgumentException("The " + elementQualifierForErrorMsg + " is referenced to " + elementName + " which is not a UIElement.");
				}
			}
			else
			{
				if (elementQualifierForErrorMsg != null && !DesignerProperties.IsInDesignTool) // Error messages are disabled at design time
					throw new ArgumentException("Could not find any element named " + elementName + " for " + elementQualifierForErrorMsg + " in the visual tree.");
			}
			return null;
		}

		public static bool HitTestAny(MouseEventArgs e, bool applyZoom, bool autoMapHoverBoundsToParentOfTextBlocks, bool valueToReturnOnNullElements, params FrameworkElement[] elements)
		{
			foreach (FrameworkElement elem in elements)
			{
				if (elem == null)
				{
					return valueToReturnOnNullElements;
				}
				else
				{
					// Textblocks have a variable width, depending on the text length, when none is specified.
					bool isVariableWidthTextBlock = autoMapHoverBoundsToParentOfTextBlocks
												 && elem is TextBlock
												 && double.IsNaN((elem as TextBlock).Width)
												 && double.IsInfinity((elem as TextBlock).MaxWidth);
					// In this case the parent element is used for hit testing to avoid limiting the hover region to the text only region.
					if (HitTest(e, applyZoom, isVariableWidthTextBlock ? elem.Parent as FrameworkElement : elem))
						return true;
				}
			}
			return false;
		}

		public static bool HitTestAny(MouseEventArgs e, bool applyZoom, params FrameworkElement[] elements)
		{
			foreach (var element in elements)
				if (HitTest(e, applyZoom, element))
					return true;
			return false;
		}

		public static bool HitTest(MouseEventArgs e, bool applyZoom, FrameworkElement element)
		{
			Rect? rect = element.GetBoundsRelativeTo(Application.Current.RootVisual);
			if (!rect.HasValue)
				return false;

			Rect box = rect.Value;
			Point pt = e.GetSafePosition(null);
			if (applyZoom)
				pt = MapToZoomValue(pt);
			// Determine if the mouse lies within the element bounds
			return (pt.X > box.Left && pt.X < box.Right
				 && pt.Y > box.Top && pt.Y < box.Bottom);
		}

		public static Point GetAbsoluteElementPos(bool applyZoom, FrameworkElement element)
		{
			Point pt = element.TransformToVisual(null).Transform(new Point());
			if (applyZoom)
				pt = MapToZoomValue(pt);
			return pt;
			//Rect rect = element.GetBoundsRelativeTo(Application.Current.RootVisual).Value;
			//return new Point(rect.Left, rect.Top);
		}

		//public static Point GetAbsoluteMousePos(MouseEventArgs e)
		//{
		//    // This will not work for MouseLeave events
		//    Point pt = e.GetSafePosition(null);// Application.Current.RootVisual.TransformToVisual(null).Transform(e.GetPosition(Application.Current.RootVisual));
		//    return pt;
		//}

		//public static Point GetAbsoluteMousePos(MouseEventArgs e, FrameworkElement element)
		//{
		//    Point pt = element.TransformToVisual(null).Transform(e.GetPosition(element));
		//    return MapToZoomLevel(pt);
		//}

		private static Point MapToZoomValue(Point pt)
		{
			double zoomFactor = Application.Current.Host.Content.ZoomFactor;
			pt.X /= zoomFactor;
			pt.Y /= zoomFactor;
			return pt;
		}

		public static LinearGradientBrush MakeColorGradient(Color startColor, Color endColor, double angle)
		{
			GradientStopCollection gradientStopCollection = new GradientStopCollection();
			gradientStopCollection.Add(new GradientStop { Color = startColor, Offset = 0 });
			gradientStopCollection.Add(new GradientStop { Color = endColor, Offset = 1 });
			LinearGradientBrush brush = new LinearGradientBrush(gradientStopCollection, angle);
			return brush;
		}

		//public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double? from, double? to)
		//{
		//    return CreateStoryBoard(beginTime, duration, element, targetProperty, from, to, false);
		//}

		//public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double? from, double? to, bool beginNow)
		//{
		//    DoubleAnimation da = new DoubleAnimation
		//    {
		//        From = from,
		//        To = to,
		//        Duration = new TimeSpan(0, 0, 0, 0, duration)
		//    };

		//    if (element != null)
		//        Storyboard.SetTarget(da, element);
		//    Storyboard.SetTargetProperty(da, new PropertyPath(targetProperty));

		//    Storyboard sb = new Storyboard();
		//    sb.Children.Add(da);
		//    sb.BeginTime = new TimeSpan(0, 0, 0, 0, beginTime);
		//    if (beginNow)
		//        sb.Begin();
		//    RegisterStoryBoardTarget(sb, element);
		//    return sb;
		//}

		public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double value, EasingFunctionBase easingFunction)
		{
			return CreateStoryBoard(beginTime, duration, element, targetProperty, value, easingFunction, false);
		}

		public static Storyboard CreateStoryBoard(int beginTime, int duration, FrameworkElement element, string targetProperty, double value, EasingFunctionBase easingFunction, bool beginNow)
		{
			EasingDoubleKeyFrame edkf = new EasingDoubleKeyFrame
			{
				KeyTime = new TimeSpan(0, 0, 0, 0, duration),
				Value = value,
				EasingFunction = easingFunction
			};

			DoubleAnimationUsingKeyFrames da = new DoubleAnimationUsingKeyFrames();
			da.KeyFrames.Add(edkf);

			if (element != null)
				Storyboard.SetTarget(da, element);

			if (targetProperty != null)
				Storyboard.SetTargetProperty(da, new PropertyPath(targetProperty));

			Storyboard sb = new Storyboard();
			sb.Children.Add(da);
			sb.BeginTime = new TimeSpan(0, 0, 0, 0, beginTime);

			if (beginNow)
				sb.Begin();

			RegisterStoryBoardTarget(sb, element);
			return sb;
		}

		public static void RegisterVisualStateGroupTargets(VisualStateGroup visualStateGroup, params FrameworkElement[] targetElements)
		{
			foreach (VisualState state in visualStateGroup.States)
				RegisterStoryBoardTargets(state.Storyboard, targetElements);
		}

		public static void RegisterStoryBoardTargets(Storyboard storyBoard, params FrameworkElement[] targetElements)
		{
			foreach (FrameworkElement targetElement in targetElements)
				RegisterStoryBoardTarget(storyBoard, targetElement);
		}

		public static void RegisterStoryBoardTarget(Storyboard storyBoard, FrameworkElement targetElement)
		{
			targetElement.Dispatcher.BeginInvoke(delegate
			{
				foreach (Timeline child in storyBoard.Children.OfType<Timeline>()
															  .Where(tl => Storyboard.GetTargetName(tl) == targetElement.Name))
					Storyboard.SetTarget(child, targetElement);
			});
		}

		public static T GetStyleValue<T>(Style style, DependencyProperty dp)
		{
			Setter setter = GetStyleSetter(style, dp);
			return setter.Value == null ? default(T) : (T)setter.Value;
		}

		public static Setter GetStyleSetter(Style style, DependencyProperty dp)
		{
			return style.Setters.OfType<Setter>().Where(s => s.Property == dp).FirstOrDefault();
		}

		//private static bool HitTest(Point point, FrameworkElement element)
		//{
		//    List<UIElement> hits = System.Windows.Media.VisualTreeHelper.FindElementsInHostCoordinates(point, element) as List<UIElement>;
		//    return (hits.Contains(element));
		//}



		//        private static bool? _isInDesignMode;
		//        public static bool IsInDesignModeStatic
		//        {
		//            get
		//            {
		//                if (!_isInDesignMode.HasValue)
		//                {
		//#if DEBUG
		//                    _isInDesignMode = DesignerProperties.IsInDesignTool;
		//#else
		//                    _isInDesignMode = false;
		//#endif
		//                }
		//                return _isInDesignMode.Value;
		//            }
		//        }

		///// <summary>
		///// Provides a custom implementation of DesignerProperties.GetIsInDesignMode
		///// to work around an issue.
		///// </summary>
		//internal static class DesignerProperties
		//{
		//    /// <summary>
		//    /// Returns whether the control is in design mode (running under Blend
		//    /// or Visual Studio).
		//    /// </summary>
		//    /// <param name="element">The element from which the property value is
		//    /// read.</param>
		//    /// <returns>True if in design mode.</returns>
		//    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "element", Justification =
		//        "Matching declaration of System.ComponentModel.DesignerProperties.GetIsInDesignMode (which has a bug and is not reliable).")]
		//    public static bool GetIsInDesignMode(DependencyObject element)
		//    {
		//        if (!_isInDesignMode.HasValue)
		//        {
		//            _isInDesignMode =
		//                (null == Application.Current) ||
		//                Application.Current.GetType() == typeof(Application);
		//        }
		//        return _isInDesignMode.Value;
		//    }

		//    /// <summary>
		//    /// Stores the computed InDesignMode value.
		//    /// </summary>
		//    private static bool? _isInDesignMode;
		//}

	}
}
