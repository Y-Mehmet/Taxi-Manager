using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LevelGenerator
{
    // --- Constants and Structs ---
    private const int GRID_WIDTH = 7;
    private const int GRID_HEIGHT = 11;
    private static readonly Vector2Int GRID_CENTER = new Vector2Int(3, 5);

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

    private struct Blocker
    {
        public Vector2Int Position; 
        public Vector2Int Direction;
    }

    // --- Public Generation Method ---
    public static LevelDefinition GenerateLevel(int levelNumber, int? underpassOverride = null, int? conveyorOverride = null, int? passengerOverride = null, int? colorOverride = null)
    {
        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ BAŞLADI ---");
        var levelDef = new LevelDefinition(levelNumber);
        var rng = new System.Random(levelNumber);
        var difficultyParams = CalculateDifficultyParameters(levelNumber, rng, underpassOverride, conveyorOverride, passengerOverride, colorOverride);
        GenerateConveyorPassengers(levelDef, difficultyParams, rng);
        var solutionOrder = GenerateSolvableLayout(levelDef, difficultyParams, rng);
        GenerateWagonTrainFromLayout(levelDef, solutionOrder, difficultyParams, rng);
        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ TAMAMLANDI ---");
        return levelDef;
    }

    // --- Generation Steps ---
    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, System.Random rng, int? underpassOverride, int? conveyorOverride, int? passengerOverride, int? colorOverride)
    {
        var p = new DifficultyParameters { LevelNumber = levelNumber, PassengerCapacity = 4, UnderpassSequenceLength = 6 };
        int tier = (levelNumber - 1) / 10;
        p.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0);
        if (p.IsBossLevel) { p.NumInitialPassengers = 7 + tier; p.NumUnderpasses = 2 + tier; }
        else { int baseDifficulty = tier * 5; int levelInTier = (levelNumber - 1) % 10; p.NumInitialPassengers = 4 + baseDifficulty + levelInTier; p.NumUnderpasses = tier; }
        p.NumColors = Mathf.Clamp(3 + tier, 3, 11);
        if (levelNumber > 19 && rng.NextDouble() < 0.40) { p.numConveyorPassengers = rng.Next(10, 31); } else { p.numConveyorPassengers = 0; }
        if (underpassOverride.HasValue) p.NumUnderpasses = underpassOverride.Value;
        if (passengerOverride.HasValue) p.NumInitialPassengers = passengerOverride.Value;
        if (colorOverride.HasValue) p.NumColors = Mathf.Clamp(colorOverride.Value, 2, 11);
        if (conveyorOverride.HasValue) p.numConveyorPassengers = conveyorOverride.Value;
        p.NumColors = Mathf.Min(p.NumColors, System.Enum.GetValues(typeof(HyperCasualColor)).Length);
        Debug.Log($"Zorluk: Yolcu={p.NumInitialPassengers}, AltGeçit={p.NumUnderpasses}, Renk={p.NumColors}, Konveyör={p.numConveyorPassengers}");
        return p;
    }

    private static void GenerateConveyorPassengers(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        if (p.numConveyorPassengers <= 0) return;
        var colors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors);
        for (int i = 0; i < p.numConveyorPassengers; i++) { levelDef.conveyorPassengers.Add(new PassengerSpawnData { color = colors[rng.Next(colors.Count)] }); }
    }

    private static List<PlacedObject> GenerateSolvableLayout(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        var occupied = new List<Vector2Int>();
        var solutionOrder = new List<PlacedObject>();
        var colors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors);
        int totalObjects = p.NumInitialPassengers + p.NumUnderpasses;
        var spawns = new List<Vector2Int>();
        for (int x = 1; x < GRID_WIDTH - 1; x++) for (int y = 1; y < GRID_HEIGHT - 1; y++) spawns.Add(new Vector2Int(x, y));
        spawns = spawns.OrderBy(pos => Vector2.Distance(pos, GRID_CENTER)).ToList();
        var colorOrder = Enumerable.Range(0, totalObjects).Select(_ => colors[rng.Next(colors.Count)]).ToList();
        int underpassesLeft = p.NumUnderpasses;
        foreach (var color in colorOrder)
        {
            bool placed = false;
            for (int i = 0; i < 1000; i++)
            {
                var pos = spawns[rng.Next(spawns.Count)];
                var dir = GetRandomDirection(rng);
                if (underpassesLeft > 0 && rng.Next(0, 3) == 0)
                {
                    if (TryPlaceUnderpass(levelDef, p, pos, dir, occupied, colors, rng, out var item)) { solutionOrder.Add(PlacedObject.CreateUnderpass(item)); underpassesLeft--; placed = true; break; }
                }
                else
                {
                    if (TryPlacePassenger(levelDef, color, pos, dir, occupied, solutionOrder.Count < colors.Count, out var item)) { solutionOrder.Add(PlacedObject.CreatePassenger(item)); placed = true; break; }
                }
            }
            if (!placed) Debug.LogWarning($"Geçerli bir yer bulunamadı!");
        }
        return solutionOrder;
    }
    
    private static void GenerateWagonTrainFromLayout(LevelDefinition levelDef, List<PlacedObject> solutionOrder, DifficultyParameters p, System.Random rng)
    {
        foreach (var obj in solutionOrder)
        {
            if (obj.IsUnderpass) { foreach (var color in obj.UnderpassData.passengerSequence) { for (int i = 0; i < p.PassengerCapacity; i++) levelDef.wagons.Add(new WagonSpawnData(color, 1)); } }
            else { for (int i = 0; i < p.PassengerCapacity; i++) levelDef.wagons.Add(new WagonSpawnData(obj.RepresentativeColor, 1)); }
        }
        if (p.IsBossLevel) { levelDef.wagons = levelDef.wagons.OrderBy(_ => rng.Next()).ToList(); }
    }

    // --- Placement and Validation Logic ---
    private static bool TryPlacePassenger(LevelDefinition levelDef, HyperCasualColor color, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, bool mustBeUnblocked, out PassengerSpawnData placedPassenger)
    {
        placedPassenger = default;
        if (!IsValidPlacement(pos, occupied)) return false;
        if (mustBeUnblocked && !IsPathClear(pos, dir, occupied)) return false;
        if (CreatesSharedTargetDeadlock(pos, dir, levelDef)) return false; // NEW CHECK
        if (CreatesCircularDeadlock(pos, dir, levelDef)) return false;
        placedPassenger = new PassengerSpawnData { position = pos, direction = dir, color = color };
        levelDef.initialPassengerGroups.Add(placedPassenger);
        occupied.Add(pos);
        return true;
    }

    private static bool TryPlaceUnderpass(LevelDefinition levelDef, DifficultyParameters p, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, List<HyperCasualColor> colors, System.Random rng, out UnderpassSpawnData placedUnderpass)
    {
        placedUnderpass = default;
        var passengerSpawnPos = pos + dir;
        if (!IsValidPlacement(pos, occupied) || !IsValidPlacement(passengerSpawnPos, occupied)) return false;
        if (CreatesSharedTargetDeadlock(passengerSpawnPos, dir, levelDef)) return false; // NEW CHECK
        if (CreatesCircularDeadlock(passengerSpawnPos, dir, levelDef)) return false;
        var sequence = Enumerable.Range(0, p.UnderpassSequenceLength).Select(_ => colors[rng.Next(colors.Count)]).ToList();
        placedUnderpass = new UnderpassSpawnData { position = pos, direction = dir, passengerSequence = sequence };
        levelDef.underpasses.Add(placedUnderpass);
        occupied.Add(pos);
        occupied.Add(passengerSpawnPos);
        return true;
    }

    private static bool IsPathClear(Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied) => !occupied.Contains(pos + dir);
    private static Vector2Int GetRandomDirection(System.Random rng) { int v = rng.Next(4); return v == 0 ? Vector2Int.up : v == 1 ? Vector2Int.down : v == 2 ? Vector2Int.left : Vector2Int.right; }
    private static bool IsValidPlacement(Vector2Int pos, List<Vector2Int> occupied) => !(pos.x <= 0 || pos.x >= GRID_WIDTH - 1 || pos.y <= 0 || pos.y >= GRID_HEIGHT - 1 || occupied.Contains(pos));

    // --- Deadlock Detection ---
    private static List<Blocker> GetAllBlockers(LevelDefinition levelDef)
    {
        var blockers = new List<Blocker>();
        levelDef.initialPassengerGroups.ForEach(pass => blockers.Add(new Blocker { Position = pass.position, Direction = pass.direction }));
        levelDef.underpasses.ForEach(u => blockers.Add(new Blocker { Position = u.position + u.direction, Direction = u.direction }));
        return blockers;
    }

    private static bool CreatesSharedTargetDeadlock(Vector2Int candidatePos, Vector2Int candidateDir, LevelDefinition levelDef)
    {
        var candidateTarget = candidatePos + candidateDir;
        var allBlockers = GetAllBlockers(levelDef);
        foreach (var blocker in allBlockers)
        {
            var blockerTarget = blocker.Position + blocker.Direction;
            if (blockerTarget == candidateTarget) return true;
        }
        return false;
    }

    private static bool CreatesCircularDeadlock(Vector2Int candidatePos, Vector2Int candidateDir, LevelDefinition levelDef)
    {
        var blockers = GetAllBlockers(levelDef);
        var candidate = new Blocker { Position = candidatePos, Direction = candidateDir };
        blockers.Add(candidate);
        
        var dependencyChain = new List<Blocker>();
        var current = candidate;

        for (int i = 0; i < blockers.Count + 1; i++) 
        {
            dependencyChain.Add(current);
            var targetCell = current.Position + current.Direction;
            var nextBlockerIndex = blockers.FindIndex(b => b.Position == targetCell);
            if (nextBlockerIndex == -1) return false;
            var nextInChain = blockers[nextBlockerIndex];
            if (nextInChain.Position == candidate.Position) return true;
            if (dependencyChain.Any(b => b.Position == nextInChain.Position)) return true;
            current = nextInChain;
        }
        return true;
    }
}
