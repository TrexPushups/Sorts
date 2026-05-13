namespace Sorts.Core;

/// <summary>
/// Finds the minimum of the unsorted portion and moves it to the front.
/// Makes at most n-1 swaps — fewer than Bubble Sort.
/// </summary>
public class SelectionSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Selection",
        Best     = "O(n²)",
        Average  = "O(n²)",
        Worst    = "O(n²)",
        Space    = "O(1)",
        IsStable = false
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        int n   = arr.Length;

        for (int i = 0; i < n - 1; i++)
        {
            int minIdx = i;
            for (int j = i + 1; j < n; j++)
                if (arr[j].CompareTo(arr[minIdx]) < 0) minIdx = j;

            if (minIdx != i)
                (arr[i], arr[minIdx]) = (arr[minIdx], arr[i]);
        }
        return arr;
    }
}
