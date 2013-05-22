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
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using System.Linq;

namespace ESRI.ArcGIS.SilverlightMapApp
{
	/// <summary>
	/// Navigation control supporting pan, zoom and rotation.
	/// </summary>
	[TemplatePart(Name = "RotateRing", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PanLeft", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PanRight", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PanUp", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PanDown", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "ZoomSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "TransformRotate", Type = typeof(RotateTransform))]
	[TemplatePart(Name = "ZoomFullExtent", Type = typeof(Button))]
	[TemplatePart(Name = "ResetRotation", Type = typeof(Button))]
    [TemplatePart(Name = "ZoomInButton", Type = typeof(Button))]
    [TemplatePart(Name = "ZoomOutButton", Type = typeof(Button))]
	public class Navigation : Control
	{
		#region private fields

		FrameworkElement RotateRing;
		FrameworkElement PanLeft;
		FrameworkElement PanRight;
		FrameworkElement PanUp;
		FrameworkElement PanDown;
		RotateTransform TransformRotate;
		Button ZoomFullExtent;
		Button ResetRotation;
        Button ZoomInButton;
        Button ZoomOutButton;

		MouseEventHandler rootVisual_MouseMoveHandler;
		MouseEventHandler rootVisual_MouseLeaveHandler;
		MouseButtonEventHandler rootVisual_MouseLeftButtonUp;
		Slider ZoomSlider;

        private double _panFactor = 0.5;
        double[] layerResolutions;
        private double _zoomLevel = 0;
        private double _lastZoomLevel = -1;
        bool _hasDefinedLevels = false;
        bool _resolutionsDefined = false;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Navigation"/> class.
		/// </summary>
		public Navigation()
		{
			DefaultStyleKey = typeof(Navigation);
		}

		/// <summary>
		/// When overridden in a derived class, is invoked whenever application code or
		/// internal processes (such as a rebuilding layout pass) call
		/// <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			RotateRing = GetTemplateChild("RotateRing") as FrameworkElement;
			TransformRotate = GetTemplateChild("TransformRotate") as RotateTransform;
			PanLeft = GetTemplateChild("PanLeft") as FrameworkElement;
			PanRight = GetTemplateChild("PanRight") as FrameworkElement;
			PanUp = GetTemplateChild("PanUp") as FrameworkElement;
			PanDown = GetTemplateChild("PanDown") as FrameworkElement;
			ZoomSlider = GetTemplateChild("ZoomSlider") as Slider;
			ZoomFullExtent = GetTemplateChild("ZoomFullExtent") as Button;
			ResetRotation = GetTemplateChild("ResetRotation") as Button;
            ZoomInButton = GetTemplateChild("ZoomInButton") as Button;
            ZoomOutButton = GetTemplateChild("ZoomOutButton") as Button;
			
			enablePanElement(PanLeft);
			enablePanElement(PanRight);
			enablePanElement(PanUp);
			enablePanElement(PanDown);

			if (ZoomSlider != null)
			{
                if (Map != null)
                {
                    SetupZoom();
                }
                ZoomSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(ZoomSlider_ValueChanged);
                ZoomSlider.LostMouseCapture += new MouseEventHandler(ZoomSlider_LostMouseCapture);
			}
            if (ZoomInButton !=null)
                ZoomInButton.Click += new RoutedEventHandler(ZoomInButton_Click);
            if (ZoomOutButton != null)
                ZoomOutButton.Click += new RoutedEventHandler(ZoomOutButton_Click);
			if (RotateRing != null)
			{
				RotateRing.MouseLeftButtonDown += new MouseButtonEventHandler(RotateRing_MouseLeftButtonDown);
			}
			if (ZoomFullExtent != null)
				ZoomFullExtent.Click += new RoutedEventHandler(ZoomFullExtent_Click);
			if(ResetRotation !=null)
				ResetRotation.Click += new RoutedEventHandler(ResetRotation_Click);
			this.MouseEnter += new MouseEventHandler(Navigation_MouseEnter);
			this.MouseLeave += new MouseEventHandler(Navigation_MouseLeave);
			ChangeVisualState(false);
		}

		private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (ZoomSlider != null && ZoomSlider.Visibility == Visibility.Visible)
            {
                double value = ZoomSlider.Value;
                if (value > 0)
                {
                    value--;
                    ZoomSlider.Value = value;
                    Map.ZoomToResolution(layerResolutions[Convert.ToInt32(Math.Round(value))]);
                }
            }
            else
            {
                Map.Zoom(0.5);
            }
        }

		private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider != null && ZoomSlider.Visibility == Visibility.Visible)
            {
                double value = ZoomSlider.Value;
                if (value < layerResolutions.Length - 2)
                {
                    value++;
                    ZoomSlider.Value = value;
                    Map.ZoomToResolution(layerResolutions[Convert.ToInt32(Math.Round(value))]);
                }

            }
            else
            {
                Map.Zoom(2.0);
            }
        }

        private void Map_ExtentChanged(object sender, ExtentEventArgs args)
        {
            if (_hasDefinedLevels && _resolutionsDefined == false)
                SetupZoom();
            double level = getValueFromMap(Map.Extent);
            if (ZoomSlider != null && level >= 0)
            {
                ZoomSlider.Value = level;
                _lastZoomLevel = level;
                _zoomLevel = level;
            }
            
        }

		private void ResetRotation_Click(object sender, RoutedEventArgs e)
		{
			Storyboard s = new Storyboard();
			s.Duration = TimeSpan.FromMilliseconds(500);
			DoubleAnimationUsingKeyFrames anim = new DoubleAnimationUsingKeyFrames();
			SplineDoubleKeyFrame spline = new SplineDoubleKeyFrame() { KeyTime = s.Duration.TimeSpan, Value = 0, KeySpline = new KeySpline() { ControlPoint1 = new System.Windows.Point(0, 0.1), ControlPoint2 = new System.Windows.Point(0.1, 1) } };
			anim.KeyFrames.Add(spline);
			spline.Value = 0;
			anim.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath("Rotation"));
			s.Children.Add(anim);
			Storyboard.SetTarget(anim, Map);
			s.Completed += (sender2, e2) =>
			{
				s.Stop();
				Map.Rotation = 0;
			};
			s.Begin();
		}

		private void ZoomFullExtent_Click(object sender, RoutedEventArgs e)
		{
			if (Map != null)
				Map.ZoomTo(Map.Layers.GetFullExtent());
		}

		#region State management

		private void Navigation_MouseLeave(object sender, MouseEventArgs e)
		{
			mouseOver = false;
			if(!trackingRotation)
				ChangeVisualState(true);
		}

		private void Navigation_MouseEnter(object sender, MouseEventArgs e)
		{
			mouseOver = true;
			ChangeVisualState(true);
		}

		private bool mouseOver = false;

		private void ChangeVisualState(bool useTransitions)
		{
			if (mouseOver)
			{
				GoToState(useTransitions, "MouseOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}
		}

		private bool GoToState(bool useTransitions, string stateName)
		{
			return VisualStateManager.GoToState(this, stateName, useTransitions);
		}

		#endregion

		#region Rotation

		private void RotateRing_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			startAppMouseTracking();
			startMousePos = e.GetPosition(this);
			trackingRotation = true;
		}

		private void startAppMouseTracking()
		{
			if(rootVisual_MouseMoveHandler ==null)
				rootVisual_MouseMoveHandler = new MouseEventHandler(RootVisual_MouseMove);
			if (rootVisual_MouseLeftButtonUp == null)
				rootVisual_MouseLeftButtonUp = new MouseButtonEventHandler(RootVisual_MouseLeftButtonUp);
			if (rootVisual_MouseLeaveHandler == null)
				rootVisual_MouseLeaveHandler = new MouseEventHandler(RootVisual_MouseLeave); ;

            UIElement root = System.Windows.Application.Current.RootVisual;
			root.MouseMove += rootVisual_MouseMoveHandler;
			root.MouseLeave += rootVisual_MouseLeaveHandler;
			root.MouseLeftButtonUp += rootVisual_MouseLeftButtonUp;
		}
		
		private void stopAppMouseTracking()
		{
            UIElement root = System.Windows.Application.Current.RootVisual;
			root.MouseMove -= rootVisual_MouseMoveHandler;
			root.MouseLeave -= rootVisual_MouseLeaveHandler;
			root.MouseLeftButtonUp -= rootVisual_MouseLeftButtonUp;
		}
		
		Point startMousePos;

		private void RootVisual_MouseMove(object sender, MouseEventArgs e)
		{
			Point p = e.GetPosition(this);
			double delta = (p.Y - startMousePos.Y); // +(startMousePos.X - p.X);
			startMousePos = p;
			angle += delta;
			SetMapRotation(angle);
		}

		private double angle = 0;
		private bool trackingRotation = false;

		private void SetMapRotation(double angle)
		{
			if (TransformRotate != null)
				TransformRotate.Angle = angle;

			if (this.Map != null)
			{
				this.Map.Rotation = angle;
			}
		}

		private void RootVisual_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			stopAppMouseTracking();
			trackingRotation = false;
			ChangeVisualState(true);
		}

		private void RootVisual_MouseLeave(object sender, MouseEventArgs e)
		{
			stopAppMouseTracking();
			trackingRotation = false;
			ChangeVisualState(true);
		}
		
		#endregion

		#region Zoom
		
		private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            _zoomLevel = e.NewValue;
		}

		private void ZoomSlider_LostMouseCapture(object sender, MouseEventArgs e)
		{
            int level = Convert.ToInt32(Math.Round(_zoomLevel));
            if (level!=_lastZoomLevel)
                Map.ZoomToResolution(layerResolutions[level]);
            _lastZoomLevel = level;
        }


		#endregion

		private void enablePanElement(FrameworkElement element)
		{
			if (element == null) return;
			element.MouseLeave += new MouseEventHandler(panElement_MouseLeftButtonUp);
			element.MouseLeftButtonDown += new MouseButtonEventHandler(panElement_MouseLeftButtonDown);
			element.MouseLeftButtonUp += new MouseButtonEventHandler(panElement_MouseLeftButtonUp);
		}

		private void panElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (Map == null || sender == null) return;

            Envelope env = Map.Extent;
			if (env == null) return;
			double x = 0, y = 0;
			MapPoint oldCenter = env.GetCenter();
            MapPoint newCenter = null;
            var height = env.Height * _panFactor;
            var width = env.Width * _panFactor;
            // if units are degrees (the default), limit or alter panning to the lat/lon limits
            if (sender == PanUp) // North
            {
                y = oldCenter.Y + height;
                newCenter = new MapPoint(oldCenter.X, y);
            }
            else if (sender == PanRight) // East
            {
                x = oldCenter.X + width;
                newCenter = new MapPoint(x, oldCenter.Y);
            }
            else if (sender == PanLeft) // West
            {
                x = oldCenter.X - width;
                newCenter = new MapPoint(x, oldCenter.Y);
            } 
            else if (sender == PanDown) // South
            {
                y = oldCenter.Y - height;
                newCenter = new MapPoint(oldCenter.X, y);
            }

            if (newCenter != null)
                Map.PanTo(newCenter);

		}

		private void panElement_MouseLeftButtonUp(object sender, MouseEventArgs e)
		{
			if (Map == null) return;
		}


        /// <summary>
        /// Sets up the parameters of the ZoomSlider
        /// </summary>
        public void SetupZoom()
        {
            System.Collections.Generic.List<double> resolutions = new System.Collections.Generic.List<double>();
            foreach (Layer layer in Map.Layers)
            {
                if (layer is TiledMapServiceLayer)
                {
                    TiledMapServiceLayer tlayer = layer as TiledMapServiceLayer;
                    if (tlayer.TileInfo == null || tlayer.TileInfo.Lods == null) continue;
                    var res = from t in tlayer.TileInfo.Lods
                              select t.Resolution;
                    resolutions.AddRange(res);
                }
            }
            resolutions.Sort();
            layerResolutions = resolutions.Distinct().Reverse().ToArray();
            int count = layerResolutions.Length;
            if (ZoomSlider != null)
            {
                if (count > 0)
                {
                    ZoomSlider.Minimum = 0;
                    ZoomSlider.Maximum = count - 1;
                    Map.MaximumResolution = layerResolutions[0];
                    Map.MinimumResolution = layerResolutions[count - 1];
                    double level = getValueFromMap(Map.Extent);
                    if (level >= 0) ZoomSlider.Value = level;
                    ZoomSlider.Visibility = Visibility.Visible;
                    _resolutionsDefined = true;
                }
                else
                {
                    ZoomSlider.Visibility = Visibility.Collapsed;
                }
            }
        }



        private double getValueFromMap(ESRI.ArcGIS.Client.Geometry.Envelope extent)
        {
            if (layerResolutions == null || layerResolutions.Length == 0 ||
                Map == null || extent == null) return -1;
            double mapRes = extent.Width / Map.ActualWidth;
            for (int i = 0; i < layerResolutions.Length - 1; i++)
            {
                double thisRes = layerResolutions[i];
                double nextRes = layerResolutions[i + 1];
                if (mapRes >= thisRes)
                {
                    return i;
                }
                if (mapRes < thisRes && mapRes > nextRes)
                {
                    return  i + (thisRes - mapRes) / (thisRes - nextRes);
                }
            }
            return Convert.ToDouble(layerResolutions.Length - 1);
        }


		#region Properties

		/// <summary>
		/// Identifies the <see cref="Map"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MapProperty = DependencyProperty.Register("Map", typeof(Map), typeof(Navigation), new PropertyMetadata(OnMapPropertyChanged));

		private static void OnMapPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
            Navigation nav = d as Navigation;
            Map map = e.NewValue as Map;
			Map oldmap = e.OldValue as Map;

			if (oldmap != null)
			{
                oldmap.RotationChanged -= nav.Map_RotationChanged;
                oldmap.ExtentChanged -= nav.Map_ExtentChanged;
				if (oldmap.Layers != null)
                    oldmap.Layers.LayersInitialized -= nav.Layers_LayersInitialized;
			}
			if (map != null)
			{
                map.RotationChanged += nav.Map_RotationChanged;
                map.ExtentChanged += nav.Map_ExtentChanged;
				if (map.Layers != null)
                    map.Layers.LayersInitialized += nav.Layers_LayersInitialized;
			}
			else
			{
                nav._hasDefinedLevels = false;
			}
		}

        private void Layers_LayersInitialized(object sender, EventArgs args)
		{
			_hasDefinedLevels = false;
			foreach (Layer layer in Map.Layers)
			{
				if (layer is TiledMapServiceLayer)
				{
					_hasDefinedLevels = true;
					break;
				}
			}
			SetupZoom();
		}

		/// <summary>
		/// Gets or sets the map that the scale bar is buddied to.
		/// </summary>
		public Map Map
		{
			get { return GetValue(MapProperty) as Map; }
			set { SetValue(MapProperty, value); }
		}

        /// <summary>
        ///  Factor used in panning map. The factor is used as a portion of current width and height of map extent. Default is 0.5.
        /// </summary>
        public Double PanFactor { get { return _panFactor; } set { _panFactor = value; } }

		private void Map_RotationChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			double value = (double)e.NewValue;
			if (TransformRotate != null && TransformRotate.Angle != value)
				TransformRotate.Angle = value;
			angle = value;
		}

		#endregion
	}
}
