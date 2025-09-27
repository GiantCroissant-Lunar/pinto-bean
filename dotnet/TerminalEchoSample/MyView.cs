using System.Text;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace TerminalEchoSample;

internal sealed class MyView : Window
{
    private readonly TextView _logView;
    private readonly TextField _inputField;
    private readonly Button _sendButton;
    private readonly Label _statusLabel;
    private readonly StringBuilder _logBuffer = new();

    public MyView()
    {
        Title = $"Terminal Echo Sample ({Application.QuitKey} to quit)";
        Width = Dim.Fill();
        Height = Dim.Fill();

        _logView = new TextView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill() - 2,
            ReadOnly = true,
            WordWrap = true
        };

        _inputField = new TextField
        {
            Y = Pos.Bottom(_logView),
            Width = Dim.Fill() - 12
        };
        _inputField.Accepting += OnInputAccepting;

        _sendButton = new Button
        {
            Text = "_Send",
            X = Pos.Right(_inputField) + 1,
            Y = Pos.Bottom(_logView),
            IsDefault = true
        };
        _sendButton.Accepting += OnSendAccepting;

        _statusLabel = new Label
        {
            Text = "Type and press Enter or Send to echo.",
            X = 0,
            Y = Pos.Bottom(_sendButton)
        };

        Add(_logView, _inputField, _sendButton, _statusLabel);

        AppendLog("Console Terminal Echo Sample");
        AppendLog("--------------------------------");
        AppendLog("Type into the prompt below and press Enter or Send.");
        AppendLog("Ctrl+Q to exit.");

        _inputField.SetFocus();
    }

    private void OnInputAccepting(object? sender, CommandEventArgs args)
    {
        Submit();
        args.Handled = true;
    }

    private void OnSendAccepting(object? sender, CommandEventArgs args)
    {
        Submit();
        args.Handled = true;
    }

    private void Submit()
    {
        var text = _inputField.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        AppendLog($"> {text}");
        AppendLog($"[echo] {text}");

        _inputField.Text = string.Empty;
        _inputField.SetFocus();
    }

    private void AppendLog(string line)
    {
        if (_logBuffer.Length > 0)
        {
            _logBuffer.AppendLine();
        }

        _logBuffer.Append(line);
        _logView.Text = _logBuffer.ToString();
        _logView.MoveEnd();
    }
}
