using System.Windows;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace eyetracker
{
    public class ImageButton : ToggleButton
    {
        private static readonly DependencyProperty ActiveIconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(ImageButton));
        private static readonly DependencyProperty LookedCountProperty = DependencyProperty.Register("lookedcount", typeof(int), typeof(ImageButton));

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(ActiveIconProperty); }
            set { SetValue(ActiveIconProperty, value); }
        }

        public int lookedcount
        {
            get { return (int)GetValue(LookedCountProperty); }
            set { SetValue(LookedCountProperty, value); }
        }

        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton)));
        }
    }
}
