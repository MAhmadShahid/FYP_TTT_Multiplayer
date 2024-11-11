using System;
using System.Collections.Generic;

namespace HelloWorld
{
    class Program
    {
        public static Random rand = new Random();

        public static Dictionary<int, Dictionary<int, bool>> playerMoveStates = new Dictionary<int, Dictionary<int, bool>>();
        public static Dictionary<int, int> cellOwner = new Dictionary<int, int>();
        public static char[] markers = { 'X', 'V', 'O', 'U' };
        // Configurations for current match.
        public static int _gridSize = 5;
        public static int _playerCount = 3;
        public static int _currentTurn = 0;

        static Tuple<Tuple<int, int>, Tuple<int, int>> horizontalRule = new Tuple<Tuple<int, int>, Tuple<int, int>>(
        new Tuple<int, int>(0, -1),        // Left = { Row = 0, Column = -1 }
        new Tuple<int, int>(0, 1)          // Right = { Row = 0, Column = 1 }
        );


        // Tuple<int, int> horizontalRule = new Tuple<int, int>( -1, 1 );
        static Tuple<Tuple<int, int>, Tuple<int, int>> verticalRule = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, 0),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, 0)          // Right = { Row = 0, Column = 1 }
        );

        static Tuple<Tuple<int, int>, Tuple<int, int>> diagonalRule1 = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, -1),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, 1)          // Right = { Row = 0, Column = 1 }
        );

        static Tuple<Tuple<int, int>, Tuple<int, int>> diagonalRule2 = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, 1),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, -1)          // Right = { Row = 0, Column = 1 }
        );

        public static Dictionary<int, Dictionary<int, int>> winCondition = new Dictionary<int, Dictionary<int, int>>
        {
            [3] = new Dictionary<int, int> { [2] = 3 },
            [5] = new Dictionary<int, int>
            {
                [2] = 5,
                [3] = 4,
                [4] = 3
            },
        };

        static void Main(string[] args)
        {
            Program.DisplayGrid();
            Program.AddPlayers();

            Program.PlayGame4();
        }



        static void MakePlay(int cellValue)
        {
            // if cell is already marked, either by current player or not
            if (cellOwner.ContainsKey(cellValue))
                return;

            // mark the current cell
            cellOwner[cellValue] = Program._currentTurn;
            Program.playerMoveStates[_currentTurn][cellValue] = true;

            // display new grid
            Program.DisplayGrid();

            // check win condition
            if (    Program.CheckWinCondition(Program._currentTurn, cellValue, horizontalRule)  ||
                    Program.CheckWinCondition(Program._currentTurn, cellValue, verticalRule)    ||
                    Program.CheckWinCondition(Program._currentTurn, cellValue, diagonalRule1)   ||
                    Program.CheckWinCondition(Program._currentTurn, cellValue, diagonalRule2)       )

            {
                Console.WriteLine($"Player: {Program._currentTurn} wins ({markers[_currentTurn]})");
                return;
            }    

            // next player turn
            Program._currentTurn = (Program._currentTurn + 1) % Program._playerCount;
        }

        static bool CheckWinCondition(int player, int currentCellMarked, Tuple<Tuple<int, int>, Tuple<int, int>> ruling)
        {
            //Dictionary<string, Dictionary<string, int>> horizontalRule = new Dictionary<string, Doctomp>
            //{
            //    ["Left"] = 
            //}

            int currentRow = currentCellMarked / Program._gridSize;
            int startingColumn  = currentCellMarked % Program._gridSize;
            int score = 1;

            // check horizontal rule
            bool left = true, right = true;
            Tuple<int, int> leftRule = ruling.Item1;
            Tuple<int, int> rightRule = ruling.Item2;

            int leftRowCounter = leftRule.Item1, leftColumnCounter = leftRule.Item2;
            int rightRowCounter = rightRule.Item1, rightColumnCounter = rightRule.Item2;
            score = 1; // reset score            

            while (left || right)
            {
                // check left column
                if(left)
                {
                    int leftCellColumn = startingColumn + leftColumnCounter;
                    int leftCellRow = currentRow + leftRowCounter;
                    if (leftCellColumn >= 0 && leftCellRow >= 0)
                    {
                        int leftCellValue = Program.GetCellValue(leftCellRow, leftCellColumn);
                        if (cellOwner.ContainsKey(leftCellValue) && cellOwner[leftCellValue] == player)
                        {
                            score++;
                            leftRowCounter += leftRule.Item1;
                            leftColumnCounter += leftRule.Item2;
                        }
                        else
                            left = false;
                    }
                    else
                        left = false;
                }

                // check right column
                if(right)
                {
                    int rightCellColumn = startingColumn + rightColumnCounter;
                    int rightCellRow = currentRow + rightRowCounter;
                    if (rightCellColumn < Program._gridSize && rightCellRow < Program._gridSize)
                    {
                        int rightCellValue = Program.GetCellValue(rightCellRow, rightCellColumn);
                        if (cellOwner.ContainsKey(rightCellValue) && cellOwner[rightCellValue] == player)
                        {
                            score++;
                            rightRowCounter += rightRule.Item1;
                            rightColumnCounter += rightRule.Item2;
                        }
                        else
                            right = false;
                    }
                    else
                        right = false;
                }
            }

            if (score == Program.winCondition[Program._gridSize][Program._playerCount])
                return true;

            return false;
        }

        static void PlayGame1()
        {
            Program.MakePlay(14);
            Program.MakePlay(13);
            Program.MakePlay(12);
            Program.MakePlay(11);
            Program.MakePlay(19);
            Program.MakePlay(9);
            Program.MakePlay(4);
            Program.MakePlay(8);
            Program.MakePlay(18);
            Program.MakePlay(7);
            Program.MakePlay(24);
            Program.MakePlay(6);
            Program.MakePlay(17);
        }

        static void PlayGame2()
        {
            Program.MakePlay(0);
            Program.MakePlay(5);
            Program.MakePlay(10);
            Program.MakePlay(6);
            Program.MakePlay(2);
            Program.MakePlay(11);
            Program.MakePlay(15);
            Program.MakePlay(16);
            Program.MakePlay(1);
        }

        static void PlayGame3()
        {
            Program.MakePlay(12);
            Program.MakePlay(16);
            Program.MakePlay(7);
            Program.MakePlay(22);
            Program.MakePlay(11);
            Program.MakePlay(13);
            Program.MakePlay(21);
            Program.MakePlay(20);
            Program.MakePlay(17);
            Program.MakePlay(23);
            Program.MakePlay(18);
            Program.MakePlay(19);
            Program.MakePlay(24);
        }

        static void PlayGame4()
        {
            // grid size = 5
            // player count = 3

            Program.MakePlay(12);
            Program.MakePlay(3);
            Program.MakePlay(13);
            Program.MakePlay(7);
            Program.MakePlay(0);
            Program.MakePlay(17);
            Program.MakePlay(8);
            Program.MakePlay(2);
            Program.MakePlay(6);
            Program.MakePlay(16);
            Program.MakePlay(1);

        }

        static void PlayGame5()
        {
            // grid size = 5
            // player count = 4

            Program.MakePlay(12);
            Program.MakePlay(6);
            Program.MakePlay(7);
            Program.MakePlay(10);
            Program.MakePlay(8);
            Program.MakePlay(16);
            Program.MakePlay(11);
            Program.MakePlay(4);
            Program.MakePlay(15);
            Program.MakePlay(17);
            Program.MakePlay(3);
        }

        static int GetCellValue(int row, int column)
        {
            return (row * Program._gridSize) + column;
        }

        static void AddPlayers()
        {
            for (int player = 0; player < _playerCount; player++)
                Program.playerMoveStates.Add(player, new Dictionary<int, bool>());

            // Program._currentTurn = Program.rand.Next(2);
            Program._currentTurn = 0;
        }


        // Display/UI functions
        static void DisplayGrid()
        {
            Console.WriteLine(); Console.WriteLine();

            for (int row = 0; row < _gridSize; row++)
            {
                for (int column = 0; column < _gridSize; column++)
                {
                    int currentCell = (row * Program._gridSize) + column;


                    if(cellOwner.ContainsKey(currentCell))
                        Console.Write($"\t{markers[cellOwner[currentCell]]}\t");
                    else
                        Console.Write($"\t{currentCell}\t");

                    //if (cellOwner.TryGetValue(currentCell, out currentCell))
                    //    Console.Write($"\t{markers[currentCell]}\t");
                    //else
                    //    Console.Write($"\t{currentCell}\t");

                    if (column != _gridSize - 1)
                        Console.Write("|");
                }

                Console.WriteLine();
                Program.WriteRowSeperator(_gridSize);
            }
        }


        static void WriteRowSeperator(int gridSize)
        {
            int spacePerTab = 7;
            for (int column = 0; column < gridSize; column++)
            {
                for (int cursor = 0; cursor < (column == 0 ? 16 : 15); cursor++)
                    Console.Write("-");

                if (column != gridSize - 1)
                    Console.Write("+");
            }
            Console.WriteLine();

        }
    }
}