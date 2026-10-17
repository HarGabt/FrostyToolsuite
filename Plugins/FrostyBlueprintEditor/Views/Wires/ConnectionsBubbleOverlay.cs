using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Renders directional bubbles for all visible wires in a few shared draw calls
    /// instead of one draw call per wire.
    /// Each instance owns its own <see cref="BubbleOverlayManager"/> so multiple
    /// blueprint windows stay isolated.
    /// </summary>
    public class ConnectionsBubbleOverlay : FrameworkElement
    {
        public static readonly DependencyProperty BubbleOffsetProperty = DependencyProperty.Register(
            nameof(BubbleOffset), typeof(double), typeof(ConnectionsBubbleOverlay),
            new FrameworkPropertyMetadata(0.0d, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Current dash offset shared by all bubble groups. Bind to <see cref="BubbleAnimationManager.Offset"/>.
        /// </summary>
        public double BubbleOffset
        {
            get => (double)GetValue(BubbleOffsetProperty);
            set => SetValue(BubbleOffsetProperty, value);
        }

        /// <summary>
        /// The per-overlay geometry manager that wires in the same editor register with.
        /// </summary>
        public BubbleOverlayManager Manager { get; private set; }

        private readonly Dictionary<Brush, Pen> _bubblePens = new Dictionary<Brush, Pen>();
        private DashStyle _dashStyle;

        public ConnectionsBubbleOverlay()
        {
            Manager = new BubbleOverlayManager(InvalidateVisual);

            IsHitTestVisible = false;
            ClipToBounds = false;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BindingOperations.SetBinding(this, BubbleOffsetProperty, BubbleAnimationManager.CreateOffsetBinding());
            InvalidateVisual();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(this, BubbleOffsetProperty);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double bubbleSize = BubbleAnimationManager.BubbleSize;
            double spacing = BubbleAnimationManager.Spacing;
            if (bubbleSize <= 0 || spacing <= 0)
                return;

            if (_dashStyle == null)
            {
                _dashStyle = new DashStyle(new DoubleCollection { 0, spacing }, BubbleOffset);
            }
            else
            {
                _dashStyle.Offset = BubbleOffset;
            }

            foreach (KeyValuePair<Brush, GeometryGroup> group in Manager.Groups)
            {
                Brush brush = group.Key;
                GeometryGroup geometry = group.Value;

                if (geometry == null || geometry.Children.Count == 0)
                    continue;

                if (!_bubblePens.TryGetValue(brush, out Pen pen) || pen == null)
                {
                    pen = new Pen(brush, bubbleSize)
                    {
                        DashStyle = _dashStyle,
                        DashCap = PenLineCap.Round,
                        LineJoin = PenLineJoin.Round,
                        StartLineCap = PenLineCap.Round,
                        EndLineCap = PenLineCap.Round
                    };
                    _bubblePens[brush] = pen;
                }
                else
                {
                    pen.DashStyle = _dashStyle;
                }

                drawingContext.DrawGeometry(null, pen, geometry);
            }
        }
    }
}
