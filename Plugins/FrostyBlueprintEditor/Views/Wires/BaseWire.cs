using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Base implementation for a drawn Wire which goes from 1 point to another.
    /// </summary>
    public abstract class BaseWire : Shape
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnGeometryInvalidatingPropertyChanged));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(nameof(Target), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnGeometryInvalidatingPropertyChanged));

        public static readonly DependencyProperty ShowDirectionalBubblesProperty = DependencyProperty.Register(
            nameof(ShowDirectionalBubbles), typeof(bool), typeof(BaseWire),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnShowDirectionalBubblesChanged));

        public static readonly DependencyProperty BubbleSizeProperty = DependencyProperty.Register(
            nameof(BubbleSize), typeof(double), typeof(BaseWire),
            new FrameworkPropertyMetadata(BubbleAnimationManager.BubbleSize, OnBubbleAppearanceChanged));

        public static readonly DependencyProperty EnableDirectionalBubblesProperty = DependencyProperty.Register(
            nameof(EnableDirectionalBubbles), typeof(bool), typeof(BaseWire),
            new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty SourceExitRightProperty = DependencyProperty.Register(
            nameof(SourceExitRight), typeof(bool), typeof(BaseWire),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TargetEnterRightProperty = DependencyProperty.Register(
            nameof(TargetEnterRight), typeof(bool), typeof(BaseWire),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the start point of this wire.
        /// </summary>
        public Point Source
        {
            get => (Point)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point of this wire.
        /// </summary>
        public Point Target
        {
            get => (Point)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets whether animated directional bubbles are drawn on the wire.
        /// </summary>
        public bool ShowDirectionalBubbles
        {
            get => (bool)GetValue(ShowDirectionalBubblesProperty);
            set => SetValue(ShowDirectionalBubblesProperty, value);
        }

        /// <summary>
        /// Gets or sets the diameter of each directional bubble in pixels.
        /// </summary>
        public double BubbleSize
        {
            get => (double)GetValue(BubbleSizeProperty);
            set => SetValue(BubbleSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this wire is allowed to show directional bubbles.
        /// When false, <see cref="ShowDirectionalBubbles"/> is forced off regardless of the global option.
        /// </summary>
        public bool EnableDirectionalBubbles
        {
            get => (bool)GetValue(EnableDirectionalBubblesProperty);
            set => SetValue(EnableDirectionalBubblesProperty, value);
        }

        public bool SourceExitRight
        {
            get => (bool)GetValue(SourceExitRightProperty);
            set => SetValue(SourceExitRightProperty, value);
        }

        public bool TargetEnterRight
        {
            get => (bool)GetValue(TargetEnterRightProperty);
            set => SetValue(TargetEnterRightProperty, value);
        }

        private readonly StreamGeometry _geometry = new StreamGeometry
        {
            FillRule = FillRule.EvenOdd
        };

        private bool _geometryDirty = true;
        private Point _lastSource;
        private Point _lastTarget;

        /// <summary>
        /// Returns whether the cached geometry needs to be redrawn. Derived classes can override this
        /// to include additional dependency properties in the dirty check.
        /// </summary>
        protected virtual bool HasGeometryChanged()
        {
            return _geometryDirty || Source != _lastSource || Target != _lastTarget;
        }

        /// <summary>
        /// Updates the cached geometry state after a redraw. Derived classes overriding
        /// <see cref="HasGeometryChanged"/> should also override this to store their own state.
        /// </summary>
        protected virtual void UpdateGeometryState()
        {
            _lastSource = Source;
            _lastTarget = Target;
            _geometryDirty = false;
        }

        private bool _bubbleGeometryRegistered;
        private Geometry _lastBubbleGeometry;
        private Brush _lastBubbleBrush;
        private ConnectionsBubbleOverlay _parentOverlay;

        protected BaseWire()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private static void OnGeometryInvalidatingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BaseWire)d)._geometryDirty = true;
        }

        private static void OnShowDirectionalBubblesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wire = (BaseWire)d;
            if ((bool)e.NewValue)
                wire.StartBubbleAnimation();
            else
                wire.StopBubbleAnimation();
        }

        private static void OnBubbleAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var wire = (BaseWire)d;
            wire._bubbleGeometryRegistered = false;
            wire._lastBubbleGeometry = null;
            wire._lastBubbleBrush = null;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentOverlay = FindParentOverlay();
            ShowDirectionalBubbles = EnableDirectionalBubbles && EditorOptions.AnimatedDirectionalBubbles;
            EditorOptions.Updated += OnEditorOptionsUpdated;

            if (ShowDirectionalBubbles)
            {
                StartBubbleAnimation();
                RegisterBubbleGeometry();
                InvalidateVisual();
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && ShowDirectionalBubbles && _bubbleAnimationStarted)
            {
                RegisterBubbleGeometry();
                InvalidateVisual();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            EditorOptions.Updated -= OnEditorOptionsUpdated;
            StopBubbleAnimation();
            _parentOverlay = null;
        }

        private void OnEditorOptionsUpdated()
        {
            ShowDirectionalBubbles = EnableDirectionalBubbles && EditorOptions.AnimatedDirectionalBubbles;
        }

        private ConnectionsBubbleOverlay FindParentOverlay()
        {
            DependencyObject current = this;
            while (current != null)
            {
                if (current is ConnectionsBubbleOverlay overlay)
                    return overlay;

                var parent = VisualTreeHelper.GetParent(current);
                if (parent is System.Windows.Controls.Panel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is ConnectionsBubbleOverlay sibling)
                            return sibling;
                    }
                }
                current = parent;
            }
            return null;
        }

        private bool _bubbleAnimationStarted;

        private void StartBubbleAnimation()
        {
            if (_bubbleAnimationStarted)
                return;

            BubbleAnimationManager.AddReference();
            _bubbleAnimationStarted = true;
        }

        private void StopBubbleAnimation()
        {
            if (!_bubbleAnimationStarted)
                return;

            RemoveBubbleGeometry();
            BubbleAnimationManager.RemoveReference();
            _bubbleAnimationStarted = false;
        }

        private void RemoveBubbleGeometry()
        {
            if (!_bubbleGeometryRegistered)
                return;

            _parentOverlay?.Manager.RemoveGeometry(this);
            _bubbleGeometryRegistered = false;
            _lastBubbleGeometry = null;
            _lastBubbleBrush = null;
        }

        /// <summary>
        /// Invalidates the cached wire geometry so it is redrawn on the next render pass.
        /// </summary>
        protected void InvalidateGeometry()
        {
            _geometryDirty = true;
            InvalidateVisual();
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                if (!HasGeometryChanged())
                    return _geometry;

                using (StreamGeometryContext context = _geometry.Open())
                {
                    DrawWire(context);
                }

                UpdateGeometryState();

                return _geometry;
            }
        }

        private void RegisterBubbleGeometry()
        {
            if (!ShowDirectionalBubbles || BubbleSize <= 0 || Stroke == null)
            {
                RemoveBubbleGeometry();
                return;
            }

            Geometry geometry = DefiningGeometry;
            if (geometry == null)
            {
                RemoveBubbleGeometry();
                return;
            }

            bool brushChanged = !ReferenceEquals(Stroke, _lastBubbleBrush);
            bool geometryReferenceChanged = !ReferenceEquals(geometry, _lastBubbleGeometry);

            if (!_bubbleGeometryRegistered || geometryReferenceChanged || brushChanged)
            {
                _parentOverlay?.Manager.UpdateGeometry(this, geometry, Stroke);
                _bubbleGeometryRegistered = true;
                _lastBubbleGeometry = geometry;
                _lastBubbleBrush = Stroke;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            bool geometryWasDirty = HasGeometryChanged();
            RegisterBubbleGeometry();

            if (_bubbleGeometryRegistered && geometryWasDirty)
            {
                _parentOverlay?.InvalidateVisual();
            }
        }

        protected abstract void DrawWire(StreamGeometryContext context);
    }
}
