using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace DataFlow.UI.Controls
{
    public class NavButton : Button
    {
        static NavButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavButton), new FrameworkPropertyMetadata(typeof(NavButton)));
        }
        public Uri NavLink
        {
            get { return (Uri)GetValue(NavLinkProperty); }
            set { SetValue(NavLinkProperty, value); }
        }
        public static readonly DependencyProperty NavLinkProperty = 
            DependencyProperty.Register("NavLink", typeof(Uri), typeof(NavButton), new PropertyMetadata(null));

        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(NavButton), new PropertyMetadata(false));

        public static readonly DependencyProperty IconProperty = 
            DependencyProperty.Register("Icon", typeof(Geometry), typeof(NavButton), new PropertyMetadata(null));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(NavButton), new PropertyMetadata(string.Empty));

        public bool ShowRightDivider
        {
            get => (bool)GetValue(ShowRightDividerProperty);
            set => SetValue(ShowRightDividerProperty, value);
        }
        public static readonly DependencyProperty ShowRightDividerProperty =
            DependencyProperty.Register(nameof(ShowRightDivider), typeof(bool), typeof(NavButton), new PropertyMetadata(false));


    }
}
