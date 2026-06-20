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
        protected override void DrawWire(StreamGeometryContext context)
        {
            switch (EditorOptions.WireStyle)
            {
                case ConnectionStyle.Curvy:
                {
                    context.BeginFigure(Source, false, false);
                    Point curve1 = new Point(Source.X + 85, Source.Y);
                    Point curve2 = new Point(Target.X - 85, Target.Y);
                    
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
                    Point curve1 = new Point(Source.X + 25, Source.Y);
                    context.LineTo(curve1, true, false);
            
                    Point curve2 = new Point(Target.X - 25, Target.Y);
                    context.LineTo(curve2, true, false);
                    context.LineTo(Target, true, false);
                } break;
            }
        }
    }
}