using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GenerationalBoardGameTournament.Game;
using GenerationalBoardGameTournament.AI;

namespace GenerationalBoardGameTournament {
    public partial class Form1 : Form {
        AIBattleships[] poolAI;
        int countAI;
        int totalAI;
        int generation;
        int boardWidth = 10;
        int boardHeight = 10;
        double variance = 0.5;

        public Form1() {
            InitializeComponent();
            GenerateAI((int)numericUpDown1.Value);
        }

        private void GenerateAI(int number) {
            poolAI = new AIBattleships[number];
            countAI = number;
            totalAI = 0;
            generation = 0;
            for (int i = 0; i < number; i++) {
                AIBattleships ai = new AIBattleships(i, boardWidth, boardHeight, variance, MakeSeed());
                poolAI[i] = ai;
                totalAI++;
            }
        }

        private int MakeSeed() {
            DateTime now = DateTime.Now;
            int seed = now.Year + now.Hour + now.Minute + now.Second + now.Millisecond;
            return seed;
        }

        private List<Tuple<bool, double[]>> RunTournament(int kValue) {
            Tournament tournament = new Tournament(kValue);
            return tournament.RunMatrixMinimum(poolAI);
            //return tournament.RunMatrix(poolAI);
        }

        private void Cull(double cutOffELO) {
            poolAI = poolAI.OrderByDescending(item => item.ELOScore).ToArray();
            countAI = 0;
            for(int i = 0; i < poolAI.Length; i++) {
                if (poolAI[i].ELOScore > cutOffELO) {
                    //Console.WriteLine("ID: " + poolAI[i].ID + " ELO: " + poolAI[i].ELOScore);
                    countAI = i;
                }
            }
        }

        private void Procreate(double mutationRate, bool inheritELO) {
            int currentAICount = countAI;
            for (int i = 0; i < poolAI.Length; i++) {
                if (currentAICount > 0)
                    if (i > currentAICount) {
                        poolAI[i] = new AIBattleships(i, boardWidth, boardHeight, variance, MakeSeed());
                    }
                    else {
                        poolAI[i] = poolAI[i % currentAICount].GetChild(mutationRate, totalAI, inheritELO);
                    }
                else
                    poolAI[i] = poolAI[i].GetChild(mutationRate * 10, totalAI, inheritELO);
                countAI++;
                totalAI++;
            }
            generation++;
        }

        private string[] CountFamily() {
            // Count which families have been most succesful by means of counting how many members each family has
            // There are poolAI.Length number of families, ranging from 0 to max
            int[] familyMembers = new int[poolAI.Length];
            string[] familyMemberData = new string[poolAI.Length + 2];
            int lastGen = generation - 1;
            familyMemberData[0] = "After; generation; " + lastGen + " the following; families; have this; many members; left;";
            familyMemberData[1] = "Family;Members";
            foreach(AIBattleships ai in poolAI) {
                familyMembers[ai.Family]++;
            }
            for (int i = 0; i < poolAI.Length; i++) {
                familyMemberData[i + 2] = i + ";" + familyMembers[i];
            }
            return familyMemberData;
        }

        private void RunGenerations(int generations, int iterations, int minK, int maxK) {
            bool inheritELO = false;
            for (int e = 0; e < 2; e++) {
                for (int k = minK; k <= maxK; k += 5) {
                    List<Tuple<bool, double[]>>[][] sumResults = new List<Tuple<bool, double[]>>[iterations][];
                    for (int j = 0; j < iterations; j++) {
                        // Create new AI pool
                        GenerateAI((int)numericUpDown1.Value);

                        // Run a certain number or generations for that AI pool
                        List<Tuple<bool, double[]>>[] sumResultsGenerations = new List<Tuple<bool, double[]>>[generations];
                        for (int i = 0; i < generations; i++) {
                            Console.WriteLine("Starting tournament for generation " + i + " of iteration " + j);
                            List<Tuple<bool, double[]>> resultsGeneration = RunTournament(k);
                            sumResultsGenerations[i] = resultsGeneration;
                            Cull((double)numericUpDown3.Value);
                            Procreate((double)numericUpDown6.Value, inheritELO);
                            Console.WriteLine("Tournament completed, new generation prepared");
                        }
                        sumResults[j] = sumResultsGenerations;
                    }
                    HandleResults(sumResults, iterations, generations, k, inheritELO, (double)numericUpDown3.Value);
                    Console.WriteLine("All done!");
                }
                inheritELO = true;
            }
        }

        /// <summary>
        /// Take in the tuples with results
        /// Calculate averages and deviations for: generation, iteration, whole
        /// depending on whether 
        /// </summary>
        /// <param name="sumResults"></param>
        private void HandleResults(List<Tuple<bool, double[]>>[][] sumResults, int iterations, int generations, int kValue, bool inheritELO, double cutOffELO) {
            string path = "../../../../Data/" + "ResultsK" + kValue + "Inherit" + inheritELO + ".csv";
            File.WriteAllText(path, "Start report at " + DateTime.Now.ToString("hh: mm:ss tt") + "," + iterations + ",iterations," + generations + ",generations, kValue:," + kValue + ",Inherit ELO:," + inheritELO + ",ELO cutoff:," + cutOffELO + System.Environment.NewLine);
            string index = "Iteration, Generation, Total correct guesses, Total wrong guesses, Average rating difference correct guesses, Standard deviation, Average predicted outcome difference correct guesses, Standard deviation,"
                + " Average rating difference wrong guesses, Standard deviation, Average predicted outcome difference wrong guesses, Standard deviation" + System.Environment.NewLine;
            File.AppendAllText(path, index);
            // Calculate the averages and standard deviations per generation
            List<Tuple<bool, double[]>> completeResults = new List<Tuple<bool, double[]>>();
            for (int i = 0; i < iterations; i++) {
                List<Tuple<bool, double[]>> iterationResults = new List<Tuple<bool, double[]>>();
                for (int j = 0; j < generations; j++) {
                    List<Tuple<bool, double[]>> generationResults = sumResults[i][j];
                    CalculateAveragesAndDeviations(generationResults, path, i.ToString(), j.ToString());
                    iterationResults.AddRange(generationResults);
                }
                CalculateAveragesAndDeviations(iterationResults, path, i.ToString(), "Average");
                completeResults.AddRange(iterationResults);
            }
            CalculateAveragesAndDeviations(completeResults, path, "Average", "");
        }

        private void CalculateAveragesAndDeviations(List<Tuple<bool, double[]>> results, string path, string iteration, string generation) {
            // double in Tuple contains: difference in rating, and the difference in expected outcome for both players (second is related to the first)
            int correctGuesses = 0;
            int wrongGuesses = 0;
            double averageRatingDifferenceCorrect = 0;
            double averageRatingDifferenceWrong = 0;
            double averageOutcomeDifferenceCorrect = 0;
            double averageOutcomeDifferenceWrong = 0;
            foreach (Tuple<bool, double[]> data in results) {
                if (data.Item1) {
                    averageRatingDifferenceCorrect += data.Item2[0];
                    averageOutcomeDifferenceCorrect += data.Item2[1];
                    correctGuesses++;
                }
                else {
                    averageRatingDifferenceWrong += data.Item2[0];
                    averageOutcomeDifferenceWrong += data.Item2[1];
                    wrongGuesses++;
                }
            }
            averageOutcomeDifferenceCorrect /= correctGuesses;
            averageRatingDifferenceCorrect /= correctGuesses;
            averageOutcomeDifferenceWrong /= wrongGuesses;
            averageRatingDifferenceWrong /= wrongGuesses;
            // After doing the averages, now it's time for the deviations
            double deviationOutcomeDifferenceCorrect = 0;
            double deviationOutcomeDifferenceWrong = 0;
            double deviationRatingDifferenceCorrect = 0;
            double deviationRatingDifferenceWrong = 0;
            foreach (Tuple<bool, double[]> data in results) {
                if (data.Item1) {
                    deviationRatingDifferenceCorrect += Math.Pow(data.Item2[0] - averageRatingDifferenceCorrect, 2);
                    deviationOutcomeDifferenceCorrect += Math.Pow(data.Item2[1] - averageOutcomeDifferenceCorrect, 2);
                }
                else {
                    deviationRatingDifferenceWrong += Math.Pow(data.Item2[0] - averageRatingDifferenceWrong, 2);
                    deviationOutcomeDifferenceWrong += Math.Pow(data.Item2[1] - averageOutcomeDifferenceWrong, 2);
                }
            }
            deviationOutcomeDifferenceCorrect = Math.Sqrt(deviationOutcomeDifferenceCorrect / correctGuesses);
            deviationRatingDifferenceCorrect = Math.Sqrt(deviationRatingDifferenceCorrect / correctGuesses);
            deviationOutcomeDifferenceWrong = Math.Sqrt(deviationOutcomeDifferenceWrong / wrongGuesses);
            deviationRatingDifferenceWrong = Math.Sqrt(deviationRatingDifferenceWrong / wrongGuesses);

            string d = ",";
            string dataDump = iteration + d + generation + d + correctGuesses + d + wrongGuesses + d + 
                averageRatingDifferenceCorrect + d + deviationRatingDifferenceCorrect + d + 
                averageOutcomeDifferenceCorrect + d + deviationOutcomeDifferenceCorrect + d + 
                averageRatingDifferenceWrong + d + deviationRatingDifferenceWrong + d + 
                averageOutcomeDifferenceWrong + d + deviationOutcomeDifferenceWrong + d + System.Environment.NewLine;
            File.AppendAllText(path, dataDump);
        }

        private void button1_Click(object sender, EventArgs e) {
            GenerateAI((int)numericUpDown1.Value);
        }

        private void button2_Click(object sender, EventArgs e) {
            RunTournament((int)numericUpDown2.Value);
        }

        private void button3_Click(object sender, EventArgs e) {
            Cull((double)numericUpDown3.Value);
        }

        private void button4_Click(object sender, EventArgs e) {
            Procreate((double)numericUpDown6.Value, checkBox1.Checked);
        }

        private void button5_Click(object sender, EventArgs e) {
            RunGenerations((int)numericUpDown4.Value, (int)numericUpDown5.Value, (int)numericUpDown2.Value, (int)numericUpDown7.Value);
        }
    }
}
