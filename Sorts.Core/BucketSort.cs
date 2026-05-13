namespace Sorts.Core;

/// <summary>
/// Distributes values into equally-spaced buckets, sorts each, then concatenates.
/// Best for uniformly distributed floating-point values in [0, 1).
/// </summary>
public class BucketSort : ISortingAlgorithm<double>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Bucket",
        Best     = "O(n+k)",
        Average  = "O(n+k)",
        Worst    = "O(n²)",
        Space    = "O(n+k)",
        IsStable = true
    };

    public double[] Sort(double[] array)
    {
        int n = array.Length;
        if (n <= 1) return (double[])array.Clone();

        var buckets = new List<double>[n];
        for (int i = 0; i < n; i++) buckets[i] = [];

        foreach (double x in array)
        {
            int idx = (int)(x * n);
            if (idx >= n) idx = n - 1;
            buckets[idx].Add(x);
        }

        double[] arr = new double[n];
        int      k   = 0;
        foreach (var bucket in buckets)
        {
            bucket.Sort();
            foreach (double v in bucket) arr[k++] = v;
        }
        return arr;
    }
}
