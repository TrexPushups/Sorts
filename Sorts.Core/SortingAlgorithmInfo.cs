namespace Sorts.Core;

public record class SortingAlgorithmInfo
{

    public required string Name        { get; init; }
    public required string Best        { get; init; }
    public required string Average     { get; init; }
    public required string Worst       { get; init; }
    public required string Space       { get; init; }
    public bool   IsStable    { get; init; }

    public override string ToString() =>
    $"{Name} | Best: {Best} | Avg: {Average} | Worst: {Worst} | Space: {Space} | Stable: {IsStable}";

}
