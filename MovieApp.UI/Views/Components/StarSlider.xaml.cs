using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MovieApp.UI.Views.Components
{
    public sealed partial class StarSlider : UserControl
    {
        // 1. We create a DependencyProperty so you can bind to this slider in XAML
        // just like you would with a normal WinUI control (e.g. Value="{x:Bind ...}")
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(StarSlider), new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private bool isDragging = false;
        private const int MaxStars = 5;

        public StarSlider()
        {
            this.InitializeComponent();

            // If the window resizes, we need to recalculate the gold mask width
            this.SizeChanged += (s, e) => UpdateMaskWidth();
        }

        // 2. Whenever the Value changes (either from dragging or from the ViewModel), update the UI
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StarSlider slider)
            {
                slider.UpdateMaskWidth();
            }
        }

        // 3. Pointer Events to handle clicking and dragging
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDragging = true;
            RootGrid.CapturePointer(e.Pointer); // Locks the mouse to this control while dragging
            CalculateHalfStep(e);
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                CalculateHalfStep(e);
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDragging = false;
            RootGrid.ReleasePointerCapture(e.Pointer);
        }

        // 4. The Magic Math: Snapping to 0.5 increments
        private void CalculateHalfStep(PointerRoutedEventArgs e)
        {
            if (RootGrid.ActualWidth == 0)
            {
                return;
            }

            // Get where the mouse is relative to the left edge
            var point = e.GetCurrentPoint(RootGrid).Position;

            // Prevent the value from going below 0 or above the max width
            double x = Math.Clamp(point.X, 0, RootGrid.ActualWidth);

            // Figure out how many pixels wide a single star is
            double starWidth = RootGrid.ActualWidth / MaxStars;

            // Calculate the exact decimal rating based on mouse position (e.g., 3.14)
            double rawRating = x / starWidth;

            // Multiply by 2, round to a whole number, then divide by 2 to snap to nearest 0.5
            Value = Math.Round(rawRating * 2, MidpointRounding.AwayFromZero) / 2.0;
        }

        // 5. Visually update the gold stars
        private void UpdateMaskWidth()
        {
            if (RootGrid.ActualWidth == 0)
            {
                return;
            }

            double starWidth = RootGrid.ActualWidth / MaxStars;

            // Set the scroll viewer mask to only reveal the filled stars up to our current Value
            FilledMask.Width = Value * starWidth;
        }
    }
}