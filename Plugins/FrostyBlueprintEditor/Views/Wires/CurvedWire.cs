using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Similar to <see cref="Wire"/> except this provides dependency properties for curve points
    /// </summary>
    public class CurvedWire : BaseWire
    {
        public static readonly DependencyProperty FirstCurveProperty = DependencyProperty.Register(nameof(CurvePoint1), typeof(Point), typeof(CurvedWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnCurvePointChanged));
        public static readonly DependencyProperty SecondCurveProperty = DependencyProperty.Register(nameof(CurvePoint2), typeof(Point), typeof(CurvedWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender, OnCurvePointChanged));

        private static void OnCurvePointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CurvedWire)d).InvalidateGeometry();
        }

        private Point _lastCurvePoint1;
        private Point _lastCurvePoint2;
        private ConnectionStyle _lastWireStyle;

        /// <summary>
        /// Gets or sets the start point of this wire.
        /// </summary>
        public Point CurvePoint1
        {
            get => (Point)GetValue(FirstCurveProperty);
            set => SetValue(FirstCurveProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point of this wire.
        /// </summary>
        public Point CurvePoint2
        {
            get => (Point)GetValue(SecondCurveProperty);
            set => SetValue(SecondCurveProperty, value);
        }

        protected override bool HasGeometryChanged()
        {
            return base.HasGeometryChanged()
                   || CurvePoint1 != _lastCurvePoint1
                   || CurvePoint2 != _lastCurvePoint2
                   || EditorOptions.WireStyle != _lastWireStyle;
        }

        protected override void UpdateGeometryState()
        {
            base.UpdateGeometryState();
            _lastCurvePoint1 = CurvePoint1;
            _lastCurvePoint2 = CurvePoint2;
            _lastWireStyle = EditorOptions.WireStyle;
        }

        protected override void DrawWire(StreamGeometryContext context)
        {
            switch (EditorOptions.WireStyle)
            {
                case ConnectionStyle.Curvy:
                {
                    context.BeginFigure(Source, false, false);
                    
                    context.PolyBezierTo(new List<Point> {CurvePoint1, CurvePoint2, Target}, true, false);
                } break;
                case ConnectionStyle.Straight:
                {
                    context.BeginFigure(Source, false, false);
                    context.LineTo(Target, true, false);
                } break;
                case ConnectionStyle.StartStop:
                {
                    context.BeginFigure(Source, false, false);
                    context.LineTo(CurvePoint1, true, false);
                    context.LineTo(CurvePoint2, true, false);
                    context.LineTo(Target, true, false);
                } break;
            }
        }
    }
}
