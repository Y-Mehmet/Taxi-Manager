using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LevelGenerator
{
    // --- Constants and Structs ---
    private const int GRID_WIDTH = 7;
    private const int GRID_HEIGHT = 11;
    private const int MAX_GENERATION_RETRIES = 100000;

    private class PlacedObject
    {
        public PassengerSpawnData PassengerData;
        public UnderpassSpawnData UnderpassData;
        public bool IsUnderpass { get; private set; }
        public HyperCasualColor RepresentativeColor => IsUnderpass ? UnderpassData.passengerSequence.First() : PassengerData.color;
        public static PlacedObject CreatePassenger(PassengerSpawnData data) => new PlacedObject { PassengerData = data, IsUnderpass = false };
        public static PlacedObject CreateUnderpass(UnderpassSpawnData data) => new PlacedObject { UnderpassData = data, IsUnderpass = true };
    }

    private struct DifficultyParameters
    {
        public int LevelNumber, NumInitialPassengers, NumUnderpasses, NumColors, numConveyorPassengers, PassengerCapacity, UnderpassSequenceLength;
        public bool IsBossLevel;
    }

    private class PassengerNode
    {
        public Vector2Int Position; 
        public Vector2Int Direction;
        public PassengerNode BlockedBy;
        public PassengerNode(Vector2Int pos, Vector2Int dir) { Position = pos; Direction = dir; BlockedBy = null; }
    }

    // --- PUBLIC GENERATION METHOD ---
    public static LevelDefinition GenerateLevel(int levelNumber, int? underpassOverride = null, int? conveyorOverride = null, int? passengerOverride = null, int? colorOverride = null)
    {
        var rng = new System.Random(levelNumber);
        var difficultyParams = CalculateDifficultyParameters(levelNumber, rng, underpassOverride, conveyorOverride, passengerOverride, colorOverride);

        LevelDefinition levelDef;
        List<PlacedObject> solutionOrder = null; // Must be declared here
        bool isSolvable;
        int retries = 0;

        // --- GENERATE-AND-TEST LOOP ---
        do
        {
            levelDef = new LevelDefinition(levelNumber);
            // This now returns the crucial solution order
            solutionOrder = AttemptToGenerateLayout(levelDef, difficultyParams, rng);
            
            isSolvable = IsLayoutSolvable(levelDef);

            if (!isSolvable)
            {
                retries++;
                Debug.LogWarning($"Level {levelNumber} failed validation. Retrying... ({retries}/{MAX_GENERATION_RETRIES})");
            }

        } while (!isSolvable && retries < MAX_GENERATION_RETRIES);

        if (!isSolvable)
        {
            Debug.LogError($"FAILED to generate a solvable layout for Level {levelNumber} after {MAX_GENERATION_RETRIES} attempts.");
            return null; // Signal failure
        }

        Debug.Log($"--- Level {levelNumber} validation PASSED. Finalizing... ---");
        GenerateConveyorPassengers(levelDef, difficultyParams, rng);
        // Use the solutionOrder that was validated with the successful layout
        GenerateWagonTrainFromLayout(levelDef, solutionOrder, difficultyParams, rng);
        return levelDef;
    }

    // --- LAYOUT VALIDATION (THE "SUPER-CHECK") ---
    private static bool IsLayoutSolvable(LevelDefinition levelDef)
    {
        var allNodes = new List<PassengerNode>();
        var nodeMap = new Dictionary<Vector2Int, PassengerNode>();
        foreach (var p in levelDef.initialPassengerGroups) { var node = new PassengerNode(p.position, p.direction); allNodes.Add(node); if (!nodeMap.ContainsKey(p.position)) nodeMap.Add(p.position, node); }
        foreach (var u in levelDef.underpasses) { var pos = u.position + u.direction; var node = new PassengerNode(pos, u.direction); allNodes.Add(node); if (!nodeMap.ContainsKey(pos)) nodeMap.Add(pos, node); }
        foreach (var node in allNodes) { var targetPos = node.Position + node.Direction; if (nodeMap.ContainsKey(targetPos)) { node.BlockedBy = nodeMap[targetPos]; } }
        var visiting = new HashSet<PassengerNode>();
        var visited = new HashSet<PassengerNode>();
        foreach (var node in allNodes) { if (!visited.Contains(node)) { if (HasCycleDFS(node, visiting, visited)) return false; } }

        // --- New Check for Head-on Collisions ---
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = i + 1; j < allNodes.Count; j++)
            {
                var nodeA = allNodes[i];
                var nodeB = allNodes[j];

                // Check for head-on collision in the same column
                if (nodeA.Position.x == nodeB.Position.x && nodeA.Direction.y != 0 && nodeA.Direction.y == -nodeB.Direction.y)
                {
                    if ((nodeA.Position.y > nodeB.Position.y && nodeA.Direction.y < 0) || (nodeB.Position.y > nodeA.Position.y && nodeB.Direction.y < 0))
                    {
                        Debug.LogWarning($"Level validation failed: Head-on collision detected in column {nodeA.Position.x} between node at {nodeA.Position} and node at {nodeB.Position}.");
                        return false;
                    }
                }

                // Check for head-on collision in the same row
                if (nodeA.Position.y == nodeB.Position.y && nodeA.Direction.x != 0 && nodeA.Direction.x == -nodeB.Direction.x)
                {
                    if ((nodeA.Position.x > nodeB.Position.x && nodeA.Direction.x < 0) || (nodeB.Position.x > nodeA.Position.x && nodeB.Direction.x < 0))
                    {
                        Debug.LogWarning($"Level validation failed: Head-on collision detected in row {nodeA.Position.y} between node at {nodeA.Position} and node at {nodeB.Position}.");
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool HasCycleDFS(PassengerNode node, HashSet<PassengerNode> visiting, HashSet<PassengerNode> visited)
    {
        visiting.Add(node);
        if (node.BlockedBy != null) { if (visiting.Contains(node.BlockedBy)) return true; if (!visited.Contains(node.BlockedBy)) { if (HasCycleDFS(node.BlockedBy, visiting, visited)) return true; } }
        visiting.Remove(node);
        visited.Add(node);
        return false;
    }

    // --- ATTEMPT TO GENERATE A LAYOUT ---
    private static List<PlacedObject> AttemptToGenerateLayout(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        var occupied = new List<Vector2Int>();
        var solutionOrder = new List<PlacedObject>(); // This is now correctly created and returned
        var colors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors);
        int totalObjects = p.NumInitialPassengers + p.NumUnderpasses;
        var spawns = new List<Vector2Int>();
        for (int x = 1; x < GRID_WIDTH - 1; x++) for (int y = 1; y < GRID_HEIGHT - 1; y++) spawns.Add(new Vector2Int(x, y));
        spawns = spawns.OrderBy(pos => Vector2.Distance(pos, new Vector2Int(3,5))).ToList();
        int underpassesLeft = p.NumUnderpasses;
        for(int i = 0; i < totalObjects; i++)
        {
            bool placed = false;
            for (int j = 0; j < 500; j++)
            {
                var pos = spawns[rng.Next(spawns.Count)];
                var dir = GetRandomDirection(rng);
                if (underpassesLeft > 0 && rng.Next(0, 3) == 0)
                {
                    if (TryPlaceUnderpass(levelDef, p, pos, dir, occupied, colors, rng, out var underpass)) 
                    { 
                        solutionOrder.Add(PlacedObject.CreateUnderpass(underpass));
                        underpassesLeft--; 
                        placed = true; 
                        break; 
                    }
                }
                else
                {
                    if (TryPlacePassenger(levelDef, colors[rng.Next(colors.Count)], pos, dir, occupied, out var passenger)) 
                    { 
                        solutionOrder.Add(PlacedObject.CreatePassenger(passenger));
                        placed = true; 
                        break; 
                    }
                }
            }
        }
        return solutionOrder; // Return the generated order
    }
    
    // --- Simplified Placement Logic ---
    private static bool TryPlacePassenger(LevelDefinition levelDef, HyperCasualColor color, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, out PassengerSpawnData placedPassenger)
    {
        placedPassenger = default;
        if (!IsValidPlacement(pos, occupied)) return false;
        levelDef.initialPassengerGroups.Add(new PassengerSpawnData { position = pos, direction = dir, color = color });
        occupied.Add(pos);
        placedPassenger = levelDef.initialPassengerGroups.Last();
        return true;
    }

    private static bool TryPlaceUnderpass(LevelDefinition levelDef, DifficultyParameters p, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, List<HyperCasualColor> colors, System.Random rng, out UnderpassSpawnData placedUnderpass)
    {
        placedUnderpass = default;
        var passengerSpawnPos = pos + dir;
        if (!IsValidPlacement(pos, occupied) || !IsValidPlacement(passengerSpawnPos, occupied)) return false;
        var sequence = Enumerable.Range(0, p.UnderpassSequenceLength).Select(_ => colors[rng.Next(colors.Count)]).ToList();
        levelDef.underpasses.Add(new UnderpassSpawnData { position = pos, direction = dir, passengerSequence = sequence });
        occupied.Add(pos);
        occupied.Add(passengerSpawnPos);
        placedUnderpass = levelDef.underpasses.Last();
        return true;
    }

    // --- Other Helper Methods (Some are Unchanged) ---
    private static Vector2Int GetRandomDirection(System.Random rng) { int v = rng.Next(4); return v == 0 ? Vector2Int.up : v == 1 ? Vector2Int.down : v == 2 ? Vector2Int.left : Vector2Int.right; }
    private static bool IsValidPlacement(Vector2Int pos, List<Vector2Int> occupied) => !(pos.x <= 0 || pos.x >= GRID_WIDTH - 1 || pos.y <= 0 || pos.y >= GRID_HEIGHT - 1 || occupied.Contains(pos));
    private static void GenerateConveyorPassengers(LevelDefinition levelDef, DifficultyParameters p, System.Random rng) { if (p.numConveyorPassengers <= 0) return; var c = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors); for (int i = 0; i < p.numConveyorPassengers; i++) { levelDef.conveyorPassengers.Add(new PassengerSpawnData { color = c[rng.Next(c.Count)] }); } }
    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, System.Random rng, int? u, int? c, int? ps, int? clr) { var p = new DifficultyParameters { LevelNumber = levelNumber, PassengerCapacity = 4, UnderpassSequenceLength = 6 }; int tier = (levelNumber - 1) / 10; p.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0); if (p.IsBossLevel) { p.NumInitialPassengers = 7 + tier; p.NumUnderpasses = 2 + tier; } else { int baseDifficulty = tier * 5; int levelInTier = (levelNumber - 1) % 10; p.NumInitialPassengers = 4 + baseDifficulty + levelInTier; p.NumUnderpasses = tier; } p.NumColors = Mathf.Clamp(3 + tier, 3, 11); if (levelNumber > 19 && rng.NextDouble() < 0.40) { p.numConveyorPassengers = rng.Next(10, 31); } else { p.numConveyorPassengers = 0; } if (u.HasValue) p.NumUnderpasses = u.Value; if (ps.HasValue) p.NumInitialPassengers = ps.Value; if (clr.HasValue) p.NumColors = Mathf.Clamp(clr.Value, 2, 11); if (c.HasValue) p.numConveyorPassengers = c.Value; p.NumColors = Mathf.Min(p.NumColors, System.Enum.GetValues(typeof(HyperCasualColor)).Length); return p; }
    private static void GenerateWagonTrainFromLayout(LevelDefinition levelDef, List<PlacedObject> solutionOrder, DifficultyParameters p, System.Random rng) { foreach (var obj in solutionOrder) { if (obj.IsUnderpass) { foreach (var color in obj.UnderpassData.passengerSequence) { for (int i = 0; i < p.PassengerCapacity; i++) levelDef.wagons.Add(new WagonSpawnData(color, 1)); } } else { for (int i = 0; i < p.PassengerCapacity; i++) levelDef.wagons.Add(new WagonSpawnData(obj.RepresentativeColor, 1)); } } foreach (var conveyorPassenger in levelDef.conveyorPassengers) { for (int i = 0; i < p.PassengerCapacity; i++) { levelDef.wagons.Add(new WagonSpawnData(conveyorPassenger.color, 1)); } } if (p.IsBossLevel || levelDef.wagons.Count > 10) { levelDef.wagons = levelDef.wagons.OrderBy(_ => rng.Next()).ToList(); } }
}
