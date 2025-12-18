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
//                 Debug.LogWarning($"Level {levelNumber} failed validation. Retrying... ({retries}/{MAX_GENERATION_RETRIES})");
            }

        } while (!isSolvable && retries < MAX_GENERATION_RETRIES);

        if (!isSolvable)
        {
            Debug.LogError($"FAILED to generate a solvable layout for Level {levelNumber} after {MAX_GENERATION_RETRIES} attempts.");
            return null; // Signal failure
        }

        /* Debug.Log($"--- Level {levelNumber} validation PASSED. Finalizing... ---"); */
        
        // Detect rare color (least used color in initial passengers)
        HyperCasualColor? rareColor = DetectRareColor(levelDef, difficultyParams);
        
        // If rare color exists, place it strategically in underpass
        if (rareColor.HasValue && difficultyParams.NumUnderpasses > 0)
        {
            PlaceRareColorInUnderpass(levelDef, rareColor.Value, rng, difficultyParams);
        }
        
        // Generate conveyor WITHOUT rare color (rare color is in underpass, hard to reach)
        GenerateConveyorPassengers(levelDef, difficultyParams, rng, rareColor);
        
        // Use the solutionOrder that was validated with the successful layout
        GenerateWagonTrainFromLayout(levelDef, solutionOrder, difficultyParams, rng, rareColor);
        return levelDef;
    }

    // --- LAYOUT VALIDATION (THE "SUPER-CHECK") ---
    private static bool IsLayoutSolvable(LevelDefinition levelDef)
    {
        var allNodes = new List<PassengerNode>();
        var nodeMap = new Dictionary<Vector2Int, PassengerNode>();
        
        // Add all regular passengers
        foreach (var p in levelDef.initialPassengerGroups) 
        { 
            var node = new PassengerNode(p.position, p.direction); 
            allNodes.Add(node); 
            if (!nodeMap.ContainsKey(p.position)) 
                nodeMap.Add(p.position, node); 
        }
        
        // Add underpass positions - BOTH the underpass itself AND the spawned passenger
        foreach (var u in levelDef.underpasses) 
        { 
            // The underpass structure itself blocks movement
            var underpassNode = new PassengerNode(u.position, Vector2Int.zero); // No direction, it's static
            allNodes.Add(underpassNode); 
            if (!nodeMap.ContainsKey(u.position)) 
                nodeMap.Add(u.position, underpassNode);
            
            // The passenger spawned by the underpass
            var passengerSpawnPos = u.position + u.direction;
            var spawnedPassengerNode = new PassengerNode(passengerSpawnPos, u.direction); 
            allNodes.Add(spawnedPassengerNode); 
            if (!nodeMap.ContainsKey(passengerSpawnPos)) 
                nodeMap.Add(passengerSpawnPos, spawnedPassengerNode); 
        }
        
        // Build dependency graph
        foreach (var node in allNodes) 
        { 
            // Skip nodes with no direction (static blockers like underpass structures)
            if (node.Direction == Vector2Int.zero) continue;
            
            var targetPos = node.Position + node.Direction; 
            if (nodeMap.ContainsKey(targetPos)) 
            { 
                node.BlockedBy = nodeMap[targetPos]; 
            } 
        }
        
        // Check for cycles
        var visiting = new HashSet<PassengerNode>();
        var visited = new HashSet<PassengerNode>();
        foreach (var node in allNodes) 
        { 
            if (!visited.Contains(node)) 
            { 
                if (HasCycleDFS(node, visiting, visited)) 
                    return false; 
            } 
        }

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
//                         Debug.LogWarning($"Level validation failed: Head-on collision detected in column {nodeA.Position.x} between node at {nodeA.Position} and node at {nodeB.Position}.");
                        return false;
                    }
                }

                // Check for head-on collision in the same row
                if (nodeA.Position.y == nodeB.Position.y && nodeA.Direction.x != 0 && nodeA.Direction.x == -nodeB.Direction.x)
                {
                    if ((nodeA.Position.x > nodeB.Position.x && nodeA.Direction.x < 0) || (nodeB.Position.x > nodeA.Position.x && nodeB.Direction.x < 0))
                    {
//                         Debug.LogWarning($"Level validation failed: Head-on collision detected in row {nodeA.Position.y} between node at {nodeA.Position} and node at {nodeB.Position}.");
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

    // --- ATTEMPT TO GENERATE A LAYOUT (COMPACT CLUSTER-BASED) ---
    private static List<PlacedObject> AttemptToGenerateLayout(LevelDefinition levelDef, DifficultyParameters p, System.Random rng)
    {
        var occupied = new List<Vector2Int>();
        var solutionOrder = new List<PlacedObject>();
        var colors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors);
        int totalObjects = p.NumInitialPassengers + p.NumUnderpasses;
        int underpassesLeft = p.NumUnderpasses;
        
        // Start from center and build outward in a compact manner
        Vector2Int centerPos = new Vector2Int(3, 5);
        
        for(int i = 0; i < totalObjects; i++)
        {
            bool placed = false;
            
            // Get candidate positions prioritizing proximity to existing placements
            var candidates = GetProximitySortedCandidates(occupied, centerPos, rng);
            
            for (int j = 0; j < candidates.Count && !placed; j++)
            {
                var pos = candidates[j];
                var dir = GetRandomDirection(rng);
                
                if (underpassesLeft > 0 && rng.Next(0, 3) == 0)
                {
                    if (TryPlaceUnderpass(levelDef, p, pos, dir, occupied, colors, rng, out var underpass)) 
                    { 
                        solutionOrder.Add(PlacedObject.CreateUnderpass(underpass));
                        underpassesLeft--; 
                        placed = true; 
                    }
                }
                else
                {
                    if (TryPlacePassenger(levelDef, colors[rng.Next(colors.Count)], pos, dir, occupied, out var passenger)) 
                    { 
                        solutionOrder.Add(PlacedObject.CreatePassenger(passenger));
                        placed = true; 
                    }
                }
            }
        }
        return solutionOrder;
    }
    
    // Get positions sorted by proximity to already-placed objects (creates compact clusters)
    private static List<Vector2Int> GetProximitySortedCandidates(List<Vector2Int> occupied, Vector2Int centerPos, System.Random rng)
    {
        var allPositions = new List<Vector2Int>();
        
        // Generate all valid positions
        for (int x = 1; x < GRID_WIDTH - 1; x++)
        {
            for (int y = 1; y < GRID_HEIGHT - 1; y++)
            {
                var pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos))
                {
                    allPositions.Add(pos);
                }
            }
        }
        
        // If nothing placed yet, start from center area
        if (occupied.Count == 0)
        {
            return allPositions.OrderBy(pos => Vector2.Distance(pos, centerPos)).ToList();
        }
        
        // Sort by proximity to ANY occupied cell (creates tight clusters)
        var sorted = allPositions.OrderBy(pos => 
        {
            float minDist = float.MaxValue;
            foreach (var occ in occupied)
            {
                float dist = Vector2.Distance(pos, occ);
                if (dist < minDist) minDist = dist;
            }
            
            // Prioritize adjacent cells (distance ~1), then nearby cells
            // Add small random factor to avoid predictable patterns
            return minDist + (float)rng.NextDouble() * 0.3f;
        }).ToList();
        
        return sorted;
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

    // Detect the rarest color (least used) for strategic placement
    private static HyperCasualColor? DetectRareColor(LevelDefinition levelDef, DifficultyParameters p)
    {
        // Only use rare color strategy for boss levels or hard levels
        if (!p.IsBossLevel && levelDef.initialPassengerGroups.Count < 10) return null;
        
        var colorCounts = new Dictionary<HyperCasualColor, int>();
        var allColors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().Take(p.NumColors).ToList();
        
        // Initialize counts
        foreach (var color in allColors)
        {
            colorCounts[color] = 0;
        }
        
        // Count colors in initial passengers
        foreach (var passenger in levelDef.initialPassengerGroups)
        {
            if (colorCounts.ContainsKey(passenger.color))
                colorCounts[passenger.color]++;
        }
        
        // Find the least used color (rare color)
        var rareColor = colorCounts.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
        
        // Only consider it rare if it's used less than average
        int totalPassengers = levelDef.initialPassengerGroups.Count;
        int avgPerColor = totalPassengers / p.NumColors;
        
        if (colorCounts[rareColor] < avgPerColor * 0.7f) // Less than 70% of average
        {
            return rareColor;
        }
        
        return null;
    }
    
    // Place rare color in the LAST underpass (hardest to reach)
    private static void PlaceRareColorInUnderpass(LevelDefinition levelDef, HyperCasualColor rareColor, System.Random rng, DifficultyParameters p)
    {
        if (levelDef.underpasses.Count == 0) return;
        
        // Get the last underpass (usually hardest to reach)
        var lastUnderpass = levelDef.underpasses[levelDef.underpasses.Count - 1];
        
        // Replace 3-4 colors in the sequence with rare color
        int rareCount = rng.Next(3, 5); // 3 or 4 rare colors
        int sequenceLength = lastUnderpass.passengerSequence.Count;
        
        // Place rare colors at the END of the sequence (hardest to complete)
        for (int i = 0; i < rareCount && i < sequenceLength; i++)
        {
            int index = sequenceLength - 1 - i; // Start from end
            lastUnderpass.passengerSequence[index] = rareColor;
        }
        
        /* Debug.Log($"Placed {rareCount} {rareColor} passengers in last underpass sequence"); */
    }
    
    // Calculate accessibility score for each underpass (lower = harder to reach)
    private static Dictionary<int, int> CalculateUnderpassAccessibility(LevelDefinition levelDef)
    {
        var accessibility = new Dictionary<int, int>();
        
        if (levelDef.underpasses.Count == 0) return accessibility;
        
        for (int i = 0; i < levelDef.underpasses.Count; i++)
        {
            var underpass = levelDef.underpasses[i];
            int score = 0; // Lower = harder
            
            // Check how many other underpasses block this one
            for (int j = 0; j < levelDef.underpasses.Count; j++)
            {
                if (i == j) continue;
                
                var blocker = levelDef.underpasses[j];
                
                // Same direction blocking check
                if (underpass.direction == blocker.direction)
                {
                    // Check if blocker is "in front" of this underpass
                    bool isBlocking = false;
                    
                    if (underpass.direction == Vector2Int.right)
                    {
                        // Blocker is to the left (must complete blocker first)
                        if (blocker.position.x < underpass.position.x && blocker.position.y == underpass.position.y)
                            isBlocking = true;
                    }
                    else if (underpass.direction == Vector2Int.left)
                    {
                        // Blocker is to the right
                        if (blocker.position.x > underpass.position.x && blocker.position.y == underpass.position.y)
                            isBlocking = true;
                    }
                    else if (underpass.direction == Vector2Int.up)
                    {
                        // Blocker is below
                        if (blocker.position.y < underpass.position.y && blocker.position.x == underpass.position.x)
                            isBlocking = true;
                    }
                    else if (underpass.direction == Vector2Int.down)
                    {
                        // Blocker is above
                        if (blocker.position.y > underpass.position.y && blocker.position.x == underpass.position.x)
                            isBlocking = true;
                    }
                    
                    if (isBlocking)
                    {
                        score -= 10; // MUCH harder to reach (must complete blocker first)
                    }
                }
            }
            
            // Check how many initial passengers block this underpass
            int blockingPassengers = 0;
            foreach (var passenger in levelDef.initialPassengerGroups)
            {
                // Simple distance check (closer passengers = more blocking)
                float distance = Vector2.Distance(passenger.position, underpass.position);
                if (distance < 3f) // Within 3 cells
                {
                    blockingPassengers++;
                }
            }
            
            score -= blockingPassengers; // More blocking passengers = harder to reach
            
            accessibility[i] = score;
            /* Debug.Log($"Underpass {i} at {underpass.position} dir={underpass.direction}: accessibility score = {score}"); */
        }
        
        return accessibility;
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
    private static void GenerateConveyorPassengers(LevelDefinition levelDef, DifficultyParameters p, System.Random rng, HyperCasualColor? rareColor = null) 
    { 
        if (p.numConveyorPassengers <= 0) return; 
        
        // MAX 20 CONVEYOR PASSENGERS (hard limit)
        int actualCount = Mathf.Min(p.numConveyorPassengers, 20);
        
        var availableColors = System.Enum.GetValues(typeof(HyperCasualColor)).Cast<HyperCasualColor>().ToList().GetRange(0, p.NumColors);
        
        // If rare color exists and conveyor is hard to reach, use it here
        // Otherwise, exclude rare color from conveyor
        if (rareColor.HasValue)
        {
            availableColors.Remove(rareColor.Value); // Don't use rare color in conveyor (it's in underpass)
        }
        
        for (int i = 0; i < actualCount; i++) 
        { 
            levelDef.conveyorPassengers.Add(new PassengerSpawnData { color = availableColors[rng.Next(availableColors.Count)] }); 
        } 
    }
    
    private static DifficultyParameters CalculateDifficultyParameters(int levelNumber, System.Random rng, int? u, int? c, int? ps, int? clr) 
    { 
        var p = new DifficultyParameters { LevelNumber = levelNumber, PassengerCapacity = 4, UnderpassSequenceLength = 6 }; 
        int tier = (levelNumber - 1) / 10; 
        p.IsBossLevel = (levelNumber > 0 && levelNumber % 10 == 0); 
        
        // Calculate difficulty progression
        // Boss level (10, 20, 30) = HARDEST in tier (same as level 19, 29, 39...)
        // Next tier starts easier: Level 11 < Level 10
        // Progression: 1 < 2 < 3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 (BOSS)
        //              11 < 12 < 13 < 14 < 15 < 16 < 17 < 18 < 19 < 20 (BOSS)
        
        int levelInTier = (levelNumber - 1) % 10; // 0-9
        
        // Base difficulty increases with tier
        int baseDifficulty = tier * 4;
        
        // Progressive difficulty within tier
        int progressiveDifficulty;
        if (p.IsBossLevel)
        {
            // Boss level = Hardest in tier (level 9 equivalent + boss bonus)
            progressiveDifficulty = 9 + 3; // +3 boss bonus
        }
        else
        {
            progressiveDifficulty = levelInTier; // 0-8 for levels 1-9
        }
        
        // Total passengers: base + progressive
        p.NumInitialPassengers = 4 + baseDifficulty + progressiveDifficulty;
        
        // Underpasses: gradually increase, boss gets most
        if (p.IsBossLevel)
        {
            p.NumUnderpasses = 3 + tier; // Boss: 3, 4, 5, 6...
        }
        else if (levelInTier >= 7)
        {
            p.NumUnderpasses = 2 + tier; // Late in tier: many underpasses
        }
        else if (levelInTier >= 4)
        {
            p.NumUnderpasses = 1 + tier; // Mid tier: some underpasses
        }
        else if (levelInTier >= 1)
        {
            p.NumUnderpasses = tier; // Early tier: fewer underpasses
        }
        else
        {
            p.NumUnderpasses = Mathf.Max(0, tier - 1); // Very early: minimal underpasses
        }
        
        // Colors: gradually increase
        p.NumColors = Mathf.Clamp(3 + tier, 3, 11);
        
        // Conveyor passengers: only after level 10, boss gets most
        if (levelNumber > 10)
        {
            if (p.IsBossLevel)
            {
                p.numConveyorPassengers = rng.Next(15, 21); // Boss: 15-20 (LOTS!)
            }
            else if (levelInTier >= 7 && rng.NextDouble() < 0.6)
            {
                p.numConveyorPassengers = rng.Next(10, 16); // Late in tier: 10-15
            }
            else if (levelInTier >= 4 && rng.NextDouble() < 0.4)
            {
                p.numConveyorPassengers = rng.Next(5, 11); // Mid tier: 5-10
            }
            else
            {
                p.numConveyorPassengers = 0; // Early tier: no conveyor
            }
        }
        else
        {
            p.numConveyorPassengers = 0; // No conveyor before level 11
        }
        
        // Apply overrides
        if (u.HasValue) p.NumUnderpasses = u.Value; 
        if (ps.HasValue) p.NumInitialPassengers = ps.Value; 
        if (clr.HasValue) p.NumColors = Mathf.Clamp(clr.Value, 2, 11); 
        if (c.HasValue) p.numConveyorPassengers = Mathf.Min(c.Value, 20); // Enforce max 20
        
        p.NumColors = Mathf.Min(p.NumColors, System.Enum.GetValues(typeof(HyperCasualColor)).Length); 
        
        /* Debug.Log($"Level {levelNumber} (Tier {tier}, InTier {levelInTier}): Passengers={p.NumInitialPassengers}, Underpasses={p.NumUnderpasses}, Conveyor={p.numConveyorPassengers}, Colors={p.NumColors}, IsBoss={p.IsBossLevel}"); */
        
        return p; 
    }
    private static void GenerateWagonTrainFromLayout(LevelDefinition levelDef, List<PlacedObject> solutionOrder, DifficultyParameters p, System.Random rng, HyperCasualColor? rareColor = null) 
    { 
        var allWagons = new List<WagonSpawnData>();
        var colorAccessibility = new Dictionary<HyperCasualColor, int>(); // Lower = harder to access
        
        // Step 1: Calculate underpass accessibility (blocking analysis)
        var underpassAccessibility = CalculateUnderpassAccessibility(levelDef);
        
        // Step 2: Assign accessibility scores to each passenger/underpass
        int accessibilityScore = 0;
        int underpassIndex = 0;
        
        foreach (var obj in solutionOrder) 
        { 
            if (obj.IsUnderpass) 
            { 
                // Get this underpass's accessibility score (how hard to reach)
                int underpassScore = underpassAccessibility.ContainsKey(underpassIndex) 
                    ? underpassAccessibility[underpassIndex] 
                    : accessibilityScore;
                
                // Underpass passengers are harder to access (need to complete sequence)
                foreach (var color in obj.UnderpassData.passengerSequence) 
                { 
                    for (int i = 0; i < p.PassengerCapacity; i++) 
                    {
                        allWagons.Add(new WagonSpawnData(color, 1));
                    }
                    
                    // Track accessibility (LOWER score = placed earlier = harder to reach)
                    // Use underpass accessibility score
                    if (!colorAccessibility.ContainsKey(color))
                        colorAccessibility[color] = underpassScore;
                    else
                        colorAccessibility[color] = Mathf.Min(colorAccessibility[color], underpassScore);
                }
                
                underpassIndex++;
                accessibilityScore += 3; // Underpasses add significant difficulty
            } 
            else 
            { 
                var color = obj.PassengerData.color;
                for (int i = 0; i < p.PassengerCapacity; i++) 
                {
                    allWagons.Add(new WagonSpawnData(color, 1));
                }
                
                if (!colorAccessibility.ContainsKey(color))
                    colorAccessibility[color] = accessibilityScore;
                else
                    colorAccessibility[color] = Mathf.Min(colorAccessibility[color], accessibilityScore);
                    
                accessibilityScore += 1;
            } 
        }
        
        // Step 2: Add conveyor passengers (always at the end, hardest to access)
        var conveyorWagons = new List<WagonSpawnData>();
        foreach (var conveyorPassenger in levelDef.conveyorPassengers) 
        { 
            var color = conveyorPassenger.color;
            for (int i = 0; i < p.PassengerCapacity; i++) 
            { 
                conveyorWagons.Add(new WagonSpawnData(color, 1)); 
            }
            
            // Conveyor colors are VERY hard to access
            if (!colorAccessibility.ContainsKey(color))
                colorAccessibility[color] = 1000; // Very high score = very hard
        }
        
        // Step 3: Apply difficulty-based ordering strategy
        if (p.IsBossLevel)
        {
            // BOSS LEVEL: Maximum chaos with color clustering and rare color waves
            allWagons = ApplyBossLevelOrdering(allWagons, colorAccessibility, rng, rareColor);
        }
        else if (levelDef.wagons.Count > 15 || p.NumUnderpasses > 0)
        {
            // HARD LEVEL: Strategic ordering (hard-to-reach colors first) with rare color waves
            allWagons = ApplyStrategicOrdering(allWagons, colorAccessibility, rng, rareColor);
        }
        else
        {
            // EASY LEVEL: Mostly fair ordering with slight shuffle
            allWagons = allWagons.OrderBy(_ => rng.Next()).ToList();
        }
        
        // Add conveyor wagons at the end (or strategically mixed for boss)
        if (p.IsBossLevel && conveyorWagons.Count > 0)
        {
            // Boss: Mix some conveyor wagons in the middle for extra chaos
            int insertPoint = allWagons.Count / 2;
            allWagons.InsertRange(insertPoint, conveyorWagons);
        }
        else
        {
            allWagons.AddRange(conveyorWagons);
        }
        
        // IMPORTANT: Break long sequences (max 8 consecutive same color)
        allWagons = BreakLongSequences(allWagons, 8, rng);
        
        levelDef.wagons = allWagons;
    }
    
    // Break long sequences of same color wagons (max consecutive limit)
    private static List<WagonSpawnData> BreakLongSequences(List<WagonSpawnData> wagons, int maxConsecutive, System.Random rng)
    {
        if (wagons.Count == 0) return wagons;
        
        var result = new List<WagonSpawnData>();
        var currentColor = wagons[0].color;
        int consecutiveCount = 0;
        
        // Collect all available colors for breaking sequences
        var availableColors = wagons.GroupBy(w => w.color)
                                   .Where(g => g.Count() > 0)
                                   .Select(g => g.Key)
                                   .ToList();
        
        for (int i = 0; i < wagons.Count; i++)
        {
            var wagon = wagons[i];
            
            if (wagon.color == currentColor)
            {
                consecutiveCount++;
                
                // If we hit the limit, insert a different color wagon
                if (consecutiveCount > maxConsecutive)
                {
                    // Find a different color to break the sequence
                    var breakColor = availableColors.Where(c => c != currentColor).OrderBy(_ => rng.Next()).FirstOrDefault();
                    
                    if (breakColor != default(HyperCasualColor))
                    {
                        // Find a wagon of different color from remaining wagons
                        int breakIndex = -1;
                        for (int j = i + 1; j < wagons.Count; j++)
                        {
                            if (wagons[j].color != currentColor)
                            {
                                breakIndex = j;
                                break;
                            }
                        }
                        
                        if (breakIndex != -1)
                        {
                            // Swap current wagon with the different colored one
                            var temp = wagons[i];
                            wagons[i] = wagons[breakIndex];
                            wagons[breakIndex] = temp;
                            
                            wagon = wagons[i]; // Update wagon reference
                            currentColor = wagon.color;
                            consecutiveCount = 1;
                        }
                    }
                }
            }
            else
            {
                currentColor = wagon.color;
                consecutiveCount = 1;
            }
            
            result.Add(wagon);
        }
        
        return result;
    }
    
    // Strategic ordering: Hard-to-reach passengers' wagons come FIRST in waves
    private static List<WagonSpawnData> ApplyStrategicOrdering(List<WagonSpawnData> wagons, Dictionary<HyperCasualColor, int> accessibility, System.Random rng, HyperCasualColor? rareColor = null)
    {
        // Prioritize rare color if it exists, otherwise use hardest color
        HyperCasualColor targetColor;
        if (rareColor.HasValue)
        {
            targetColor = rareColor.Value;
        }
        else
        {
            targetColor = accessibility.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
        }
        
        // Separate target color from others
        var targetWagons = wagons.Where(w => w.color == targetColor).ToList();
        var otherWagons = wagons.Where(w => w.color != targetColor).ToList();
        
        // Shuffle others for variety
        otherWagons = otherWagons.OrderBy(_ => rng.Next()).ToList();
        
        // Create wave: Insert target wagons in concentrated bursts at the beginning
        var result = new List<WagonSpawnData>();
        int targetIndex = 0;
        int waveSize = Mathf.Min(targetWagons.Count / 2, 6); // Wave size: up to 6 wagons
        
        // First wave of target color (concentrated at start)
        for (int i = 0; i < waveSize && targetIndex < targetWagons.Count; i++)
        {
            result.Add(targetWagons[targetIndex++]);
        }
        
        // Mix in some other colors
        int mixCount = rng.Next(2, 5);
        for (int i = 0; i < mixCount && otherWagons.Count > 0; i++)
        {
            result.Add(otherWagons[0]);
            otherWagons.RemoveAt(0);
        }
        
        // Second wave of target color (smaller)
        int secondWaveSize = Mathf.Min(targetWagons.Count - targetIndex, 4);
        for (int i = 0; i < secondWaveSize && targetIndex < targetWagons.Count; i++)
        {
            result.Add(targetWagons[targetIndex++]);
        }
        
        // Add remaining wagons mixed
        while (targetIndex < targetWagons.Count || otherWagons.Count > 0)
        {
            // Randomly pick from target or other
            if (targetIndex < targetWagons.Count && (otherWagons.Count == 0 || rng.NextDouble() < 0.4))
            {
                result.Add(targetWagons[targetIndex++]);
            }
            else if (otherWagons.Count > 0)
            {
                result.Add(otherWagons[0]);
                otherWagons.RemoveAt(0);
            }
        }
        
        return result;
    }
    
    // Boss level ordering: Create multiple color waves (Tower Defense style)
    private static List<WagonSpawnData> ApplyBossLevelOrdering(List<WagonSpawnData> wagons, Dictionary<HyperCasualColor, int> accessibility, System.Random rng, HyperCasualColor? rareColor = null)
    {
        // Group wagons by color and sort by accessibility (hardest first)
        var colorGroups = wagons.GroupBy(w => w.color)
                                .OrderBy(g => {
                                    // Prioritize rare color if it exists
                                    if (rareColor.HasValue && g.Key == rareColor.Value)
                                        return -1; // Rare color first
                                    return accessibility.ContainsKey(g.Key) ? accessibility[g.Key] : 999;
                                })
                                .Select(g => g.ToList())
                                .ToList();
        
        if (colorGroups.Count == 0) return wagons;
        
        var result = new List<WagonSpawnData>();
        var colorIndices = new int[colorGroups.Count]; // Track how many wagons used from each color
        
        // Create waves: Each wave focuses on 1-2 colors but mixes in others
        int totalWagons = wagons.Count;
        int wavesCreated = 0;
        int maxWaves = Mathf.Min(colorGroups.Count * 2, 6); // Max 6 waves
        
        while (result.Count < totalWagons && wavesCreated < maxWaves)
        {
            // Pick 1-2 dominant colors for this wave (prioritize hardest colors first)
            int dominantColorIndex = wavesCreated % colorGroups.Count;
            
            // Wave size: 4-8 wagons
            int waveSize = rng.Next(4, 9);
            int waveDominantCount = (int)(waveSize * 0.6f); // 60% dominant color
            
            // Add dominant color wagons
            for (int i = 0; i < waveDominantCount && colorIndices[dominantColorIndex] < colorGroups[dominantColorIndex].Count; i++)
            {
                result.Add(colorGroups[dominantColorIndex][colorIndices[dominantColorIndex]++]);
            }
            
            // Mix in other colors (40%)
            int mixCount = waveSize - waveDominantCount;
            for (int i = 0; i < mixCount; i++)
            {
                // Pick random color that still has wagons
                var availableColors = colorGroups.Select((group, idx) => new { group, idx })
                                                 .Where(x => colorIndices[x.idx] < x.group.Count)
                                                 .ToList();
                
                if (availableColors.Count == 0) break;
                
                var chosen = availableColors[rng.Next(availableColors.Count)];
                result.Add(chosen.group[colorIndices[chosen.idx]++]);
            }
            
            wavesCreated++;
        }
        
        // Add any remaining wagons randomly
        for (int i = 0; i < colorGroups.Count; i++)
        {
            while (colorIndices[i] < colorGroups[i].Count)
            {
                result.Add(colorGroups[i][colorIndices[i]++]);
            }
        }
        
        return result;
    }
}
