using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace BlueprintEditorPlugin.Views.Wires
{
    /// <summary>
    /// Manages a single shared animation for directional bubbles on all wires.
    /// Uses one <see cref="DoubleAnimation"/> on a singleton <see cref="Animatable"/>
    /// so hundreds of wires can bind to the same offset without creating per-wire clocks.
    /// </summary>
    public class BubbleAnimationManager : Animatable
    {
        private static readonly BubbleAnimationManager _instance = new BubbleAnimationManager();
        public static BubbleAnimationManager Instance => _instance;

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            nameof(Offset), typeof(double), typeof(BubbleAnimationManager),
            new FrameworkPropertyMetadata(0.0d));

        /// <summary>
        /// Current dash offset shared by all wires showing directional bubbles.
        /// </summary>
        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            private set => SetValue(OffsetProperty, value);
        }

        /// <summary>
        /// Distance between bubbles in pixels.
        /// </summary>
        public static double Spacing { get; set; } = 24.0d;

        /// <summary>
        /// Diameter of each bubble in pixels.
        /// </summary>
        public static double BubbleSize { get; set; } = 6.0d;

        /// <summary>
        /// Seconds for a bubble to travel one <see cref="Spacing"/>.
        /// </summary>
        public static double Duration { get; set; } = 1.2d;

        private int _referenceCount;
        private DoubleAnimation _animation;
        private bool _isRunning;

        private BubbleAnimationManager()
        {
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BubbleAnimationManager();
        }

        /// <summary>
        /// Increments the reference count and starts the shared animation if it was not already running.
        /// </summary>
        public static void AddReference()
        {
            if (_instance._referenceCount++ == 0)
                _instance.Start();
        }

        /// <summary>
        /// Decrements the reference count and stops the shared animation when no wires need it.
        /// </summary>
        public static void RemoveReference()
        {
            if (--_instance._referenceCount <= 0)
            {
                _instance._referenceCount = 0;
                _instance.Stop();
            }
        }

        private void Start()
        {
            Stop();

            if (Spacing <= 0 || Duration <= 0)
                return;

            _animation = new DoubleAnimation
            {
                From = 0,
                To = -Spacing,
                Duration = TimeSpan.FromSeconds(Duration),
                RepeatBehavior = RepeatBehavior.Forever
            };

            BeginAnimation(OffsetProperty, _animation);
            _isRunning = true;
        }

        private void Stop()
        {
            if (!_isRunning)
                return;

            BeginAnimation(OffsetProperty, null);
            Offset = 0;
            _isRunning = false;
        }

        /// <summary>
        /// Creates a binding that ties a wire's <see cref="Shape.StrokeDashOffsetProperty"/> to the shared offset.
        /// </summary>
        public static BindingBase CreateOffsetBinding()
        {
            return new Binding(nameof(Offset))
            {
                Source = _instance,
                Mode = BindingMode.OneWay
            };
        }
    }
}
