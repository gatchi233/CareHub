using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CareHub.Pages.UI.Behaviors
{
    public class SinBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? []);

            entry.Text = digits.Length switch
            {
                > 6 => $"{digits[..3]}-{digits[3..6]}-{digits[6..Math.Min(9, digits.Length)]}",
                > 3 => $"{digits[..3]}-{digits[3..]}",
                > 0 => digits,
                _ => ""
            };
        }
    }

    public class DobBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;

            // Digits only
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

            if (digits.Length >= 8)
                digits = digits[..8]; // YYYYMMDD max

            string formatted = digits.Length switch
            {
                >= 7 => $"{digits[..4]}-{digits[4..6]}-{digits[6..]}",
                >= 5 => $"{digits[..4]}-{digits[4..]}",
                >= 1 => digits,
                _ => ""
            };

            if (entry.Text != formatted)
                entry.Text = formatted;
        }
    }

    public class PhoneNumberBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? []);

            entry.Text = digits.Length switch
            {
                > 6 => $"({digits[..3]}) {digits[3..6]}-{digits[6..Math.Min(10, digits.Length)]}",
                > 3 => $"({digits[..3]}) {digits[3..]}",
                > 0 => $"({digits}",
                _ => ""
            };
        }
    }

}

