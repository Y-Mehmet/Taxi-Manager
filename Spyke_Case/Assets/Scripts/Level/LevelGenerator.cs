
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// "Önce Çözüm, Sonra Problem" mantığıyla çalışan, tüm oyun kurallarını
/// dikkate alarak ve STRUCT veri yapılarıyla uyumlu, çözülebilir seviyeler üreten algoritma.
/// </summary>
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
        
        // Alt geçitler artık çok renkli olduğu için, çözüm sırası için ilk rengi baz alıyoruz.
        public HyperCasualColor RepresentativeColor => IsUnderpass ? UnderpassData.passengerSequence.First() : PassengerData.color;

        public static PlacedObject CreatePassenger(PassengerSpawnData data)
        {
            return new PlacedObject { PassengerData = data, IsUnderpass = false };
        }

        public static PlacedObject CreateUnderpass(UnderpassSpawnData data)
        {
            return new PlacedObject { UnderpassData = data, IsUnderpass = true };
        }
    }

    private struct DifficultyParameters
    {
        public int LevelNumber;
        public bool IsBossLevel;
        public int NumInitialPassengers;
        public int NumUnderpasses;
        public int NumColors;
        public int numConveyorPassengers; // YENİ
        public int PassengerCapacity;
        public int UnderpassSequenceLength;
    }

    public static LevelDefinition GenerateLevel(int levelNumber, int? underpassOverride = null, int? conveyorOverride = null, int? passengerOverride = null, int? colorOverride = null)
    {
        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ BAŞLADI ---");
        var levelDef = new LevelDefinition(levelNumber);
        var difficultyParams = CalculateDifficultyParameters(levelNumber, underpassOverride, conveyorOverride, passengerOverride, colorOverride);
        
        GenerateConveyorPassengers(levelDef, difficultyParams);
        var solutionOrder = GenerateSolvableLayout(levelDef, difficultyParams);
        GenerateWagonTrainFromLayout(levelDef, solutionOrder, difficultyParams);

        Debug.Log($"--- SEVİYE {levelNumber} ÜRETİMİ TAMAMLANDI ---");
        return levelDef;
    }

    private static void GenerateConveyorPassengers(LevelDefinition levelDef, DifficultyParameters p)
    {
        if (p.numConveyorPassengers <= 0) return;

        var allColors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList();
        var colorsInLevel = allColors.GetRange(0, p.NumColors);
        var rng = new System.Random();

        for (int i = 0; i < p.numConveyorPassengers; i++)
        {
            levelDef.conveyorPassengers.Add(new PassengerSpawnData
            {
                color = colorsInLevel[rng.Next(colorsInLevel.Count)],
            });
        }
    }

    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, int? underpassOverride, int? conveyorOverride, int? passengerOverride, int? colorOverride)
    {
        var parameters = new DifficultyParameters();
        parameters.LevelNumber = levelNumber;
        parameters.PassengerCapacity = 4;
        parameters.UnderpassSequenceLength = 6;

        int tier = (levelNumber - 1) / 10;
        parameters.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0);

        // Varsayılan değerler
        if (parameters.IsBossLevel)
        {
            parameters.NumInitialPassengers = 7 + tier;
            parameters.NumUnderpasses = 2 + tier;
        }
        else
        {
            int baseDifficulty = tier * 5;
            int levelInTier = (levelNumber - 1) % 10;
            parameters.NumInitialPassengers = 4 + baseDifficulty + levelInTier;
            parameters.NumUnderpasses = tier;
        }
        parameters.numConveyorPassengers = 0;
        parameters.NumColors = Mathf.Clamp(3 + tier, 3, 11);

        // Override'ları uygula
        if (underpassOverride.HasValue)
        {
            Debug.Log($"[Generator] Manuel Alt Geçit Değeri: {underpassOverride.Value}");
            parameters.NumUnderpasses = underpassOverride.Value;
        }
        if (conveyorOverride.HasValue)
        {
            Debug.Log($"[Generator] Manuel Konveyör Yolcu Değeri: {conveyorOverride.Value}");
            parameters.numConveyorPassengers = conveyorOverride.Value;
        }
        if (passengerOverride.HasValue)
        {
            Debug.Log($"[Generator] Manuel Yolcu Değeri: {passengerOverride.Value}");
            parameters.NumInitialPassengers = passengerOverride.Value;
        }
        if (colorOverride.HasValue)
        {
            Debug.Log($"[Generator] Manuel Renk Değeri: {colorOverride.Value}");
            parameters.NumColors = Mathf.Clamp(colorOverride.Value, 2, 11);
        }

        int maxAvailableColors = System.Enum.GetValues(typeof(HyperCasualColor)).Length;
        parameters.NumColors = Mathf.Min(parameters.NumColors, maxAvailableColors);

        Debug.Log($"Zorluk Parametreleri: Yolcu={parameters.NumInitialPassengers}, AltGeçit={parameters.NumUnderpasses}, Renk={parameters.NumColors}, Konveyör={parameters.numConveyorPassengers}");
        return parameters;
    }

    private static List<PlacedObject> GenerateSolvableLayout(LevelDefinition levelDef, DifficultyParameters p)
    {
        var occupiedPositions = new List<Vector2Int>();
        var solutionOrder = new List<PlacedObject>();
        var rng = new System.Random();

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
                    // DEĞİŞİKLİK: Artık renk havuzunu da gönderiyoruz.
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
    
    private static void GenerateWagonTrainFromLayout(LevelDefinition levelDef, List<PlacedObject> solutionOrder, DifficultyParameters p)
    {
        foreach (var placedObject in solutionOrder)
        {
            if (placedObject.IsUnderpass)
            {
                // DEĞİŞİKLİK: Alt geçidin içindeki her bir yolcu için sırayla vagon oluştur.
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
                // Normal yolcular için eskisi gibi devam et.
                for (int i = 0; i < p.PassengerCapacity; i++)
                {
                    levelDef.wagons.Add(new WagonSpawnData(placedObject.RepresentativeColor, 1));
                }
            }
        }

        if (p.IsBossLevel && levelDef.wagons.Count > 1)
        {
            var rng = new System.Random();
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

    // DEĞİŞİKLİK: Metot imzası ve iç mantığı güncellendi.
    private static bool TryPlaceUnderpass(LevelDefinition levelDef, DifficultyParameters p, Vector2Int pos, Vector2Int dir, List<Vector2Int> occupied, List<HyperCasualColor> colorsInLevel, System.Random rng, out UnderpassSpawnData placedUnderpass)
    {
        placedUnderpass = new UnderpassSpawnData();
        var passengerSpawnPos = pos + dir;

        if (!IsValidPlacement(pos, occupied)) return false;
        if (!IsValidPlacement(passengerSpawnPos, occupied)) return false;
        if (CreatesDeadlock(passengerSpawnPos, dir, levelDef)) return false;

        // Rastgele renklerden oluşan yolcu dizisi oluştur.
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
