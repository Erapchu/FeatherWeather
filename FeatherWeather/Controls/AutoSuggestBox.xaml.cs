using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FeatherWeather.Controls;

public partial class AutoSuggestBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(AutoSuggestBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(AutoSuggestBox), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(AutoSuggestBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsSuggestionListOpenProperty = DependencyProperty.Register(
        nameof(IsSuggestionListOpen), typeof(bool), typeof(AutoSuggestBox),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(AutoSuggestBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextMemberPathProperty = DependencyProperty.Register(
        nameof(TextMemberPath), typeof(string), typeof(AutoSuggestBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty InputHeightProperty = DependencyProperty.Register(
        nameof(InputHeight), typeof(double), typeof(AutoSuggestBox), new PropertyMetadata(36d));

    public AutoSuggestBox()
    {
        InitializeComponent();
        InputBox.PreviewKeyDown += OnInputPreviewKeyDown;
        SuggestionsList.PreviewMouseLeftButtonUp += OnSuggestionClicked;
        SuggestionsPopup.Closed += (_, _) => IsSuggestionListOpen = false;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public bool IsSuggestionListOpen
    {
        get => (bool)GetValue(IsSuggestionListOpenProperty);
        set => SetValue(IsSuggestionListOpenProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string TextMemberPath
    {
        get => (string)GetValue(TextMemberPathProperty);
        set => SetValue(TextMemberPathProperty, value);
    }

    public double InputHeight
    {
        get => (double)GetValue(InputHeightProperty);
        set => SetValue(InputHeightProperty, value);
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when SuggestionsList.Items.Count > 0:
                IsSuggestionListOpen = true;
                SuggestionsList.SelectedIndex = Math.Min(
                    SuggestionsList.SelectedIndex + 1,
                    SuggestionsList.Items.Count - 1);
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
                break;

            case Key.Up when IsSuggestionListOpen && SuggestionsList.Items.Count > 0:
                SuggestionsList.SelectedIndex = SuggestionsList.SelectedIndex <= 0
                    ? SuggestionsList.Items.Count - 1
                    : SuggestionsList.SelectedIndex - 1;
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
                break;

            case Key.Enter when IsSuggestionListOpen && SuggestionsList.SelectedItem is not null:
                CommitSuggestion();
                e.Handled = true;
                break;

            case Key.Escape when IsSuggestionListOpen:
                IsSuggestionListOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void OnSuggestionClicked(object sender, MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(SuggestionsList, e.OriginalSource as DependencyObject)
            is ListBoxItem item)
        {
            SuggestionsList.SelectedItem = item.DataContext;
            CommitSuggestion();
        }
    }

    private void CommitSuggestion()
    {
        object? item = SuggestionsList.SelectedItem;
        if (item is null)
            return;

        SelectedItem = item;
        Text = GetText(item);
        IsSuggestionListOpen = false;
        InputBox.CaretIndex = InputBox.Text.Length;
        InputBox.Focus();
    }

    private string GetText(object item)
    {
        if (string.IsNullOrWhiteSpace(TextMemberPath))
            return item.ToString() ?? string.Empty;

        PropertyInfo? property = item.GetType().GetProperty(TextMemberPath);
        return property?.GetValue(item)?.ToString() ?? string.Empty;
    }
}
