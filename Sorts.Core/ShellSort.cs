namespace Sorts.Core;

/// <summary>
/// Generalises insertion sort by comparing elements separated by a shrinking gap.
/// Uses the Ciura gap sequence for good practical performance.
/// </summary>
public class ShellSort<T> : ISortingAlgorithm<T> where T : IComparable<T>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Shell",
        Best     = "O(n log n)",
        Average  = "O(n^1.3)",
        Worst    = "O(n²)",
        Space    = "O(1)",
        IsStable = false
    };

    // Ciura gap sequence
    private static readonly int[] Gaps = { 701, 301, 132, 57, 23, 10, 4, 1 };

    public T[] Sort(T[] array)
    {
        T[] arr = (T[])array.Clone();

        foreach (int gap in Gaps)
        {
            for (int i = gap; i < arr.Length; i++)
            {
                T   temp = arr[i];
                int j    = i;
                while (j >= gap && arr[j - gap].CompareTo(temp) > 0)
                {
                    arr[j] = arr[j - gap];
                    j -= gap;
                }
                arr[j] = temp;
            }
        }
        return arr;
    }
}
