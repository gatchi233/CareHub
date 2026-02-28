using CommunityToolkit.Maui.Views;
using MedReminder.Resources.Styles;
using Microsoft.Maui.Controls.Shapes;

namespace MedReminder.Pages.UI.Popups;

public partial class ActionPopup : Popup
{
    public enum PopupMode
    {
        List,
        Form,
        Adjust,
        Order,
        Sort,
        Observation,
        NewMedOrder
    }

    public record PopupResult(
        string? SelectedKey = null,
        string? Field1 = null,
        string? Field2 = null,
        int? AdjustDelta = null,
        string? Field3 = null,
        string? Field4 = null,
        string? Field5 = null,
        string? Field6 = null
    );

    private PopupMode _mode;

    // For Form mode validation (optional)
    private Func<string?, string?, (bool ok, string? error)>? _validator;

    // For Adjust mode
    private int _currentStock;

    public ActionPopup()
    {
        InitializeComponent();
    }

    // Sort (grid layout: 1 featured + 3 paired rows)
    public void ConfigureSort()
    {
        _mode = PopupMode.Sort;
        TitleLabel.Text = "SORT INVENTORY";

        ListContainer.IsVisible = false;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = true;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = false;

        Size = new Size(420, 380);
    }

    private void OnSortOptionTapped(object sender, TappedEventArgs e)
    {
        var key = e.Parameter as string;
        Close(new PopupResult(SelectedKey: key));
    }

    // List (flat vertical list, kept for potential reuse)
    public void ConfigureList(string title, IEnumerable<(string key, string text)> items)
    {
        _mode = PopupMode.List;

        TitleLabel.Text = title;

        ListContainer.IsVisible = true;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = false;

        ListStack.Children.Clear();

foreach (var (key, text) in items)
{
    var cardBg = (Color)Application.Current.Resources["Popup_Card_Background"];
    var cardBorder = (Color)Application.Current.Resources["Popup_Card_Border"];
    var fontPrimary = (Color)Application.Current.Resources["Popup_Font_Primary"];

    var row = new Border
    {
        BackgroundColor = cardBg,
        Stroke = cardBorder,
        StrokeThickness = 1,
        Padding = new Thickness(12, 10),
        StrokeShape = new RoundRectangle { CornerRadius = 10 },
        Content = new Label
        {
            Text = text,
            FontSize = 13,
            TextColor = fontPrimary,
            VerticalOptions = LayoutOptions.Center
        }
    };

    var tap = new TapGestureRecognizer();
    tap.Tapped += (_, __) => Close(new PopupResult(SelectedKey: key));
    row.GestureRecognizers.Add(tap);

    ListStack.Children.Add(row);
}    }

    //Form
    public void ConfigureForm(
        string title,
        string? message,
        string primaryText,
        (string label, string placeholder, Keyboard keyboard)? field1,
        (string label, string placeholder, Keyboard keyboard)? field2,
        Func<string?, string?, (bool ok, string? error)>? validator = null)
    {
        _mode = PopupMode.Form;

        TitleLabel.Text = title;

        ListContainer.IsVisible = false;
        FormContainer.IsVisible = true;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = false;

        _validator = validator;

        if (!string.IsNullOrWhiteSpace(message))
        {
            MessageLabel.Text = message;
            MessageLabel.IsVisible = true;
        }
        else
        {
            MessageLabel.IsVisible = false;
        }

        PrimaryButtonLabel.Text = primaryText;

        // Field1
        if (field1.HasValue)
        {
            Field1Container.IsVisible = true;
            Field1Label.Text = field1.Value.label;
            Field1Entry.Placeholder = field1.Value.placeholder;
            Field1Entry.Keyboard = field1.Value.keyboard;
        }
        else
        {
            Field1Container.IsVisible = false;
            Field1Entry.Text = string.Empty;
        }

        // Field2
        if (field2.HasValue)
        {
            Field2Container.IsVisible = true;
            Field2Label.Text = field2.Value.label;
            Field2Entry.Placeholder = field2.Value.placeholder;
            Field2Entry.Keyboard = field2.Value.keyboard;
        }
        else
        {
            Field2Container.IsVisible = false;
            Field2Entry.Text = string.Empty;
        }
    }

    // Adjust Stock
    public void ConfigureAdjust(string title, string medName, int currentStock, int reorderLevel)
    {
        _mode = PopupMode.Adjust;
        _currentStock = currentStock;

        TitleLabel.Text = title;
        ListContainer.IsVisible = false;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = true;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = false;

        AdjustMedNameLabel.Text = medName;
        AdjustEntry.Text = string.Empty;

        var isLow = currentStock <= reorderLevel;
        CurrentStockLabel.Text = currentStock.ToString();
        CurrentStockLabel.TextColor = isLow
            ? (Color)Application.Current!.Resources["Alert_Warning"]
            : (Color)Application.Current!.Resources["Popup_Font_Primary"];

        // Build quick-adjust buttons
        QuickButtonsRow.Children.Clear();
        var negColor = (Color)Application.Current!.Resources["Button_Delete"];
        var posColor = (Color)Application.Current!.Resources["Button_Create"];

        foreach (var delta in new[] { -25, -10, -5, 5, 10, 25 })
        {
            var d = delta;
            var btn = new Border
            {
                StrokeThickness = 0,
                HeightRequest = 30,
                MinimumWidthRequest = 46,
                Padding = new Thickness(10, 0),
                BackgroundColor = d < 0 ? negColor : posColor,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    Text = d < 0 ? d.ToString() : $"+{d}",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                AdjustEntry.Text = d.ToString();
                UpdateAdjustPreview();
            };
            btn.GestureRecognizers.Add(tap);
            QuickButtonsRow.Children.Add(btn);
        }

        // Size the popup to fit the richer content
        Size = new Size(420, 520);

        UpdateAdjustPreview();
    }

    private void OnAdjustEntryChanged(object sender, TextChangedEventArgs e)
        => UpdateAdjustPreview();

    private void UpdateAdjustPreview()
    {
        var text = AdjustEntry?.Text?.Trim();

        if (string.IsNullOrEmpty(text) || !int.TryParse(text, out var delta) || delta == 0)
        {
            NewStockLabel.Text = "—";
            NewStockLabel.TextColor = (Color)Application.Current!.Resources["Popup_Font_Primary"];
            NewStockWarningLabel.IsVisible = false;
            return;
        }

        var newStock = _currentStock + delta;

        if (newStock < 0)
        {
            NewStockLabel.Text = "0  (capped)";
            NewStockLabel.TextColor = (Color)Application.Current!.Resources["Alert_Error"];
            NewStockWarningLabel.Text = $"⚠  Would result in {newStock}. Stock will be set to 0.";
            NewStockWarningLabel.IsVisible = true;
        }
        else
        {
            NewStockLabel.Text = newStock.ToString();
            NewStockLabel.TextColor = delta > 0
                ? (Color)Application.Current!.Resources["Alert_Success"]
                : (Color)Application.Current!.Resources["Alert_Warning"];
            NewStockWarningLabel.IsVisible = false;
        }
    }

    private void OnAdjustCancelTapped(object sender, EventArgs e) => Close(null);

    private void OnAdjustApplyTapped(object sender, EventArgs e)
    {
        var text = AdjustEntry?.Text?.Trim();

        if (string.IsNullOrEmpty(text) || !int.TryParse(text, out var delta) || delta == 0)
            return;

        // Cap so stock never goes below zero
        var clampedDelta = Math.Max(delta, -_currentStock);
        Close(new PopupResult(AdjustDelta: clampedDelta));
    }

    // Create Order
    public void ConfigureOrder(string title, string medName, int currentStock, int reorderLevel)
    {
        _mode = PopupMode.Order;

        TitleLabel.Text = title;
        ListContainer.IsVisible = false;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = true;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = false;

        OrderMedNameLabel.Text = medName;
        OrderQtyEntry.Text = string.Empty;
        OrderNotesEntry.Text = string.Empty;

        var isLow = currentStock <= reorderLevel;
        OrderLowBadge.IsVisible = isLow;
        OrderStockLabel.Text = $"Stock: {currentStock}";
        OrderStockLabel.TextColor = isLow
            ? (Color)Application.Current!.Resources["Alert_Warning"]
            : (Color)Application.Current!.Resources["Popup_Font_Primary"];
        OrderReorderLabel.Text = $"Reorder: {reorderLevel}";

        // Quick quantity buttons (positive only — ordering always adds)
        QuickOrderButtonsRow.Children.Clear();
        var posColor = (Color)Application.Current!.Resources["Button_Create"];

        foreach (var qty in new[] { 10, 25, 50, 100, 200 })
        {
            var q = qty;
            var btn = new Border
            {
                StrokeThickness = 0,
                HeightRequest = 30,
                MinimumWidthRequest = 46,
                Padding = new Thickness(10, 0),
                BackgroundColor = posColor,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    Text = $"+{q}",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OrderQtyEntry.Text = q.ToString();
            btn.GestureRecognizers.Add(tap);
            QuickOrderButtonsRow.Children.Add(btn);
        }

        Size = new Size(420, 490);
    }

    private void OnOrderCancelTapped(object sender, EventArgs e) => Close(null);

    private async void OnOrderCreateTapped(object sender, EventArgs e)
    {
        var qtyText = OrderQtyEntry.Text?.Trim();
        var notes   = OrderNotesEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(qtyText))
        {
            await Application.Current!.MainPage!.DisplayAlert("Required", "Please enter a quantity.", "OK");
            return;
        }

        if (!int.TryParse(qtyText, out var qty) || qty <= 0)
        {
            await Application.Current!.MainPage!.DisplayAlert("Invalid", "Please enter a positive number.", "OK");
            return;
        }

        Close(new PopupResult(
            Field1: qtyText,
            Field2: string.IsNullOrWhiteSpace(notes) ? null : notes));
    }

    // Observation
    public void ConfigureObservation(string title,
        string? temp = null, string? bpHigh = null, string? bpLow = null,
        string? pulse = null, string? spo2 = null, string? notes = null)
    {
        _mode = PopupMode.Observation;
        TitleLabel.Text = title;

        ListContainer.IsVisible = false;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = true;
        NewMedOrderContainer.IsVisible = false;

        ObsTempEntry.Text = temp ?? string.Empty;
        ObsBpHighEntry.Text = bpHigh ?? string.Empty;
        ObsBpLowEntry.Text = bpLow ?? string.Empty;
        ObsPulseEntry.Text = pulse ?? string.Empty;
        ObsSpo2Entry.Text = spo2 ?? string.Empty;
        ObsNotesEditor.Text = notes ?? string.Empty;

        Size = new Size(420, 560);
    }

    private void OnObservationCancelTapped(object sender, EventArgs e) => Close(null);

    private async void OnObservationSaveTapped(object sender, EventArgs e)
    {
        var temp = ObsTempEntry.Text?.Trim();
        var bpHigh = ObsBpHighEntry.Text?.Trim();
        var bpLow = ObsBpLowEntry.Text?.Trim();
        var pulse = ObsPulseEntry.Text?.Trim();
        var spo2 = ObsSpo2Entry.Text?.Trim();
        var notes = ObsNotesEditor.Text?.Trim();

        // At least one vital must be filled
        bool hasAny = !string.IsNullOrEmpty(temp)
                   || !string.IsNullOrEmpty(bpHigh)
                   || !string.IsNullOrEmpty(bpLow)
                   || !string.IsNullOrEmpty(pulse)
                   || !string.IsNullOrEmpty(spo2)
                   || !string.IsNullOrEmpty(notes);

        if (!hasAny)
        {
            await Application.Current!.MainPage!.DisplayAlert("Required", "Please fill in at least one field.", "OK");
            return;
        }

        // Validate BP: if one is filled, both should be filled
        bool hasBpHigh = !string.IsNullOrEmpty(bpHigh);
        bool hasBpLow = !string.IsNullOrEmpty(bpLow);
        if (hasBpHigh != hasBpLow)
        {
            await Application.Current!.MainPage!.DisplayAlert("Invalid", "Please enter both systolic and diastolic values for blood pressure.", "OK");
            return;
        }

        Close(new PopupResult(
            Field1: string.IsNullOrEmpty(temp) ? null : temp,
            Field2: string.IsNullOrEmpty(bpHigh) ? null : bpHigh,
            Field3: string.IsNullOrEmpty(bpLow) ? null : bpLow,
            Field4: string.IsNullOrEmpty(pulse) ? null : pulse,
            Field5: string.IsNullOrEmpty(spo2) ? null : spo2,
            Field6: string.IsNullOrEmpty(notes) ? null : notes
        ));
    }

    // New Med Order
    public void ConfigureNewMedOrder(string title, string? medName = null)
    {
        _mode = PopupMode.NewMedOrder;
        TitleLabel.Text = title;

        ListContainer.IsVisible = false;
        FormContainer.IsVisible = false;
        AdjustContainer.IsVisible = false;
        OrderContainer.IsVisible = false;
        SortContainer.IsVisible = false;
        ObservationContainer.IsVisible = false;
        NewMedOrderContainer.IsVisible = true;

        NmoMedNameEntry.Text = medName ?? string.Empty;
        NmoMedNameEntry.IsReadOnly = !string.IsNullOrWhiteSpace(medName);
        NmoUnitPicker.SelectedIndex = 0;
        NmoIndicationEntry.Text = string.Empty;
        NmoQtyEntry.Text = string.Empty;
        NmoReorderEntry.Text = string.Empty;
        NmoNotesEntry.Text = string.Empty;

        // Quick quantity buttons
        NmoQuickButtonsRow.Children.Clear();
        var posColor = (Color)Application.Current!.Resources["Button_Create"];

        foreach (var qty in new[] { 10, 25, 50, 100, 200 })
        {
            var q = qty;
            var btn = new Border
            {
                StrokeThickness = 0,
                HeightRequest = 30,
                MinimumWidthRequest = 46,
                Padding = new Thickness(10, 0),
                BackgroundColor = posColor,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    Text = $"+{q}",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => NmoQtyEntry.Text = q.ToString();
            btn.GestureRecognizers.Add(tap);
            NmoQuickButtonsRow.Children.Add(btn);
        }

        Size = new Size(420, 620);
    }

    private void OnNmoCancelTapped(object sender, EventArgs e) => Close(null);

    private async void OnNmoCreateTapped(object sender, EventArgs e)
    {
        var name = NmoMedNameEntry.Text?.Trim();
        var unit = NmoUnitPicker.SelectedItem as string;
        var indication = NmoIndicationEntry.Text?.Trim();
        var qtyText = NmoQtyEntry.Text?.Trim();
        var reorderText = NmoReorderEntry.Text?.Trim();
        var notes = NmoNotesEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await Application.Current!.MainPage!.DisplayAlert("Required", "Please enter a medication name.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(qtyText))
        {
            await Application.Current!.MainPage!.DisplayAlert("Required", "Please enter a quantity.", "OK");
            return;
        }

        if (!int.TryParse(qtyText, out var qty) || qty <= 0)
        {
            await Application.Current!.MainPage!.DisplayAlert("Invalid", "Please enter a positive number for quantity.", "OK");
            return;
        }

        if (unit == "Select unit") unit = null;

        Close(new PopupResult(
            Field1: name,
            Field2: string.IsNullOrWhiteSpace(unit) ? null : unit,
            Field3: string.IsNullOrWhiteSpace(indication) ? null : indication,
            Field4: qtyText,
            Field5: string.IsNullOrWhiteSpace(reorderText) ? null : reorderText,
            Field6: string.IsNullOrWhiteSpace(notes) ? null : notes
        ));
    }

    private void OnCloseTapped(object sender, EventArgs e) => Close(null);
    private void OnCancelClicked(object sender, EventArgs e) => Close(null);

    private async void OnPrimaryClicked(object sender, EventArgs e)
    {
        if (_mode != PopupMode.Form)
            return;

        var v1 = Field1Entry.Text?.Trim();
        var v2 = Field2Entry.Text?.Trim();

        if (_validator is not null)
        {
            var (ok, error) = _validator(v1, v2);
            if (!ok)
            {
                await Application.Current.MainPage.DisplayAlert("Invalid", error ?? "Invalid input.", "OK");
                return;
            }
        }

        Close(new PopupResult(Field1: v1, Field2: string.IsNullOrWhiteSpace(v2) ? null : v2));
    }
}
