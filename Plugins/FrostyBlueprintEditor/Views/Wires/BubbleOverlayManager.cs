using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Collects per-wire geometries from <see cref="BaseWire"/> instances and merges them into
    /// one <see cref="GeometryGroup"/> per brush so a <see cref="ConnectionsBubbleOverlay"/> can
    /// draw all bubbles in a constant number of draw calls.
    /// Each overlay instance owns its own manager so multiple blueprint windows stay isolated.
    /// </summary>
    public class BubbleOverlayManager
    {
        private class Registration
        {
            public Geometry Geometry;
            public Brush Brush;
            public GeometryGroup Group;
        }

        private readonly Dictionary<BaseWire, Registration> _registrations = new Dictionary<BaseWire, Registration>();
        private readonly Dictionary<Brush, GeometryGroup> _groups = new Dictionary<Brush, GeometryGroup>();
        private readonly Action _invalidateAction;

        public BubbleOverlayManager(Action invalidateAction)
        {
            _invalidateAction = invalidateAction;
        }

        /// <summary>
        /// Gets the merged geometry groups keyed by the brush used to stroke them.
        /// </summary>
        public IEnumerable<KeyValuePair<Brush, GeometryGroup>> Groups => _groups;

        /// <summary>
        /// Registers or updates the geometry a wire contributes to this overlay's geometry.
        /// </summary>
        public void UpdateGeometry(BaseWire wire, Geometry geometry, Brush brush)
        {
            if (wire == null || geometry == null || brush == null)
                return;

            if (_registrations.TryGetValue(wire, out Registration existing))
            {
                if (ReferenceEquals(existing.Geometry, geometry) && ReferenceEquals(existing.Brush, brush))
                    return;

                existing.Group.Children.Remove(existing.Geometry);
                if (existing.Group.Children.Count == 0)
                {
                    _groups.Remove(existing.Brush);
                }
            }

            if (!_groups.TryGetValue(brush, out GeometryGroup group))
            {
                group = new GeometryGroup();
                _groups[brush] = group;
            }

            group.Children.Add(geometry);

            _registrations[wire] = new Registration
            {
                Geometry = geometry,
                Brush = brush,
                Group = group
            };

            _invalidateAction?.Invoke();
        }

        /// <summary>
        /// Removes a wire's geometry from this overlay's geometry.
        /// </summary>
        public void RemoveGeometry(BaseWire wire)
        {
            if (wire == null || !_registrations.TryGetValue(wire, out Registration existing))
                return;

            existing.Group.Children.Remove(existing.Geometry);
            if (existing.Group.Children.Count == 0)
            {
                _groups.Remove(existing.Brush);
            }

            _registrations.Remove(wire);
            _invalidateAction?.Invoke();
        }
    }
}
