namespace Sorts.Core;

/// <summary>
/// Counts occurrences of each value, then reconstructs the sorted array.
/// Only applicable to non-negative integers within a known, bounded range.
/// k = the maximum value in the array.
/// </summary>
public class CountingSort : ISortingAlgorithm<int>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Counting",
        Best     = "O(n+k)",
        Average  = "O(n+k)",
        Worst    = "O(n+k)",
        Space    = "O(k)",
        IsStable = true
    };

    public int[] Sort(int[] array)
    {
        if (array.Length == 0) return [];

        int max = array[0];
        foreach (int x in array) if (x > max) max = x;

        int[] count = new int[max + 1];
        foreach (int x in array) count[x]++;

        int[] arr = new int[array.Length];
        int   idx = 0;
        for (int v = 0; v <= max; v++)
            while (count[v]-- > 0)
                arr[idx++] = v;

        return arr;
    }
}
