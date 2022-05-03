using Sudoku.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Collections;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Models.Attributes;

namespace ConsoleApp1
{
    public class Class1 : ISolverSudoku
    {
        private static RobustSudokuModel robustModel = new RobustSudokuModel();



        public void Solve(GrilleSudoku s)
        {
            //var model = new NaiveSudokuModel();
            robustModel.SolveSudoku(s);

        }

        public GridSudoku Solve(GridSudoku s)
        {
            throw new NotImplementedException();
        }
    }

    public class GrilleSudoku
    {
    }

    /// <summary>
    /// Ce deuxième modèle n'apporte pas encore tout à fait satisfaction mais apporte un réel progrès vis à vis du modèle naïf. J'ai rajouté des commentaires là où de nouveaux tests peuvent être entrepris.
    /// le moteur d'inférence n'a plus qu'à compiler le modèle une seule fois, et il résout d'autres Sudokus faciles.
    /// Il manque cependant de résoudre les Sudokus plus difficiles. Ceci dit, il trouve la bonne valeur pour une majorité de cellules, et du coup on peut envisager de l'utiliser de façon itérative en ne conservant que les valeurs pour lesquelles les probabilités sont les meilleures et en réinjectant dans CellsPrior.ObservedValue comme dans l'exemple Cyclist d'Infer.Net, plutôt que d'utiliser cellsPosterior.Point ou cellsProbsPosterior[cellIndex].GetMode() comme c'est le cas actuellement ce qui revient à ignorant les probabilités réelles et conserver toutes les valeurs du premier coup)
    /// </summary>
    public class RobustSudokuModel
    {

        public InferenceEngine InferenceEngine;

        private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
        private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();


        // Cf https://en.wikipedia.org/wiki/Categorical_distribution et https://en.wikipedia.org/wiki/Categorical_distribution#Bayesian_inference_using_conjugate_prior pour le choix des distributions
        // et le chapitre 6 de https://dotnet.github.io/infer/InferNet101.pdf pour l'implémentation dans Infer.Net

        private VariableArray<Dirichlet> cellsPrior;
        private VariableArray<Vector> probCells;
        public VariableArray<int> Cells;

        private const double EpsilonProba = 0.00000001;
        private static double FixedValueProba = 1.0 - ((CellDomain.Count - 1) * EpsilonProba);

        public static object Enumerable { get; private set; }
        public VariableArray<Dirichlet> CellsPrior { get => cellsPrior; set => cellsPrior = value; }
        public VariableArray<Vector> ProbCells { get => ProbCells1; set => ProbCells1 = value; }
        public VariableArray<Vector> ProbCells1 { get => probCells; set => probCells = value; }

        public RobustSudokuModel()
        {


            Range valuesRange = new Range(CellDomain.Count).Named("valuesRange");
            Range cellsRange = new Range(CellIndices.Count).Named("cellsRange");


            CellsPrior = Variable.Array<Dirichlet>(cellsRange).Named("CellsPrior");
            ProbCells = Variable.Array<Vector>(cellsRange).Named("ProbCells");
            ProbCells[cellsRange] = Variable<Vector>.Random(CellsPrior[cellsRange]);
            ProbCells.SetValueRange(valuesRange);


            // Initialisation des distribution a priori de façon uniforme (les valeurs sont équiprobables pour chaque cellule)

            Dirichlet[] dirUnifArray =
                Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();
            CellsPrior.ObservedValue = dirUnifArray;

            Cells = Variable.Array<int>(cellsRange);
            Cells[cellsRange] = Variable.Discrete(ProbCells[cellsRange]);



            //Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                foreach (var neighbourCellIndex in GrilleSudoku.VoisinagesParCellule[cellIndex])
                {
                    if (neighbourCellIndex > cellIndex)
                    {
                        Variable.ConstrainFalse(Cells[cellIndex] == Cells[neighbourCellIndex]);
                    }
                }
            }


            //Todo: tester d'autres algo et paramétrages associés

            IAlgorithm algo = new ExpectationPropagation();
            //IAlgorithm algo = new GibbsSampling();
            //IAlgorithm algo = new VariationalMessagePassing();
            //IAlgorithm algo = new MaxProductBeliefPropagation();

            //algo.DefaultNumberOfIterations = 50;
            //algo.DefaultNumberOfIterations = 200;


            InferenceEngine = new InferenceEngine(algo);

            //InferenceEngine.OptimiseForVariables = new IVariable[] { Cells };

        }


        public virtual void SolveSudoku(GrilleSudoku s)
        {
            Dirichlet[] dirArray = Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();

            //On affecte les valeurs fournies par le masque à résoudre en affectant les distributions de probabilités initiales
            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] > 0)
                {

                    Vector v = Vector.Zero(CellDomain.Count);
                    v[s.Cellules[cellIndex] - 1] = 1.0;


                    //Todo: Alternative: le fait de mettre une proba non nulle permet d'éviter l'erreur "zero probability" du Sudoku Easy-n°2, mais le Easy#3 n'est plus résolu

                    //Vector v = Vector.Constant(CellDomain.Count, EpsilonProba);
                    //v[s.Cellules[cellIndex] - 1] = FixedValueProba;

                    dirArray[cellIndex] = Dirichlet.PointMass(v);
                }
            }

            CellsPrior.ObservedValue = dirArray;


            // Todo: tester en inférant sur d'autres variables aléatoire,
            // et/ou en ayant une approche itérative: On conserve uniquement les cellules dont les valeurs ont les meilleures probabilités et on réinjecte ces valeurs dans CellsPrior comme c'est également fait dans le projet neural nets. 



            DistributionRefArray<Discrete, int> cellsPosterior = (DistributionRefArray<Discrete, int>)InferenceEngine.Infer(Cells);
            var cellValues = cellsPosterior.Point.Select(i => i + 1).ToList();

            //Autre possibilité de variable d'inférence (bis)
            //Dirichlet[] cellsProbsPosterior = InferenceEngine.Infer<Dirichlet[]>(ProbCells);

            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] == 0)
                {

                    s.Cellules[cellIndex] = cellValues[cellIndex];

                    //Autre possibilité de variable d'inférence (bis)
                    //var mode = cellsProbsPosterior[cellIndex].GetMode();
                    //var value = mode.IndexOf(1.0) + 1;
                    //s.Cellules[cellIndex] = value;
                }
            }


        }

        internal void SolveSudoku(GridSudoku s)
        {
            throw new NotImplementedException();
        }
    }

    public class VariableArray<T>
    {
    }

    public class InferenceEngine
    {
    }



    /// <summary>
    /// Ce premier modèle est très faible: d'une part, il ne résout que quelques Sudokus faciles, d'autre part, le modèle est recompilé à chaque fois, ce qui prend beaucoup de temps
    /// </summary>
    public class NaiveSudokuModel
    {

        private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
        private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();


        public virtual void SolveSudoku(GrilleSudoku s)
        {

            var algo = new ExpectationPropagation();
            var engine = new InferenceEngine(algo);

            //Implémentation naïve: une variable aléatoire entière par cellule
            var cells = new List<Variable<int>>(CellIndices.Count);

            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                //On initialise le vecteur de probabilités de façon uniforme pour les chiffres de 1 à 9
                var baseProbas = Enumerable.Repeat(1.0, CellDomain.Count).ToList();
                //Création et ajout de la variable aléatoire
                var cell = Variable.Discrete(baseProbas.ToArray());
                cells.Add(cell);

            }

            //Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                foreach (var neighbourCellIndex in GrilleSudoku.VoisinagesParCellule[cellIndex])
                {
                    if (neighbourCellIndex > cellIndex)
                    {
                        Variable.ConstrainFalse(cells[cellIndex] == cells[neighbourCellIndex]);
                    }
                }
            }

            //On affecte les valeurs fournies par le masque à résoudre comme variables observées
            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] > 0)
                {
                    cells[cellIndex].ObservedValue = s.Cellules[cellIndex] - 1;
                }
            }

            foreach (var cellIndex in GrilleSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] == 0)
                {
                    var result = (Discrete)engine.Infer(cells[cellIndex]);
                    s.Cellules[cellIndex] = result.Point + 1;
                }
            }

        }



    }



}