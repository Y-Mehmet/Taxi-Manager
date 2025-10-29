using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LevelGenerator
{
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

    public static LevelDefinition GenerateLevel(int levelNumber, int? underpassOverride = null, int? conveyorOverride = null, int? passengerOverride = null, int? colorOverride = null)
    {
        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ BAŞLADI ---");
        var levelDef = new LevelDefinition(levelNumber);
        var rng = new System.Random(levelNumber); // Seed with level number for deterministic generation

        var difficultyParams = CalculateDifficultyParameters(levelNumber, rng, underpassOverride, conveyorOverride, passengerOverride, colorOverride);
        
        GenerateConveyorPassengers(levelDef, difficultyParams, rng);
        var solutionOrder = GenerateSolvableLayout(levelDef, difficultyParams, rng);
        GenerateWagonTrainFromLayout(levelDef, solutionOrder, difficultyParams, rng);

        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ TAMAMLANDI ---");
        return levelDef;
    }

    private static void GenerateConveyorPassengers(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        if (p.numConveyorPassengers <= 0) return;

        var allColors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList();
        var colorsInLevel = allColors.GetRange(0, p.NumColors);

        for (int i = 0; i < p.numConveyorPassengers; i++)
        {
            levelDef.conveyorPassengers.Add(new PassengerSpawnData
            {
                color = colorsInLevel[rng.Next(colorsInLevel.Count)],
            });
        }
    }

    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, System.Random rng, int? underpassOverride, int? conveyorOverride, int? passengerOverride, int? colorOverride)
    {
        var p = new DifficultyParameters();
        p.LevelNumber = levelNumber;
        p.PassengerCapacity = 4;
        p.UnderpassSequenceLength = 6;

        int tier = (levelNumber - 1) / 10;
        p.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0);

        // --- Default difficulty calculation ---
        if (p.IsBossLevel)
        {
            p.NumInitialPassengers = 7 + tier;
            p.NumUnderpasses = 2 + tier;
        }
        else
        {
            int baseDifficulty = tier * 5;
            int levelInTier = (levelNumber - 1) % 10;
            p.NumInitialPassengers = 4 + baseDifficulty + levelInTier;
            p.NumUnderpasses = tier;
        }
        p.NumColors = Mathf.Clamp(3 + tier, 3, 11);

        // Default conveyor logic
        if (levelNumber > 19 && rng.NextDouble() < 0.40) // 40% chance after level 19
        {
            p.numConveyorPassengers = rng.Next(10, 31); // 10 to 30 passengers
        }
        else
        {
            p.numConveyorPassengers = 0;
        }

        // --- Apply Overrides --- 
        if (underpassOverride.HasValue) p.NumUnderpasses = underpassOverride.Value;
        if (passengerOverride.HasValue) p.NumInitialPassengers = passengerOverride.Value;
        if (colorOverride.HasValue) p.NumColors = Mathf.Clamp(colorOverride.Value, 2, 11);
        // This one is last to ensure it can override the default logic above
        if (conveyorOverride.HasValue) p.numConveyorPassengers = conveyorOverride.Value;

        int maxAvailableColors = System.Enum.GetValues(typeof(HyperCasualColor)).Length;
        p.NumColors = Mathf.Min(p.NumColors, maxAvailableColors);

        Debug.Log($"Zorluk: Yolcu={p.NumInitialPassengers}, AltGeçit={p.NumUnderpasses}, Renk={p.NumColors}, Konveyör={p.numConveyorPassengers}");
        return p;
    }

    private static List<PlacedObject> GenerateSolvableLayout(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        var occupiedPositions = new List<Vector2Int>();
        var solutionOrder = new List<PlacedObject>();
        var allColors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList();
        var colorsInLevel = allColors.GetRange(0, p.NumColors);
        int totalObjectsToPlace = p.NumInitialPassengers + p.NumUnderpasses;
        var potentialSpawns = new List<Vector2Int>();
        for (int x = 1; x < GRID_WIDTH - 1; x++) for (int y = 1; y < GRID_HEIGHT - 1; y++) potentialSpawns.Add(new Vector2Int(x, y));
        potentialSpawns = potentialSpawns.OrderBy(pos => Vector2.Distance(pos, GRID_CENTER)).ToList();
        
        List<HyperCasualColor> colorPlacementOrder = new List<HyperCasualColor>();
        for (int i = 0; i < totalObjectsToPlace; i++)
        {
            colorPlacementOrder.Add(colorsInLevel[rng.Next(colorsInLevel.Count)]);
        }

        int underpassesLeftToPlace = p.NumUnderpasses;
        foreach (var colorToPlace in colorPlacementOrder)
        {
            bool placed = false;
            for (int i = 0; i < 1000; i++)
            {
                var basePos = potentialSpawns[rng.Next(potentialSpawns.Count)];
                var direction = GetRandomDirection(rng);
                bool tryPlaceUnderpass = underpassesLeftToPlace > 0 && rng.Next(0, 3) == 0;

                if (tryPlaceUnderpass)
                {
                    if (TryPlaceUnderpass(levelDef, p, basePos, direction, occupiedPositions, colorsInLevel, rng, out var placedUnderpass))
                    {
                        solutionOrder.Add(PlacedObject.CreateUnderpass(placedUnderpass));
                        underpassesLeftToPlace--;
                        placed = true;
                        break;
                    }
                }
                else
                {
                    bool mustBeUnblocked = solutionOrder.Count < colorsInLevel.Count;
                    if (TryPlacePassenger(levelDef, colorToPlace, basePos, direction, occupiedPositions, mustBeUnblocked, out var placedPassenger))
                    {
                        solutionOrder.Add(PlacedObject.CreatePassenger(placedPassenger));
                        placed = true;
                        break;
                    }
                }
            }
            if (!placed) Debug.LogWarning($"Geçerli bir yer bulunamadı!");
        }
        return solutionOrder;
    }
    
    private static void GenerateWagonTrainFromLayout(LevelDefinition levelDef, List<PlacedObject> solutionOrder, DifficultyParameters p, System.Random rng)
    {
        foreach (var placedObject in solutionOrder)
        {
            if (placedObject.IsUnderpass)
            {
                foreach (var passengerColor in placedObject.UnderpassData.passengerSequence)
                {
                    for (int i = 0; i < p.PassengerCapacity; i++)
                    {
                        levelDef.wagons.Add(new WagonSpawnData(passengerColor, 1));
                    }
                }
            }
            else
            {
                for (int i = 0; i < p.PassengerCapacity; i++)
                {
                    levelDef.wagons.Add(new WagonSpawnData(placedObject.RepresentativeColor, 1));
                }
            }
        }

        if (p.IsBossLevel && levelDef.wagons.Count > 1)
        {
            int n = levelDef.wagons.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                (levelDef.wagons[k], levelDef.wagons[n]) = (levelDef.wagons[n], levelDef.wagons[k]);
            }
        }
    }

    #region Yerleştirme ve Kontrol Metotları

    private static bool TryPlacePassenger(LevelDefinition levelDef, HyperCasualColor color, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, bool mustBeUnblocked, out PassengerSpawnData placedPassenger)
    {
        placedPassenger = new PassengerSpawnData(); 
        if (!IsValidPlacement(pos, occupied)) return false;
        if (CreatesDeadlock(pos, dir, levelDef)) return false;
        if (mustBeUnblocked && !IsPathClear(pos, dir, occupied)) return false;

        placedPassenger = new PassengerSpawnData { position = pos, direction = dir, color = color };
        levelDef.initialPassengerGroups.Add(placedPassenger);
        occupied.Add(pos);
        return true;
    }

    private static bool TryPlaceUnderpass(LevelDefinition levelDef, DifficultyParameters p, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, List<HyperCasualColor> colorsInLevel, System.Random rng, out UnderpassSpawnData placedUnderpass)
    {
        placedUnderpass = new UnderpassSpawnData();
        var passengerSpawnPos = pos + dir;

        if (!IsValidPlacement(pos, occupied)) return false;
        if (!IsValidPlacement(passengerSpawnPos, occupied)) return false;
        if (CreatesDeadlock(passengerSpawnPos, dir, levelDef)) return false;

        var sequence = new List<HyperCasualColor>();
        for (int i = 0; i < p.UnderpassSequenceLength; i++)
        {
            sequence.Add(colorsInLevel[rng.Next(colorsInLevel.Count)]);
        }

        placedUnderpass = new UnderpassSpawnData
        {
            position = pos,
            direction = dir,
            passengerSequence = sequence
        };
        levelDef.underpasses.Add(placedUnderpass);
        occupied.Add(pos);
        occupied.Add(passengerSpawnPos);
        return true;
    }

    private static bool IsPathClear(Vector2Int position, Vector2Int direction, List<Vector2Int> occupiedPositions)
    {
        return !occupiedPositions.Contains(position + direction);
    }

    private static Vector2Int GetRandomDirection(System.Random rng)
    {
        int val = rng.Next(4);
        if (val == 0) return Vector2Int.up;
        if (val == 1) return Vector2Int.down;
        if (val == 2) return Vector2Int.left;
        return Vector2Int.right;
    }

    private static bool IsValidPlacement(Vector2Int position, List<Vector2Int> occupiedPositions)
    {
        if (position.x <= 0 || position.x >= GRID_WIDTH - 1 || position.y <= 0 || position.y >= GRID_HEIGHT - 1) return false;
        if (occupiedPositions.Contains(position)) return false;
        return true;
    }

    private static bool CreatesDeadlock(Vector2Int position, Vector2Int direction, LevelDefinition levelDef)
    {
        var allPassengerPoints = levelDef.initialPassengerGroups.Select(pass => pass).ToList();
        allPassengerPoints.AddRange(levelDef.underpasses.Select(u => new PassengerSpawnData { position = u.position + u.direction, direction = u.direction, color = u.passengerSequence.First() }));

        foreach (var passenger in allPassengerPoints)
        {
            if (passenger.position.x == position.x)
            {
                if (direction.y == 1 && passenger.direction.y == -1 && passenger.position.y > position.y) return true;
                if (direction.y == -1 && passenger.direction.y == 1 && passenger.position.y < position.y) return true;
            }
            if (passenger.position.y == position.y)
            {
                if (direction.x == 1 && passenger.direction.x == -1 && passenger.position.x > position.x) return true;
                if (direction.x == -1 && passenger.direction.x == 1 && passenger.position.x < position.x) return true;
            }
        }
        return false;
    }
    #endregion
}