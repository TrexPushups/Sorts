namespace Sorts.Core;

/// <summary>
/// Sorts digit-by-digit from least to most significant,
/// using a stable counting pass at each digit level.
/// k = number of digits in the maximum value.
/// </summary>
public class RadixSort : ISortingAlgorithm<int>
{
    public static SortingAlgorithmInfo Info { get; } = new()
    {
        Name     = "Radix",
        Best     = "O(nk)",
        Average  = "O(nk)",
        Worst    = "O(nk)",
        Space    = "O(n+k)",
        IsStable = true
    };

    public int[] Sort(int[] array)
    {
        if (array.Length == 0) return [];

        int[] arr = (int[])array.Clone();
        int   max = arr[0];
        foreach (int x in arr) if (x > max) max = x;

        for (int exp = 1; max / exp > 0; exp *= 10)
            CountByDigit(arr, exp);

        return arr;
    }

    private static void CountByDigit(int[] arr, int exp)
    {
        int   n      = arr.Length;
        int[] output = new int[n];
        int[] count  = new int[10];

        foreach (int x in arr) count[(x / exp) % 10]++;
        for (int i = 1; i < 10; i++) count[i] += count[i - 1];
        for (int i = n - 1; i >= 0; i--)
        {
            int digit = (arr[i] / exp) % 10;
            output[--count[digit]] = arr[i];
        }
        Array.Copy(output, arr, n);
    }
}
