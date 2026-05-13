using System.Dynamic;

namespace Sorts.Core;

public interface ISortingAlgorithm<T> where T: IComparable<T>
{
    static abstract SortingAlgorithmInfo Info { get;}

    T[] Sort(T[] array);
}