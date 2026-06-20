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
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(nameof(Target), typeof(Point), typeof(BaseWire), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));
        
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
        
        private readonly StreamGeometry _geometry = new StreamGeometry
        {
            FillRule = FillRule.EvenOdd
        };

        protected override Geometry DefiningGeometry
        {
            get
            {
                using (StreamGeometryContext context = _geometry.Open())
                {
                    DrawWire(context);
                }
                
                return _geometry;
            }
        }

        protected abstract void DrawWire(StreamGeometryContext context);
    }
}