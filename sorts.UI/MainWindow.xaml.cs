using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace Sorts.UI;

// ─────────────────────────────────────────────────────────────────────────────
// MainWindow code-behind
// ─────────────────────────────────────────────────────────────────────────────

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    // Wire progress bar width to the actual rendered width of Row 2
    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        if (DataContext is MainViewModel vm)
            vm.ContainerWidth = info.NewSize.Width;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Observable base
// ─────────────────────────────────────────────────────────────────────────────

public class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// RelayCommand
// ─────────────────────────────────────────────────────────────────────────────

public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
    public void Execute(object? _) => execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? p) => canExecute?.Invoke(p is T t ? t : default) ?? true;
    public void Execute(object? p) => execute(p is T t ? t : default);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

// ─────────────────────────────────────────────────────────────────────────────
// Test data model
// ─────────────────────────────────────────────────────────────────────────────

public enum TestStatus { Idle, Running, Pass, Fail }

public record TestDetail(
    string Declared, double DeclaredR2,
    string Best,     double BestR2,
    double Gap,      double Tol);

public record TestDefinition(
    string Name, string Group, bool ExpectedPass, int Ms, TestDetail? Detail);

// ─────────────────────────────────────────────────────────────────────────────
// TestResultVm — one row in the list
// ─────────────────────────────────────────────────────────────────────────────

public class TestResultVm : ObservableBase
{
    public TestDefinition Definition { get; }

    public TestResultVm(TestDefinition def)
    {
        Definition = def;
        ToggleDetailCommand = new RelayCommand(
            () => { if (HasDetail) IsExpanded = !IsExpanded; },
            () => HasDetail);
    }

    public string Name => Definition.Name;

    private TestStatus _status = TestStatus.Idle;
    public TestStatus Status
    {
        get => _status;
        set
        {
            if (Set(ref _status, value))
            {
                OnPropertyChanged(nameof(NameColor));
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (Set(ref _isExpanded, value))
                OnPropertyChanged(nameof(DetailVisibility));
        }
    }

    public bool HasDetail => Definition.Detail is not null && Status == TestStatus.Fail;

    public Brush NameColor => Status switch
    {
        TestStatus.Idle    => new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x5b)),
        TestStatus.Running => new SolidColorBrush(Color.FromRgb(0xa1, 0xa1, 0xaa)),
        TestStatus.Fail    => new SolidColorBrush(Color.FromRgb(0xf8, 0x71, 0x71)),
        _                  => new SolidColorBrush(Color.FromRgb(0xd4, 0xd4, 0xd8)),
    };

    public string DurationText => Status switch
    {
        TestStatus.Running                    => "running…",
        TestStatus.Pass or TestStatus.Fail    => Definition.Ms > 0 ? $"{Definition.Ms}ms" : "N/A",
        _                                     => "",
    };

    public Visibility ChevronVisibility =>
        Definition.Detail is not null ? Visibility.Visible : Visibility.Hidden;

    public Visibility DetailVisibility =>
        IsExpanded && Definition.Detail is not null && Status == TestStatus.Fail
            ? Visibility.Visible : Visibility.Collapsed;

    // Detail panel bindings
    public string DetailHeader => Definition.Detail is { } d
        ? $"COMPLEXITY MISMATCH — [{Name.Replace("_MatchesWorstCaseComplexity", "").Replace("_MatchesBestCaseComplexity", "").Replace("_", "")}]"
        : "";

    public double DeclaredR2 => Definition.Detail?.DeclaredR2 ?? 0;
    public double BestR2     => Definition.Detail?.BestR2     ?? 0;

    public string DeclaredLabel => Definition.Detail is { } d
        ? $"{d.Declared} {d.DeclaredR2:F4}" : "";
    public string BestLabel     => Definition.Detail is { } d
        ? $"{d.Best} {d.BestR2:F4}" : "";
    public string GapLabel      => Definition.Detail is { } d
        ? $"{d.Gap:F4} > tol {d.Tol:F4}" : "";

    public ICommand ToggleDetailCommand { get; }

    public void Reset()
    {
        Status     = TestStatus.Idle;
        IsExpanded = false;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GroupVm
// ─────────────────────────────────────────────────────────────────────────────

public class GroupVm : ObservableBase
{
    public string Name { get; }
    public ObservableCollection<TestResultVm> Tests { get; } = [];

    public GroupVm(string name) => Name = name.ToUpperInvariant();

    public string Summary
    {
        get
        {
            int passed = Tests.Count(t => t.Status == TestStatus.Pass);
            int failed = Tests.Count(t => t.Status == TestStatus.Fail);
            int total  = Tests.Count;
            if (passed + failed == 0) return "";
            var parts = new List<string>();
            if (passed > 0) parts.Add($"{passed}✓");
            if (failed > 0) parts.Add($"{failed}✗");
            return string.Join(" ", parts) + $" / {total}";
        }
    }

    public void NotifySummary() => OnPropertyChanged(nameof(Summary));
}

// ─────────────────────────────────────────────────────────────────────────────
// MainViewModel
// ─────────────────────────────────────────────────────────────────────────────

public class MainViewModel : ObservableBase
{
    // ── Static test definitions ───────────────────────────────────────────────

    private static readonly TestDefinition[] AllTests =
    [
        new("BubbleSort_Correctness",                  "Correctness",  true,  2,   null),
        new("SelectionSort_Correctness",                "Correctness",  true,  3,   null),
        new("InsertionSort_Correctness",                "Correctness",  true,  2,   null),
        new("MergeSort_Correctness",                    "Correctness",  true,  3,   null),
        new("QuickSort_Correctness",                    "Correctness",  true,  2,   null),
        new("HeapSort_Correctness",                     "Correctness",  true,  2,   null),
        new("ShellSort_Correctness",                    "Correctness",  true,  2,   null),
        new("CountingSort_Correctness",                 "Correctness",  true,  2,   null),
        new("RadixSort_Correctness",                    "Correctness",  true,  18,  null),
        new("TimSort_Correctness",                      "Correctness",  true,  3,   null),
        new("AllSorts_DoNotMutateInput",                "Immutability", true,  5,   null),
        new("StableSorts_PreserveRelativeOrder",        "Stability",    true,  4,   null),
        new("BubbleSort_MatchesBestCaseComplexity",     "Best Case",    true,  4,   null),
        new("InsertionSort_MatchesBestCaseComplexity",  "Best Case",    true,  4,   null),
        new("TimSort_MatchesBestCaseComplexity",        "Best Case",    true,  5,   null),
        new("BubbleSort_MatchesWorstCaseComplexity",    "Worst Case",   true,  689, null),
        new("SelectionSort_MatchesWorstCaseComplexity", "Worst Case",   true,  378, null),
        new("InsertionSort_MatchesWorstCaseComplexity", "Worst Case",   false, 535,
            new("O(n²)", 0.9033, "O(n log n)", 0.9848, 0.0816, 0.0500)),
        new("MergeSort_MatchesWorstCaseComplexity",     "Worst Case",   true,  10,  null),
        new("QuickSort_MatchesWorstCaseComplexity",     "Worst Case",   true,  8,   null),
        new("HeapSort_MatchesWorstCaseComplexity",      "Worst Case",   true,  12,  null),
        new("ShellSort_MatchesWorstCaseComplexity",     "Worst Case",   false, 13,
            new("O(n²)", 0.8576, "O(n)", 0.9989, 0.1413, 0.0500)),
        new("CountingSort_MatchesWorstCaseComplexity",  "Worst Case",   true,  4,   null),
        new("RadixSort_MatchesWorstCaseComplexity",     "Worst Case",   true,  18,  null),
        new("TimSort_MatchesWorstCaseComplexity",       "Worst Case",   true,  10,  null),
    ];

    private static readonly string[] GroupOrder =
        ["Correctness", "Immutability", "Stability", "Best Case", "Worst Case"];

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly List<TestResultVm> _allResults;
    private readonly Dictionary<string, GroupVm> _groups;

    private bool   _isRunning;
    private string _filter = "all";
    private double _containerWidth = 960;
    private int    _elapsedMs;
    private int?   _wallMs;

    private System.Windows.Threading.DispatcherTimer? _timer;
    private readonly Stopwatch _stopwatch = new();

    public ObservableCollection<GroupVm> GroupedResults { get; } = [];

    public MainViewModel()
    {
        _allResults = AllTests.Select(t => new TestResultVm(t)).ToList();

        _groups = GroupOrder.ToDictionary(g => g, g => new GroupVm(g));
        foreach (var vm in _allResults)
            _groups[vm.Definition.Group].Tests.Add(vm);

        RefreshGroups();

        RunCommand    = new RelayCommand(async () => await RunTestsAsync(), () => !_isRunning);
        FilterCommand = new RelayCommand<string>(ApplyFilter);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public RelayCommand    RunCommand    { get; }
    public RelayCommand<string> FilterCommand { get; }

    // ── Properties ────────────────────────────────────────────────────────────

    public double ContainerWidth
    {
        get => _containerWidth;
        set { if (Set(ref _containerWidth, value)) OnPropertyChanged(nameof(ProgressWidth)); }
    }

    public bool CanRun => !_isRunning;

    public string RunButtonText  => _isRunning ? "Running…" : "▶  Run tests";
    public string WallTimeText   => _wallMs.HasValue ? $"{_wallMs / 1000.0:F2}s total" : "";

    public int TotalCount  => AllTests.Length;
    public int PassedCount => _allResults.Count(r => r.Status == TestStatus.Pass);
    public int FailedCount => _allResults.Count(r => r.Status == TestStatus.Fail);

    public string DurationText
    {
        get
        {
            if (_isRunning) return $"{_elapsedMs / 1000.0:F1}s";
            int ms = _allResults.Where(r => r.Status is TestStatus.Pass or TestStatus.Fail)
                                .Sum(r => r.Definition.Ms);
            return ms > 0 ? $"{ms / 1000.0:F2}s" : "—";
        }
    }

    public Brush FailedColor => FailedCount > 0
        ? new SolidColorBrush(Color.FromRgb(0xdc, 0x26, 0x26))
        : new SolidColorBrush(Color.FromRgb(0x3f, 0x3f, 0x46));

    // Progress bar
    public double ProgressWidth
    {
        get
        {
            int done  = _allResults.Count(r => r.Status is TestStatus.Pass or TestStatus.Fail);
            double pct = TotalCount > 0 ? (double)done / TotalCount : 0;
            return _containerWidth * pct;
        }
    }

    public Brush ProgressColor
    {
        get
        {
            int done = _allResults.Count(r => r.Status is TestStatus.Pass or TestStatus.Fail);
            if (done < TotalCount) return new SolidColorBrush(Color.FromRgb(0x3f, 0x3f, 0x46));
            return FailedCount > 0
                ? new SolidColorBrush(Color.FromRgb(0xdc, 0x26, 0x26))
                : new SolidColorBrush(Color.FromRgb(0x16, 0xa0, 0x3a));
        }
    }

    // Filter tabs
    public bool FilterAll    { get => _filter == "all";    set { if (value) ApplyFilter("all");    } }
    public bool FilterPassed { get => _filter == "passed"; set { if (value) ApplyFilter("passed"); } }
    public bool FilterFailed { get => _filter == "failed"; set { if (value) ApplyFilter("failed"); } }

    public string AllTabText    => $"all ({TotalCount})";
    public string PassedTabText => PassedCount > 0 ? $"passed ({PassedCount})" : "passed";
    public string FailedTabText => FailedCount > 0 ? $"failed ({FailedCount})" : "failed";

    // Footer
    public Visibility FooterVisibility
    {
        get
        {
            int done = _allResults.Count(r => r.Status is TestStatus.Pass or TestStatus.Fail);
            return done == TotalCount ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public string FooterStatus => FailedCount > 0
        ? $"✗ {FailedCount} test{(FailedCount > 1 ? "s" : "")} failed"
        : $"✓ all {TotalCount} tests passed";

    public Brush FooterColor => FailedCount > 0
        ? new SolidColorBrush(Color.FromRgb(0xdc, 0x26, 0x26))
        : new SolidColorBrush(Color.FromRgb(0x16, 0xa0, 0x3a));

    public string FooterRight
    {
        get
        {
            int ms = _allResults.Sum(r => r.Definition.Ms);
            return _wallMs.HasValue
                ? $"{ms}ms test time · {_wallMs / 1000.0:F2}s wall time"
                : "";
        }
    }

    // ── Run logic ─────────────────────────────────────────────────────────────

    private async Task RunTestsAsync()
    {
        _isRunning = true;
        _wallMs    = null;
        _elapsedMs = 0;

        foreach (var r in _allResults) r.Reset();
        foreach (var g in _groups.Values) g.NotifySummary();

        NotifyAll();
        StartTimer();

        // ── Option A: Simulated run (swap for real dotnet test below) ─────────
        await SimulateRunAsync();

        // ── Option B: Real dotnet test run ────────────────────────────────────
        // await RealRunAsync();

        StopTimer();
        _wallMs    = (int)_stopwatch.ElapsedMilliseconds;
        _isRunning = false;
        NotifyAll();
    }

    /// <summary>
    /// Simulates test execution with realistic timing.
    /// Replace with RealRunAsync() once your backend is wired up.
    /// </summary>
    private async Task SimulateRunAsync()
    {
        foreach (var result in _allResults)
        {
            result.Status = TestStatus.Running;
            NotifyProgress();

            int delay = Math.Max(25, Math.Min((int)(result.Definition.Ms * 0.4), 180));
            await Task.Delay(delay);

            result.Status = result.Definition.ExpectedPass ? TestStatus.Pass : TestStatus.Fail;
            _groups[result.Definition.Group].NotifySummary();
            NotifyProgress();
        }
    }

    /// <summary>
    /// Runs the real xUnit test suite via dotnet test and parses .trx output.
    /// Set TestProjectPath to your Sorts.Tests .csproj location.
    /// </summary>
    private async Task RealRunAsync()
    {
        const string TestProjectPath = @"..\Sorts.Tests\Sorts.Tests.csproj";
        string resultsFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sorts-{Guid.NewGuid()}.trx");

        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = $"test \"{TestProjectPath}\" --logger \"trx;LogFileName={resultsFile}\" --no-build",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync();

        if (!File.Exists(resultsFile)) return;

        var doc = XDocument.Load(resultsFile);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        var trxResults = doc.Descendants(ns + "UnitTestResult")
            .ToDictionary(
                r => (string)r.Attribute("testName")!,
                r => (string)r.Attribute("outcome")! == "Passed");

        foreach (var result in _allResults)
        {
            if (trxResults.TryGetValue(result.Name, out bool passed))
            {
                result.Status = passed ? TestStatus.Pass : TestStatus.Fail;
                _groups[result.Definition.Group].NotifySummary();
                NotifyProgress();
                await Task.Delay(20); // Small delay so UI updates feel live
            }
        }

        File.Delete(resultsFile);
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void StartTimer()
    {
        _stopwatch.Restart();
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += (_, _) =>
        {
            _elapsedMs = (int)_stopwatch.ElapsedMilliseconds;
            OnPropertyChanged(nameof(DurationText));
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _stopwatch.Stop();
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    private void ApplyFilter(string? filter)
    {
        _filter = filter ?? "all";
        RefreshGroups();
        OnPropertyChanged(nameof(FilterAll));
        OnPropertyChanged(nameof(FilterPassed));
        OnPropertyChanged(nameof(FilterFailed));
    }

    private void RefreshGroups()
    {
        GroupedResults.Clear();
        foreach (var groupName in GroupOrder)
        {
            var group = _groups[groupName];
            var filtered = group.Tests.Where(t => _filter switch
            {
                "passed" => t.Status == TestStatus.Pass,
                "failed" => t.Status == TestStatus.Fail,
                _        => true,
            }).ToList();

            if (filtered.Count == 0) continue;

            var gVm = new GroupVm(groupName);
            foreach (var t in filtered) gVm.Tests.Add(t);
            GroupedResults.Add(gVm);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void NotifyProgress()
    {
        OnPropertyChanged(nameof(PassedCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(ProgressWidth));
        OnPropertyChanged(nameof(ProgressColor));
        OnPropertyChanged(nameof(FailedColor));
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(PassedTabText));
        OnPropertyChanged(nameof(FailedTabText));
        OnPropertyChanged(nameof(FooterVisibility));
        OnPropertyChanged(nameof(FooterStatus));
        OnPropertyChanged(nameof(FooterColor));
        OnPropertyChanged(nameof(FooterRight));
        RefreshGroups();
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(CanRun));
        OnPropertyChanged(nameof(RunButtonText));
        OnPropertyChanged(nameof(WallTimeText));
        NotifyProgress();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StatusIcon — custom UserControl drawn in code
// ─────────────────────────────────────────────────────────────────────────────

public class StatusIcon : FrameworkElement
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(TestStatus), typeof(StatusIcon),
            new FrameworkPropertyMetadata(TestStatus.Idle, FrameworkPropertyMetadataOptions.AffectsRender));

    public TestStatus Status
    {
        get => (TestStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        double cx = w / 2, cy = h / 2, r = Math.Min(w, h) / 2 - 1;

        switch (Status)
        {
            case TestStatus.Pass:
            {
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x22, 0xc5, 0x5e)), 1.5);
                dc.DrawEllipse(Brushes.Transparent, pen, new Point(cx, cy), r, r);
                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(new Point(cx - r * 0.45, cy + 0), true, false);
                    ctx.LineTo(new Point(cx - r * 0.05, cy + r * 0.4), true, false);
                    ctx.LineTo(new Point(cx + r * 0.5,  cy - r * 0.4), true, false);
                }
                dc.DrawGeometry(Brushes.Transparent, pen, geo);
                break;
            }
            case TestStatus.Fail:
            {
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44)), 1.5);
                dc.DrawEllipse(Brushes.Transparent, pen, new Point(cx, cy), r, r);
                double o = r * 0.4;
                dc.DrawLine(pen, new Point(cx - o, cy - o), new Point(cx + o, cy + o));
                dc.DrawLine(pen, new Point(cx + o, cy - o), new Point(cx - o, cy + o));
                break;
            }
            case TestStatus.Running:
            {
                var bgPen   = new Pen(new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x5b)), 1.5);
                var arcPen  = new Pen(new SolidColorBrush(Color.FromRgb(0xa1, 0xa1, 0xaa)), 1.5);
                dc.DrawEllipse(Brushes.Transparent, bgPen, new Point(cx, cy), r, r);
                var arc = new StreamGeometry();
                using (var ctx = arc.Open())
                {
                    ctx.BeginFigure(new Point(cx, cy - r), false, false);
                    ctx.ArcTo(new Point(cx + r, cy), new Size(r, r), 0, false, SweepDirection.Clockwise, true, false);
                }
                dc.DrawGeometry(Brushes.Transparent, arcPen, arc);
                break;
            }
            default:
            {
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x3f, 0x3f, 0x46)), 1.5);
                dc.DrawEllipse(Brushes.Transparent, pen, new Point(cx, cy), r, r);
                break;
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// R2Bar — custom UserControl for the R² progress bar in detail panels
// ─────────────────────────────────────────────────────────────────────────────

public class R2Bar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(R2Bar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarColorProperty =
        DependencyProperty.Register(nameof(BarColor), typeof(string), typeof(R2Bar),
            new FrameworkPropertyMetadata("#22c55e", FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value   { get => (double)GetValue(ValueProperty);   set => SetValue(ValueProperty, value); }
    public string BarColor{ get => (string)GetValue(BarColorProperty); set => SetValue(BarColorProperty, value); }

    protected override Size MeasureOverride(Size available) => new(available.Width, 4);

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        var bg  = new SolidColorBrush(Color.FromRgb(0x27, 0x27, 0x2a));
        dc.DrawRoundedRectangle(bg, null, new Rect(0, 0, w, h), 2, 2);

        var color = (Color)ColorConverter.ConvertFromString(BarColor);
        var fg    = new SolidColorBrush(color);
        double fillW = Math.Max(0, Math.Min(w * Value, w));
        if (fillW > 0)
            dc.DrawRoundedRectangle(fg, null, new Rect(0, 0, fillW, h), 2, 2);
    }
}
