using ConsoleGame.Plugin.Audio;
using ConsoleGame.Plugin.PureDiProbe.Core;
using Microsoft.Extensions.DependencyInjection;

await PureDiProbeRunner.RunAsync(args, context =>
{
    context.Services.AddLogging();
    context.Services.AddAudioSubsystem(context.Configuration);
    return ValueTask.CompletedTask;
}, CancellationToken.None);
