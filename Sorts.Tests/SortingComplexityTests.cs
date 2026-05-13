using System;
using System.Diagnostics;
using System.Linq;
using Sorts.Core;
using Xunit;

namespace Sorts.Tests;

/// <summary>
/// Empirically validates that each sorting algorithm's observed time-growth
/// matches the Big O class declared in its Info.Worst property.
///
/// APPROACH
/// ────────
/// Big O cannot be proven by timing alone, but it can be falsified.
/// We run each sort on arrays of increasing size, measure elapsed ticks,
/// then fit the observed (n, time) pairs against several growth models via
/// least-squares regression. The algorithm passes if its declared model
/// fits better (lower residual error) than the next-worse complexity class.
///
/// Growth models fitted:
///   O(n)        → f(n) = n
///   O(n log n)  → f(n) = n · log₂(n)
///   O(n^1.3)    → f(n) = n^1.3
///   O(n²)       → f(n) = n²
///
/// TOLERANCE
/// ─────────
/// Timing has noise, so we allow the declared model's R² to be at most
/// TOLERANCE worse than the best-fitting model before failing.
/// </summary>
public class SortingComplexityTests
{
    // ── Configuration ────────────────────────────────────────────────────────

    /// <summary>Input sizes used for curve fitting. Spread matters more than quantity.</summary>
    private static readonly int[] Sizes = [100, 500, 1_000, 2_000, 5_000];

    /// <summary>
    /// Number of repeated runs per size — median is taken to suppress JIT noise.
    /// </summary>
    private const int Repetitions = 5;

    /// <summary>
    /// How much worse (in R²) the declared model may be vs the best-fit model
    /// before the test fails. 0.05 = 5 percentage points of tolerance.
    /// </summary>
    private const double Tolerance = 0.05;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static double[] MeasureTimes(Func<int[], int[]> sort)
    {
        var rng   = new Random(42); // Fixed seed → reproducible arrays
        var times = new double[Sizes.Length];

        for (int s = 0; s < Sizes.Length; s++)
        {
            int n = Sizes[s];

            var measurements = new long[Repetitions];
            for (int r = 0; r < Repetitions; r++)
            {
                int[] data = Enumerable.Range(0, n)
                                       .Select(_ => rng.Next())
                                       .ToArray();
                var sw = Stopwatch.StartNew();
                sort(data);
                sw.Stop();
                measurements[r] = sw.ElapsedTicks;
            }

            // Median suppresses outliers better than mean for timing data
            Array.Sort(measurements);
            times[s] = measurements[Repetitions / 2];
        }
        return times;
    }

    /// <summary>
    /// Fits y = c · f(x) via least squares and returns R² (coefficient of determination).
    /// R² ∈ [0, 1]; higher = better fit.
    /// </summary>
    private static double FitR2(double[] x, double[] y, Func<double, double> model)
    {
        double[] fx   = x.Select(model).ToArray();
        double   yMean = y.Average();

        // Least-squares scalar c that minimises Σ(y - c·f(x))²
        double c = fx.Zip(y, (f, yi) => f * yi).Sum()
                 / fx.Select(f => f * f).Sum();

        double[] predicted = fx.Select(f => c * f).ToArray();

        double ssTot = y.Select(yi => Math.Pow(yi - yMean, 2)).Sum();
        double ssRes = y.Zip(predicted, (yi, pi) => Math.Pow(yi - pi, 2)).Sum();

        return ssTot < 1e-10 ? 1.0 : 1.0 - ssRes / ssTot;
    }

    /// <summary>
    /// Maps a Big O string (from SortingAlgorithmInfo) to its growth function.
    /// </summary>
    private static Func<double, double> ModelFor(string bigO) => bigO switch
    {
        "O(n)"       => n => n,
        "O(n log n)" => n => n * Math.Log2(n),
        "O(n^1.3)"   => n => Math.Pow(n, 1.3),
        "O(n²)"      => n => n * n,
        "O(nk)"      => n => n * Math.Log10(n),   // k = digit count ≈ log₁₀(n)
        "O(n+k)"     => n => n,                    // k is bounded; grows as O(n)
        _            => throw new NotSupportedException($"No model for '{bigO}'")
    };

    /// <summary>
    /// Core assertion: measures sort times, fits the declared worst-case model,
    /// and asserts it is the best (or near-best) fitting model.
    /// </summary>
    private static void AssertComplexity(
        Func<int[], int[]>  sort,
        SortingAlgorithmInfo info)
    {
        double[] n     = Sizes.Select(s => (double)s).ToArray();
        double[] times = MeasureTimes(sort);

        // All candidate models — ordered slowest-growing to fastest-growing
        var models = new (string Name, Func<double, double> Fn)[]
        {
            ("O(n)",       n => n),
            ("O(n log n)", n => n * Math.Log2(n)),
            ("O(n^1.3)",   n => Math.Pow(n, 1.3)),
            ("O(nk)",      n => n * Math.Log10(n)),
            ("O(n+k)",     n => n),
            ("O(n²)",      n => n * n),
        };

        double bestR2     = models.Max(m => FitR2(n, times, m.Fn));
        double declaredR2 = FitR2(n, times, ModelFor(info.Worst));

        Assert.True(
            declaredR2 >= bestR2 - Tolerance,
            $"""
            [{info.Name}] Worst-case complexity mismatch.
              Declared : {info.Worst} (R² = {declaredR2:F4})
              Best fit : {models.MaxBy(m => FitR2(n, times, m.Fn)).Name} (R² = {bestR2:F4})
              Gap      : {bestR2 - declaredR2:F4} exceeds tolerance {Tolerance:F4}
            """);
    }

    // ── Correctness helper ───────────────────────────────────────────────────

    /// <summary>Sanity-checks that the output is actually sorted.</summary>
    private static void AssertSorted(int[] result)
    {
        for (int i = 1; i < result.Length; i++)
            Assert.True(result[i - 1] <= result[i],
                $"Array not sorted at index {i}: {result[i - 1]} > {result[i]}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Correctness tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact] public void BubbleSort_Correctness()
    {
        var s = new BubbleSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([1]));
        AssertSorted(s.Sort([]));
        AssertSorted(s.Sort([2, 1]));
    }

    [Fact] public void SelectionSort_Correctness()
    {
        var s = new SelectionSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([1, 1, 1, 1]));
    }

    [Fact] public void InsertionSort_Correctness()
    {
        var s = new InsertionSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([1, 2, 3, 4, 5])); // Already sorted
    }

    [Fact] public void MergeSort_Correctness()
    {
        var s = new MergeSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([5, 4, 3, 2, 1])); // Reverse sorted
    }

    [Fact] public void QuickSort_Correctness()
    {
        var s = new QuickSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([1, 2, 3, 4, 5])); // Already sorted — worst case without median pivot
    }

    [Fact] public void HeapSort_Correctness()
    {
        var s = new HeapSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([42]));
    }

    [Fact] public void ShellSort_Correctness()
    {
        var s = new ShellSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([]));
    }

    [Fact] public void CountingSort_Correctness()
    {
        var s = new CountingSort();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([0, 0, 0]));
    }

    [Fact] public void RadixSort_Correctness()
    {
        var s = new RadixSort();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([100, 999, 1, 50, 72]));
    }

    [Fact] public void TimSort_Correctness()
    {
        var s = new TimSort<int>();
        AssertSorted(s.Sort([5, 3, 8, 1, 9, 2, 7, 4, 6]));
        AssertSorted(s.Sort([1, 2, 3, 4, 5])); // Natural run — exercises O(n) best case
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Immutability tests — Sort() must never modify the input array
    // ─────────────────────────────────────────────────────────────────────────

    [Fact] public void AllSorts_DoNotMutateInput()
    {
        int[] original = [5, 3, 8, 1, 9, 2, 7, 4, 6];
        int[] snapshot = (int[])original.Clone();

        new BubbleSort<int>().Sort(original);
        new SelectionSort<int>().Sort(original);
        new InsertionSort<int>().Sort(original);
        new MergeSort<int>().Sort(original);
        new QuickSort<int>().Sort(original);
        new HeapSort<int>().Sort(original);
        new ShellSort<int>().Sort(original);
        new CountingSort().Sort(original);
        new RadixSort().Sort(original);
        new TimSort<int>().Sort(original);

        Assert.Equal(snapshot, original);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Big O complexity tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact] public void BubbleSort_MatchesWorstCaseComplexity()
    {
        // Force worst case: reverse-sorted input maximises swaps → O(n²)
        AssertComplexity(
            arr => new BubbleSort<int>().Sort(arr.OrderDescending().ToArray()),
            BubbleSort<int>.Info);
    }

    [Fact] public void SelectionSort_MatchesWorstCaseComplexity()
    {
        AssertComplexity(
            arr => new SelectionSort<int>().Sort(arr),
            SelectionSort<int>.Info);
    }

    [Fact] public void InsertionSort_MatchesWorstCaseComplexity()
    {
        // Worst case: reverse-sorted
        AssertComplexity(
            arr => new InsertionSort<int>().Sort(arr.OrderDescending().ToArray()),
            InsertionSort<int>.Info);
    }

    [Fact] public void MergeSort_MatchesWorstCaseComplexity()
    {
        // Merge sort has no bad-case input — always O(n log n)
        AssertComplexity(
            arr => new MergeSort<int>().Sort(arr),
            MergeSort<int>.Info);
    }

    [Fact] public void QuickSort_MatchesWorstCaseComplexity()
    {
        // Median-of-three pivot avoids the classic sorted-input worst case;
        // random data exercises the O(n log n) average case reliably
        AssertComplexity(
            arr => new QuickSort<int>().Sort(arr),
            QuickSort<int>.Info);
    }

    [Fact] public void HeapSort_MatchesWorstCaseComplexity()
    {
        AssertComplexity(
            arr => new HeapSort<int>().Sort(arr),
            HeapSort<int>.Info);
    }

    [Fact] public void ShellSort_MatchesWorstCaseComplexity()
    {
        AssertComplexity(
            arr => new ShellSort<int>().Sort(arr),
            ShellSort<int>.Info);
    }

    [Fact] public void CountingSort_MatchesWorstCaseComplexity()
    {
        // k is bounded to max value — keep values small so k doesn't dominate
        AssertComplexity(
            arr => new CountingSort().Sort(arr.Select(x => Math.Abs(x) % 1000).ToArray()),
            CountingSort.Info);
    }

    [Fact] public void RadixSort_MatchesWorstCaseComplexity()
    {
        AssertComplexity(
            arr => new RadixSort().Sort(arr.Select(Math.Abs).ToArray()),
            RadixSort.Info);
    }

    [Fact] public void TimSort_MatchesWorstCaseComplexity()
    {
        // Random data exercises the O(n log n) merge path
        AssertComplexity(
            arr => new TimSort<int>().Sort(arr),
            TimSort<int>.Info);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Best-case complexity tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact] public void BubbleSort_MatchesBestCaseComplexity()
    {
        // Best case: already sorted — early exit fires after one pass → O(n)
        var info = new SortingAlgorithmInfo
        {
            Name = BubbleSort<int>.Info.Name, Best = BubbleSort<int>.Info.Best,
            Average = BubbleSort<int>.Info.Average, Worst = BubbleSort<int>.Info.Best, // test Best as Worst
            Space = BubbleSort<int>.Info.Space, IsStable = BubbleSort<int>.Info.IsStable
        };
        AssertComplexity(
            arr => new BubbleSort<int>().Sort(arr.Order().ToArray()),
            info);
    }

    [Fact] public void InsertionSort_MatchesBestCaseComplexity()
    {
        // Best case: already sorted → O(n)
        var info = InsertionSort<int>.Info with { Worst = InsertionSort<int>.Info.Best };
        AssertComplexity(
            arr => new InsertionSort<int>().Sort(arr.Order().ToArray()),
            info);
    }

    [Fact] public void TimSort_MatchesBestCaseComplexity()
    {
        // Best case: already sorted (one big natural run) → O(n)
        var info = TimSort<int>.Info with { Worst = TimSort<int>.Info.Best };
        AssertComplexity(
            arr => new TimSort<int>().Sort(arr.Order().ToArray()),
            info);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stability tests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies stable sorts preserve the original relative order of equal elements.
    /// Uses a (key, index) pair so we can detect if equal keys were reordered.
    /// </summary>
    [Fact] public void StableSorts_PreserveRelativeOrder()
    {
        // Build input where multiple items share the same sort key
        var input = new (int Key, int OriginalIndex)[]
        {
            (3, 0), (1, 1), (2, 2), (3, 3), (1, 4), (2, 5), (3, 6)
        };

        // Wrap in a comparable that sorts by Key only
        var wrapped = input
            .Select(x => new StableItem(x.Key, x.OriginalIndex))
            .ToArray();

        ISortingAlgorithm<StableItem>[] stableSorters =
        [
            new BubbleSort<StableItem>(),
            new InsertionSort<StableItem>(),
            new MergeSort<StableItem>(),
            new TimSort<StableItem>(),
        ];

        foreach (var sorter in stableSorters)
        {
            var sorted = sorter.Sort(wrapped);

            // Within each group of equal keys, original indices must be ascending
            var groups = sorted.GroupBy(x => x.Key);
            foreach (var group in groups)
            {
                var indices = group.Select(x => x.OriginalIndex).ToList();
                for (int i = 1; i < indices.Count; i++)
                    Assert.True(indices[i] > indices[i - 1],
                        $"{sorter.GetType().Name} is not stable: " +
                        $"key={group.Key}, indices={string.Join(",", indices)}");
            }
        }
    }

    /// <summary>Helper type: comparable by Key, tracks original position.</summary>
    private record StableItem(int Key, int OriginalIndex) : IComparable<StableItem>
    {
        public int CompareTo(StableItem? other) =>
            other is null ? 1 : Key.CompareTo(other.Key);
    }
}