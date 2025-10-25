
using System.Collections.Generic;
using UnityEngine;

public enum PassengerColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Purple,
    Orange
}

public class LevelDifficultyParameters
{
    public int levelNumber;
    public int passengerCount;
    public int colorCount;
    public int wagonCount;
    public bool isBossLevel;
}

public static class LevelGenerator
{
    public static LevelDefinition GenerateLevel(int levelNumber)
    {
        // Step 1: Calculate Difficulty
        LevelDifficultyParameters difficulty = CalculateDifficulty(levelNumber);
        Debug.Log($"Level {levelNumber}: PassengerCount={difficulty.passengerCount}, ColorCount={difficulty.colorCount}, WagonCount={difficulty.wagonCount}, IsBoss={difficulty.isBossLevel}");

        // Step 2: Generate the "Problem" (Wagons)
        var wagonGenerationResult = GenerateWagons(difficulty);
        List<PassengerColor> wagonTrain = wagonGenerationResult.Item1;
        Dictionary<PassengerColor, int> colorDemand = wagonGenerationResult.Item2;

        // Step 3: Generate the "Solution" (Passengers & Underpasses)
        var solution = GenerateSolutions(difficulty, colorDemand);

        return new LevelDefinition
        {
            levelNumber = levelNumber,
            gridDataPath = "Assets/Resources/GridData.asset",
            wagonTrain = wagonTrain,
            initialPassengerGroups = solution.Item1,
            underpasses = solution.Item2
        };
    }

    private static (List<PassengerGroupDefinition>, List<UnderpassDefinition>) GenerateSolutions(LevelDifficultyParameters difficulty, Dictionary<PassengerColor, int> colorDemand, List<PassengerColor> wagonTrain)
    {
        var passengerGroups = new List<PassengerGroupDefinition>();
        var underpasses = new List<UnderpassDefinition>();
        var occupiedPositions = new List<Vector2Int>();

        var colorPriority = wagonTrain.Distinct().ToList();

        foreach (var color in colorPriority)
        {
            if (!colorDemand.ContainsKey(color)) continue;

            int demand = colorDemand[color];
            int groupsToPlace = Mathf.CeilToInt((float)demand / PassengerCapacity);

            for (int i = 0; i < groupsToPlace; i++)
            {
                bool placementFound = false;
                int placementAttempts = 0;

                while (!placementFound && placementAttempts < 200) // Increased attempts
                {
                    placementAttempts++;

                    int x = Random.Range(1, GridWidth - 1);
                    int y = Random.Range(1, GridHeight - 1);
                    var potentialPos = new Vector2Int(x, y);

                    var directions = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    var potentialDir = directions[Random.Range(0, directions.Count)];

                    var newPassengerGroup = new PassengerGroupDefinition
                    {
                        position = potentialPos,
                        direction = potentialDir,
                        color = color,
                        capacity = PassengerCapacity
                    };

                    if (IsValidPlacement(potentialPos, occupiedPositions) &&
                        !CreatesDeadlock(newPassengerGroup, passengerGroups) &&
                        !IsPathBlockedByLowerPriority(newPassengerGroup, passengerGroups, colorPriority))
                    {
                        passengerGroups.Add(newPassengerGroup);
                        occupiedPositions.Add(potentialPos);
                        placementFound = true;
                    }
                }

                if (!placementFound)
                {
                    Debug.LogWarning($"Level {difficulty.levelNumber}: Could not find a valid placement for a {color} passenger group after 200 attempts.");
                }
            }
        }

        return (passengerGroups, underpasses);
    }

    private static bool IsPathBlockedByLowerPriority(PassengerGroupDefinition newPassenger, List<PassengerGroupDefinition> existingPassengers, List<PassengerColor> colorPriority)
    {
        int newPassengerPriority = colorPriority.IndexOf(newPassenger.color);

        foreach (var existing in existingPassengers)
        {
            int existingPassengerPriority = colorPriority.IndexOf(existing.color);

            // A lower priority passenger (higher index) should not block a higher priority one.
            if (newPassengerPriority > existingPassengerPriority)
            {
                if (IsPassengerInPath(existing, newPassenger))
                    return true; // The new (lower priority) passenger is blocking an existing (higher priority) one.
            }
        }
        return false;
    }

    private static bool IsPassengerInPath(PassengerGroupDefinition pathOwner, PassengerGroupDefinition potentialBlocker)
    {
        Vector2Int pathDirection = pathOwner.direction;
        Vector2Int startPos = pathOwner.position;
        Vector2Int blockerPos = potentialBlocker.position;

        if (pathDirection.x == 1) // Moving Right
        {
            return blockerPos.y == startPos.y && blockerPos.x > startPos.x;
        }
        if (pathDirection.x == -1) // Moving Left
        {
            return blockerPos.y == startPos.y && blockerPos.x < startPos.x;
        }
        if (pathDirection.y == 1) // Moving Up
        {
            return blockerPos.x == startPos.x && blockerPos.y > startPos.y;
        }
        if (pathDirection.y == -1) // Moving Down
        {
            return blockerPos.x == startPos.x && blockerPos.y < startPos.y;
        }

        return false;
    }

    private static bool IsValidPlacement(Vector2Int position, List<Vector2Int> occupiedPositions)
    {
        // Check grid boundaries (must not be on the edge)
        if (position.x <= 0 || position.x >= GridWidth - 1 || position.y <= 0 || position.y >= GridHeight - 1)
        {
            return false;
        }

        // Check if the position is already occupied
        if (occupiedPositions.Contains(position))
        {
            return false;
        }

        return true;
    }

    private static bool CreatesDeadlock(PassengerGroupDefinition newPassenger, List<PassengerGroupDefinition> existingPassengers)
    {
        foreach (var existing in existingPassengers)
        {
            // Check for deadlock on the same row (X-axis)
            if (existing.position.y == newPassenger.position.y)
            {
                // If they are facing each other horizontally
                if ((existing.direction.x == 1 && newPassenger.direction.x == -1) || (existing.direction.x == -1 && newPassenger.direction.x == 1))
                {
                    return true;
                }
            }

            // Check for deadlock on the same column (Y-axis)
            if (existing.position.x == newPassenger.position.x)
            {
                // If they are facing each other vertically
                if ((existing.direction.y == 1 && newPassenger.direction.y == -1) || (existing.direction.y == -1 && newPassenger.direction.y == 1))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static (List<PassengerColor>, Dictionary<PassengerColor, int>) GenerateWagons(LevelDifficultyParameters difficulty)
    {
        var wagonTrain = new List<PassengerColor>();
        var colorDemand = new Dictionary<PassengerColor, int>();

        // 1. Select the colors to be used in this level
        var availableColors = new List<PassengerColor>((PassengerColor[])System.Enum.GetValues(typeof(PassengerColor)));
        var usedColors = new List<PassengerColor>();
        for (int i = 0; i < difficulty.colorCount; i++)
        {
            int randIndex = Random.Range(0, availableColors.Count);
            usedColors.Add(availableColors[randIndex]);
            availableColors.RemoveAt(randIndex);
        }

        // 2. Generate the wagon train sequence
        int wagonsPerColor = difficulty.wagonCount / difficulty.colorCount;
        int remainderWagons = difficulty.wagonCount % difficulty.colorCount;

        if (!difficulty.isBossLevel)
        {
            // For normal levels, create simple chunks of colors
            foreach (var color in usedColors)
            {
                for (int i = 0; i < wagonsPerColor; i++) wagonTrain.Add(color);
            }
        }
        else
        {
            // For boss levels, create a more interleaved/random sequence
            for (int i = 0; i < difficulty.colorCount; i++)
            {
                for (int j = 0; j < wagonsPerColor; j++) wagonTrain.Add(usedColors[i]);
            }
        }

        // Add remainder wagons to random colors
        for (int i = 0; i < remainderWagons; i++)
        {
            wagonTrain.Add(usedColors[Random.Range(0, usedColors.Count)]);
        }

        // Shuffle for more randomness on harder levels
        if (difficulty.levelNumber > 5) // Simple shuffle for some variety
        {
            for (int i = 0; i < wagonTrain.Count; i++)
            {
                int randIndex = Random.Range(i, wagonTrain.Count);
                var temp = wagonTrain[i];
                wagonTrain[i] = wagonTrain[randIndex];
                wagonTrain[randIndex] = temp;
            }
        }

        // 3. Calculate the final color demand
        foreach (var color in wagonTrain)
        {
            if (colorDemand.ContainsKey(color))
            {
                colorDemand[color]++;
            }
            else
            {
                colorDemand[color] = 1;
            }
        }

        return (wagonTrain, colorDemand);
    }

    private static LevelDifficultyParameters CalculateDifficulty(int levelNumber)
    {
        var parameters = new LevelDifficultyParameters { levelNumber = levelNumber };

        int difficultyCycle = (levelNumber - 1) / 10; // 0 for 1-10, 1 for 11-20, etc.
        int levelInCycle = (levelNumber - 1) % 10; // 0 for 1, 10, 20... 9 for 9, 19, 29...

        if (levelInCycle == 9) // Boss levels (10, 20, 30...)
        {
            parameters.isBossLevel = true;
            parameters.passengerCount = 6 + difficultyCycle * 2;
            parameters.colorCount = 3 + difficultyCycle;
            parameters.wagonCount = 20 + difficultyCycle * 10;
        }
        else // Normal levels
        {
            parameters.isBossLevel = false;
            // Start with a base and increase slightly with each level in the cycle
            parameters.passengerCount = 3 + levelInCycle / 2 + difficultyCycle;
            parameters.colorCount = 2 + levelInCycle / 3 + difficultyCycle;
            parameters.wagonCount = 8 + levelInCycle * 2 + difficultyCycle * 5;
        }

        // Clamp values to be within reasonable limits
        parameters.passengerCount = Mathf.Clamp(parameters.passengerCount, 3, 12);
        parameters.colorCount = Mathf.Clamp(parameters.colorCount, 2, 6);
        parameters.wagonCount = Mathf.Clamp(parameters.wagonCount, 8, 60);


        return parameters;
    }
}
