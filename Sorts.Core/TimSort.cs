namespace Sorts.Core;

/// <summary>
/// Hybrid of insertion sort and merge sort. Detects natural runs of size RUN,
/// insertion-sorts each run, then merges them. Used by Python and Java.
/// </summary>
public class TimSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Tim",
        Best     = "O(n)",
        Average  = "O(n log n)",
        Worst    = "O(n log n)",
        Space    = "O(n)",
        IsStable = true
    };

    private const int Run = 32;

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        int n   = arr.Length;

        // Insertion-sort each run of size RUN
        for (int i = 0; i < n; i += Run)
            InsertionSort<T>.SortRange(arr, i, Math.Min(i + Run - 1, n - 1));

        // Merge runs of increasing size
        for (int size = Run; size < n; size *= 2)
        {
            for (int left = 0; left < n; left += 2 * size)
            {
                int mid   = Math.Min(left + size - 1,     n - 1);
                int right = Math.Min(left + 2 * size - 1, n - 1);
                if (mid < right)
                    MergeSort<T>.Merge(arr, left, mid, right);
            }
        }
        return arr;
    }
}
