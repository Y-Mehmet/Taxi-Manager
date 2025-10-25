using UnityEngine;

// This class is responsible for generating level definitions based on a level number and a set of rules.
public static class LevelGenerator
{
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
    public static LevelDefinition GenerateLevel(int levelNumber)
    {
        Debug.Log($"--- Starting Generation for Level {levelNumber} ---");

        // Create a new definition for this level.
        LevelDefinition levelDef = new LevelDefinition(levelNumber);

        // Phase 2: Calculate difficulty parameters.
        DifficultyParameters difficultyParams = CalculateDifficultyParameters(levelNumber);

        // Phase 3: Generate the "problem" (wagons) will go here.

        // Phase 3: Generate the "solution" (passengers and underpasses) will go here.

        Debug.Log($"--- Finished Generation for Level {levelNumber} ---");

        return levelDef;
    }

    /// <summary>
    /// Calculates the parameters for a level based on the difficulty curve.
    /// </summary>
    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber)
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
        
        // Clamp values to be safe and within defined game limits.
        parameters.NumColors = Mathf.Clamp(parameters.NumColors, 1, 3); // Assuming 3 colors max (e.g., Red, Green, Blue)
        
        Debug.Log($"Difficulty for Level {levelNumber}: Tier={tier}, IsBoss={parameters.IsBossLevel}, Passengers={parameters.NumInitialPassengers}, Underpasses={parameters.NumUnderpasses}, Colors={parameters.NumColors}");

        return parameters;
    }
}