﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DlxLibDemo3.Model;
using Orientation = DlxLibDemo3.Model.Orientation;

namespace DlxLibDemo3
{
    public partial class BoardControl
    {
        private readonly IDictionary<char, Tuple<Orientation, int, int, PieceControl>> _pieceDetails = new Dictionary<char, Tuple<Orientation, int, int, PieceControl>>();
        private readonly Color _gridColour = Color.FromArgb(0x80, 0xCD, 0x85, 0x3F);
        private const int GridLineThickness = 4;
        private const int GridLineHalfThickness = GridLineThickness / 2;

        public BoardControl()
        {
            InitializeComponent();
        }

        public void DrawGrid()
        {
            DrawGridLines();
            DrawGridSquares();
        }

        private void DrawGridLines()
        {
            var aw = ActualWidth;
            var ah = ActualHeight;
            var sw = (aw - GridLineThickness) / 8;
            var sh = (ah - GridLineThickness) / 8;

            var gridLineBrush = new SolidColorBrush(_gridColour);

            // Horizontal grid lines
            for (var row = 0; row <= 8; row++)
            {
                var line = new Line
                    {
                        Stroke = gridLineBrush,
                        StrokeThickness = GridLineThickness,
                        X1 = 0,
                        Y1 = row * sh + GridLineHalfThickness,
                        X2 = aw,
                        Y2 = row * sh + GridLineHalfThickness
                    };
                BoardCanvas.Children.Add(line);
            }

            // Vertical grid lines
            for (var col = 0; col <= 8; col++)
            {
                var line = new Line
                {
                    Stroke = gridLineBrush,
                    StrokeThickness = GridLineThickness,
                    X1 = col * sw + GridLineHalfThickness,
                    Y1 = 0,
                    X2 = col * sw + GridLineHalfThickness,
                    Y2 = ah
                };
                BoardCanvas.Children.Add(line);
            }
        }

        private void DrawGridSquares()
        {
            var aw = ActualWidth;
            var ah = ActualHeight;
            var sw = (aw - GridLineThickness) / 8;
            var sh = (ah - GridLineThickness) / 8;

            var gridSquareBrush = new SolidColorBrush(_gridColour);

            for (var row = 0; row < 8; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    var isEven = (row + col) % 2 == 0;
                    if (!isEven)
                    {
                        continue;
                    }
                    var gridSquare = new Rectangle {Fill = gridSquareBrush};
                    var rect = new Rect(col * sw + GridLineHalfThickness, row * sh + GridLineHalfThickness, sw, sh);
                    rect.Inflate(-8, -8);
                    Canvas.SetLeft(gridSquare, rect.Left);
                    Canvas.SetTop(gridSquare, rect.Top);
                    gridSquare.Width = rect.Width;
                    gridSquare.Height = rect.Height;
                    BoardCanvas.Children.Add(gridSquare);
                }
            }
        }

        public void AddPiece(RotatedPiece rotatedPiece, int x, int y)
        {
            if (IsPieceOnBoard(rotatedPiece.Piece.Name))
            {
                throw new InvalidOperationException(string.Format("Attempt to add a piece that is already on the board - \"{0}\"!", rotatedPiece.Piece.Name));
            }

            var aw = ActualWidth;
            var ah = ActualHeight;
            var sw = (aw - GridLineThickness) / 8;
            var sh = (ah - GridLineThickness) / 8;

            var pieceControl = new PieceControl(rotatedPiece, sw);
            Canvas.SetLeft(pieceControl, x * sw + GridLineHalfThickness);
            Canvas.SetBottom(pieceControl, y * sh + GridLineHalfThickness);
            BoardCanvas.Children.Add(pieceControl);
            _pieceDetails[rotatedPiece.Piece.Name] = Tuple.Create(rotatedPiece.Orientation, x, y, pieceControl);
        }

        public void RemovePiece(char pieceName)
        {
            if (_pieceDetails.ContainsKey(pieceName))
            {
                BoardCanvas.Children.Remove(_pieceDetails[pieceName].Item4);
                _pieceDetails.Remove(pieceName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Attempt to remove a piece that is not on the board - \"{0}\"!", pieceName));
            }
        }

        public void RemovePiecesOtherThan(IList<char> pieceNames)
        {
            var pieceNamesToRemove = new List<char>();

            foreach (var pieceName in _pieceDetails.Keys)
            {
                if (pieceNames.All(pn => pn != pieceName))
                {
                    BoardCanvas.Children.Remove(_pieceDetails[pieceName].Item4);
                    pieceNamesToRemove.Add(pieceName);
                }
            }

            foreach (var pieceName in pieceNamesToRemove)
            {
                _pieceDetails.Remove(pieceName);
            }
        }

        public bool IsPieceOnBoard(char pieceName)
        {
            return _pieceDetails.ContainsKey(pieceName);
        }

        public bool IsPieceOnBoardWithOrientationandLocation(char pieceName, Orientation orientation, int x, int y)
        {
            if (_pieceDetails.ContainsKey(pieceName))
            {
                var tuple = _pieceDetails[pieceName];
                if (tuple.Item1 == orientation &&
                    tuple.Item2 == x &&
                    tuple.Item3 == y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
