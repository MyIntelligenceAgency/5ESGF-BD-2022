using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Sudoku.Shared
{
    public static class Utils
    {

        private const string PUZZLES_FOLDER_NAME = "Puzzles";

        public static void PrepareSudokus()
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "sudoku.csv");
            if (!File.Exists(filePath))
            {


                DirectoryInfo puzzlesDirectory = null;
                var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
                do
                {
                    var subDirectories = currentDirectory.GetDirectories();
                    foreach (var subDirectory in subDirectories)
                    {
                        if (subDirectory.Name == PUZZLES_FOLDER_NAME)
                        {
                            puzzlesDirectory = subDirectory;
                            break;
                        }
                    }
                    currentDirectory = currentDirectory.Parent;
                    if (currentDirectory == null)
                    {
                        throw new ApplicationException("couldn't find puzzles directory");
                    }
                } while (puzzlesDirectory == null);
                string zipPath = System.IO.Path.Combine(puzzlesDirectory.ToString(), "sudoku.zip");
                ZipFile.ExtractToDirectory(zipPath, Environment.CurrentDirectory);

            }

        }



        public static T[][] ToJaggedArray<T>(this IList<T> source, int columnLength)
        {
            var rowLength = source.Count / columnLength;
            var toReturn = new T[rowLength][];
            for (int rowIndex = 0; rowIndex < rowLength; rowIndex++)
            {
                toReturn[rowIndex] = new T[columnLength];
                for (int colIndex = 0; colIndex < columnLength; colIndex++)
                {
                    var globalIndex = rowIndex * columnLength + colIndex;
                    if (globalIndex<source.Count)
                    {
                        toReturn[rowIndex][colIndex] = source[globalIndex];
                    }
                }
            }

            return toReturn;
        }

        public static T[] Flatten<T>(this T[][] source)
        {
            return source.SelectMany(x => x).ToArray();
        }

        /// <summary>
        ///   Converts a jagged array into a multidimensional array.
        /// </summary>
        public static T[,] To2D<T>(this T[][] source)
        {
            try
            {
                int FirstDim = source.Length;
                int SecondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

                var result = new T[FirstDim, SecondDim];
                for (int i = 0; i < FirstDim; ++i)
                for (int j = 0; j < SecondDim; ++j)
                    result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            }
        }

        /// <summary>
        ///   Converts a multidimensional array into a jagged array.
        /// </summary>
        public static T[][] ToJaggedArray<T>(this T[,] twoDimensionalArray)
        {
            int rowsFirstIndex = twoDimensionalArray.GetLowerBound(0);
            int rowsLastIndex = twoDimensionalArray.GetUpperBound(0);
            int numberOfRows = rowsLastIndex + 1;

            int columnsFirstIndex = twoDimensionalArray.GetLowerBound(1);
            int columnsLastIndex = twoDimensionalArray.GetUpperBound(1);
            int numberOfColumns = columnsLastIndex + 1;

            T[][] jaggedArray = new T[numberOfRows][];
            for (int i = rowsFirstIndex; i <= rowsLastIndex; i++)
            {
                jaggedArray[i] = new T[numberOfColumns];

                for (int j = columnsFirstIndex; j <= columnsLastIndex; j++)
                {
                    jaggedArray[i][j] = twoDimensionalArray[i, j];
                }
            }
            return jaggedArray;
        }


       


    }
}