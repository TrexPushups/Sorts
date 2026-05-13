namespace Sorts.Core;

/// <summary>
/// Repeatedly swaps adjacent out-of-order elements.
/// Best case O(n) when already sorted (early-exit optimisation included).
/// </summary>
public class BubbleSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Bubble",
        Best     = "O(n)",
        Average  = "O(n²)",
        Worst    = "O(n²)",
        Space    = "O(1)",
        IsStable = true
    };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();
        int n   = arr.Length;

        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - i - 1; j++)
            {
                if (arr[j].CompareTo(arr[j + 1]) > 0)
                {
                    (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                    swapped = true;
                }
            }
            if (!swapped) break; // Already sorted — early exit
        }
        return arr;
    }
}
