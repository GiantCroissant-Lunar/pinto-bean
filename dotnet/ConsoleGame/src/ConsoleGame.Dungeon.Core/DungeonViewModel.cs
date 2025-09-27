using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConsoleGame.Dungeon.Core;

public sealed class DungeonViewModel : INotifyPropertyChanged
{
    private (int X, int Y) _player = (1, 1);

    public (int X, int Y) Player
    {
        get => _player;
        set
        {
            if (_player == value)
            {
                return;
            }

            _player = value;
            OnPropertyChanged();
        }
    }

    public int Width { get; }
    public int Height { get; }

    public ObservableCollection<(int X, int Y, char Glyph)> Entities { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public DungeonViewModel(int width = 20, int height = 10)
    {
        Width = width;
        Height = height;
        Entities.Add((Player.X, Player.Y, '@'));
    }

    public void MoveBy(int dx, int dy)
    {
        var nx = Math.Clamp(Player.X + dx, 0, Width - 1);
        var ny = Math.Clamp(Player.Y + dy, 0, Height - 1);
        Player = (nx, ny);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
