using System;

namespace ConsoleGame.Contracts;

public interface IAudioService : IDisposable
{
    bool IsEnabled { get; }

    void Play(string pathOrUrl);
}
