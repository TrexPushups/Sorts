namespace Sorts.Core;

/// <summary>
/// Inserts each element into its correct position in the already-sorted prefix.
/// Very efficient for small or nearly-sorted arrays.
/// </summary>
public class InsertionSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Insertion",
        Best     = "O(n)",
        Average  = "O(n²)",
        Worst    = "O(n²)",
        Space    = "O(1)",
        IsStable = true
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();

        for (int i = 1; i < arr.Length; i++)
        {
            T   key = arr[i];
            int j   = i - 1;
            while (j >= 0 && arr[j].CompareTo(key) > 0)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = key;
        }
        return arr;
    }

    /// <summary>Internal helper: sorts a sub-range in-place (used by TimSort).</summary>
    internal static void SortRange(T[] arr, int left, int right)
    {
        for (int i = left + 1; i <= right; i++)
        {
            T   key = arr[i];
            int j   = i - 1;
            while (j >= left && arr[j].CompareTo(key) > 0)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = key;
        }
    }
}
