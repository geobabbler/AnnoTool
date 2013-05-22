using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
 
namespace Utilities
{
    public partial class ContextMenu : UserControl
    {
        private bool hasMouseFocus = false;
        private DispatcherTimer closeMenusTimer;

        public ContextMenu()
        {
            InitializeComponent();
        }

        private void ContextMenu_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = (Button)sender;

                if (button.Tag != null && button.Tag is ContextMenu)
                {
                    DebugConsole.debug("MouseLeftButtonDown over nested ContextMenu");
                    ContextMenu cm = (ContextMenu)button.Tag;
                    double x = Canvas.GetLeft(ContextMenuStackPanel) + this.ContextMenuStackPanel.Width - 2;
                    double y = Canvas.GetTop(ContextMenuStackPanel) + ContextMenuStackPanel.Children.IndexOf(button) * button.ActualHeight;
                    
                    cm.SetPosition(x, y);
                    cm.Visibility = Visibility.Visible;
                }
                else
                    this.Visibility = Visibility.Collapsed;

                // if the user hovers over a different button we need to close any open child context menus
                foreach (object o in ContextMenuStackPanel.Children)
                {
                    Button tempButton = (Button)o;
                    if (tempButton != button)
                    {
                        if (tempButton.Tag != null && tempButton.Tag is ContextMenu)
                            ((ContextMenu)tempButton.Tag).CloseAll();
                    }
                }
            }
            catch (Exception ee)
            {
                DebugConsole.debug(ee.ToString());
            }
        }

        private void ContextMenuStackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            //DebugConsole.debug("Mouse Enter");

            hasMouseFocus = true;
        }

        private void SymbolContextMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            //DebugConsole.debug("Mouse Leave");

            hasMouseFocus = false;

            closeMenusTimer = new DispatcherTimer();
            closeMenusTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            closeMenusTimer.Tick += new EventHandler(timer_Tick);
            closeMenusTimer.Start();
            //this.Visibility = Visibility.Collapsed;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            CloseAll();
            closeMenusTimer.Stop();
        }

        // Having problems using this function recursively.  The problem is that calling this function closes ALL menus,
        // including the nested menu that user may be trying navigate to.  
        public bool CloseAll()
        {
            if (hasMouseFocus)
                return false;

            foreach (Button b in ContextMenuStackPanel.Children)
            {
                if (b.Tag != null && b.Tag is ContextMenu)
                {
                    if (!((ContextMenu)b.Tag).CloseAll())
                        return false;
                }
            }
            this.Visibility = Visibility.Collapsed;
            return true;
        }

        public void SetPosition(double x, double y)
        {
            Canvas.SetLeft(ContextMenuStackPanel, x);
            Canvas.SetTop(ContextMenuStackPanel, y);
            //Canvas.SetZIndex(ContextMenuStackPanel,100);
        }

        public void SetWidth(int width)
        {
            ContextMenuStackPanel.Width = width;
        }

        public Button CreateButton(Button button, string text, object tag, RoutedEventHandler clickHandler)
        {
            button.Tag = tag;
            button.Content = button.Name = text;
            button.Click += ContextMenu_MouseLeftButtonDown;
            button.Click += clickHandler;
            button.Style = this.Resources["MenuStyleButton"] as Style;

            if (tag != null && tag is ContextMenu)
                button.Content += " ►";

            ContextMenuStackPanel.Children.Add(button);

            return button;
        }

        public Button CreateButton(string text, object tag, RoutedEventHandler clickHandler)
        {
            return CreateButton(new Button(), text, tag, clickHandler);
        }

        public void Clear()
        {
            ContextMenuStackPanel.Children.Clear();
        }

    }
}
