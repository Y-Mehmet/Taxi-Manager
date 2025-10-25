using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class is responsible for generating level definitions based on a level number and a set of rules.
public static class LevelGenerator
{
    // Grid dimensions, excluding the non-spawnable border.
    private const int GridWidth = 7;
    private const int GridHeight = 11;

    // Private struct to hold the calculated parameters for a given difficulty.
    private struct DifficultyParameters
    {
        public int LevelNumber;
        public bool IsBossLevel;
        public int NumInitialPassengers;
        public int NumUnderpasses;
        public int NumColors;
        public int TotalWagons;
        public int PassengerCapacity;
    }

    /// <summary>
    /// Generates a complete LevelDefinition for a given level number.
    /// </summary>
    public static LevelDefinition GenerateLevel(int levelNumber, int? underpassOverride = null)
    {
        Debug.Log($"--- Starting Generation for Level {levelNumber} ---");

        // Create a new definition for this level.
        LevelDefinition levelDef = new LevelDefinition(levelNumber);

        // Phase 2: Calculate difficulty parameters.
        DifficultyParameters difficultyParams = CalculateDifficultyParameters(levelNumber, underpassOverride);

        // Phase 3.1: Generate the "problem" (wagons).
        GenerateWagons(levelDef, difficultyParams);

        // Phase 3.2: Generate the "solution" (passengers and underpasses).
        GenerateSolutions(levelDef, difficultyParams);

        Debug.Log($"--- Finished Generation for Level {levelNumber} ---");

        return levelDef;
    }

    /// <summary>
    /// Calculates the parameters for a level based on the difficulty curve.
    /// </summary>
    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, int? underpassOverride = null)
    {
        var parameters = new DifficultyParameters { LevelNumber = levelNumber };

        int tier = (levelNumber - 1) / 10; // 0 for levels 1-10, 1 for 11-20, etc.
        parameters.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0);

        if (parameters.IsBossLevel)
        {
            // --- Boss Level Logic ---
            parameters.NumColors = Mathf.Min(2 + tier, 3); // Starts with 2 colors at level 10, maxes out at 3.
            parameters.NumInitialPassengers = 3 + tier;
            parameters.NumUnderpasses = 1 + tier;
            parameters.PassengerCapacity = 4; // Default capacity
            // Boss levels have a high number of wagons to process.
            parameters.TotalWagons = (parameters.NumInitialPassengers * parameters.PassengerCapacity) + (parameters.NumUnderpasses * 16); 
        }
        else
        {
            // --- Normal Level Logic ---
            int levelWithinTier = (levelNumber - 1) % 10; // Ramps from 0 to 8 for levels like 1-9, 11-19.

            parameters.NumColors = 1 + tier;
            parameters.NumInitialPassengers = 4 + levelWithinTier; // Slowly increases number of passengers.
            parameters.NumUnderpasses = tier; // No underpasses for levels 1-10, 1 for 11-20, etc.
            parameters.PassengerCapacity = 4;
            parameters.TotalWagons = (parameters.NumInitialPassengers * parameters.PassengerCapacity) + (parameters.NumUnderpasses * 12);
        }

        // GEMINI-MODIFIED: Apply the manual override if it exists.
        if (underpassOverride.HasValue)
        {
            Debug.Log($"Manual override for underpasses: {underpassOverride.Value}");
            parameters.NumUnderpasses = underpassOverride.Value;
        }
        
        // Clamp values to be safe and within defined game limits.
        parameters.NumColors = Mathf.Clamp(parameters.NumColors, 1, 3); // Assuming 3 colors max (e.g., Red, Green, Blue)
        
        Debug.Log($"Difficulty for Level {levelNumber}: Tier={tier}, IsBoss={parameters.IsBossLevel}, Passengers={parameters.NumInitialPassengers}, Underpasses={parameters.NumUnderpasses}, Colors={parameters.NumColors}");

        return parameters;
    }

    /// <summary>
    /// Generates the sequence of wagons (the "problem") for the player to solve.
    /// </summary>
    private static void GenerateWagons(LevelDefinition levelDef, DifficultyParameters difficultyParams)
    {
        List<HyperCasualColor> availableColors = new List<HyperCasualColor> { HyperCasualColor.Blue, HyperCasualColor.Red, HyperCasualColor.Green };
        List<HyperCasualColor> colorsInLevel = availableColors.GetRange(0, difficultyParams.NumColors);

        int wagonsPerColor = difficultyParams.TotalWagons / difficultyParams.NumColors;

        for (int i = 0; i < difficultyParams.NumColors; i++)
        {
            for (int j = 0; j < wagonsPerColor; j++)
            {
                levelDef.wagons.Add(new WagonSpawnData(colorsInLevel[i], 1));
            }
        }

        // Fill any remaining wagons to match the total
        int remainingWagons = difficultyParams.TotalWagons - levelDef.wagons.Count;
        for (int i = 0; i < remainingWagons; i++)
        {
            levelDef.wagons.Add(new WagonSpawnData(colorsInLevel[i % difficultyParams.NumColors], 1));
        }

        // For boss levels, shuffle the wagon sequence to make it harder
        if (difficultyParams.IsBossLevel)
        {
            System.Random rng = new System.Random();
            int n = levelDef.wagons.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                WagonSpawnData value = levelDef.wagons[k];
                levelDef.wagons[k] = levelDef.wagons[n];
                levelDef.wagons[n] = value;
            }
        }
        
        Debug.Log($"Generated {levelDef.wagons.Count} wagons for the train.");
    }

    /// <summary>
    /// The core logic for placing passengers and underpasses onto the grid.
    /// </summary>
    private static void GenerateSolutions(LevelDefinition levelDef, DifficultyParameters difficultyParams)
    {
        List<Vector2Int> occupiedPositions = new List<Vector2Int>();
        System.Random rng = new System.Random();
        List<HyperCasualColor> colorsInLevel = levelDef.wagons.Select(w => w.color).Distinct().ToList();

        // 1. Calculate the total demand for each color in terms of passenger groups
        Dictionary<HyperCasualColor, int> passengerGroupsNeeded = new Dictionary<HyperCasualColor, int>();
        foreach (var color in colorsInLevel)
        {
            int wagonCount = levelDef.wagons.Count(w => w.color == color);
            int groups = Mathf.CeilToInt((float)wagonCount / difficultyParams.PassengerCapacity);
            passengerGroupsNeeded[color] = groups;
        }

        // 1.2. Reserve passenger groups for underpasses
        List<HyperCasualColor> colorsForUnderpasses = new List<HyperCasualColor>();
        if (difficultyParams.NumUnderpasses > 0)
        {
            Debug.Log($"Reserving passenger demand for {difficultyParams.NumUnderpasses} underpass(es).");
            for (int i = 0; i < difficultyParams.NumUnderpasses; i++)
            {
                // Find a color that is still in demand
                var availableColors = passengerGroupsNeeded.Where(kvp => kvp.Value > 0).ToList();
                if (availableColors.Count > 0)
                {
                    // Pick one and reserve it for an underpass
                    var colorToReserve = availableColors[rng.Next(availableColors.Count)].Key;
                    colorsForUnderpasses.Add(colorToReserve);
                    passengerGroupsNeeded[colorToReserve]--; // Decrement the demand for regular passengers
                }
            }
        }

        // 2. Place the remaining initial passenger groups
        int totalPassengerGroupsToPlace = passengerGroupsNeeded.Values.Sum();
        Debug.Log($"Need to place {totalPassengerGroupsToPlace} initial passenger groups.");

        for (int i = 0; i < totalPassengerGroupsToPlace; i++)
        {
            // Select a color to place
            HyperCasualColor currentColor = passengerGroupsNeeded.First(kvp => kvp.Value > 0).Key;

            int maxAttempts = 1000; // Failsafe to prevent infinite loops
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 3. Find a valid random position and direction
                Vector2Int randomPos = new Vector2Int(rng.Next(1, GridWidth - 1), rng.Next(1, GridHeight - 1));
                Vector2Int randomDir = GetRandomDirection(rng);

                var potentialPassenger = new PassengerSpawnData { position = randomPos, direction = randomDir, color = currentColor };

                // 4. Check all rules
                if (IsValidPlacement(randomPos, occupiedPositions) &&
                    !CreatesDeadlock(randomPos, randomDir, levelDef.initialPassengerGroups) &&
                    IsPathClear(randomPos, randomDir, occupiedPositions)) // Solvability Heuristic
                {
                    // 5. Add the passenger
                    levelDef.initialPassengerGroups.Add(potentialPassenger);
                    occupiedPositions.Add(randomPos);
                    passengerGroupsNeeded[currentColor]--;

                    Debug.Log($"Placed passenger group {i + 1}/{totalPassengerGroupsToPlace} ({currentColor}) at {randomPos} facing {randomDir}");
                    goto nextPassenger;
                }
            }

            Debug.LogWarning($"Could not find a valid placement for a {currentColor} passenger after {maxAttempts} attempts. The level might be unsolvable or too crowded.");
            nextPassenger:;
        }

        // 3. Place the underpasses
        Debug.Log($"Placing {colorsForUnderpasses.Count} underpass(es).");
        foreach (var underpassColor in colorsForUnderpasses)
        {
            int maxAttempts = 1000; // Failsafe
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector2Int randomPos = new Vector2Int(rng.Next(1, GridWidth - 1), rng.Next(1, GridHeight - 1));
                Vector2Int randomDir = GetRandomDirection(rng);

                if (IsValidUnderpassPlacement(randomPos, randomDir, occupiedPositions, levelDef.initialPassengerGroups))
                {
                    // Create the underpass definition
                    var underpass = new UnderpassSpawnData
                    {
                        position = randomPos,
                        direction = randomDir,
                        // For now, create a simple sequence of 4 passengers of the designated color.
                        passengerSequence = Enumerable.Repeat(underpassColor, difficultyParams.PassengerCapacity).ToList()
                    };

                    levelDef.underpasses.Add(underpass);

                    // Mark both the building and spawn point as occupied
                    occupiedPositions.Add(randomPos);
                    occupiedPositions.Add(randomPos + randomDir);

                    Debug.Log($"Placed underpass for color {underpassColor} at {randomPos}");
                    goto nextUnderpass;
                }
            }
            Debug.LogWarning($"Could not find a valid placement for an underpass after {maxAttempts} attempts.");
            nextUnderpass:;
        }
    }


    /// <summary>
    /// Checks if a grid position is valid for an underpass, considering its two-cell structure.
    /// </summary>
    private static bool IsValidUnderpassPlacement(Vector2Int position, Vector2Int direction, List<Vector2Int> occupiedPositions, List<PassengerSpawnData> existingPassengers)
    {
        Vector2Int passengerSpawnPos = position + direction;

        // 1. Check if the building position is valid.
        if (!IsValidPlacement(position, occupiedPositions))
            return false;

        // 2. Check if the passenger spawn position is valid.
        if (!IsValidPlacement(passengerSpawnPos, occupiedPositions))
            return false;

        // 3. Check if the passenger spawn position would create a deadlock.
        // Note: The color doesn't matter for the deadlock check, so we can use any.
        if (CreatesDeadlock(passengerSpawnPos, direction, existingPassengers))
            return false;

        return true;
    }

    /// <summary>
    /// Simple heuristic to check if the path directly in front of a passenger is clear.
    /// </summary>
    private static bool IsPathClear(Vector2Int position, Vector2Int direction, List<Vector2Int> occupiedPositions)
    {
        Vector2Int nextPos = position + direction;
        return !occupiedPositions.Contains(nextPos);
    }

    private static Vector2Int GetRandomDirection(System.Random rng)
    {
        int val = rng.Next(0, 4);
        if (val == 0) return Vector2Int.up;
        if (val == 1) return Vector2Int.down;
        if (val == 2) return Vector2Int.left;
        return Vector2Int.right;
    }

    /// <summary>
    /// Checks if a grid position is valid for spawning (not on an edge and not occupied).
    /// </summary>
    private static bool IsValidPlacement(Vector2Int position, List<Vector2Int> occupiedPositions)
    {
        // Rule 1: No spawning on the border.
        if (position.x <= 0 || position.x >= GridWidth - 1 || position.y <= 0 || position.y >= GridHeight - 1)
        {
            return false;
        }

        // Check if the position is already occupied.
        if (occupiedPositions.Contains(position))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if placing a passenger creates a deadlock with existing passengers.
    /// </summary>
    private static bool CreatesDeadlock(Vector2Int position, Vector2Int direction, List<PassengerSpawnData> existingPassengers)
    {
        foreach (var passenger in existingPassengers)
        {
            // Check for deadlock on the same column (x-axis)
            if (passenger.position.x == position.x)
            {
                // If the new passenger faces up (0,1) and the existing one faces down (0,-1) and is above it
                if (direction.y == 1 && passenger.direction.y == -1 && passenger.position.y > position.y)
                    return true;
                // If the new passenger faces down (0,-1) and the existing one faces up (0,1) and is below it
                if (direction.y == -1 && passenger.direction.y == 1 && passenger.position.y < position.y)
                    return true;
            }

            // Check for deadlock on the same row (y-axis)
            if (passenger.position.y == position.y)
            {
                // If the new passenger faces right (1,0) and the existing one faces left (-1,0) and is to its right
                if (direction.x == 1 && passenger.direction.x == -1 && passenger.position.x > position.x)
                    return true;
                // If the new passenger faces left (-1,0) and the existing one faces right (1,0) and is to its left
                if (direction.x == -1 && passenger.direction.x == 1 && passenger.position.x < position.x)
                    return true;
            }
        }
        return false;
    }
}