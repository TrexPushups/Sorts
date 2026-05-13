namespace Sorts.Core;

/// <summary>
/// Divides the array recursively, then merges sorted halves.
/// Guaranteed O(n log n) — preferred when stability is required.
/// </summary>
public class MergeSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Merge",
        Best     = "O(n log n)",
        Average  = "O(n log n)",
        Worst    = "O(n log n)",
        Space    = "O(n)",
        IsStable = true
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        SortRange(arr, 0, arr.Length - 1);
        return arr;
    }

    internal static void SortRange(T[] arr, int left, int right)
    {
        if (left >= right) return;
        int mid = (left + right) / 2;
        SortRange(arr, left, mid);
        SortRange(arr, mid + 1, right);
        Merge(arr, left, mid, right);
    }

    internal static void Merge(T[] arr, int left, int mid, int right)
    {
        T[] tmp = new T[right - left + 1];
        int i = left, j = mid + 1, k = 0;

        while (i <= mid && j <= right)
            tmp[k++] = arr[i].CompareTo(arr[j]) <= 0 ? arr[i++] : arr[j++];

        while (i <= mid)   tmp[k++] = arr[i++];
        while (j <= right) tmp[k++] = arr[j++];

        Array.Copy(tmp, 0, arr, left, tmp.Length);
    }
}
