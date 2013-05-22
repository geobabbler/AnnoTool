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
//using Infusion.Silverlight.Controls;
//using Infusion.Silverlight.Controls.Ribbon;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Toolkit;
//using Main.Helpers;
//using CustomLayers.Management;
//using Utilities;
using Environment;
using SL4PopupMenu;
//using Main.Windows;

namespace Zekiah.Samples
{
    public class DrawTextButton : ToolbarItem // RibbonButtonGroupButton, IMapTool
    {
        //MapHelper _map = null;
        Draw _draw = null;
        bool _activated = false;
        //IMapController _mapController = null;
        //private Utilities.ContextMenu _contextMenu;
        //private Graphic SelectedSymbol = null;
        private MapPoint _currentPoint = null;
        private bool _init = false;
        private PopupMenu _mnu = null;

        public DrawTextButton()
        {
            //this.Text = "Rectangle";
            //this.ImageSource = "Images/TextBox.png";
            //this.IsToggleButton = false;
            //this.ButtonClick += ButtonClickHandler;
            //System.Windows.Controls.ToolTipService.SetToolTip(this, "Add text annotation");
            ////_contextMenu = new Utilities.ContextMenu();
            ////_contextMenu.CreateButton("Delete Text", null, RemoveTextHandler);
            ////_contextMenu.CreateButton("Change Text", null, ChangeTextHandler);

            //_contextMenu = new ContextMenu();
            //MenuItem itmDelete = new MenuItem();
            //var hdr = itmDelete as System.Windows.Controls.HeaderedItemsControl;
            //hdr.Header = "Delete Text";
            //itmDelete.Click += new RoutedEventHandler(RemoveTextHandler);
            //MenuItem itmChange = new MenuItem();
            //var hdrChange = itmChange as System.Windows.Controls.HeaderedItemsControl;
            //hdrChange.Header = "Change Text";
            //itmChange.Click += new RoutedEventHandler(ChangeTextHandler);
            //Viewbox box = new Viewbox();
            //TextBlock blk = new TextBlock();
            //blk.FontFamily = new FontFamily("Arial");
            //blk.FontSize = 12;
            //blk.Text = "Test";
            //box.Child = blk;
            //itm.Icon = box;
            //_contextMenu.Items.Add(itmDelete);
            //_contextMenu.Items.Add(itmChange);
            //_contextMenu.CreateButton("Annotation Operations", AppManager.MapContextMenus.AnnoContextMenu, null);
            //try
            //{
            //    Environment.Ambient.LayoutRoot.Children.Add(_contextMenu);
            //}
            //catch { }
            //AppManager.Map.MouseClick += new EventHandler<Map.MouseEventArgs>(map_MouseClick);
            //AppManager.Map.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(_map_MouseRightButtonDown);
            //_map = map;
            Ambient.ChangeText += new AnnoEventHandler(ChangeTextHandler);
            Ambient.RemoveText += new AnnoEventHandler(RemoveTextHandler);
            buildPopup();

        }

        public Map BoundMap { get; set; }
        //public Panel LayoutRoot { get; set; }

        private void ButtonClickHandler(object sender, MouseButtonEventArgs e)
        {
            //this.IsSelected = false;
            //_mapController.CurrentTool = (IMapTool)this;
            //_map.ZoomTo(_map.Layers.GetFullExtent());
        }

        private void ChangeTextHandler(object sender, RoutedEventArgs e)
        {
            //Graphic g = sender as Graphic;
            _currentPoint = Ambient.SelectedSymbol.Geometry as MapPoint;
            TextSymbol ts = Ambient.SelectedSymbol.Symbol as TextSymbol;
            TextSymbolPropsWindow win = new TextSymbolPropsWindow();
            win.TextFont = ts.FontFamily.Source;
            win.TextFontSize = ts.FontSize;
            win.Annotation = ts.Text;
            win.EditMode = true;
            win.Closed += new EventHandler(win_Closed);
            win.Show();
        }

        private void RemoveTextHandler(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Are you sure you want to delete this text? Click 'OK' to confirm.", "Confirm Delete", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                RemoveSymbol(Ambient.SelectedSymbol);
            }
        }

        public void RemoveSymbol(Graphic marker)
        {
            GraphicsLayer graphicsLayer = BoundMap.Layers["AnnoLayer"] as GraphicsLayer;
            graphicsLayer.Graphics.Remove(marker);
        }

        private void DrawCompleteHandler(object sender, DrawEventArgs args)
        {
            _currentPoint = args.Geometry as MapPoint;
            TextSymbolPropsWindow win = new TextSymbolPropsWindow();
            win.EditMode = false;
            win.Closed += new EventHandler(win_Closed);
            win.Show();
        }

        void win_Closed(object sender, EventArgs e)
        {
            TextSymbolPropsWindow win = sender as TextSymbolPropsWindow;
            if ((bool)win.DialogResult)
            {
                GraphicsLayer graphicsLayer = BoundMap.Layers["AnnoLayer"] as GraphicsLayer;
                string input = win.Annotation; 

                if (!String.IsNullOrEmpty(input))
                {
                    MapPoint pt = _currentPoint;
                    TextSymbol sym = new TextSymbol();
                    sym.FontFamily = new FontFamily(win.TextFont); 
                    sym.FontSize = win.TextFontSize; 
                    sym.Text = win.Annotation; 
                    sym.Foreground = new SolidColorBrush { Color = Colors.Black };
                    Zekiah.Samples.Font f = new Font();
                    f.Family = sym.FontFamily;
                    f.Size = sym.FontSize;
                    f.Style = FontStyles.Normal;
                    f.Weight = FontWeights.Normal;
                    String s = new String(input.ToCharArray());

                    Size size = s.Measure(f);
                    sym.OffsetX = size.Width / 2;
                    sym.OffsetY = size.Height / 2;
                    ESRI.ArcGIS.Client.Graphic graphic = new ESRI.ArcGIS.Client.Graphic()
                    {
                        Geometry = pt,
                        Symbol = sym,
                    };
                    graphic.MouseEnter += new MouseEventHandler(graphic_MouseEnter);
                    graphic.MouseLeave += new MouseEventHandler(graphic_MouseLeave);
                    graphic.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(graphic_MouseRightButtonDown);
                    if (win.EditMode)
                    {
                        graphicsLayer.Graphics.Remove(Ambient.SelectedSymbol);

                    }
                    graphicsLayer.Graphics.Add(graphic);
                }
            }
        }

        void graphic_MouseLeave(object sender, MouseEventArgs e)
        {
            //Ambient.SelectedSymbol = null;
        }

        void graphic_MouseEnter(object sender, MouseEventArgs e)
        {
            //Ambient.SelectedSymbol = (Graphic)sender;
        }

        void graphic_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Graphic g = sender as Graphic;
            TextSymbol sym = g.Symbol as TextSymbol;
            MessageBox.Show(sym.Text);
        }

        #region IMapTool Members
        public bool Activated
        {
            get { return _activated; }
        }
 
        public void Activate()
        {
            if (!_init)
            {
                //Environment.Ambient.LayoutRoot.Children.Add(_contextMenu);
                _init = true;
            }
            if (_draw == null)
            {
                _draw = new Draw(BoundMap);
                //{
                //    //FillSymbol = fs,
                //    //DrawMode = DrawMode.Point
                //};
                //_draw.DrawComplete += DrawCompleteHandler;
                _draw.DrawComplete += DrawCompleteHandler;
                _activated = true;
                //this.ActiveMap = map;
                _draw.IsEnabled = true;
            }
            _draw.DrawMode = DrawMode.Point;
        }
        public void Deactivate()
        {
            _activated = false;
            if (_draw != null)
            {
                //_draw.IsEnabled = false;
                //_draw.Map = null;
                //_draw.DrawComplete -= DrawCompleteHandler;
                //_draw = null;
                _draw.DrawMode = DrawMode.None;
            }
        }

        void graphic_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Ambient.SelectedSymbol = (Graphic)sender;

            double x = e.GetPosition(null).X;
            double y = e.GetPosition(null).Y;

            _mnu.Open(new Point(x, y), new Point(0, 0), 0, BoundMap, null);
            ////_contextMenu.Visibility = Visibility.Visible;
            //_contextMenu.SetPosition(x, y);
            //_contextMenu.Visibility = Visibility.Visible;
            ////((Graphic)sender).MapTip.Visibility = Visibility.Collapsed;

            //// handle this so we don't get pass-through menus popping up
            e.Handled = true;
        }

        private void buildPopup()
        {
            _mnu = new PopupMenu();
            _mnu.AddItem("Delete Text", RemoveTextHandler); //("Delete Text", delegate { data.RemoveAt(dataGrid1.SelectedIndex); });
            _mnu.AddSeparator();
            //pm.AddSubMenu(pmTimeSub, "Get Time ", "images/arrow.png", null, null, false, null); // Attach the submenu pmTimeSub
            _mnu.AddSeparator();
            _mnu.AddItem("Edit Text", ChangeTextHandler); //("Demo2", delegate { this.Content = new Demo2(); });
        }
        #endregion
    }
}
