using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerationalBoardGameTournament.AI {
    class AIBattleships : AI {
        int _id, _boardWidth, _boardHeight, _seed, _family;
        double _variance;
        double[,] aim;
        double[,] aimMods;
        bool[,] shotTiles;
        Ship[] fleet;
        bool[] fleetDestroyed;
        double hitModifier = 0.5;
        double scoreELO = 1000;
        int wins = 0;
        bool[] foughtDirect, foughtProxy;


        public AIBattleships(int id, int boardWidth, int boardHeight, double variance, int seed) {
            _id = id;
            _family = id;
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _seed = seed;
            _variance = variance;
            SetAim(variance, seed);
            bool seedSuccess = false;
            while (!seedSuccess) {
                seedSuccess = SeedShips(seed);
                if (!seedSuccess)
                    seed++;
            }
        }

        public AIBattleships(int id, int boardWidth, int boardHeight, double variance, int seed, Ship[] fleet, double[,] aim, int family, double ELO) {
            _id = id;
            _family = family;
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _seed = seed;
            _variance = variance;
            scoreELO = ELO;
            this.fleet = fleet;
            this.aim = aim;
            NewGameReset();
        }

        /// <param name="mutationChance">Number between 0 and 1</param>
        /// <returns></returns>
        public AIBattleships GetChild(double mutationChance, int newID, bool provideELO) {
            double ELO = 1000;
            if (provideELO)
                ELO = scoreELO;
            return new AIBattleships(newID, _boardWidth, _boardHeight, _variance, _seed, CopyFleet(), ChildAim(mutationChance), _family, ELO);
        }

        private double[,] ChildAim(double mutationChance) {
            Random r = new Random();
            int mutations = 0;
            double[,] newAim = new double[_boardWidth, _boardHeight];
            for (int x = 0;x<_boardWidth; x++) {
                for (int y = 0; y<_boardHeight; y++) {
                    // Check if the aim for that particular square mutates or not
                    newAim[x, y] = aim[x, y];
                    if (r.NextDouble() <= mutationChance) {
                        newAim[x, y] = r.NextDouble() * _variance - _variance / 2;
                        mutations++;
                    }
                }
            }
            //Console.WriteLine("Child of AI " + _id + " created with " + mutations + " mutations");
            return newAim;
        }

        private Ship[] CopyFleet() {
            Ship[] newFleet = new Ship[10];
            for (int i = 0; i < 10; i++) {
                Ship newShip = fleet[i].Copy();
                newFleet[i] = newShip;
            }
            return newFleet;
        }

        public void SetupDirectProxyLists(int length, int ownTicket) {
            foughtDirect = new bool[length];
            foughtProxy = new bool[length];
            foughtDirect[ownTicket] = true;
            foughtProxy[ownTicket] = true;
        }

        public bool FoughtDirect(int id) {
            return foughtDirect[id];
        }

        public bool[] GetFoughtDirect() {
            return foughtDirect;
        }

        public void SetFoughtDirect(int id) {
            foughtDirect[id] = true;
        }

        public bool FoughtProxy(int id) {
            return foughtProxy[id];
        }

        public void SetFoughtProxy(bool[] foughtList) {
            for (int i = 0; i < Math.Min(foughtProxy.Length,foughtList.Length); i++) {
                foughtProxy[i] = foughtProxy[i] || foughtList[i];
            }
        }

        public void SetFoughtProxy(int id) {
            foughtProxy[id] = true;
        }

        public bool CheckFightsToGo() {
            for (int i = 0; i < foughtProxy.Length; i++) {
                if (!(foughtDirect[i] || foughtProxy[i])) {
                    return true;
                }
            }
            return false;
        }

        public bool CheckDirectFightsLeft() {
            foreach(bool fought in foughtDirect) {
                if (!fought)
                    return true;
            }
            return false;
        }

        public double ELOScore {
            get { return scoreELO; }
            set { scoreELO = value; }
        }

        public int Wins {
            get { return wins; }
            set { wins = value; }
        }

        public int Family {
            get { return _family; }
        }

        public void NewGameReset() {
            aimMods = new double[_boardWidth, _boardHeight];
            shotTiles = new bool[_boardWidth, _boardHeight];
            fleetDestroyed = new bool[10];
            foreach (Ship ship in fleet) {
                ship.NewGameReset();
            }
        }

        public void Turn(AIBattleships opponent) {
            // Calculate what coordinates to next hit
            double maxValue = double.MinValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            for (int x = 0; x < _boardWidth; x++) {
                for (int y = 0; y < _boardHeight; y++) {
                    if (!shotTiles[x, y]) {
                        double value = aim[x, y] + aimMods[x, y];
                        if (value > maxValue) {
                            maxValue = value;
                            maxX = x;
                            maxY = y;
                        }
                    }
                }
            }
            // Hit the opponent and get the results
            bool[] hitResults = opponent.GetHit(maxX, maxY);
            shotTiles[maxX, maxY] = true; // Also ensure that we won't be shooting in the same spot twice
            // Use the results to calculate hit modifiers for the next turn
            if (hitResults[0]) {
                // If the shot was a hit, hitmods depend on whether we sunk a ship or not
                double hitMod = hitModifier;
                if (hitResults[1]) {
                    // If a ship was sunk, put negative hitmods on the surrounding area
                    hitMod = -1;
                }
                if (maxX > 0)
                    aimMods[maxX - 1, maxY] = hitMod;
                if (maxX < _boardWidth - 1)
                    aimMods[maxX + 1, maxY] = hitMod;
                if (maxY > 0)
                    aimMods[maxX, maxY - 1] = hitMod;
                if (maxY < _boardHeight - 1)
                    aimMods[maxX, maxY + 1] = hitMod;
            }
        }

        public bool Alive {
            get {
                bool destroyed = true;
                foreach (bool b in fleetDestroyed) {
                    destroyed &= b;
                }
                return !destroyed;
            }
        }

        /// <summary>
        /// Take a hit, return 2 bools
        /// First one is if shot was a hit
        /// Second one is if a ship was destroyed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool[] GetHit(int x, int y) {
            bool hit = false;
            bool shipDestroyed = false;
            for (int i = 0; i < fleetDestroyed.Length; i++) {
                hit = fleet[i].GetHit(x, y);
                if (hit) {
                    if (!fleet[i].Alive) {
                        fleetDestroyed[i] = true;
                        shipDestroyed = true;
                    }
                    return new bool[] { hit, shipDestroyed };
                }
            }
            return new bool[] { hit, shipDestroyed };
        }

        /// <summary>
        /// SetAim initializes the aim of the AI
        /// The aim is a grid representation of the board
        /// Each tile has a certain value, and the AI keeps on hitting the tile with the highest value
        /// </summary>
        private void SetAim(double variance, int seed) {
            Random r = new Random(seed);
            aim = new double[_boardWidth, _boardHeight];
            aimMods = new double[_boardWidth, _boardHeight];
            shotTiles = new bool[_boardWidth, _boardHeight];
            for (int x = 0; x < _boardWidth; x++) {
                for (int y = 0; y < _boardHeight; y++) {
                    aim[x, y] = r.NextDouble() * variance - variance / 2;
                }
            }
        }

        private bool SeedShips(int seed) {
            fleet = new Ship[10];
            fleetDestroyed = new bool[10];
            Random random = new Random(seed);
            // Work from largest ship downwards in a deterministic fashion
            // Use random number generator with specific seed so that everything is repeatable given the same seed
            // Per ship: pick x/y coordinates (exclude the ones at the right and lower edge from distance ship-1)
            // Pick an orientation: horizontal or vertical
            // Check for collisions with the other ships:
            //    If a collision is found, restart from picking x/y coordinates
            // Idea to check collisions: 10 by 10 grid with bools for each coordinate
            // Also illegal to place ships adjacent to one another, so mark those on bool map as well
            bool[,] tempBoardFilled = new bool[10, 10];
            int shipCount = 0;
            for (int size = 5; size > 1; size--) {
                // For each size, make 6 - shipsize ships. (1x 5-length, 2x 4-length, 3x 3-length, 4x 2-length)
                int shipsMade = 0;
                int deadlockCounter = 0;
                while (shipsMade < 6 - size) {
                    if (deadlockCounter > (6 - size) * (_boardWidth * _boardHeight) * 100)
                        return false; 
                    int startX = random.Next(0, 10 - size); // 10 because upperbound is exclusive instead of inclusive
                    int startY = random.Next(0, 10 - size);
                    if (tempBoardFilled[startX, startY]) {
                        deadlockCounter++;
                        continue;
                    }
                    int orientation = random.Next(0, 2); // 0 is horizontal, 1 is vertical
                    // Check for collisions:
                    bool hadCollision = false;
                    int[][] coordinates = new int[size][];
                    for (int i = 0; i < size; i++) {
                        int x = startX + (i * (1 - orientation));
                        int y = startY + (i * orientation);
                        coordinates[i] = new int[2] { x, y };
                        // Check if the x, y is available or not
                        if (tempBoardFilled[x, y]) {
                            hadCollision = true;
                            deadlockCounter++;
                            break;
                        }
                    }
                    // If there was no collision, add a ship and make the spaces of it and the surroundings unavailable
                    if (!hadCollision) {
                        bool orient = true;
                        if (orientation > 0)
                            orient = false;
                        Ship ship = new Ship(size, orient, coordinates[0]);
                        fleet[shipCount] = ship;
                        shipCount++;
                        shipsMade++;
                        foreach(int[] coord in coordinates) {
                            tempBoardFilled[coord[0], coord[1]] = true;
                            foreach(int[] neighbour in Neighbours(coord[0], coord[1])) {
                                if (neighbour[0] >0 && neighbour[1] > 0) {
                                    tempBoardFilled[neighbour[0], neighbour[1]] = true;
                                }
                            }
                        }
                    }
                }
            }
            // Return true for a succes
            return true;
        }

        public int ID {
            get { return _id; }
        }

        private int[][] Neighbours(int x, int y) {
            int[][] neighbours = new int[4][];
            neighbours[0] = Left(x, y);
            neighbours[1] = Right(x, y);
            neighbours[2] = Up(x, y);
            neighbours[3] = Down(x, y);
            return neighbours;
        }

        private int[] Left(int x, int y) {
            int[] left = { -1, -1 };
            if (x > 0) {
                left = new int[2] { x - 1, y };
            }
            return left;
        }

        private int[] Right(int x, int y) {
            int[] right = { -1, -1 };
            if (x < _boardWidth - 1) {
                right = new int[2] { x + 1, y };
            }
            return right;
        }

        private int[] Up(int x, int y) {
            int[] up = { -1, -1 };
            if (y > 0) {
                up = new int[2] { x, y - 1 };
            }
            return up;
        }

        private int[] Down(int x, int y) {
            int[] down = { -1, -1 };
            if (x < _boardHeight - 1) {
                down = new int[2] { x, y + 1 };
            }
            return down;
        }
    }

    class Ship {
        private int length;
        private bool horizontal;
        private int[] startCoordinates;
        //private List<int[]> coordinates;
        private int[][] coordinates;
        private bool[] CoordinatesDestroyed;

        public Ship(int _length, bool _horizontal, int[] _startCoordinates) {
            length = _length;
            horizontal = _horizontal;
            startCoordinates = _startCoordinates;
            CalculateCoordinates();
        }

        public Ship Copy() {
            Ship newShip = new Ship(length, horizontal, startCoordinates);
            return newShip;
        }

        public void NewGameReset() {
            CoordinatesDestroyed = new bool[length];
        }

        private void CalculateCoordinates() {
            //coordinates = new List<int[]>(length);
            coordinates = new int[length][];
            CoordinatesDestroyed = new bool[length];
            int[] mod = { 0, 1 };
            if (horizontal) {
                mod = new int[] { 1, 0 };
            }
            for(int i = 0; i < length; i++) {
                int[] currentMod = new int[] { i * mod[0], i * mod[1] };
                coordinates[i] = new int[] { startCoordinates[0] + currentMod[0], startCoordinates[1] + currentMod[1] };
            }
        }

        public bool GetHit(int x, int y) {
            bool hit = false;
            for(int i = 0; i < CoordinatesDestroyed.Length; i++) {
                if (coordinates[i][0] ==x && coordinates[i][1] == y) {
                    hit = true;
                    CoordinatesDestroyed[i] = true;
                    return hit;
                }
            }
            return hit;
        }

        /// <summary>
        /// Check if any of the coordinatesAlive are true
        /// If so, the ship is still alive
        /// </summary>
        public bool Alive {
            get {
                bool destroyed = true;
                foreach(bool b in CoordinatesDestroyed) {
                    destroyed &= b;
                }
                return !destroyed;
            }
        }

        public int Length {
            get { return length; }
        }

        public bool IsHorizontal {
            get { return horizontal; }
        }

        public int[] StartCoordinates {
            get { return startCoordinates; }
        }
    }
}
