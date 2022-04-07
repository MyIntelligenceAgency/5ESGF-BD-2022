using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Spark.Sql;
using Sudoku.Shared;

namespace Sudoku.NorvigSparkSolver
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            var nbWorkers = 4;
            var nbCoresPerWorker = 1;
            var nbLignesMax = 100000;


            var chronometre = Stopwatch.StartNew();

            var filePath = Path.Combine(Environment.CurrentDirectory, "sudoku.csv");

            Utils.PrepareSudokus();

            SparkSession spark =
                SparkSession
                    .Builder()
                    .AppName("Norvig Solver Spark")
                    .Config("spark.executor.cores", nbCoresPerWorker)
                    .Config("spark.executor.instances", nbWorkers)
                    .GetOrCreate();

            DataFrame dataFrame = spark
                .Read()
                .Option("header", true)
                //.Option("inferSchema", true)
                .Schema("quizzes string, solutions string")
                .Csv(filePath);

            DataFrame milleSudoku = dataFrame.Limit(nbLignesMax);


            spark.Udf().Register<string, string>(
                "SukoduUDF",
                (sudoku) => SolveSudoku(sudoku));

            milleSudoku.CreateOrReplaceTempView("Resolved");
            DataFrame sqlDf = spark.Sql("SELECT quizzes, SukoduUDF(quizzes) as Resolution from Resolved");
            sqlDf.Collect();
            sqlDf.Show();

            var tempsEcoule = chronometre.Elapsed;
            Console.WriteLine($"temps d'execution: {tempsEcoule.ToString()}");

        }


        public static string SolveSudoku(string strSudoku)
        {

            var sudoku = GridSudoku.ReadSudoku(strSudoku);
            var norvigSolver = new NorvigSolver();
            var sudokuResolu = norvigSolver.Solve(sudoku);

            //return sudokuResolu.ToString();

            string game = "";
            foreach (var ligneSudoku in sudokuResolu.Cellules)
            {
                foreach (var celluleSudoku in ligneSudoku)
                {
                    game = game + celluleSudoku.ToString();
                }
            }

            return game;
        }


    }


   






}
