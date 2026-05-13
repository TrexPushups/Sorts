namespace Sorts.Core;

/// <summary>
/// Partitions around a median-of-three pivot and recurses on each side.
/// Fastest general-purpose sort in practice; worst case is rare.
/// </summary>
public class QuickSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Quick",
        Best     = "O(n log n)",
        Average  = "O(n log n)",
        Worst    = "O(n²)",
        Space    = "O(log n)",
        IsStable = false
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        SortRange(arr, 0, arr.Length - 1);
        return arr;
    }

    private static void SortRange(T[] arr, int low, int high)
    {
        if (low >= high) return;
        int pivot = Partition(arr, low, high);
        SortRange(arr, low,       pivot - 1);
        SortRange(arr, pivot + 1, high);
    }

    private static int Partition(T[] arr, int low, int high)
    {
        // Median-of-three pivot selection
        int mid = (low + high) / 2;
        if (arr[low].CompareTo(arr[mid])  > 0) (arr[low], arr[mid])  = (arr[mid],  arr[low]);
        if (arr[low].CompareTo(arr[high]) > 0) (arr[low], arr[high]) = (arr[high], arr[low]);
        if (arr[mid].CompareTo(arr[high]) > 0) (arr[mid], arr[high]) = (arr[high], arr[mid]);
        (arr[mid], arr[high]) = (arr[high], arr[mid]); // Move median pivot to end

        T   pivot = arr[high];
        int i     = low - 1;

        for (int j = low; j < high; j++)
            if (arr[j].CompareTo(pivot) <= 0)
                (arr[++i], arr[j]) = (arr[j], arr[i]);

        (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
        return i + 1;
    }
}
