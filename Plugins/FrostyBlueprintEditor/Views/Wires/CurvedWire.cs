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
        public static readonly DependencyProperty FirstCurveProperty = DependencyProperty.Register(nameof(CurvePoint1), typeof(Point), typeof(CurvedWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SecondCurveProperty = DependencyProperty.Register(nameof(CurvePoint2), typeof(Point), typeof(CurvedWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));
        
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