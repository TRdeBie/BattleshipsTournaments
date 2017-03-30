using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GenerationalBoardGameTournament.AI;

namespace GenerationalBoardGameTournament.Game {
    class GameBattleships : Game {
        AIBattleships p1, p2;
        int kValue;

        public GameBattleships(AIBattleships player1, AIBattleships player2, int kVal) {
            p1 = player1;
            p2 = player2;
            kValue = kVal;
        }

        /// <summary>
        /// Runs 2 games between its players and returns the id of the player who won
        /// </summary>
        /// <returns>ID of the winner, or -1 in a draw</returns>
        public Tuple<bool, double[]> Run() {
            double p1Score = 0;
            double p2Score = 0;
            // Game 1
            if (Round(p1, p2))
                p1Score++;
            else
                p2Score++;
            // Game 2
            if (Round(p2, p1))
                p2Score++;
            else
                p1Score++;
            p1Score /= 2;
            p2Score /= 2;

            return CalculateNewELO(p1, p2, p1Score, p2Score);
            /*
            int result = -1;
            if (p1Score > p2Score)
                result = p1.ID;
            if (p1Score < p2Score)
                result = p2.ID;
            */
        }

        /// <summary>
        /// Lets players 1 and 2 play against one another
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
        /// <returns>true if player 1 won, false otherwise</returns>
        public bool Round(AIBattleships player1, AIBattleships player2) {
            player1.NewGameReset();
            player2.NewGameReset();
            bool gameEnd = false;
            while (!gameEnd) {
                gameEnd = Turn(player1, player2);
            }
            if (player1.Alive) {
                player1.Wins++;
                return true;
            }
            player2.Wins++;
            return false;
        }

        private Tuple<bool, double[]> CalculateNewELO(AIBattleships p1, AIBattleships p2, double scoreA, double scoreB) {
            double ratingA = p1.ELOScore;
            double ratingB = p2.ELOScore;
            double ratingDif = Math.Abs(ratingA - ratingB);
            double expectedA = 1 / (1 + Math.Pow(10, (ratingB - ratingA) / 400));
            double expectedB = 1 / (1 + Math.Pow(10, (ratingA - ratingB) / 400));
            double newA = ratingA + kValue * (scoreA - expectedA);
            double newB = ratingB + kValue * (scoreB - expectedB);
            double newRatingDif = Math.Abs(newA - newB);
            p1.ELOScore = newA;
            p2.ELOScore = newB;

            double expectedAminusB = Math.Abs(expectedA - expectedB);

            int predictedWinner = 0;
            if (expectedA > expectedB)
                predictedWinner = 1;
            if (expectedB > expectedA)
                predictedWinner = -1;
            int winner = (int)(scoreA - scoreB);
            bool correctGuess = predictedWinner == winner;
            double[] values = new double[2] { ratingDif, expectedAminusB };
            Tuple<bool, double[]> results = new Tuple<bool, double[]>(correctGuess, values);
            return results;
        }

        public bool Turn(AIBattleships player1, AIBattleships player2) {
            bool gameEnd = false;
            player1.Turn(player2);
            if (!player2.Alive) {
                gameEnd = true;
                return gameEnd;
            }
            player2.Turn(player1);
            if (!player1.Alive) {
                gameEnd = true;
            }
            return gameEnd;
        }
    }
}
