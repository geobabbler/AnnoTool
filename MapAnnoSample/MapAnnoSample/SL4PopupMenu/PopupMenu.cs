
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
	public enum TriggerTypes { LeftClick, RightClick, Hover }

	//public class PopupTest : Canvas
	//{
	//    public EventHandler Opened { get; set; }

	//    public PopupTest()
	//    {
	//        this.SetValue(Canvas.ZIndexProperty, 1000000);
	//        IsOpen = false;
	//    }

	//    public bool IsOpen
	//    {
	//        get
	//        {
	//            return this.Visibility == Visibility.Visible;
	//        }
	//        set
	//        {
	//            this.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
	//            if (this.Visibility == Visibility.Visible)
	//                if(Opened != null)
	//                    Opened.BeginInvoke(this, new EventArgs(), null, null);
	//        }
	//    }
	//    //this.Loaded += delegate
	//    //{
	//    //    //if (Application.Current.RootVisual is UserControl)
	//    //    //    (Application.Current.RootVisual as UserControl).Content = (MenuPopup);
	//    //    //else
	//    //    if (this.GetVisualAncestors().OfType<Panel>().Count() > 0)
	//    //        (this.GetVisualAncestors().OfType<Panel>().Last()).Children.Add(MenuPopup);
	//    //    else if (this.GetVisualAncestors().OfType<ContentControl>().Count() > 0)
	//    //        (this.GetVisualAncestors().OfType<ContentControl>().Last()).Content = (MenuPopup);
	//    //    //else
	//    //    //    MenuPopup = MenuPopup;
	//    //    MenuPopup.Children.Add(OuterCanvas);
	//    //    OuterCanvas.Children.Add(RootGrid);
	//    //};
	//}

	public partial class PopupMenu : ContentControl
	{
		public static EventHandler<RoutedEventArgs> Click;

		public string HoverElements { get; set; }
		public string LeftClickElements { get; set; }
		public string RightClickElements { get; set; }

		//public Key ShortcutKey { get; set; }
		//public ModifierKeys ShortcutKeyModifier { get; set; }
		//public string ShortcutKeyElement { get; set; }
		//private FrameworkElement _shortcutKeyElement { get; set; }

		public bool VisualTreeGenerated { get; set; }

		public int HoverInDelay { get; set; }
		public int FadeInTime { get; set; }
		public Storyboard HoverInStoryboard { get; set; }
		private Timer _timerOpen;

		public int HoverOutDelay { get; set; }
		public int FadeOutTime { get; set; }
		public Storyboard HoverOutStoryboard { get; set; }
		private DispatcherTimer _timerClose;
		public bool IsClosing { get; set; }
		public bool IsOpening { get; set; }
		public static MouseEventArgs CapturedMouseEventArgs { get; set; }

		bool FocusOnShow { get; set; }
		bool CloseOnHoverOut { get; set; }

		protected Grid ParentOverlapGrid;
		public Brush ParentOverlapGridBrush { get; set; }
		public Thickness? ParentOverlapGridThickness { get; set; }


		private bool _isPinned;
		/// <summary>
		/// When set to true the only way to close the menu is to click again on the trigger element or by clicking on the menu itself.
		/// The outer canvas, which would otherwise be stretched over the window, is then collapsed around the menu.
		/// So this can be used to avoid blocking mouse events from elements which would normally be beneath it, but then
		/// all logic for outer clicks/hover is left on the developer to implement.
		/// </summary>
		public bool IsPinned
		{
			get { return _isPinned; }
			set
			{
				_isPinned = value;
				OuterCanvas.Width = !_isPinned ? Application.Current.Host.Content.ActualWidth : double.NaN;
				OuterCanvas.Height = !_isPinned ? Application.Current.Host.Content.ActualHeight : double.NaN;
			}
		}

		private bool _isModal;
		public bool IsModal
		{
			get { return _isModal; }
			set
			{
				_isModal = value;
				OuterCanvas.Background = _isModal ? (ModalBackground ?? new SolidColorBrush(Color.FromArgb(100, 100, 100, 100))) : new SolidColorBrush(Colors.Transparent);
			}
		}

		public Brush ModalBackground { get; set; }

		/// <summary>
		/// This event is called when the menu is being opened.
		/// </summary>
		public event EventHandler<MouseEventArgs> Opening;
		/// <summary>
		/// This event is called when the menu is open but is still transparent or at the initial state of its storyboard.
		/// Note that it is not recommended to add a submenu item in this event because references at this stage may already have been broken during the layouting process.
		/// It is therefore preferable to use Opening event which occurs just before that.</summary>
		public event EventHandler<MouseEventArgs> Showing;
		/// <summary>
		/// This event is called when the menu is open and is fully opaque or after its storyboard has completed.
		/// </summary>
		public event EventHandler<MouseEventArgs> Shown;
		/// <summary>
		/// This event is called when the menu is being closed.
		/// </summary>
		public event EventHandler<MouseEventArgs> Closing;

		private IEnumerable<UIElement> ClickedOrHoveredElements { get; set; }

		public FrameworkElement ActualTriggerElement { get; set; }
		public FrameworkElement ActuallyHoveredElement { get; set; }
		private static List<FrameworkElement> _hoverElements = new List<FrameworkElement>();
		public static List<PopupMenu> OpenMenus = new List<PopupMenu>();

		public double OffsetX { get; set; }
		public double OffsetY { get; set; }
		public bool ShowOnRight { get; set; }

		public bool PreserveItemContainerStyle { get; set; }

		public Effect ItemsControlEffect
		{
			set
			{
				this.Dispatcher.BeginInvoke(delegate
				{
					ItemsControl.Dispatcher.BeginInvoke(delegate
					{
						ItemsControl.Effect = value;
					});
				});
			}
		}

		public bool EnableItemsShadowEffect { get; set; }
		public Effect ItemsEffect
		{
			set
			{
				this.Dispatcher.BeginInvoke(delegate
				{
					ItemsControl.Dispatcher.BeginInvoke(delegate
					{
						foreach (FrameworkElement item in ItemsControl.Items)
							foreach (FrameworkElement itemChild in item.GetVisualDescendantsAndSelf())
								if (value != null && itemChild.Effect == null && !(itemChild is Panel))
									itemChild.Effect = value;
					});
				});
			}
		}

		public Popup MenuPopup { get; set; }
		public Canvas OuterCanvas { get; set; }
		public Grid RootGrid { get; set; }

		private ItemsControl _itemsControl;
		/// <summary>
		/// The ItemsControl, usually a ListBox, used to accomodate the menu items.
		/// Note that the setter method moves the PopupMenu content into the RootGrid.</summary>
		public ItemsControl ItemsControl
		{
			get // Get the first child of RootGrid
			{
				// If the PopupMenu has a content locate any ItemsControl within it, when not in design mode, and make it a child of RootGrid
				if (this.Content != null && this.Content != MenuPopup && !DesignerProperties.IsInDesignTool)
				{
					_itemsControl = (this.Content as FrameworkElement).GetVisualDescendantsAndSelf().OfType<ItemsControl>().FirstOrDefault();
					PopupMenuUtils.SetContent(RootGrid, this.Content as FrameworkElement, EnableItemsShadowEffect);
				}

				if (_itemsControl != null)
					return _itemsControl;
				else
					throw new ArgumentException("No ItemsControl element is referenced inside the PopupMenu " + this.Name + ". This is a required element.");
			}
			set // Make the ItemControl a child of RootGrid
			{
				_itemsControl = value;
				if (value.Parent != RootGrid)
					PopupMenuUtils.SetContent(RootGrid, _itemsControl, EnableItemsShadowEffect);
			}
		}


		bool _autoSelectItem = true;
		/// <summary>
		/// Automatically select ListBoxItem when hovered or clicked
		/// </summary>
		public bool AutoSelectItem
		{
			get { return _autoSelectItem; }
			set { _autoSelectItem = value; }
		}

		bool _autoMapToSelectedItem = true;
		/// <summary>
		/// Set ActualTriggerElement as the selected items of the trigger element if the latter derives from the Selector class(e.g ListBoxes, ComboBoxes and DataGrids)
		/// </summary>
		public bool AutoMapToSelectedItem
		{
			get { return _autoMapToSelectedItem; }
			set { _autoMapToSelectedItem = value; }
		}

		/// <summary>
		/// Extend the hover region to the parent element whenever a Textblock without a width value is used as trigger element.
		/// Since the width of the latter then depends on the text length it cannot be reliably used to determine where the menu should be closed.
		/// The default value is true.
		/// </summary>
		public bool AutoMapHoverBoundsToTextBlockParent { get; set; }

		/// <summary>
		/// A readonly collection of items in the ItemsControl used by the menu.
		/// To modify the collection use the ItemsControl.Items property instead.
		/// To add menu or submenus items use the AddItem or AddSubMenu functions instead.
		/// </summary>
		public ItemCollection Items
		{
			get { return ItemsControl.Items; }
		}

		#region Constructors

		public PopupMenu()
			: this(0, 0)
		{ }

		public PopupMenu(double offsetX, double offsetY)
			: this(null, null, offsetX, offsetY)
		{ }

		public PopupMenu(ItemsControl itemsControl)
			: this(itemsControl, null, 0, 0)
		{ }

		public PopupMenu(ItemsControl itemsControl, double offsetX, double offsetY)
			: this(itemsControl, null, offsetX, offsetY)
		{ }

		#endregion

		//[StyleTypedProperty(Property = "Style", StyleTargetType = typeof(PopupMenu))]
		public PopupMenu(ItemsControl itemsControl, Effect itemsEffect, double offsetX, double offsetY)
		{
			this.DefaultStyleKey = typeof(PopupMenu);
			// Default values
			AutoMapHoverBoundsToTextBlockParent = true;
			EnableItemsShadowEffect = true;
			CloseOnHoverOut = true;
			HoverInDelay = 200;
			HoverOutDelay = 300;
			FadeInTime = 150;
			FadeOutTime = 150;

			ItemsEffect = itemsEffect;
			OffsetX = offsetX;
			OffsetY = offsetY;

			// Closes the menu after a period of time when mouse moves outside the menu
			_timerClose = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HoverOutDelay) };

			// Puts all content on top of all other elements in the application
			MenuPopup = new Popup();
			// The canvas is stretched across the window when calling the Open method so as to capture any mouse activity
			// Note that the ability for the canvas to auto stretch is only enabled when its Background property contains a value(so be it as weird as it seems)
			OuterCanvas = new Canvas { Background = new SolidColorBrush(Colors.Transparent) }; // Color.FromArgb(30, 255, 0, 0)
			//OuterCanvas.KeyUp += AppRoot_KeyDown;
			// Acts as a container for the ItemsControl
			RootGrid = new Grid();

			//// Setup content elements in the following hierarchy: 
			//// this.Content >> MenuPopup >> OuterCanvas >> RootGrid
			OuterCanvas.Children.Add(RootGrid);
			MenuPopup.Child = OuterCanvas;
			this.Content = MenuPopup;

			ItemsControl = itemsControl ?? new ListBox { Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)) };

			AddMouseEventHandlersForOuterCanvas();

			if (!DesignerProperties.IsInDesignTool)
				this.Visibility = Visibility.Collapsed;

			this.Dispatcher.BeginInvoke(delegate
			{
				if (this.Parent != null)
					this.Parent.Dispatcher.BeginInvoke(delegate
					{
						if ((this.Parent as FrameworkElement).Parent != null)
							(this.Parent as FrameworkElement).Parent.Dispatcher.BeginInvoke(MenuLoaded);
						else
							MenuLoaded();
					});
				else
					MenuLoaded();
			});

		}

		///// <summary>
		///// Called when the template's tree is generated.
		///// </summary>
		//public override void OnApplyTemplate()
		//{
		//    base.OnApplyTemplate();
		//    //MenuPopup = GetTemplateChild("MenuPopup") as Popup;
		//    //OuterCanvas = GetTemplateChild("OuterCanvas") as Canvas;
		//    //RootGrid = GetTemplateChild("RootGrid") as Grid;
		//}

		private void MenuLoaded()
		{
			var root = Application.Current.RootVisual as FrameworkElement;
			// Delay mouse event assignments as far as possible using the MouseMove event to be sure the specified controls have been instanciated
			root.MouseMove += AddMarkupAssignedMouseTriggers;

			//if (!string.IsNullOrEmpty(ShortcutKeyElement))
			//    _shortcutKeyElement = Utils.FindApplicationElementByName(ShortcutKeyElement, "shortcut key");

			// If the parent element is a PopupMenuItem then show the menu on the right, disable its close on click behavior and set the parent as the hover trigger
			if (this.Parent is PopupMenuItem)
			{
				ShowOnRight = true;
				(this.Parent as PopupMenuItem).CloseOnClick = false;
				AddTrigger(TriggerTypes.Hover, this.Parent as FrameworkElement);
			}

			if (ItemsControl != null)
			{
				//ItemsControl.Dispatcher.BeginInvoke(delegate
				//{
				if (!PreserveItemContainerStyle && ItemsControl is ListBox)
				{
					ListBox lb = (ItemsControl as ListBox);
					// If the ItemContainerStyle exists then clone it for modification(as it cannot be modified directly) else create a new one
					Style style = lb.ItemContainerStyle != null ? new Style().BasedOn = lb.ItemContainerStyle : new Style(typeof(ListBoxItem));
					// Add a 'HorizontalContentAlignment = Stretch' setter if its not already there(note that "3" is the string value for HorizontalAlignment.Stretch)
					Setter setter = PopupMenuUtils.GetStyleSetter(style, ContentControl.HorizontalContentAlignmentProperty);
					if (setter == null || setter.Value.ToString() != "3")
					{
						setter = new Setter(ListBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch);
						style.Setters.Add(setter);
						lb.ItemContainerStyle = style; // Add the horizontal stretch style to the listbox items
					}
				}

				ItemsControl.UpdateLayout();
				//});
			}
		}

		private void AddMouseEventHandlersForOuterCanvas()
		{
			OuterCanvas.MouseMove += (sender, e) =>
			{
				CapturedMouseEventArgs = e;

				if (ActuallyHoveredElement != null && !IsModal)
				{
					var menuElements = RootGrid.Children[0].GetVisualChildren().OfType<FrameworkElement>().ToList();
					menuElements.Add(ActuallyHoveredElement); // Include the trigger element in the hit test so that the menu is not closed when the mouse is on it
					// I managed to figure out that the coordinate system, for elements within a Popup, does not take into account the browser zoom factor
					// This disrupted positioning for those elements when the browser zooming is activated
					// OpenMenus.Count is used here to determine if the menu element is actually inside a Popup so that the missing zoom can be recreated in code while performing the HitTest
					// This determines if mouse is not on the trigger element or on any of the menu elements(all elements initially set as content are now supposed to be in the RootGrid)
					if (!PopupMenuUtils.HitTestAny(e, OpenMenus.Count > 1, AutoMapHoverBoundsToTextBlockParent, false, menuElements.ToArray()))
					{
						if (!_timerClose.IsEnabled)
							_timerClose.Start(); // Delay closing of menu using to _timerClose.Tick
						else if (PopupMenuUtils.HitTestAny(e, OpenMenus.Count > 1, _hoverElements.Where(elem => elem != ActuallyHoveredElement).ToArray()))
							this.Close(0); // Close immediately when hovering another hover element
						else if (CloseOnHoverOut && IsOpening) // RootGrid.Width is set to zero when timerClose ticks
							this.Close(); // Close with default fadeout effect 
					}
				}
			};

			_timerClose.Tick += delegate // Used to delay closing of menus(it is started in the OuterCanvas.MouseMove event)
			{
				_timerClose.Stop();
				CloseHangingMenus(CapturedMouseEventArgs);
			};

			OuterCanvas.MouseRightButtonDown += (sender, e) => // Close the menu when a right click is made outside the popup
			{
				if (!IsModal && !PopupMenuUtils.HitTest(e, OpenMenus.Count > 1, RootGrid))
					this.Close(0);
				e.Handled = true; // Disable the default silverlight context menu
			};

			OuterCanvas.MouseLeftButtonDown += (sender, e) => // Close the menu when a left click is captured outside the popup
			{
				if (!IsModal)
				{
					CloseHangingMenus(e);
					if (!IsPinned)
						this.Close(0);
				}
				//e.Handled = true;
			};
		}

		public static void CloseHangingMenus(MouseEventArgs e)
		{
			// Close all open menus starting from the last one opened to the one actually being hovered or whose trigger element is being hovered
			foreach (PopupMenu menu in OpenMenus)
				if (!PopupMenuUtils.HitTestAny(e, true, false, true, menu.RootGrid, menu.ActuallyHoveredElement))
					menu.Close();
				else
					break;
		}

		private void AddMarkupAssignedMouseTriggers(object sender, MouseEventArgs e)
		{
			(sender as UIElement).MouseMove -= AddMarkupAssignedMouseTriggers; // Make sure this method is not called again by RootVisual.MouseEnter

			if (!string.IsNullOrEmpty(HoverElements))
				AddTrigger(TriggerTypes.Hover, HoverElements);

			if (!string.IsNullOrEmpty(RightClickElements))
				AddTrigger(TriggerTypes.RightClick, RightClickElements);

			if (!string.IsNullOrEmpty(LeftClickElements))
				AddTrigger(TriggerTypes.LeftClick, LeftClickElements);
		}

		#region AddItem

		public PopupMenuItem AddItem(FrameworkElement item)
		{
			return InsertItem(-1, null, item, null, null, null);
		}

		public PopupMenuItem AddItem(string header, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, null, header, null, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, item, clickHandler);
		}

		public PopupMenuItem AddItem(bool showLeftMargin, FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, showLeftMargin, item, clickHandler);
		}

		public PopupMenuItem AddItem(string iconUrl, FrameworkElement item, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, iconUrl, item, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(string iconUrl, string header, string tag, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, iconUrl, new TextBlock() { Text = header, Tag = tag }, null, null, clickHandler);
		}

		public PopupMenuItem AddItem(string leftIconUrl, string header, string rightIconUrl, string tag, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, null, clickHandler);
		}

		public PopupMenuItem AddItem(string leftIconUrl, string header, string rightIconUrl, string tag, string name, RoutedEventHandler clickHandler)
		{
			return InsertItem(-1, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, clickHandler);
		}

		#endregion

		#region InsertItem

		public PopupMenuItem InsertItem(int index, FrameworkElement item)
		{
			return InsertItem(index, item, null);
		}

		public PopupMenuItem InsertItem(int index, string header, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, null, new TextBlock() { Text = header }, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, FrameworkElement item, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, null, item, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, bool showLeftMargin, FrameworkElement item, RoutedEventHandler leftClickHandler)
		{
			PopupMenuItem pmi = InsertItem(index, null, item, null, null, leftClickHandler);
			pmi.ShowLeftMargin = showLeftMargin;
			return pmi;
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, FrameworkElement item, string tag, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, item, null, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, string header, string rightIconUrl, string tag, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, null, leftClickHandler);
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, string header, string rightIconUrl, string tag, string name, RoutedEventHandler leftClickHandler)
		{
			return InsertItem(index, leftIconUrl, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, leftClickHandler);
		}
		#endregion

		public PopupMenuItem AddSubMenu(PopupMenu subMenu, string header, string rightIconUrl, string tag, string name, bool closeOnClick, RoutedEventHandler clickHandler)
		{
			return InsertSubMenu(-1, subMenu, header, rightIconUrl, tag, name, closeOnClick, clickHandler);
		}

		public PopupMenuItem InsertSubMenu(int index, PopupMenu subMenu, string header, string rightIconUrl, string tag, string name, bool closeOnClick, RoutedEventHandler clickHandler)
		{
			PopupMenuItem pmi = InsertItem(index, null, new TextBlock() { Text = header, Tag = tag }, rightIconUrl, name, null);
			pmi.CloseOnClick = closeOnClick;
			subMenu.ShowOnRight = true;
			subMenu.AddTrigger(TriggerTypes.Hover, pmi);
			return pmi;
		}

		public PopupMenuItem InsertItem(int index, string leftIconUrl, FrameworkElement item, string rightIconUrl, string name, RoutedEventHandler clickHandler)
		{
			if (item.Parent != null)
				(item.Parent as Panel).Children.Remove(item);

			PopupMenuItem popupMenuItem = item is PopupMenuItem ? item as PopupMenuItem : new PopupMenuItem(leftIconUrl, item);

			if (clickHandler != null)
				popupMenuItem.Click += clickHandler;

			if (rightIconUrl != null)
				popupMenuItem.ImagePathForRightMargin = rightIconUrl;

			if (name != null)
			{
				if (ItemsControl.Items.OfType<FrameworkElement>().Where(i => i.Name == name).Count() == 0)
					popupMenuItem.Name = name;
				else
					throw new ArgumentException("An item named " + name + " already exists in the PopupMenu " + this.Name);
			}

			ItemsControl.Items.Insert(index == -1 ? ItemsControl.Items.Count : index, popupMenuItem);

			return popupMenuItem;
		}

		public void RemoveAt(int index)
		{
			ItemsControl.Items.RemoveAt(index);
		}

		public void Remove(ContentControl item)
		{
			ItemsControl.Items.Remove(item);
		}


		public void AddLeftClickElements(params UIElement[] triggerElements)
		{
			AddTrigger(TriggerTypes.LeftClick, triggerElements);
		}

		public void AddHoverElements(params UIElement[] triggerElements)
		{
			AddTrigger(TriggerTypes.Hover, triggerElements);
		}

		public void AddRightClickElements(params UIElement[] triggerElements)
		{
			AddTrigger(TriggerTypes.RightClick, triggerElements);
		}

		public void AddTrigger(TriggerTypes triggerType, string triggerElementNames)
		{
			foreach (string elementName in triggerElementNames.Split(','))
			{
				var elem = PopupMenuUtils.FindApplicationElementByName(elementName, "trigger type " + triggerType);
				if (elem != null)
					AddTrigger(triggerType, elem as UIElement);
			}
		}

		public void AddTrigger(TriggerTypes triggerType, params UIElement[] triggerElements)
		{
			foreach (FrameworkElement triggerElement in triggerElements.Where(elem => elem != null))
			{
				// If the trigger element is a PopupMenuItem in an ItemsControl then refer to its containing ContentControl instead.
				if (triggerElement is PopupMenuItem
					&& triggerElement.Parent is ItemsControl && GetContainer<Popup>(triggerElement) != null)
				{
					Popup popup = GetContainer<Popup>(triggerElement);
					// The call is delegated to the Popup.Opened event before which our ContentControl is not accessible 
					EventHandler ev = delegate
					{
						popup.Dispatcher.BeginInvoke(delegate
						{
							AddTrigger(triggerType, GetContainer<ContentControl>(triggerElement));
						});
					};
					popup.Opened += ev;
					popup.Opened += delegate { popup.Opened -= ev; }; // Remove delegate ounce it has been called.
				}
				else
				{
					switch (triggerType)
					{
						case TriggerTypes.RightClick:
							triggerElement.MouseRightButtonDown += (sender, e) => { e.Handled = true; }; // Disable the default silverlight context menu
							triggerElement.MouseRightButtonUp += TriggerElement_RightClick;
							break;

						case TriggerTypes.LeftClick:
							// The Click event is used for buttons since they do not seem to trigger the MouseLeftButton events
							if (triggerElement is ButtonBase)
								(triggerElement as ButtonBase).Click += TriggerButton_Click;
							// Objects like the ListBox, ComboBox, ListBoxItem and the ComboBoxItem do not capture MouseLeftButtonDown. MouseLeftButtonUp is then used instead
							else if (triggerElement is ListBoxItem || triggerElement is Selector)
								triggerElement.MouseLeftButtonUp += TriggerElement_LeftClick;
							else
								triggerElement.MouseLeftButtonDown += TriggerElement_LeftClick;
							break;

						case TriggerTypes.Hover:
							triggerElement.MouseEnter += TriggerElement_Hover;
							_hoverElements.Add(triggerElement);
							break;
					}
				}
			}
		}

        public void AddTrigger(TriggerTypes triggerType, UIElement triggerUiElement)
        {
            FrameworkElement triggerElement = triggerUiElement as FrameworkElement;
            //foreach (FrameworkElement triggerElement in triggerElements.Where(elem => elem != null))
            //{
                // If the trigger element is a PopupMenuItem in an ItemsControl then refer to its containing ContentControl instead.
                if (triggerElement is PopupMenuItem
                    && triggerElement.Parent is ItemsControl && GetContainer<Popup>(triggerElement) != null)
                {
                    Popup popup = GetContainer<Popup>(triggerElement);
                    // The call is delegated to the Popup.Opened event before which our ContentControl is not accessible 
                    EventHandler ev = delegate
                    {
                        popup.Dispatcher.BeginInvoke(delegate
                        {
                            AddTrigger(triggerType, GetContainer<ContentControl>(triggerElement));
                        });
                    };
                    popup.Opened += ev;
                    popup.Opened += delegate { popup.Opened -= ev; }; // Remove delegate ounce it has been called.
                }
                else
                {
                    switch (triggerType)
                    {
                        case TriggerTypes.RightClick:
                            triggerElement.MouseRightButtonDown += (sender, e) => { e.Handled = true; }; // Disable the default silverlight context menu
                            triggerElement.MouseRightButtonUp += TriggerElement_RightClick;
                            break;

                        case TriggerTypes.LeftClick:
                            // The Click event is used for buttons since they do not seem to trigger the MouseLeftButton events
                            if (triggerElement is ButtonBase)
                                (triggerElement as ButtonBase).Click += TriggerButton_Click;
                            // Objects like the ListBox, ComboBox, ListBoxItem and the ComboBoxItem do not capture MouseLeftButtonDown. MouseLeftButtonUp is then used instead
                            else if (triggerElement is ListBoxItem || triggerElement is Selector)
                                triggerElement.MouseLeftButtonUp += TriggerElement_LeftClick;
                            else
                                triggerElement.MouseLeftButtonDown += TriggerElement_LeftClick;
                            break;

                        case TriggerTypes.Hover:
                            triggerElement.MouseEnter += TriggerElement_Hover;
                            _hoverElements.Add(triggerElement);
                            break;
                    }
                }
            //}
        }

		public void TriggerButton_Click(object sender, RoutedEventArgs e)
		{
			Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(OpenMenus.Count == 0, sender as FrameworkElement);
			Open(mousePos, ShowOnRight, 0, sender as FrameworkElement, e as MouseButtonEventArgs);
		}

		public void TriggerElement_RightClick(object triggerElement, MouseButtonEventArgs e)
		{
			var element = GetItemUnderMouse(triggerElement as FrameworkElement, e, AutoMapToSelectedItem, AutoSelectItem)
				?? triggerElement as FrameworkElement;
			Point mousePos = e.GetSafePosition(null);
			Open(mousePos, null, 0, element, e);
		}

		public void TriggerElement_LeftClick(object triggerElement, MouseButtonEventArgs e)
		{
			if (MenuPopup.IsOpen)
			{
				this.Close();
			}
			else
			{
				var element = GetItemUnderMouse(triggerElement as FrameworkElement, e, AutoMapToSelectedItem, AutoSelectItem);
				if (element != null)
				{
					Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(OpenMenus.Count == 0, element);
					Open(mousePos, ShowOnRight, 0, element, e);
				}
				else
				{
					ActualTriggerElement = triggerElement as FrameworkElement; // This would normally be set in the Open method
				}
			}
		}

		public void TriggerElement_Hover(object triggerElement, MouseEventArgs e)
		{
			if (_timerClose != null)
				_timerClose.Stop();

			var element = GetItemUnderMouse(triggerElement as FrameworkElement, e, AutoMapToSelectedItem, AutoSelectItem);
			if (element != null)
			{
				ActuallyHoveredElement = triggerElement as FrameworkElement;
				Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(OpenMenus.Count == 0, element);
				Open(mousePos, ShowOnRight, HoverInDelay, element, e);
			}
		}

		public void OpenNextTo(FrameworkElement triggerElement, bool showOnRight)
		{
			Point mousePos = PopupMenuUtils.GetAbsoluteElementPos(OpenMenus.Count == 0, triggerElement);
			this.Open(mousePos, showOnRight, 0, triggerElement, null);
		}

		public void Open(Point mousePos, bool showOnRight, int delay, FrameworkElement triggerElement, MouseEventArgs e)
		{
			Point offset = showOnRight ? new Point(triggerElement.ActualWidth - 1, 0)
									   : new Point(0, triggerElement.ActualHeight);

			this.Open(mousePos, offset, delay, triggerElement, e);
		}

		public void Open(Point mousePos, Point? offset, int delay, FrameworkElement triggerElement, MouseEventArgs e)
		{
			ActualTriggerElement = triggerElement;

			if (!MenuPopup.IsOpen)
			{
				if (ParentOverlapGridBrush != null)
					ShowParentOverlapGrid(triggerElement);

				RootGrid.Margin = new Thickness(
						mousePos.X + OffsetX + (offset.HasValue ? offset.Value.X : 0),
						mousePos.Y + OffsetY + (offset.HasValue ? offset.Value.Y : 0),
						0,
						0);

				if (!VisualTreeGenerated)
					ItemsControl.SizeChanged += KeepMenuWithinLayoutBounds; // Make sure the menu stays within root layout bounds

				if (ItemsControl is ListBox)
					(ItemsControl as ListBox).SelectedIndex = -1; // Reset selected item in menu

				if (Opening != null)
					Opening(triggerElement, e);

				MenuPopup.IsOpen = true;
				if (!OpenMenus.Contains(this))
					OpenMenus.Insert(0, this); // Add the actual menu on top of the list of open menus

				RootGrid.Width = 0;
				IsOpening = true;

				// Just call the setter of the IsPinned property to resize OuterCanvas appropriately
				IsPinned = _isPinned;

				// Start opening the menu only after a period of time specified by the delay parameter in milliseconds
				// During this period, if the OuterCanvas is hovered, the menu is closed via the Close method.
				_timerOpen = new Timer(delegate
				{
					MenuPopup.Dispatcher.BeginInvoke(delegate()
					{
						// If menu has not already been closed by hovering on OuterCanvas
						if (MenuPopup.IsOpen)
						{
							IsOpening = false;
							RootGrid.Width = double.NaN;

							if (Showing != null)
								Showing(triggerElement, e);

							(HoverInStoryboard ?? PopupMenuUtils.CreateStoryBoard(0, FadeInTime, RootGrid, "UIElement.Opacity", 1, null, true)).Completed += delegate
							{
								if (Shown != null)
									Shown(triggerElement, e);
							};

							if (FocusOnShow)
								ItemsControl.Focus();
						}
					});
				}, null, delay, Timeout.Infinite);

				VisualTreeGenerated = true;
			}
		}

		private void ShowParentOverlapGrid(FrameworkElement triggerElement)
		{
			if (ParentOverlapGrid == null)
			{
				ParentOverlapGrid = new Grid { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
				RootGrid.Children.Add(ParentOverlapGrid);
			}

			if (ShowOnRight)
			{
				if (!ParentOverlapGridThickness.HasValue)
					ParentOverlapGridThickness = new Thickness(1, -1, 1, 3);

				ParentOverlapGrid.Height = triggerElement.ActualHeight + ParentOverlapGridThickness.Value.Top + ParentOverlapGridThickness.Value.Bottom;
				ParentOverlapGrid.Width = ParentOverlapGridThickness.Value.Left + ParentOverlapGridThickness.Value.Right;
				OffsetX = 2;
			}
			else
			{
				if (!ParentOverlapGridThickness.HasValue)
					ParentOverlapGridThickness = new Thickness(0, 1, 0, 1);

				ParentOverlapGrid.Height = ParentOverlapGridThickness.Value.Top + ParentOverlapGridThickness.Value.Bottom;
				ParentOverlapGrid.Width = triggerElement.ActualWidth + ParentOverlapGridThickness.Value.Left + ParentOverlapGridThickness.Value.Right;
			}

			ParentOverlapGrid.Margin = new Thickness(-ParentOverlapGridThickness.Value.Left, -ParentOverlapGridThickness.Value.Top, 0, 0);
			ParentOverlapGrid.Background = ParentOverlapGridBrush;
		}

		private void KeepMenuWithinLayoutBounds(object sender, SizeChangedEventArgs e)
		{
			Thickness margin = RootGrid.Margin;
			if (margin.Left + ItemsControl.ActualWidth > Application.Current.Host.Content.ActualWidth)
			{
				margin.Left = Application.Current.Host.Content.ActualWidth - ItemsControl.ActualWidth;
				RootGrid.Margin = margin;
			}

			if (margin.Top + ItemsControl.ActualHeight > Application.Current.Host.Content.ActualHeight)
			{
				margin.Top = Application.Current.Host.Content.ActualHeight - ItemsControl.ActualHeight;
				RootGrid.Margin = margin;
			}
		}

		/// <summary>
		/// Get the item under the mouse
		/// </summary>
		/// <param name="senderElement">The clicked or hovered element</param>
		/// <param name="returnSelectableItemIfAny">Return the clicked or hovered item inside the trigger element if the latter is a DataGrid, a ListBox or a TreeView</param>
		/// <param name="selectItemIfSelectable">Set the selected property the ListBox or Datagrid item to true if found</param>
		/// <returns>The item under the mouse</returns>
		private FrameworkElement GetItemUnderMouse(FrameworkElement senderElement, MouseEventArgs e, bool returnSelectableItemIfAny, bool selectItemIfSelectable)
		{
			ClickedOrHoveredElements = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(Application.Current.RootVisual), senderElement.GetVisualAncestors().Last() as FrameworkElement);

			FrameworkElement elem = ClickedOrHoveredElements.OfType<ListBoxItem>().FirstOrDefault();
			if (senderElement is ListBox || elem != null)
			{
				if (senderElement is ListBox && elem == null) // element is a ListBox with no selected item
					return null;
				if (selectItemIfSelectable)
					(elem as ListBoxItem).IsSelected = true;
				if (returnSelectableItemIfAny)
					senderElement = elem;
			}
			else if (senderElement is DataGrid)
			{
				if ((elem = ClickedOrHoveredElements.OfType<DataGridRow>().FirstOrDefault()) == null) // no DataGridRow was clicked upon
					return null;
				if (selectItemIfSelectable)
					(senderElement as DataGrid).SelectedIndex = (elem as DataGridRow).GetIndex();
				if (returnSelectableItemIfAny)
					senderElement = elem;
			}
			else if (senderElement is TreeView)
			{
				if ((elem = ClickedOrHoveredElements.OfType<TreeViewItem>().FirstOrDefault()) == null) // no TreeViewItem was clicked upon
					return null;
				if (selectItemIfSelectable)
					(senderElement as TreeView).GetContainerFromItem(elem).IsSelected = true;
				if (returnSelectableItemIfAny)
					senderElement = elem;
			}
			return senderElement;
		}

		public static void CloseAllOpenMenus()
		{
			foreach (PopupMenu menu in OpenMenus)
				if (!menu.IsPinned)
					menu.Close();
		}

		public void Close()
		{
			Close(FadeOutTime);
		}

		public void Close(int transitionTime)
		{
			if (_timerOpen != null)
			{
				_timerOpen.Change(0, Timeout.Infinite);
				_timerOpen.Dispose();
				_timerOpen = null;
			}

			IsClosing = true;

			if (Closing != null)
				Closing(this, CapturedMouseEventArgs);

			Storyboard sbClose = HoverOutStoryboard ?? PopupMenuUtils.CreateStoryBoard(0, transitionTime, RootGrid, "UIElement.Opacity", 0, null);
			sbClose.Begin();
			sbClose.Completed += delegate
			{
				MenuPopup.IsOpen = false;
				OpenMenus.Remove(this);
				IsClosing = false;
			};

			if (AutoSelectItem && ActualTriggerElement != null)
			{
				if (ActualTriggerElement is ListBoxItem)
					(ActualTriggerElement as ListBoxItem).IsSelected = false;
				if (ActualTriggerElement is PopupMenuItem)
					(GetContainer<ListBoxItem>(ActualTriggerElement)).IsSelected = false;
			}

			ActualTriggerElement = null;
			ActuallyHoveredElement = null;

			if (_timerClose != null)
				_timerClose.Stop();
		}

		#region Items Manipulation

		public ContentControl GetContentControlItem(int index)
		{
			return (ContentControl)(ItemsControl.ItemContainerGenerator.ContainerFromItem(ItemsControl.Items[index]));
		}

		public PopupMenuItem PopupMenuItem(string name)
		{
			return GetItem<PopupMenuItem>(name);
		}

		public PopupMenuItem PopupMenuItem(int index)
		{
			return GetItem<PopupMenuItem>(index);
		}

		/// <summary>
		/// Get the object of type T in the menu control by index position.
		/// </summary>
		/// <typeparam name="T">The object type</typeparam>
		/// <param name="index">The object  index</param>
		public T GetItem<T>(int index) where T : class
		{
			T item = (ItemsControl.Items[index] as FrameworkElement).GetVisualDescendantsAndSelf()
				.Where(i => i is T)
				.Select(i => i as T).FirstOrDefault();

			if (item == default(T))
				throw new Exception(string.Format("{0} at item {1} is not of type {2}", ItemsControl.Items[index].GetType(), index, typeof(T).ToString()));
			else
				return item;
		}

		public T GetChildItem<T>(UIElement element)
		{
			foreach (object item in (element as UIElement).GetVisualChildren())
			{
				if (item != null && item is T)
					return (T)item;
			}
			return default(T);
		}

		/// <summary>
		/// Get the last clicked element associated with any of the PopupMenu control trigger events.
		/// </summary>
		/// <typeparam name="T">The type of the object</typeparam>
		public T GetClickedElement<T>()
		{
			return GetElement<T>(ClickedOrHoveredElements);
		}

		private static T GetElement<T>(IEnumerable<UIElement> elements)
		{
			return elements == null ? default(T) : elements.OfType<T>().FirstOrDefault();
		}

		private static T GetElement<T>(IEnumerable<UIElement> elements, int index)
		{
			foreach (object elem in elements.OfType<T>())
				if (index-- <= 0)
					return (T)elem;
			return default(T);
		}

		/// <summary>
		/// Find a ContentControl having elements with a specific tag value.
		/// This method only works after the visual tree has been created.
		/// </summary>
		public ContentControl FindItemContainerByTag(object tag)
		{
			return FindItemContainersByTag(tag).FirstOrDefault();
		}

		/// <summary>
		/// Find a list of ContentControls having elements with a specific tag value.
		/// This method only works after the visual tree has been created.
		/// </summary>
		public List<ContentControl> FindItemContainersByTag(object tag)
		{
			return FindItemsByTag<FrameworkElement>(tag).Select(e => GetContainer<ContentControl>(e)).ToList();
		}

		public T FindItemByTag<T>(object tag) where T : class
		{
			return FindItemsByTag<T>(tag).FirstOrDefault();
		}

		/// <summary>
		/// Find a list of ContentControls having elements with any of the tags specified.
		/// This method only works after the visual tree has been created.
		/// </summary>
		/// <param name="tags">A comma delimited list of tags that will be used as identifier.</param>
		public List<T> FindItemsByTag<T>(params object[] tags) where T : class
		{
			List<T> elements = new List<T>();
			foreach (object tag in tags)
			{
				//foreach (FrameworkElement item in Items)
				//    foreach (T element in item.GetVisualChildrenAndSelf().OfType<T>())
				//        if ((element as FrameworkElement).Tag != null && (element as FrameworkElement).Tag.Equals(tag))
				//            elements.Add(element as T);
				// If no element was found search all the visual tree instead(only works after the latter has been created)
				//if (elements.Count == 0)
				elements = ItemsControl.GetVisualDescendantsAndSelf().OfType<T>()
					.Where(i => i is FrameworkElement
						&& (i as FrameworkElement).Tag != null
						&& (i as FrameworkElement).Tag.Equals(tag))
					.Select(i => i as T).ToList();
			}
			return elements;
		}

		/// <summary>
		/// Find the ContentControl containing a control with a name matching a regex pattern.
		/// This method only works after the visual tree has been created.
		/// </summary>
		/// <param name="regexPattern">The regex pattern to match the element name</param>
		public ContentControl FindItemContainerByName(string regexPattern)
		{
			return FindItemsByName<FrameworkElement>(regexPattern).Select(e => GetContainer<ContentControl>(e)).FirstOrDefault();
		}

		/// <summary>
		/// Find the ContentControls containing controls with names matching a regex pattern.
		/// This method only works after the visual tree has been created.
		/// </summary>
		/// <param name="regexPattern">The regex pattern to match the element names</param>
		public List<ContentControl> FindItemContainersByName(string regexPattern)
		{
			return FindItemsByName<FrameworkElement>(regexPattern).Select(e => GetContainer<ContentControl>(e)).ToList();
		}

		public T FindItemByName<T>(string regexPattern) where T : class
		{
			return FindItemsByName<T>(regexPattern).FirstOrDefault();
		}

		public List<T> FindItemsByName<T>(string regexPattern) where T : class
		{
			List<T> elements = new List<T>();
			//foreach (FrameworkElement item in Items)
			//    foreach (T element in item.GetVisualChildrenAndSelf().OfType<T>())
			//        if ((new Regex(regexPattern).IsMatch((element as FrameworkElement).Name ?? "")))
			//            elements.Add(element as T);

			//// If no element was found search all the visual tree instead(only works after the latter has been created)
			//if (elements.Count == 0)
			elements = ItemsControl.GetVisualDescendantsAndSelf().OfType<T>()
				.Where(i => (new Regex(regexPattern).IsMatch((i as FrameworkElement).Name ?? "")))
				.Select(i => i as T).ToList();

			return elements;
		}

		public static T GetContainer<T>(FrameworkElement item) where T : class
		{
			T container = item.GetVisualAncestors().OfType<T>().FirstOrDefault();
			if (container == null)
			{
				FrameworkElement elem = item;
				while (!(elem is T) && elem != null && elem.Parent != null)
				{
					if (elem.Parent is ItemsControl && (elem.Parent as ItemsControl).ItemContainerGenerator.ContainerFromItem(elem) != null)
						elem = (elem.Parent as ItemsControl).ItemContainerGenerator.ContainerFromItem(elem) as FrameworkElement;
					else
						elem = elem.Parent as FrameworkElement;
				}
				if (elem != null)
					container = elem as T;
			}
			return container;
		}

		public T GetItem<T>(string name)
		{
			return Items.GetVisualDescendantsAndSelf()
				.Where(i => i is T && (i as FrameworkElement).Name == name)
				.Select(i => (T)(i as object)).First();
		}

		public void AddSeparator()
		{
			AddSeparator(null);
		}

		public void AddSeparator(string tag)
		{
			ItemsControl.Items.Add(new PopupMenuSeparator(tag));
		}

		public void InsertSeparator(int index, string tag)
		{
			ItemsControl.Items.Insert(index, new PopupMenuSeparator(tag));
		}

		public void SetVisibilityByTag(string tag, bool visible)
		{
			foreach (ContentControl item in FindItemContainersByTag(tag))
				if (item != null)
					item.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}
		#endregion


	}
}

