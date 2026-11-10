using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// A basic wire, which follows the <see cref="EditorOptions"/> set by the user
    /// </summary>
    public class Wire : BaseWire
    {
        private ConnectionStyle _lastWireStyle;

        protected override bool HasGeometryChanged()
        {
            return base.HasGeometryChanged() || EditorOptions.WireStyle != _lastWireStyle;
        }

        protected override void UpdateGeometryState()
        {
            base.UpdateGeometryState();
            _lastWireStyle = EditorOptions.WireStyle;
        }

        protected override void DrawWire(StreamGeometryContext context)
        {
            double absDx = Math.Abs(Target.X - Source.X);
            double absDy = Math.Abs(Target.Y - Source.Y);
            double offset = Math.Max(40, Math.Min(150, absDx * 0.5));

            if (absDy > absDx)
            {
                offset = Math.Max(offset, Math.Min(300, absDy * 0.5));
            }

            double sourceOffset = SourceExitRight ? offset : -offset;
            double targetOffset = TargetEnterRight ? offset : -offset;

            switch (EditorOptions.WireStyle)
            {
                case ConnectionStyle.Curvy:
                {
                    context.BeginFigure(Source, false, false);
                    Point curve1 = new Point(Source.X + sourceOffset, Source.Y);
                    Point curve2 = new Point(Target.X + targetOffset, Target.Y);
                    
                    context.PolyBezierTo(new List<Point> {curve1, curve2, Target}, true, false);
                } break;
                case ConnectionStyle.Straight:
                {
                    context.BeginFigure(Source, false, false);
                    context.LineTo(Target, true, false);
                } break;
                case ConnectionStyle.StartStop:
                {
                    context.BeginFigure(Source, false, false);
                    Point curve1 = new Point(Source.X + sourceOffset, Source.Y);
                    context.LineTo(curve1, true, false);
            
                    Point curve2 = new Point(Target.X + targetOffset, Target.Y);
                    context.LineTo(curve2, true, false);
                    context.LineTo(Target, true, false);
                } break;
            }
        }
    }
}