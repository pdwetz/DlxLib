using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DlxLib.EnumerableArrayAdapter;

// I have used variable names c, r and j deliberately to make it easier to
// relate the code back to the original "Dancing Links" paper:
//
//      Dancing Links (Donald E. Knuth, Stanford University)
//      http://arxiv.org/pdf/cs/0011047v1.pdf

namespace DlxLib
{
    /// <summary>
    /// Use this class to solve exact cover problems.
    /// </summary>
    public class Dlx
    {
        private readonly CancellationToken _cancellationToken;

        private class SearchData
        {
            public SearchData(ColumnObject root)
            {
                Root = root;
            }

            public ColumnObject Root { get; private set; }
            public int IterationCount { get; private set; }
            public int SolutionCount { get; private set; }

            public void IncrementIterationCount()
            {
                IterationCount++;
            }

            public void IncrementSolutionCount()
            {
                SolutionCount++;
            }

            public void PushCurrentSolutionRowIndex(int rowIndex)
            {
                _currentSolution.Push(rowIndex);
            }

            public void PopCurrentSolutionRowIndex()
            {
                _currentSolution.Pop();
            }

            public Solution CurrentSolution
            {
                get { return new Solution(_currentSolution.ToList()); }
            }

            private readonly Stack<int> _currentSolution = new Stack<int>();
        }

        /// <summary>
        /// Callers should use this constructor when they do not need to be able to request cancellation.
        /// </summary>
        public Dlx()
            : this(CancellationToken.None)
        {
        }

        /// <summary>
        /// Callers should use this constructor when they need to be able to request cancellation.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken" /> that
        /// the <see cref="Dlx.Solve" /> method will observe.</param>
        public Dlx(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given a 2-dimensional array of <see cref="System.Boolean" />.
        /// </summary>
        /// <param name="matrix">A matrix of <see cref="System.Boolean" /> values representing an exact cover problem.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        public IEnumerable<Solution> Solve(bool[,] matrix)
        {
            return Solve<bool>(matrix);
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given a 2-dimensional array of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the matrix.</typeparam>
        /// <param name="matrix">A matrix of <typeparamref name="T"/> values representing an exact cover problem.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        public IEnumerable<Solution> Solve<T>(T[,] matrix)
        {
            var defaultEqualityComparerT = EqualityComparer<T>.Default;
            var defaultT = default(T);
            Func<T, bool> predicate = t => !defaultEqualityComparerT.Equals(t, defaultT);
            return Solve(matrix, predicate);
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given a 2-dimensional array of <typeparamref name="T"/>
        /// and a predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the matrix.</typeparam>
        /// <param name="matrix">A matrix of <typeparamref name="T"/> values representing an exact cover problem.</param>
        /// <param name="predicate">A predicate which is invoked for each value in the matrix to determine
        /// whether the value represents a logical 1 or a logical 0 indicated by returning <c>true</c>
        /// or <c>false</c> respectively.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        public IEnumerable<Solution> Solve<T>(T[,] matrix, Func<T, bool> predicate)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");
            return Solve(matrix, m => new Enumerable2DArray<T>(m), r => r, predicate);
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given an arbitrary data structure representing
        /// the matrix.
        /// </summary>
        /// <typeparam name="TData">The type of the data structure that represents the exact cover problem.</typeparam>
        /// <typeparam name="TRow">The type of the data structure that represents rows in the matrix.</typeparam>
        /// <typeparam name="TCol">The type of the data structure that represents columns in the matrix.</typeparam>
        /// <param name="data">The top-level data structure that represents the exact cover problem.</param>
        /// <param name="iterateRows">A System.Action delegate that will be invoked to iterate the rows in the matrix.</param>
        /// <param name="iterateCols">A System.Action delegate that will be invoked to iterate the columns
        /// in a particular row in the matrix.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        [Obsolete("The API of this method is too complicated - use the Solve<TData, TRow, TCol> overload that has Funcs that return IEnumerable instead", false)]
        public IEnumerable<Solution> Solve<TData, TRow, TCol>(
            TData data,
            Action<TData, Action<TRow>> iterateRows,
            Action<TRow, Action<TCol>> iterateCols)
        {
            var defaultEqualityComparerTCol = EqualityComparer<TCol>.Default;
            var defaultTCol = default(TCol);
            Func<TCol, bool> predicate = col => !defaultEqualityComparerTCol.Equals(col, defaultTCol);
            return Solve(data, iterateRows, iterateCols, predicate);
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given an arbitrary data structure representing
        /// the matrix and a predicate.
        /// </summary>
        /// <typeparam name="TData">The type of the data structure that represents the exact cover problem.</typeparam>
        /// <typeparam name="TRow">The type of the data structure that represents rows in the matrix.</typeparam>
        /// <typeparam name="TCol">The type of the data structure that represents columns in the matrix.</typeparam>
        /// <param name="data">The top-level data structure that represents the exact cover problem.</param>
        /// <param name="iterateRows">A System.Action delegate that will be invoked to iterate the rows in the matrix.</param>
        /// <param name="iterateCols">A System.Action delegate that will be invoked to iterate the columns
        /// in a particular row in the matrix.</param>
        /// <param name="predicate">A predicate which is invoked for each value in the matrix to determine
        /// whether the value represents a logical 1 or a logical 0 indicated by returning <c>true</c>
        /// or <c>false</c> respectively.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        [Obsolete("The API of this method is too complicated - use the Solve<TData, TRow, TCol> overload that has Funcs that return IEnumerable instead", false)]
        public IEnumerable<Solution> Solve<TData, TRow, TCol>(
            TData data,
            Action<TData, Action<TRow>> iterateRows,
            Action<TRow, Action<TCol>> iterateCols,
            Func<TCol, bool> predicate)
        {
            if (data.Equals(default(TData))) throw new ArgumentNullException("data");
            var root = BuildInternalStructure(
                data,
                 d =>
                 {
                     // Could we yield return instead ?
                     var rows = new List<TRow>();
                     iterateRows(d, row => rows.Add(row));
                     return rows;
                 },
                row =>
                {
                    // Could we yield return instead ?
                    var cols = new List<TCol>();
                    iterateCols(row, col => cols.Add(col));
                    return cols;
                },
                predicate);
            return Search(0, new SearchData(root));
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given an arbitrary data structure representing
        /// the matrix.
        /// </summary>
        /// <typeparam name="TData">The type of the data structure that represents the exact cover problem.</typeparam>
        /// <typeparam name="TRow">The type of the data structure that represents rows in the matrix.</typeparam>
        /// <typeparam name="TCol">The type of the data structure that represents columns in the matrix.</typeparam>
        /// <param name="data">The top-level data structure that represents the exact cover problem.</param>
        /// <param name="iterateRows">A System.Func delegate that will be invoked to iterate the rows in the matrix.</param>
        /// <param name="iterateCols">A System.Func delegate that will be invoked to iterate the columns
        /// in a particular row in the matrix.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        public IEnumerable<Solution> Solve<TData, TRow, TCol>(
            TData data,
            Func<TData, IEnumerable<TRow>> iterateRows,
            Func<TRow, IEnumerable<TCol>> iterateCols)
        {
            var defaultEqualityComparerTCol = EqualityComparer<TCol>.Default;
            var defaultTCol = default(TCol);
            Func<TCol, bool> predicate = col => !defaultEqualityComparerTCol.Equals(col, defaultTCol);
            return Solve(data, iterateRows, iterateCols, predicate);
        }

        /// <summary>
        /// Find all possible solutions to an exact cover problem given an arbitrary data structure representing
        /// the matrix and a predicate.
        /// </summary>
        /// <typeparam name="TData">The type of the data structure that represents the exact cover problem.</typeparam>
        /// <typeparam name="TRow">The type of the data structure that represents rows in the matrix.</typeparam>
        /// <typeparam name="TCol">The type of the data structure that represents columns in the matrix.</typeparam>
        /// <param name="data">The top-level data structure that represents the exact cover problem.</param>
        /// <param name="iterateRows">A System.Func delegate that will be invoked to iterate the rows in the matrix.</param>
        /// <param name="iterateCols">A System.Func delegate that will be invoked to iterate the columns
        /// in a particular row in the matrix.</param>
        /// <param name="predicate">A predicate which is invoked for each value in the matrix to determine
        /// whether the value represents a logical 1 or a logical 0 indicated by returning <c>true</c>
        /// or <c>false</c> respectively.</param>
        /// <returns>Yields <see cref="Solution" /> objects as they are found.</returns>
        public IEnumerable<Solution> Solve<TData, TRow, TCol>(
            TData data,
            Func<TData, IEnumerable<TRow>> iterateRows,
            Func<TRow, IEnumerable<TCol>> iterateCols,
            Func<TCol, bool> predicate)
        {
            if (data.Equals(default(TData))) throw new ArgumentNullException("data");
            var root = BuildInternalStructure(data, iterateRows, iterateCols, predicate);
            return Search(0, new SearchData(root));
        }

        /// <summary>
        /// Occurs once when the internal search algorithm starts.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs once when the internal search algorithm finishes.
        /// </summary>
        public event EventHandler Finished;

        /// <summary>
        /// Occurs when the caller requests cancellation via the CancellationToken passed to <see cref="Dlx(CancellationToken)" />.
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Occurs for each set of rows considered by the internal search algorithm.
        /// </summary>
        public event EventHandler<SearchStepEventArgs> SearchStep;

        /// <summary>
        /// Occurs for each solution found to the original matrix.
        /// </summary>
        public event EventHandler<SolutionFoundEventArgs> SolutionFound;

        private bool IsCancelled()
        {
            return _cancellationToken.IsCancellationRequested;
        }

        // private static ColumnObject BuildInternalStructure<TData, TRow, TCol>(
        //     TData data,
        //     Action<TData, Action<TRow>> iterateRows,
        //     Action<TRow, Action<TCol>> iterateCols,
        //     Func<TCol, bool> predicate)
        // {
        //     var root = new ColumnObject();
        // 
        //     int? numColumns = null;
        //     var rowIndex = 0;
        //     var colIndexToListHeader = new Dictionary<int, ColumnObject>();
        // 
        //     iterateRows(data, row =>
        //     {
        //         DataObject firstDataObjectInThisRow = null;
        //         var localRowIndex = rowIndex;
        //         var colIndex = 0;
        // 
        //         iterateCols(row, col =>
        //         {
        //             if (localRowIndex == 0)
        //             {
        //                 var listHeader = new ColumnObject();
        //                 root.AppendColumnHeader(listHeader);
        //                 colIndexToListHeader[colIndex] = listHeader;
        //             }
        // 
        //             if (predicate(col))
        //             {
        //                 // Create a new DataObject and add it to the appropriate list header.
        //                 var listHeader = colIndexToListHeader[colIndex];
        //                 var dataObject = new DataObject(listHeader, localRowIndex);
        // 
        //                 if (firstDataObjectInThisRow != null)
        //                     firstDataObjectInThisRow.AppendToRow(dataObject);
        //                 else
        //                     firstDataObjectInThisRow = dataObject;
        //             }
        // 
        //             colIndex++;
        //         });
        // 
        //         if (numColumns.HasValue)
        //         {
        //             if (colIndex != numColumns)
        //             {
        //                 throw new ArgumentException("All rows must contain the same number of columns!", "data");
        //             }
        //         }
        //         else
        //         {
        //             numColumns = colIndex;
        //         }
        // 
        //         rowIndex++;
        //     });
        // 
        //     return root;
        // }

        private static ColumnObject BuildInternalStructure<TData, TRow, TCol>(
            TData data,
            Func<TData, IEnumerable<TRow>> iterateRows,
            Func<TRow, IEnumerable<TCol>> iterateCols,
            Func<TCol, bool> predicate)
        {
            var root = new ColumnObject();

            int? numColumns = null;
            var rowIndex = 0;
            var colIndexToListHeader = new Dictionary<int, ColumnObject>();

            foreach (var row in iterateRows(data))
            {
                DataObject firstDataObjectInThisRow = null;
                var localRowIndex = rowIndex;
                var colIndex = 0;

                foreach (var col in iterateCols(row))
                {
                    if (localRowIndex == 0)
                    {
                        var listHeader = new ColumnObject();
                        root.AppendColumnHeader(listHeader);
                        colIndexToListHeader[colIndex] = listHeader;
                    }

                    if (predicate(col))
                    {
                        // Create a new DataObject and add it to the appropriate list header.
                        var listHeader = colIndexToListHeader[colIndex];
                        var dataObject = new DataObject(listHeader, localRowIndex);

                        if (firstDataObjectInThisRow != null)
                            firstDataObjectInThisRow.AppendToRow(dataObject);
                        else
                            firstDataObjectInThisRow = dataObject;
                    }

                    colIndex++;
                }

                if (numColumns.HasValue)
                {
                    if (colIndex != numColumns)
                    {
                        throw new ArgumentException("All rows must contain the same number of columns!", "data");
                    }
                }
                else
                {
                    numColumns = colIndex;
                }

                rowIndex++;
            }

            return root;
        }

        private static bool MatrixIsEmpty(ColumnObject root)
        {
            return root.NextColumnObject == root;
        }

        private IEnumerable<Solution> Search(int k, SearchData searchData)
        {
            try
            {
                if (k == 0) RaiseStarted();

                if (IsCancelled())
                {
                    RaiseCancelled();
                    yield break;
                }

                RaiseSearchStep(searchData.IterationCount, searchData.CurrentSolution.RowIndexes);
                searchData.IncrementIterationCount();

                if (MatrixIsEmpty(searchData.Root))
                {
                    if (searchData.CurrentSolution.RowIndexes.Any())
                    {
                        searchData.IncrementSolutionCount();
                        var solutionIndex = searchData.SolutionCount - 1;
                        var solution = searchData.CurrentSolution;
                        RaiseSolutionFound(solution, solutionIndex);
                        yield return solution;
                    }

                    yield break;
                }

                var c = ChooseColumnWithLeastRows(searchData.Root);
                CoverColumn(c);

                for (var r = c.Down; r != c; r = r.Down)
                {
                    if (IsCancelled())
                    {
                        RaiseCancelled();
                        yield break;
                    }

                    searchData.PushCurrentSolutionRowIndex(r.RowIndex);

                    for (var j = r.Right; j != r; j = j.Right)
                        CoverColumn(j.ListHeader);

                    var recursivelyFoundSolutions = Search(k + 1, searchData);
                    foreach (var solution in recursivelyFoundSolutions) yield return solution;

                    for (var j = r.Left; j != r; j = j.Left)
                        UncoverColumn(j.ListHeader);

                    searchData.PopCurrentSolutionRowIndex();
                }

                UncoverColumn(c);

            }
            finally
            {
                if (k == 0) RaiseFinished();
            }
        }

        private static ColumnObject ChooseColumnWithLeastRows(ColumnObject root)
        {
            ColumnObject chosenColumn = null;

            for (var columnHeader = root.NextColumnObject; columnHeader != root; columnHeader = columnHeader.NextColumnObject)
            {
                if (chosenColumn == null || columnHeader.NumberOfRows < chosenColumn.NumberOfRows)
                    chosenColumn = columnHeader;
            }

            return chosenColumn;
        }

        private static void CoverColumn(ColumnObject c)
        {
            c.UnlinkColumnHeader();

            for (var i = c.Down; i != c; i = i.Down)
            {
                for (var j = i.Right; j != i; j = j.Right)
                {
                    j.ListHeader.UnlinkDataObject(j);
                }
            }
        }

        private static void UncoverColumn(ColumnObject c)
        {
            for (var i = c.Up; i != c; i = i.Up)
            {
                for (var j = i.Left; j != i; j = j.Left)
                {
                    j.ListHeader.RelinkDataObject(j);
                }
            }

            c.RelinkColumnHeader();
        }

        private void RaiseStarted()
        {
            var handler = Started;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseFinished()
        {
            var handler = Finished;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseCancelled()
        {
            var handler = Cancelled;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseSearchStep(int iteration, IEnumerable<int> rowIndexes)
        {
            var handler = SearchStep;
            if (handler != null) handler(this, new SearchStepEventArgs(iteration, rowIndexes));
        }

        private void RaiseSolutionFound(Solution solution, int solutionIndex)
        {
            var handler = SolutionFound;
            if (handler != null) handler(this, new SolutionFoundEventArgs(solution, solutionIndex));
        }
    }
}
