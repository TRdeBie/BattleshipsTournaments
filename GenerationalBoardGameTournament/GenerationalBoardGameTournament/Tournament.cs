using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenerationalBoardGameTournament.AI;
using GenerationalBoardGameTournament.Game;

namespace GenerationalBoardGameTournament {
    class Tournament {
        int kValue;

        public Tournament(int k) {
            kValue = k;
        }

        /// <summary>
        /// In a pool of n ships, 
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public List<Tuple<bool, double[]>> RunMatrixMinimum(AIBattleships[] pool) {
            // In order to give every AI a proxy to every other AI, let the first AI play against every other AI
            // Then test how well their initial score will help predict rest of games
            for (int i = 1; i < pool.Length; i++) {
                GameBattleships newGame = new GameBattleships(pool[0], pool[1], kValue);
                newGame.Run();
            }
            // After those initial games to figure out the standings, start to collect data on predictions
            // Do essentially a round robin but skipping the very first AI
            List<Tuple<bool, double[]>> tournamentResults = new List<Tuple<bool, double[]>>();
            // Do a round robin tournament
            for (int i = 1; i < pool.Length - 1; i++) {
                //Console.WriteLine("Matches for AI " + pool[i].ID);
                for (int j = i + 1; j < pool.Length; j++) {
                    GameBattleships newGame = new GameBattleships(pool[i], pool[j], kValue);
                    tournamentResults.Add(newGame.Run());
                }
            }
            return tournamentResults;
        }

        /// <summary>
        /// Make the AI fight one another in a specific manner
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public List<Tuple<bool, double[]>> RunMatrix(AIBattleships[] pool) {
            bool unmatchedProxies = true;
            for (int i = 0; i < pool.Length; i++) {
                pool[i].SetupDirectProxyLists(pool.Length, i);
            }
            int numberMatches = 0;
            while (unmatchedProxies) {
                // Every round, keep track of which AI have already had a match and skip those
                bool[] roundParticipants = new bool[pool.Length];
                for (int i = 0; i < pool.Length; i++) {
                    if (!roundParticipants[i]) {

                        // If the AI hasn't already participated, find the first other AI that's not already on their list of direct of proxy opponents
                        int j = -1;
                        bool fightFound = false;
                        while (!fightFound && j < pool.Length - 1) {
                            j++;
                            if (!pool[i].FoughtDirect(j) && !pool[i].FoughtProxy(j)) {
                                if (!roundParticipants[j]) {
                                    fightFound = true;
                                }
                            }
                        }
                        if (fightFound) {
                            GameBattleships newGame = new GameBattleships(pool[i], pool[j], kValue);
                            //Console.WriteLine(pool[i].ID + " vs " + pool[j].ID + " ! Fight!");
                            newGame.Run();
                            numberMatches++;
                            roundParticipants[i] = true;
                            roundParticipants[j] = true;
                            pool[i].SetFoughtDirect(j);
                            pool[j].SetFoughtDirect(i);
                            pool[i].SetFoughtProxy(pool[j].GetFoughtDirect());
                            pool[j].SetFoughtProxy(pool[i].GetFoughtDirect());
                            // Also, update the ones that i and j have fought directly against, to let them know they've know fought in proxy against j and i
                            for (int k = 0; k < pool.Length; k++) {
                                if (pool[i].FoughtDirect(k)) {
                                    pool[k].SetFoughtProxy(j);
                                }
                                if (pool[j].FoughtDirect(k)) {
                                    pool[k].SetFoughtProxy(i);
                                }
                            }
                        }
                        else
                            roundParticipants[i] = true;
                    }
                }
                unmatchedProxies = CheckProxies(pool);
            }
            Console.WriteLine(numberMatches);
            // After having either fought directly or by proxy with every other AI, let every AI fight directly against the ones they've only battled by proxy so far
            // Record how accurate the results are
            List<Tuple<bool, double[]>> scores = new List<Tuple<bool, double[]>>();
            bool fightsLeft = true;
            while (fightsLeft) {
                // Every round, keep track of which AI have already had a match and skip those
                bool[] roundParticipants = new bool[pool.Length];
                for (int i = 0; i < pool.Length; i++) {
                    if (!roundParticipants[i]) {
                        // If not already participated in that round, check the nearest AI it hasn't had a direct fight with yet (And which also hasn't participated in the round yet)
                        int j = -1;
                        bool fightFound = false;
                        while (!fightFound && j < pool.Length - 1) {
                            j++;
                            if (!pool[i].FoughtDirect(j) && !roundParticipants[j]) {
                                fightFound = true;
                            }
                        }
                        if (fightFound) {
                            GameBattleships newGame = new GameBattleships(pool[i], pool[j], kValue);
                            //Console.WriteLine(pool[i].ID + " vs " + pool[j].ID + " ! For realsies! Fight!");
                            scores.Add(newGame.Run());
                            roundParticipants[i] = true;
                            roundParticipants[j] = true;
                            pool[i].SetFoughtDirect(j);
                            pool[j].SetFoughtDirect(i);
                        }
                        else
                            roundParticipants[i] = true;
                    }
                }
                fightsLeft = CheckFightsLeft(pool);
            }
            return scores;
        }

        private bool CheckProxies(AIBattleships[] pool) {
            foreach (AIBattleships ai in pool) {
                if (ai.CheckFightsToGo())
                    return true;
            }
            return false;
        }

        private bool CheckFightsLeft(AIBattleships[] pool) {
            foreach(AIBattleships ai in pool) {
                if (ai.CheckDirectFightsLeft())
                    return true;
            }
            return false;
        }

        public Tuple<bool, double[]>[] Run(AIBattleships[] pool) {
            int numberGames = (int)((double)pool.Length / 2) * (pool.Length - 1) + 1;
            int game = 0;
            Tuple<bool, double[]>[] tournamentResults = new Tuple<bool, double[]>[numberGames];
            // Do a round robin tournament
            for (int i = 0; i < pool.Length - 1; i++) {
                Console.WriteLine("Matches for AI " + pool[i].ID);
                for (int j = i + 1; j < pool.Length; j++) {
                    GameBattleships newGame = new GameBattleships(pool[i], pool[j], kValue);
                    tournamentResults[game] = newGame.Run();
                    game++;
                }
            }
            return tournamentResults;
        }
    }
}
