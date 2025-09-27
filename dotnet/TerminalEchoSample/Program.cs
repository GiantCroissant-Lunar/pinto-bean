using Terminal.Gui;
using Terminal.Gui.App;
using TerminalEchoSample;

Application.Init();

try
{
    Application.Run(new MyView());
}
finally
{
    Application.Shutdown();
}
