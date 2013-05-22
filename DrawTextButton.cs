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
using Infusion.Silverlight.Controls;
using Infusion.Silverlight.Controls.Ribbon;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using Main.Helpers;
using CustomLayers.Management;
using Utilities;
using Main.Windows;

namespace Main
{
    public class DrawTextButton : RibbonButtonGroupButton, IMapTool
    {
        MapHelper _map = null;
        Draw _draw = null;
        bool _activated = false;
        IMapController _mapController = null;
        private Utilities.ContextMenu _contextMenu;
        private Graphic SelectedSymbol = null;
        private MapPoint _currentPoint = null;

        public DrawTextButton(Ribbon rbn)
        {
            //this.Text = "Rectangle";
            this.ImageSource = "Images/TextBox.png";
            this.IsToggleButton = false;
            this.ButtonClick += ButtonClickHandler;
            System.Windows.Controls.ToolTipService.SetToolTip(this, "Add text annotation");
            _contextMenu = new Utilities.ContextMenu();
            _contextMenu.CreateButton("Delete Text", null, RemoveTextHandler);
            _contextMenu.CreateButton("Change Text", null, ChangeTextHandler);
            //_contextMenu.CreateButton("Annotation Operations", AppManager.MapContextMenus.AnnoContextMenu, null);
            AppManager.LayoutRoot.Children.Add(_contextMenu);
            //AppManager.Map.MouseClick += new EventHandler<Map.MouseEventArgs>(map_MouseClick);
            //AppManager.Map.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(_map_MouseRightButtonDown);
            //_map = map;
        }
        private void ButtonClickHandler(object sender, MouseButtonEventArgs e)
        {
            //this.IsSelected = false;
            _mapController.CurrentTool = (IMapTool)this;
            //_map.ZoomTo(_map.Layers.GetFullExtent());
        }

        private void ChangeTextHandler(object sender, RoutedEventArgs e)
        {
            //Graphic g = sender as Graphic;
            _currentPoint = SelectedSymbol.Geometry as MapPoint;
            TextSymbol ts = SelectedSymbol.Symbol as TextSymbol;
            TextSymbolPropsWindow win = new TextSymbolPropsWindow();
            win.TextFont = ts.FontFamily.Source;
            win.TextFontSize = ts.FontSize;
            win.Annotation = ts.Text;
            win.EditMode = true;
            win.Closed += new EventHandler(win_Closed);
            win.Show();
            //MessageBox.Show("show change dialog here");
            //RemoveSymbol(SelectedSymbol);
        }

        private void RemoveTextHandler(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Are you sure you want to delete this text? Click 'OK' to confirm.", "Confirm Delete", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                RemoveSymbol(SelectedSymbol);
            }
        }

        public void RemoveSymbol(Graphic marker)
        {
            GraphicsLayer graphicsLayer = _map.Map.Layers["defaultTextGraphicsLayer"] as GraphicsLayer;
            graphicsLayer.Graphics.Remove(marker);
        }

        private void DrawCompleteHandler(object sender, DrawEventArgs args)
        {
            _currentPoint = args.Geometry as MapPoint;
            TextSymbolPropsWindow win = new TextSymbolPropsWindow();
            win.EditMode = false;
            win.Closed += new EventHandler(win_Closed);
            win.Show();
            //GraphicsLayer graphicsLayer = _map.Map.Layers["defaultTextGraphicsLayer"] as GraphicsLayer;
            //string input = System.Windows.Browser.HtmlPage.Window.Prompt("Enter text to display");

            //if (!String.IsNullOrEmpty(input))
            //{
            //    MapPoint pt = args.Geometry as MapPoint;
            //    TextSymbol sym = new TextSymbol();
            //    sym.FontFamily = new FontFamily("Arial");
            //    sym.FontSize = 20;
            //    sym.Text = input;
            //    sym.Foreground = new SolidColorBrush { Color = Colors.Black };
            //    Main.Helpers.Font f = new Font();
            //    f.Family = sym.FontFamily;
            //    f.Size = sym.FontSize;
            //    f.Style = FontStyles.Normal;
            //    f.Weight = FontWeights.Normal;
            //    String s = new String(input.ToCharArray());
                
            //    Size size = s.Measure(f);
            //    sym.OffsetX = size.Width / 2;
            //    sym.OffsetY = size.Height / 2;
            //    //MessageBox.Show("h:" + size.Height.ToString() + " w:" + size.Width.ToString());
            //    //TODO: Apply offest to center label
            //    ESRI.ArcGIS.Client.Graphic graphic = new ESRI.ArcGIS.Client.Graphic()
            //    {
            //        Geometry = args.Geometry,
            //        Symbol = sym,
            //    };
            //    graphic.MouseLeftButtonUp += new MouseButtonEventHandler(graphic_MouseLeftButtonUp);
            //    graphic.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(graphic_MouseRightButtonDown);
            //    graphicsLayer.Graphics.Add(graphic);            //_map.PreviousExtents.Push(_map.Map.Extent);
            //}
            //_map.NextExtents.Clear();
            //_map.Map.ZoomTo(args.Geometry as ESRI.ArcGIS.Client.Geometry.Envelope);
        }

        void win_Closed(object sender, EventArgs e)
        {
            TextSymbolPropsWindow win = sender as TextSymbolPropsWindow;
            GraphicsLayer graphicsLayer = _map.Map.Layers["defaultTextGraphicsLayer"] as GraphicsLayer;
            string input = win.Annotation; // System.Windows.Browser.HtmlPage.Window.Prompt("Enter text to display");

            if (!String.IsNullOrEmpty(input))
            {
                MapPoint pt = _currentPoint;
                TextSymbol sym = new TextSymbol();
                sym.FontFamily = new FontFamily(win.TextFont); // new FontFamily("Arial");
                sym.FontSize = win.TextFontSize; // 20;
                sym.Text = win.Annotation; // input;
                sym.Foreground = new SolidColorBrush { Color = Colors.Black };
                Main.Helpers.Font f = new Font();
                f.Family = sym.FontFamily;
                f.Size = sym.FontSize;
                f.Style = FontStyles.Normal;
                f.Weight = FontWeights.Normal;
                String s = new String(input.ToCharArray());

                Size size = s.Measure(f);
                sym.OffsetX = size.Width / 2;
                sym.OffsetY = size.Height / 2;
                //MessageBox.Show("h:" + size.Height.ToString() + " w:" + size.Width.ToString());
                //TODO: Apply offest to center label
                ESRI.ArcGIS.Client.Graphic graphic = new ESRI.ArcGIS.Client.Graphic()
                {
                    Geometry = pt,
                    Symbol = sym,
                };
                graphic.MouseLeftButtonUp += new MouseButtonEventHandler(graphic_MouseLeftButtonUp);
                graphic.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(graphic_MouseRightButtonDown);
                if (win.EditMode)
                {
                    graphicsLayer.Graphics.Remove(SelectedSymbol);
                    
                }//_map.PreviousExtents.Push(_map.Map.Extent);
                graphicsLayer.Graphics.Add(graphic);
            }
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
        public IMapController MapController
        {
            get
            {
                return _mapController;
            }
            set
            {
                _mapController = value;
            }
        }
        public MapHelper ActiveMap
        {
            get
            {
                return _map;
            }
            set
            {

                _map = value;
                _draw = null;
                //var fs = new SimpleFillSymbol();
                //fs.Fill = new SolidColorBrush(Color.FromArgb(150, 255, 0, 0));
                //fs.BorderBrush = new SolidColorBrush(Color.FromArgb(225, 255, 0, 0));
                //fs.BorderThickness = 2;
                _draw = new Draw(_map.Map)
                {
                    //FillSymbol = fs,
                    DrawMode = DrawMode.Point
                };
                _draw.DrawComplete += DrawCompleteHandler;
            }
        }
        public void Activate(MapHelper map)
        {
            _activated = true;
            this.ActiveMap = map;
            _draw.IsEnabled = true;
        }
        public void Deactivate()
        {
            _activated = false;
            if (_draw != null)
            {
                _draw.IsEnabled = false;
                //_draw.Map = null;
                _draw.DrawComplete -= DrawCompleteHandler;
                _draw = null;
            }
        }

        void graphic_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSymbol = (Graphic)sender;

            double x = e.GetPosition(AppManager.Map).X;
            double y = e.GetPosition(AppManager.Map).Y;
            _contextMenu.SetPosition(x, y);
            _contextMenu.Visibility = Visibility.Visible;
            //((Graphic)sender).MapTip.Visibility = Visibility.Collapsed;

            // handle this so we don't get pass-through menus popping up
            e.Handled = true;
        }
        #endregion
    }
}
