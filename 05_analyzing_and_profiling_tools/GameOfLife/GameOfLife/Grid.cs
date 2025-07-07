using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameOfLife
{
    // Original Grid class - unchanged for baseline measurement
    class Grid
    {
        private int SizeX;
        private int SizeY;
        private Cell[,] cells;
        private Cell[,] nextGenerationCells;
        private static Random rnd;
        private Canvas drawCanvas;
        private Ellipse[,] cellsVisuals;

        // Performance tracking fields
        private PerformanceCounter performanceCounter;
        private DispatcherTimer performanceTimer;
        private int generationCount = 0;
        private long totalUpdateTime = 0;
        private long totalGraphicsTime = 0;

        public Grid(Canvas c)
        {
            drawCanvas = c;
            rnd = new Random();
            SizeX = (int)(c.Width / 5);
            SizeY = (int)(c.Height / 5);
            cells = new Cell[SizeX, SizeY];
            nextGenerationCells = new Cell[SizeX, SizeY];
            cellsVisuals = new Ellipse[SizeX, SizeY];

            // Initialize performance tracking
            performanceCounter = new PerformanceCounter();
            InitializePerformanceTracking();

            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                {
                    cells[i, j] = new Cell(i, j, 0, false);
                    nextGenerationCells[i, j] = new Cell(i, j, 0, false);
                }

            SetRandomPattern();
            InitCellsVisuals();
            UpdateGraphics();
        }

        private void InitializePerformanceTracking()
        {
            performanceTimer = new DispatcherTimer();
            performanceTimer.Interval = TimeSpan.FromSeconds(5); // Report every 5 seconds
            performanceTimer.Tick += (sender, e) => ReportPerformanceMetrics();
            performanceTimer.Start();
        }

        private void ReportPerformanceMetrics()
        {
            if (generationCount > 0)
            {
                var avgUpdateTime = totalUpdateTime / generationCount;
                var avgGraphicsTime = totalGraphicsTime / generationCount;
                var memoryUsage = GC.GetTotalMemory(false);

                var report = $@"
=== GAME OF LIFE PERFORMANCE BASELINE ===
Grid Size: {SizeX} x {SizeY} ({SizeX * SizeY} cells)
Generations Processed: {generationCount}
Average Update Time: {avgUpdateTime:F2} ms
Average Graphics Time: {avgGraphicsTime:F2} ms
Total Average Time: {avgUpdateTime + avgGraphicsTime:F2} ms per generation
Memory Usage: {memoryUsage / 1024 / 1024:F2} MB
FPS Estimate: {1000.0 / (avgUpdateTime + avgGraphicsTime):F1} fps
Visual Elements: {SizeX * SizeY} Ellipses
Event Handlers: {SizeX * SizeY * 2} mouse event subscriptions
============================================";

                Debug.WriteLine(report);
                System.Diagnostics.Trace.WriteLine(report);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                {
                    cells[i, j] = new Cell(i, j, 0, false);
                    nextGenerationCells[i, j] = new Cell(i, j, 0, false);
                    cellsVisuals[i, j].Fill = Brushes.Gray;
                }
        }

        void MouseMove(object sender, MouseEventArgs e)
        {
            var cellVisual = sender as Ellipse;

            int i = (int)cellVisual.Margin.Left / 5;
            int j = (int)cellVisual.Margin.Top / 5;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!cells[i, j].IsAlive)
                {
                    cells[i, j].IsAlive = true;
                    cells[i, j].Age = 0;
                    cellVisual.Fill = Brushes.White;
                }
            }
        }

        public void UpdateGraphics()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                    cellsVisuals[i, j].Fill = cells[i, j].IsAlive
                                                  ? (cells[i, j].Age < 2 ? Brushes.White : Brushes.DarkGray)
                                                  : Brushes.Gray;

            sw.Stop();
            totalGraphicsTime += sw.ElapsedMilliseconds;
        }

        public void InitCellsVisuals()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                {
                    cellsVisuals[i, j] = new Ellipse();
                    cellsVisuals[i, j].Width = cellsVisuals[i, j].Height = 5;
                    double left = cells[i, j].PositionX;
                    double top = cells[i, j].PositionY;
                    cellsVisuals[i, j].Margin = new Thickness(left, top, 0, 0);
                    cellsVisuals[i, j].Fill = Brushes.Gray;
                    drawCanvas.Children.Add(cellsVisuals[i, j]);

                    cellsVisuals[i, j].MouseMove += MouseMove;
                    cellsVisuals[i, j].MouseLeftButtonDown += MouseMove;
                }

            sw.Stop();
            Debug.WriteLine($"InitCellsVisuals took: {sw.ElapsedMilliseconds} ms");
            UpdateGraphics();
        }

        public static bool GetRandomBoolean()
        {
            return rnd.NextDouble() > 0.8;
        }

        public void SetRandomPattern()
        {
            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                    cells[i, j].IsAlive = GetRandomBoolean();
        }

        public void UpdateToNextGeneration()
        {
            for (int i = 0; i < SizeX; i++)
                for (int j = 0; j < SizeY; j++)
                {
                    cells[i, j].IsAlive = nextGenerationCells[i, j].IsAlive;
                    cells[i, j].Age = nextGenerationCells[i, j].Age;
                }

            UpdateGraphics();
        }

        public void Update()
        {
            var sw = Stopwatch.StartNew();

            bool alive = false;
            int age = 0;

            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    // Using the "optimized" version from original code
                    CalculateNextGeneration(i, j, ref alive, ref age);
                    nextGenerationCells[i, j].IsAlive = alive;
                    nextGenerationCells[i, j].Age = age;
                }
            }

            sw.Stop();
            totalUpdateTime += sw.ElapsedMilliseconds;
            generationCount++;

            UpdateToNextGeneration();
        }

        public Cell CalculateNextGeneration(int row, int column)    // UNOPTIMIZED
        {
            bool alive;
            int count, age;

            alive = cells[row, column].IsAlive;
            age = cells[row, column].Age;
            count = CountNeighbors(row, column);

            if (alive && count < 2)
                return new Cell(row, column, 0, false);

            if (alive && (count == 2 || count == 3))
            {
                cells[row, column].Age++;
                return new Cell(row, column, cells[row, column].Age, true);
            }

            if (alive && count > 3)
                return new Cell(row, column, 0, false);

            if (!alive && count == 3)
                return new Cell(row, column, 0, true);

            return new Cell(row, column, 0, false);
        }

        public void CalculateNextGeneration(int row, int column, ref bool isAlive, ref int age)     // OPTIMIZED
        {
            isAlive = cells[row, column].IsAlive;
            age = cells[row, column].Age;

            int count = CountNeighbors(row, column);

            if (isAlive && count < 2)
            {
                isAlive = false;
                age = 0;
            }

            if (isAlive && (count == 2 || count == 3))
            {
                cells[row, column].Age++;
                isAlive = true;
                age = cells[row, column].Age;
            }

            if (isAlive && count > 3)
            {
                isAlive = false;
                age = 0;
            }

            if (!isAlive && count == 3)
            {
                isAlive = true;
                age = 0;
            }
        }

        public int CountNeighbors(int i, int j)
        {
            int count = 0;

            if (i != SizeX - 1 && cells[i + 1, j].IsAlive) count++;
            if (i != SizeX - 1 && j != SizeY - 1 && cells[i + 1, j + 1].IsAlive) count++;
            if (j != SizeY - 1 && cells[i, j + 1].IsAlive) count++;
            if (i != 0 && j != SizeY - 1 && cells[i - 1, j + 1].IsAlive) count++;
            if (i != 0 && cells[i - 1, j].IsAlive) count++;
            if (i != 0 && j != 0 && cells[i - 1, j - 1].IsAlive) count++;
            if (j != 0 && cells[i, j - 1].IsAlive) count++;
            if (i != SizeX - 1 && j != 0 && cells[i + 1, j - 1].IsAlive) count++;

            return count;
        }

        // Memory leak detection methods
        public void CheckForMemoryLeaks()
        {
            var report = $@"
                    === MEMORY LEAK ANALYSIS ===
                    Canvas Children Count: {drawCanvas.Children.Count}
                    Expected Children Count: {SizeX * SizeY}
                    Ellipse Array Size: {cellsVisuals.Length}
                    Mouse Event Subscriptions: {SizeX * SizeY * 2} (estimated)
                    Current Memory: {GC.GetTotalMemory(false) / 1024 / 1024:F2} MB
                    Memory after GC: {GC.GetTotalMemory(true) / 1024 / 1024:F2} MB
                    ==============================";

            Debug.WriteLine(report);
        }

        // Force garbage collection and measure
        public void ForceGarbageCollection()
        {
            var beforeGC = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var afterGC = GC.GetTotalMemory(false);

            Debug.WriteLine($"Memory before GC: {beforeGC / 1024 / 1024:F2} MB");
            Debug.WriteLine($"Memory after GC: {afterGC / 1024 / 1024:F2} MB");
            Debug.WriteLine($"Memory freed: {(beforeGC - afterGC) / 1024 / 1024:F2} MB");
        }

        // Cleanup method to prevent memory leaks
        public void Cleanup()
        {
            performanceTimer?.Stop();

            // Remove event handlers to prevent memory leaks
            if (cellsVisuals != null)
            {
                for (int i = 0; i < SizeX; i++)
                    for (int j = 0; j < SizeY; j++)
                    {
                        if (cellsVisuals[i, j] != null)
                        {
                            cellsVisuals[i, j].MouseMove -= MouseMove;
                            cellsVisuals[i, j].MouseLeftButtonDown -= MouseMove;
                        }
                    }
            }

            drawCanvas?.Children.Clear();
        }
    }

    public class PerformanceCounter
    {
        private Stopwatch stopwatch;
        private long totalOperations = 0;
        private long totalTime = 0;

        public PerformanceCounter()
        {
            stopwatch = new Stopwatch();
        }

        public void StartOperation()
        {
            stopwatch.Restart();
        }

        public void EndOperation()
        {
            stopwatch.Stop();
            totalTime += stopwatch.ElapsedMilliseconds;
            totalOperations++;
        }

        public double GetAverageTime()
        {
            return totalOperations > 0 ? (double)totalTime / totalOperations : 0;
        }

        public void Reset()
        {
            totalOperations = 0;
            totalTime = 0;
        }
    }
}