using BenchmarkDotNet.Running;
using Yokan.PintoBean.Benchmarks;

namespace Yokan.PintoBean.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SelectionStrategyBenchmarks>();
    }
}