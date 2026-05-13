namespace Sorts.Core;

/// <summary>
/// Builds a max-heap, then repeatedly extracts the maximum.
/// Guaranteed O(n log n) with no extra memory.
/// </summary>
public class HeapSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Heap",
        Best     = "O(n log n)",
        Average  = "O(n log n)",
        Worst    = "O(n log n)",
        Space    = "O(1)",
        IsStable = false
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        int n   = arr.Length;

        // Build max-heap
        for (int i = n / 2 - 1; i >= 0; i--)
            Heapify(arr, n, i);

        // Extract elements one by one
        for (int i = n - 1; i > 0; i--)
        {
            (arr[0], arr[i]) = (arr[i], arr[0]);
            Heapify(arr, i, 0);
        }
        return arr;
    }

    private static void Heapify(T[] arr, int n, int root)
    {
        int largest = root, l = 2 * root + 1, r = 2 * root + 2;
        if (l < n && arr[l].CompareTo(arr[largest]) > 0) largest = l;
        if (r < n && arr[r].CompareTo(arr[largest]) > 0) largest = r;
        if (largest != root)
        {
            (arr[root], arr[largest]) = (arr[largest], arr[root]);
            Heapify(arr, n, largest);
        }
    }
}
