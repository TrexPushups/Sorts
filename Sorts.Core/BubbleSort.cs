namespace Sorts.Core;

/// <summary>
/// BubbleSort Algorithm
/// 
/// Repeatedly swaps adjacent out-of-order elements.
/// Best case O(n) when already sorted (early-exit optimisation included).
/// 
///
/// GIVEN an unsorted array of comparable elements
///
/// FOR each pass over the array
///     SET swapped = false
///
///     FOR each adjacent pair (arr[j], arr[j+1]) in the unsorted portion
///         IF arr[j] is greater than arr[j+1]
///             SWAP arr[j] and arr[j+1]
///             SET swapped = true
///         END IF
///     END FOR
///
///     IF no swaps occurred during this pass
///         BREAK  -- array is already sorted, no need to continue
///     END IF
///
/// END FOR
///
/// RETURN sorted array
///
/// COMPLEXITY
///   Best    O(n)   -- already sorted, early exit fires after one pass
///   Average O(n²)  -- each element bubbles roughly n/2 positions
///   Worst   O(n²)  -- reverse sorted, every pair swapped every pass
///   Space   O(1)   -- swaps are in-place, no extra memory needed
///   Stable  YES    -- equal elements are never swapped, order preserved
/// 
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
        
        int n = array.Length;
        for(int i=0; i<n-1; i++)
        {
            bool swapped = false;
            for(int j=0; j < n-i-1; j++)
            {
                if (array[j].CompareTo(array[j+1]) > 0)
                {
                    (array[j], array[j+1]) = (array[j+1], array[j]);
                    swapped = true;
                }
            }

            if (!swapped)
            {
                break;
            }
        }
        return array;
    }
}
