# Procedural Generation Scaffolds

Detailed implementations for procedural generation systems. Supplements the PATTERN blocks in the parent SKILL.md.

---

## 1. Complete NoiseGenerator

Full noise system with fractal Brownian motion, domain warping, and 2D/3D sampling.

```csharp
using UnityEngine;

/// <summary>
/// Configuration for noise-based generation. Each biome or terrain layer gets its own asset.
/// Exposes all fractal noise parameters for designer tuning.
/// </summary>
[CreateAssetMenu(fileName = "New Noise Config", menuName = "ProcGen/Noise Config")]
public class NoiseConfig : ScriptableObject
{
    [Header("Base Noise")]
    [Tooltip("Base frequency of the noise. Lower = broader features.")]
    [SerializeField, Range(0.001f, 1f)] private float frequency = 0.05f;

    [Tooltip("Output amplitude multiplier.")]
    [SerializeField, Range(0f, 200f)] private float amplitude = 10f;

    [Header("Fractal (fBm)")]
    [Tooltip("Number of noise layers. More octaves = more detail.")]
    [SerializeField, Range(1, 8)] private int octaves = 4;

    [Tooltip("Amplitude multiplier per octave. Lower = smoother.")]
    [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;

    [Tooltip("Frequency multiplier per octave. Higher = more detail per layer.")]
    [SerializeField, Range(1f, 4f)] private float lacunarity = 2f;

    [Header("Transform")]
    [Tooltip("Offset to pan the noise field. Useful for variety without changing seed.")]
    [SerializeField] private Vector2 offset;

    [Tooltip("Vertical offset for 3D noise sampling.")]
    [SerializeField] private float zOffset;

    [Header("Domain Warping")]
    [Tooltip("Strength of domain warping. 0 = disabled.")]
    [SerializeField, Range(0f, 2f)] private float domainWarpStrength;

    [Tooltip("Frequency of the warp noise.")]
    [SerializeField, Range(0.001f, 0.5f)] private float domainWarpFrequency = 0.02f;

    [Header("Output")]
    [Tooltip("Remap output: value = Lerp(outputMin, outputMax, normalizedNoise).")]
    [SerializeField] private float outputMin;
    [SerializeField] private float outputMax = 1f;
    [SerializeField] private bool clampOutput = true;

    public float Frequency => frequency;
    public float Amplitude => amplitude;
    public int Octaves => octaves;
    public float Persistence => persistence;
    public float Lacunarity => lacunarity;
    public Vector2 Offset => offset;
    public float ZOffset => zOffset;
    public float DomainWarpStrength => domainWarpStrength;
    public float DomainWarpFrequency => domainWarpFrequency;
    public float OutputMin => outputMin;
    public float OutputMax => outputMax;
    public bool ClampOutput => clampOutput;
}
```

```csharp
using UnityEngine;

/// <summary>
/// Stateless noise sampling utilities. Provides fBm, domain warping, and multi-dimensional sampling.
/// All methods are static and thread-safe (no shared state).
/// </summary>
public static class NoiseGenerator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        // No static state to reset
    }

    /// <summary>
    /// Samples 2D fractal Brownian motion noise.
    /// </summary>
    /// <param name="x">World-space X coordinate.</param>
    /// <param name="y">World-space Y coordinate.</param>
    /// <param name="config">Noise configuration asset.</param>
    /// <returns>Noise value remapped to [outputMin, outputMax].</returns>
    public static float Sample2D(float x, float y, NoiseConfig config)
    {
        float sx = x;
        float sy = y;

        // Apply domain warping if enabled
        if (config.DomainWarpStrength > 0f)
        {
            float warpX = SampleFBm2D(
                x + 100f, y + 100f,
                config.DomainWarpFrequency, config.Octaves,
                config.Persistence, config.Lacunarity, Vector2.zero);
            float warpY = SampleFBm2D(
                x + 300f, y + 300f,
                config.DomainWarpFrequency, config.Octaves,
                config.Persistence, config.Lacunarity, Vector2.zero);

            sx += warpX * config.DomainWarpStrength / config.Frequency;
            sy += warpY * config.DomainWarpStrength / config.Frequency;
        }

        float raw = SampleFBm2D(sx, sy, config.Frequency, config.Octaves,
                                config.Persistence, config.Lacunarity, config.Offset);

        return RemapOutput(raw, config);
    }

    /// <summary>
    /// Samples 3D fractal Brownian motion noise. Useful for volumetric effects or caves.
    /// </summary>
    /// <param name="x">World-space X coordinate.</param>
    /// <param name="y">World-space Y coordinate.</param>
    /// <param name="z">World-space Z coordinate.</param>
    /// <param name="config">Noise configuration asset.</param>
    /// <returns>Noise value remapped to [outputMin, outputMax].</returns>
    public static float Sample3D(float x, float y, float z, NoiseConfig config)
    {
        // Unity lacks built-in 3D Perlin, so we combine two 2D samples
        // For production, consider Unity.Mathematics.noise.cnoise(float3)
        float xy = SampleFBm2D(x, y, config.Frequency, config.Octaves,
                               config.Persistence, config.Lacunarity, config.Offset);
        float yz = SampleFBm2D(y, z + config.ZOffset, config.Frequency, config.Octaves,
                               config.Persistence, config.Lacunarity, config.Offset);

        float raw = (xy + yz) * 0.5f;

        return RemapOutput(raw, config);
    }

    /// <summary>
    /// Generates a full 2D noise map for the given dimensions.
    /// </summary>
    /// <param name="width">Map width in samples.</param>
    /// <param name="height">Map height in samples.</param>
    /// <param name="config">Noise configuration asset.</param>
    /// <returns>Float array of size width * height, row-major order.</returns>
    public static float[] GenerateMap2D(int width, int height, NoiseConfig config)
    {
        var map = new float[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[y * width + x] = Sample2D(x, y, config);

        return map;
    }

    /// <summary>
    /// Raw fBm sampling without domain warping or output remapping.
    /// </summary>
    private static float SampleFBm2D(float x, float y, float frequency,
        int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float sum = 0f;
        float freq = frequency;
        float amp = 1f;
        float maxAmp = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + offset.x) * freq;
            float sampleY = (y + offset.y) * freq;

            // Mathf.PerlinNoise returns [0, 1]
            float sample = Mathf.PerlinNoise(sampleX, sampleY);
            sum += sample * amp;
            maxAmp += amp;

            amp *= persistence;
            freq *= lacunarity;
        }

        // Normalize to [0, 1]
        return sum / maxAmp;
    }

    /// <summary>Remaps a normalized [0,1] value using the config's output range.</summary>
    private static float RemapOutput(float normalized, NoiseConfig config)
    {
        float value = Mathf.Lerp(config.OutputMin, config.OutputMax, normalized);

        if (config.ClampOutput)
            value = Mathf.Clamp(value, config.OutputMin, config.OutputMax);

        return value * config.Amplitude;
    }
}
```

---

## 2. Complete Grid\<T\>

Generic grid container with coordinate helpers, neighbor iteration, bounds checking, and world-grid conversion.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic 2D grid container. Supports coordinate conversion, neighbor queries,
/// bounds checking, and enumeration. Serializable for Inspector display.
/// </summary>
[System.Serializable]
public class Grid<T> : IEnumerable<(Vector2Int coord, T value)>
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 origin = Vector3.zero;

    private T[] _cells;

    /// <summary>Grid width in cells.</summary>
    public int Width => width;

    /// <summary>Grid height in cells.</summary>
    public int Height => height;

    /// <summary>Size of each cell in world units.</summary>
    public float CellSize => cellSize;

    /// <summary>World-space origin of the grid (bottom-left corner).</summary>
    public Vector3 Origin => origin;

    /// <summary>Total number of cells in the grid.</summary>
    public int Count => width * height;

    /// <summary>
    /// Creates a new grid with the specified dimensions.
    /// </summary>
    /// <param name="width">Number of columns.</param>
    /// <param name="height">Number of rows.</param>
    /// <param name="cellSize">World-space size of each cell.</param>
    /// <param name="origin">World-space position of the bottom-left corner.</param>
    public Grid(int width, int height, float cellSize = 1f, Vector3 origin = default)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Grid dimensions must be positive.");

        if (cellSize <= 0f)
            throw new ArgumentException("Cell size must be positive.");

        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.origin = origin;
        _cells = new T[width * height];
    }

    /// <summary>Returns true if the coordinate is within grid bounds.</summary>
    public bool InBounds(Vector2Int coord)
        => coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;

    /// <summary>Access a cell by grid coordinate. Throws if out of bounds.</summary>
    public T this[Vector2Int coord]
    {
        get
        {
            ValidateBounds(coord);

            return _cells[coord.y * width + coord.x];
        }
        set
        {
            ValidateBounds(coord);
            _cells[coord.y * width + coord.x] = value;
        }
    }

    /// <summary>Access a cell by x,y indices.</summary>
    public T this[int x, int y]
    {
        get => this[new Vector2Int(x, y)];
        set => this[new Vector2Int(x, y)] = value;
    }

    /// <summary>
    /// Converts grid coordinates to world position (center of cell).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int coord)
        => origin + new Vector3(
            (coord.x + 0.5f) * cellSize,
            0f,
            (coord.y + 0.5f) * cellSize);

    /// <summary>
    /// Converts a world position to grid coordinates.
    /// Does NOT clamp to bounds -- caller should check InBounds.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - origin;

        return new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.z / cellSize));
    }

    /// <summary>
    /// Returns the 4 cardinal neighbors (up, down, left, right) that are in bounds.
    /// </summary>
    public IEnumerable<Vector2Int> GetCardinalNeighbors(Vector2Int coord)
    {
        Vector2Int up = coord + Vector2Int.up;
        Vector2Int down = coord + Vector2Int.down;
        Vector2Int left = coord + Vector2Int.left;
        Vector2Int right = coord + Vector2Int.right;

        if (InBounds(up))
            yield return up;

        if (InBounds(down))
            yield return down;

        if (InBounds(left))
            yield return left;

        if (InBounds(right))
            yield return right;
    }

    /// <summary>
    /// Returns all 8 neighbors (cardinal + diagonal) that are in bounds.
    /// </summary>
    public IEnumerable<Vector2Int> GetAllNeighbors(Vector2Int coord)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var neighbor = new Vector2Int(coord.x + dx, coord.y + dy);

                if (InBounds(neighbor))
                    yield return neighbor;
            }
        }
    }

    /// <summary>Fills all cells with the specified value.</summary>
    public void Fill(T value)
    {
        Array.Fill(_cells, value);
    }

    /// <summary>Fills all cells using a factory function that receives the coordinate.</summary>
    public void Fill(Func<Vector2Int, T> factory)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _cells[y * width + x] = factory(new Vector2Int(x, y));
    }

    /// <summary>
    /// Tries to get a cell value. Returns false if out of bounds.
    /// </summary>
    public bool TryGet(Vector2Int coord, out T value)
    {
        if (InBounds(coord))
        {
            value = _cells[coord.y * width + coord.x];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Enumerates all cells as (coordinate, value) pairs.</summary>
    public IEnumerator<(Vector2Int coord, T value)> GetEnumerator()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                yield return (new Vector2Int(x, y), _cells[y * width + x]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void ValidateBounds(Vector2Int coord)
    {
        if (!InBounds(coord))
            throw new IndexOutOfRangeException(
                $"Grid coordinate {coord} is out of bounds ({width}x{height}).");
    }

    /// <summary>
    /// Draws grid lines using Gizmos. Call from OnDrawGizmos/OnDrawGizmosSelected.
    /// Only available for grids that do not require T-specific rendering.
    /// </summary>
    public void DrawGizmos(Color color)
    {
        Gizmos.color = color;

        for (int x = 0; x <= width; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
            Vector3 end = origin + new Vector3(x * cellSize, 0f, height * cellSize);
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = origin + new Vector3(0f, 0f, y * cellSize);
            Vector3 end = origin + new Vector3(width * cellSize, 0f, y * cellSize);
            Gizmos.DrawLine(start, end);
        }
    }
}
```

---

## 3. Complete DungeonGenerator

BSP-based dungeon generation with room/corridor data, connectivity validation via flood fill.

```csharp
using UnityEngine;

/// <summary>
/// Configuration for dungeon generation. Each dungeon type or difficulty tier
/// gets its own asset. All parameters tunable by designers in Inspector.
/// </summary>
[CreateAssetMenu(fileName = "New Dungeon Config", menuName = "ProcGen/Dungeon Config")]
public class DungeonConfig : ScriptableObject
{
    [Header("Map Dimensions")]
    [SerializeField, Min(20)] private int mapWidth = 64;
    [SerializeField, Min(20)] private int mapHeight = 64;

    [Header("Rooms")]
    [Tooltip("Minimum room width/height in cells.")]
    [SerializeField, Range(4, 20)] private int minRoomSize = 6;
    [Tooltip("Maximum room width/height in cells.")]
    [SerializeField, Range(6, 40)] private int maxRoomSize = 14;
    [SerializeField, Range(3, 30)] private int roomCountMin = 5;
    [SerializeField, Range(5, 40)] private int roomCountMax = 12;
    [Tooltip("Padding between room edges and BSP partition edges.")]
    [SerializeField, Range(1, 5)] private int roomPadding = 2;

    [Header("BSP")]
    [Tooltip("Maximum BSP recursion depth. More depth = more potential rooms.")]
    [SerializeField, Range(2, 10)] private int splitDepth = 5;
    [Tooltip("Minimum split ratio to prevent thin slices.")]
    [SerializeField, Range(0.3f, 0.7f)] private float minSplitRatio = 0.4f;

    [Header("Corridors")]
    [SerializeField, Range(1, 5)] private int corridorWidth = 2;
    [Tooltip("If true, corridors bend at right angles. If false, straight line.")]
    [SerializeField] private bool bendCorridors = true;

    [Header("Special Rooms")]
    [SerializeField] private GameObject bossRoomPrefab;
    [SerializeField] private GameObject treasureRoomPrefab;
    [SerializeField] private GameObject startRoomPrefab;

    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public int MinRoomSize => minRoomSize;
    public int MaxRoomSize => maxRoomSize;
    public int RoomCountMin => roomCountMin;
    public int RoomCountMax => roomCountMax;
    public int RoomPadding => roomPadding;
    public int SplitDepth => splitDepth;
    public float MinSplitRatio => minSplitRatio;
    public int CorridorWidth => corridorWidth;
    public bool BendCorridors => bendCorridors;
    public GameObject BossRoomPrefab => bossRoomPrefab;
    public GameObject TreasureRoomPrefab => treasureRoomPrefab;
    public GameObject StartRoomPrefab => startRoomPrefab;
}
```

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a rectangular room in the dungeon.
/// </summary>
public class Room
{
    private readonly List<Room> _connections = new();
    private readonly RectInt _bounds;

    /// <summary>Grid-space bounds of the room.</summary>
    public RectInt Bounds => _bounds;

    /// <summary>Center coordinate of the room.</summary>
    public Vector2Int Center => new(_bounds.x + _bounds.width / 2,
                                    _bounds.y + _bounds.height / 2);

    /// <summary>Floor area in cells.</summary>
    public int Area => _bounds.width * _bounds.height;

    /// <summary>Tag for special room types (boss, treasure, start).</summary>
    public string Tag { get; set; } = "";

    /// <summary>Connected rooms (set during corridor generation).</summary>
    public List<Room> Connections => _connections;

    public Room(RectInt bounds) => _bounds = bounds;
}

/// <summary>
/// Represents a corridor connecting two rooms.
/// </summary>
public class Corridor
{
    private readonly Vector2Int? _bendPoint;
    private readonly Vector2Int _start;
    private readonly Vector2Int _end;
    private readonly int _width;

    /// <summary>Start point (center of source room).</summary>
    public Vector2Int Start => _start;

    /// <summary>End point (center of destination room).</summary>
    public Vector2Int End => _end;

    /// <summary>Corridor width in cells.</summary>
    public int Width => _width;

    /// <summary>Bend point for L-shaped corridors. Null for straight corridors.</summary>
    public Vector2Int? BendPoint => _bendPoint;

    public Corridor(Vector2Int start, Vector2Int end, int width, Vector2Int? bendPoint = null)
    {
        _start = start;
        _end = end;
        _width = width;
        _bendPoint = bendPoint;
    }
}

/// <summary>
/// Output of dungeon generation. Contains all rooms, corridors, and the walkable grid.
/// </summary>
public class DungeonData
{
    private readonly List<Room> _rooms = new();
    private readonly List<Corridor> _corridors = new();

    /// <summary>All rooms in the dungeon.</summary>
    public List<Room> Rooms => _rooms;

    /// <summary>All corridors connecting rooms.</summary>
    public List<Corridor> Corridors => _corridors;

    /// <summary>Walkable grid. True = floor, False = wall.</summary>
    public Grid<bool> WalkableGrid { get; set; }

    /// <summary>The starting room (player spawn).</summary>
    public Room StartRoom { get; set; }

    /// <summary>The exit/boss room.</summary>
    public Room EndRoom { get; set; }
}

/// <summary>
/// Internal BSP tree node used during generation.
/// </summary>
internal class BspNode
{
    private readonly RectInt _area;

    public RectInt Area => _area;
    public BspNode Left { get; set; }
    public BspNode Right { get; set; }
    public Room Room { get; set; }

    public bool IsLeaf => Left == null && Right == null;

    public BspNode(RectInt area) => _area = area;
}

/// <summary>
/// Generates dungeons using Binary Space Partition (BSP) with corridor connection
/// and connectivity validation. Implements IGenerator for use with runtime/editor runners.
/// </summary>
public class DungeonGenerator : IGenerator<DungeonData>
{
    private readonly DungeonConfig _config;

    public DungeonGenerator(DungeonConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Generates a complete dungeon layout.
    /// </summary>
    public DungeonData Generate(GenerationContext context)
    {
        var data = new DungeonData();
        var rng = context.Random;

        // Step 1: BSP split
        var root = new BspNode(new RectInt(0, 0, _config.MapWidth, _config.MapHeight));
        SplitNode(root, 0, rng);

        // Step 2: Place rooms in leaf nodes
        var leaves = new List<BspNode>();
        CollectLeaves(root, leaves);

        foreach (var leaf in leaves)
        {
            Room room = CreateRoomInPartition(leaf.Area, rng);

            if (room != null)
            {
                leaf.Room = room;
                data.Rooms.Add(room);
            }
        }

        // Step 3: Enforce room count constraints
        if (data.Rooms.Count < _config.RoomCountMin)
            return Generate(context); // Retry with same seed progression

        if (data.Rooms.Count > _config.RoomCountMax)
            data.Rooms.RemoveRange(_config.RoomCountMax, data.Rooms.Count - _config.RoomCountMax);

        // Step 4: Connect sibling rooms via corridors
        ConnectBspSiblings(root, data, rng);

        // Step 5: Build walkable grid
        data.WalkableGrid = new Grid<bool>(_config.MapWidth, _config.MapHeight);
        CarveRooms(data);
        CarveCorridors(data);

        // Step 6: Validate connectivity; add emergency corridors between disconnected rooms
        if (!ConnectivityValidator.IsFullyConnected(data.WalkableGrid, data.Rooms))
            RepairConnectivity(data, rng);

        // Step 7: Assign special rooms
        AssignSpecialRooms(data, rng);

        return data;
    }

    private void SplitNode(BspNode node, int depth, SeededRandom rng)
    {
        if (depth >= _config.SplitDepth)
            return;

        // Determine split direction based on aspect ratio
        bool isSplitHorizontal = node.Area.width < node.Area.height;
        if (node.Area.width == node.Area.height)
            isSplitHorizontal = rng.Float01() > 0.5f;

        int minSize = _config.MinRoomSize + _config.RoomPadding * 2;

        if (isSplitHorizontal)
        {
            if (node.Area.height < minSize * 2)
                return;

            int minSplit = Mathf.RoundToInt(node.Area.height * _config.MinSplitRatio);
            int maxSplit = node.Area.height - minSplit;

            if (minSplit >= maxSplit)
                return;

            int split = rng.Range(minSplit, maxSplit);

            node.Left = new BspNode(new RectInt(
                node.Area.x, node.Area.y, node.Area.width, split));
            node.Right = new BspNode(new RectInt(
                node.Area.x, node.Area.y + split, node.Area.width, node.Area.height - split));
        }
        else
        {
            if (node.Area.width < minSize * 2)
                return;

            int minSplit = Mathf.RoundToInt(node.Area.width * _config.MinSplitRatio);
            int maxSplit = node.Area.width - minSplit;

            if (minSplit >= maxSplit)
                return;

            int split = rng.Range(minSplit, maxSplit);

            node.Left = new BspNode(new RectInt(
                node.Area.x, node.Area.y, split, node.Area.height));
            node.Right = new BspNode(new RectInt(
                node.Area.x + split, node.Area.y, node.Area.width - split, node.Area.height));
        }

        SplitNode(node.Left, depth + 1, rng);
        SplitNode(node.Right, depth + 1, rng);
    }

    private Room CreateRoomInPartition(RectInt partition, SeededRandom rng)
    {
        int maxW = Mathf.Min(_config.MaxRoomSize, partition.width - _config.RoomPadding * 2);
        int maxH = Mathf.Min(_config.MaxRoomSize, partition.height - _config.RoomPadding * 2);

        if (maxW < _config.MinRoomSize || maxH < _config.MinRoomSize)
            return null;

        int roomW = rng.Range(_config.MinRoomSize, maxW + 1);
        int roomH = rng.Range(_config.MinRoomSize, maxH + 1);

        int roomX = rng.Range(partition.x + _config.RoomPadding,
                              partition.x + partition.width - roomW - _config.RoomPadding + 1);
        int roomY = rng.Range(partition.y + _config.RoomPadding,
                              partition.y + partition.height - roomH - _config.RoomPadding + 1);

        return new Room(new RectInt(roomX, roomY, roomW, roomH));
    }

    private void CollectLeaves(BspNode node, List<BspNode> leaves)
    {
        if (node == null)
            return;

        if (node.IsLeaf)
        {
            leaves.Add(node);
            return;
        }

        CollectLeaves(node.Left, leaves);
        CollectLeaves(node.Right, leaves);
    }

    private Room FindRoomInSubtree(BspNode node)
    {
        if (node == null)
            return null;

        if (node.Room != null)
            return node.Room;

        return FindRoomInSubtree(node.Left) ?? FindRoomInSubtree(node.Right);
    }

    private void ConnectBspSiblings(BspNode node, DungeonData data, SeededRandom rng)
    {
        if (node == null || node.IsLeaf)
            return;

        ConnectBspSiblings(node.Left, data, rng);
        ConnectBspSiblings(node.Right, data, rng);

        Room leftRoom = FindRoomInSubtree(node.Left);
        Room rightRoom = FindRoomInSubtree(node.Right);

        if (leftRoom != null && rightRoom != null)
        {
            Vector2Int? bend = null;
            if (_config.BendCorridors)
                bend = new Vector2Int(leftRoom.Center.x, rightRoom.Center.y);

            var corridor = new Corridor(leftRoom.Center, rightRoom.Center,
                                        _config.CorridorWidth, bend);
            data.Corridors.Add(corridor);
            leftRoom.Connections.Add(rightRoom);
            rightRoom.Connections.Add(leftRoom);
        }
    }

    private void CarveRooms(DungeonData data)
    {
        foreach (var room in data.Rooms)
        {
            for (int x = room.Bounds.x; x < room.Bounds.x + room.Bounds.width; x++)
                for (int y = room.Bounds.y; y < room.Bounds.y + room.Bounds.height; y++)
                    if (data.WalkableGrid.InBounds(new Vector2Int(x, y)))
                        data.WalkableGrid[new Vector2Int(x, y)] = true;
        }
    }

    private void CarveCorridors(DungeonData data)
    {
        foreach (var corridor in data.Corridors)
        {
            if (corridor.BendPoint.HasValue)
            {
                CarveLine(data.WalkableGrid, corridor.Start, corridor.BendPoint.Value, corridor.Width);
                CarveLine(data.WalkableGrid, corridor.BendPoint.Value, corridor.End, corridor.Width);
            }
            else
            {
                CarveLine(data.WalkableGrid, corridor.Start, corridor.End, corridor.Width);
            }
        }
    }

    private void CarveLine(Grid<bool> grid, Vector2Int from, Vector2Int to, int width)
    {
        int halfWidth = width / 2;
        int dx = from.x < to.x ? 1 : from.x > to.x ? -1 : 0;
        int dy = from.y < to.y ? 1 : from.y > to.y ? -1 : 0;

        var current = from;

        while (current != to)
        {
            CarveCell(grid, current, halfWidth);

            if (current.x != to.x)
                current.x += dx;
            else if (current.y != to.y)
                current.y += dy;
        }

        CarveCell(grid, to, halfWidth);
    }

    private void CarveCell(Grid<bool> grid, Vector2Int center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                var pos = new Vector2Int(center.x + dx, center.y + dy);

                if (grid.InBounds(pos))
                    grid[pos] = true;
            }
        }
    }

    private void RepairConnectivity(DungeonData data, SeededRandom rng)
    {
        // Simple repair: connect each room to the nearest unconnected room
        for (int i = 1; i < data.Rooms.Count; i++)
        {
            var corridor = new Corridor(
                data.Rooms[i - 1].Center, data.Rooms[i].Center,
                _config.CorridorWidth,
                _config.BendCorridors
                    ? new Vector2Int(data.Rooms[i - 1].Center.x, data.Rooms[i].Center.y)
                    : null);
            data.Corridors.Add(corridor);

            CarveLine(data.WalkableGrid, corridor.Start,
                      corridor.BendPoint ?? corridor.End, corridor.Width);

            if (corridor.BendPoint.HasValue)
                CarveLine(data.WalkableGrid, corridor.BendPoint.Value,
                          corridor.End, corridor.Width);
        }
    }

    private void AssignSpecialRooms(DungeonData data, SeededRandom rng)
    {
        if (data.Rooms.Count < 2)
            return;

        // Start room = first room, end room = room farthest from start
        data.StartRoom = data.Rooms[0];
        data.StartRoom.Tag = "Start";

        float maxDist = 0f;

        foreach (var room in data.Rooms)
        {
            float dist = Vector2Int.Distance(data.StartRoom.Center, room.Center);

            if (dist > maxDist)
            {
                maxDist = dist;
                data.EndRoom = room;
            }
        }

        if (data.EndRoom != null)
            data.EndRoom.Tag = "Boss";
    }
}

/// <summary>
/// Validates dungeon connectivity using flood fill.
/// </summary>
public static class ConnectivityValidator
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        // No static state to reset
    }

    /// <summary>
    /// Returns true if all room centers are reachable from the first room via walkable cells.
    /// </summary>
    /// <param name="walkableGrid">Grid where true = walkable floor.</param>
    /// <param name="rooms">List of rooms to check connectivity for.</param>
    /// <returns>True if all rooms are mutually reachable.</returns>
    public static bool IsFullyConnected(Grid<bool> walkableGrid, List<Room> rooms)
    {
        if (rooms == null || rooms.Count <= 1)
            return true;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(rooms[0].Center);
        visited.Add(rooms[0].Center);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in walkableGrid.GetCardinalNeighbors(current))
            {
                if (walkableGrid[neighbor] && visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return rooms.All(r => visited.Contains(r.Center));
    }
}
```

---

## 4. Complete SeededRandom

Deterministic random wrapper with typed helpers, spatial sampling, and seed derivation.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic random number generator wrapping System.Random.
/// Each subsystem (terrain, loot, enemies) gets its own instance derived
/// from a master seed, ensuring reproducibility and isolation.
/// NOT thread-safe -- use Unity.Mathematics.Random for Jobs/Burst.
/// </summary>
public class SeededRandom
{
    private readonly System.Random _rng;
    private readonly int _seed;

    /// <summary>The seed this instance was initialized with.</summary>
    public int Seed => _seed;

    /// <summary>Creates a new deterministic RNG with the given seed.</summary>
    public SeededRandom(int seed)
    {
        _seed = seed;
        _rng = new System.Random(seed);
    }

    // --- Integer ---

    /// <summary>Returns a random int in [min, max).</summary>
    public int Range(int min, int max) => _rng.Next(min, max);

    /// <summary>Returns a non-negative random int.</summary>
    public int Next() => _rng.Next();

    // --- Float ---

    /// <summary>Returns a random float in [0, 1).</summary>
    public float Float01() => (float)_rng.NextDouble();

    /// <summary>Returns a random float in [min, max).</summary>
    public float Range(float min, float max) => min + Float01() * (max - min);

    // --- Boolean ---

    /// <summary>Returns true with the given probability [0, 1].</summary>
    public bool Chance(float probability) => Float01() < probability;

    // --- Spatial ---

    /// <summary>Returns a random point inside a unit circle (radius 1).</summary>
    public Vector2 PointInCircle()
    {
        float angle = Float01() * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(Float01()); // Sqrt for uniform distribution

        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    /// <summary>Returns a random point inside a unit sphere (radius 1).</summary>
    public Vector3 PointInSphere()
    {
        // Rejection sampling for uniform distribution
        Vector3 point;
        do
        {
            point = new Vector3(
                Range(-1f, 1f),
                Range(-1f, 1f),
                Range(-1f, 1f));
        }
        while (point.sqrMagnitude > 1f);

        return point;
    }

    /// <summary>Returns a random point on the surface of a unit sphere.</summary>
    public Vector3 PointOnSphere()
    {
        return PointInSphere().normalized;
    }

    /// <summary>Returns a random direction as a normalized Vector2.</summary>
    public Vector2 Direction2D()
    {
        float angle = Float01() * Mathf.PI * 2f;

        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // --- Collections ---

    /// <summary>
    /// Selects a random item from a list using weighted probabilities.
    /// Higher weight = more likely to be selected.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="entries">List of (item, weight) tuples. Weights must be positive.</param>
    /// <returns>The selected item.</returns>
    public T WeightedChoice<T>(IReadOnlyList<(T item, float weight)> entries)
    {
        if (entries == null || entries.Count == 0)
            throw new ArgumentException("Entries list must not be empty.");

        float totalWeight = 0f;
        foreach (var e in entries)
            totalWeight += e.weight;

        if (totalWeight <= 0f)
            throw new ArgumentException("Total weight must be positive.");

        float roll = Float01() * totalWeight;
        float cumulative = 0f;

        foreach (var e in entries)
        {
            cumulative += e.weight;

            if (roll < cumulative)
                return e.item;
        }

        // Floating point edge case: return last item
        return entries[^1].item;
    }

    /// <summary>Returns a random element from a list.</summary>
    public T Choose<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List must not be empty.");

        return list[Range(0, list.Count)];
    }

    /// <summary>
    /// Shuffles a list in place using the Fisher-Yates algorithm.
    /// </summary>
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // --- Seed Derivation ---

    /// <summary>
    /// Derives a child seed for a named subsystem. Deterministic for the
    /// same master seed + subsystem name combination.
    /// Uses StringComparison.Ordinal for cross-platform stability.
    /// </summary>
    /// <param name="masterSeed">The master seed for the entire run.</param>
    /// <param name="subsystem">Subsystem identifier (e.g., "terrain", "loot", "enemies").</param>
    /// <returns>A deterministic derived seed.</returns>
    public static int DeriveSeed(int masterSeed, string subsystem)
        => HashCode.Combine(masterSeed, subsystem.GetHashCode(StringComparison.Ordinal));

    /// <summary>
    /// Creates a child SeededRandom for a named subsystem.
    /// </summary>
    /// <param name="subsystem">Subsystem identifier.</param>
    /// <returns>A new SeededRandom with a derived seed.</returns>
    public SeededRandom CreateSubsystem(string subsystem)
        => new SeededRandom(DeriveSeed(Seed, subsystem));

    // --- Utility ---

    /// <summary>
    /// Generates a random seed suitable for new runs. Uses system time.
    /// </summary>
    public static int GenerateRandomSeed()
        => System.Environment.TickCount;
}
```

---

## 5. Complete ContentPlacer

Budget-driven content placement with mandatory-first guarantee, weighted random, and validation.

```csharp
using UnityEngine;

/// <summary>
/// A single entry in a content budget. Defines one placeable item with constraints.
/// </summary>
[System.Serializable]
public struct ContentEntry
{
    [Tooltip("The prefab to instantiate.")]
    public GameObject prefab;

    [Tooltip("Category for grouping (e.g., 'enemy', 'pickup', 'hazard').")]
    public string category;

    [Tooltip("Display name for editor readability.")]
    public string displayName;

    [Tooltip("Relative selection weight. Higher = more common.")]
    [Range(0.01f, 100f)] public float weight;

    [Tooltip("Minimum guaranteed placements. Budget must accommodate sum of all minimums.")]
    [Min(0)] public int minimum;

    [Tooltip("Maximum allowed placements. 0 = unlimited.")]
    [Min(0)] public int maximum;

    [Tooltip("Optional: restrict to rooms with this tag (e.g., 'Boss', 'Treasure').")]
    public string requiredRoomTag;
}
```

```csharp
using UnityEngine;

/// <summary>
/// Defines what content can be placed and in what quantities.
/// Create one per floor, biome, or difficulty tier.
/// </summary>
[CreateAssetMenu(fileName = "New Content Budget", menuName = "ProcGen/Content Budget")]
public class ContentBudget : ScriptableObject
{
    [Tooltip("Total placement slots available. Must be >= sum of all entry minimums.")]
    [SerializeField, Min(1)] private int totalSlots = 20;

    [Tooltip("Content entries defining what can be placed.")]
    [SerializeField] private ContentEntry[] entries;

    public int TotalSlots => totalSlots;
    public ContentEntry[] Entries => entries;

    /// <summary>
    /// Validates that the budget is internally consistent.
    /// </summary>
    /// <returns>True if valid; false with error details in message.</returns>
    public bool Validate(out string message)
    {
        int totalMinimum = 0;
        foreach (var entry in entries)
            totalMinimum += entry.minimum;

        if (totalMinimum > totalSlots)
        {
            message = $"Sum of minimums ({totalMinimum}) exceeds totalSlots ({totalSlots}).";
            return false;
        }

        foreach (var entry in entries)
        {
            if (entry.maximum > 0 && entry.minimum > entry.maximum)
            {
                message = $"Entry '{entry.displayName}': minimum ({entry.minimum}) > maximum ({entry.maximum}).";
                return false;
            }
        }

        message = "Valid";
        return true;
    }
}
```

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of content placement. Contains the selected prefabs and their metadata.
/// </summary>
public struct PlacementResult
{
    /// <summary>The prefab to instantiate.</summary>
    public GameObject Prefab;

    /// <summary>Category of the placed content.</summary>
    public string Category;

    /// <summary>Required room tag, if any.</summary>
    public string RequiredRoomTag;
}

/// <summary>
/// Places content according to budget constraints. Guarantees all minimums
/// are satisfied before filling remaining slots with weighted random.
/// </summary>
public class ContentPlacer
{
    private readonly Dictionary<string, int> _placedCounts = new();
    private readonly Dictionary<int, int> _entryPlacedCounts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        // No static state to reset
    }

    /// <summary>
    /// Generates a list of content placements respecting all budget constraints.
    /// </summary>
    /// <param name="budget">The content budget to consume.</param>
    /// <param name="rng">Seeded random for deterministic selection.</param>
    /// <returns>Ordered list of placement results.</returns>
    public List<PlacementResult> GeneratePlacements(ContentBudget budget, SeededRandom rng)
    {
        _placedCounts.Clear();
        _entryPlacedCounts.Clear();

        if (!budget.Validate(out string validationMessage))
        {
            GameDebug.LogError($"ContentPlacer: Invalid budget -- {validationMessage}");
            return new List<PlacementResult>();
        }

        var result = new List<PlacementResult>();

        // Phase 1: Place all mandatory minimums FIRST
        for (int entryIdx = 0; entryIdx < budget.Entries.Length; entryIdx++)
        {
            var entry = budget.Entries[entryIdx];

            for (int i = 0; i < entry.minimum; i++)
            {
                result.Add(CreatePlacement(entry));
                IncrementCounts(entry.category, entryIdx);
            }
        }

        // Phase 2: Fill remaining slots with weighted random
        int remaining = budget.TotalSlots - result.Count;

        for (int i = 0; i < remaining; i++)
        {
            var eligible = BuildWeightedList(budget);

            if (eligible.Count == 0)
                break;

            var (chosen, _) = rng.WeightedChoice(eligible);
            result.Add(CreatePlacement(budget.Entries[chosen]));
            IncrementCounts(budget.Entries[chosen].category, chosen);
        }

        // Phase 3: Shuffle to avoid mandatory items always appearing first
        rng.Shuffle(result);

        return result;
    }

    /// <summary>
    /// Validates that a placement result list satisfies the budget constraints.
    /// Useful for post-generation verification.
    /// </summary>
    /// <param name="placements">The generated placements to validate.</param>
    /// <param name="budget">The budget to validate against.</param>
    /// <returns>True if all constraints are met.</returns>
    public static bool ValidatePlacements(List<PlacementResult> placements, ContentBudget budget)
    {
        var counts = new Dictionary<string, int>();

        foreach (var p in placements)
        {
            counts.TryGetValue(p.Category, out int count);
            counts[p.Category] = count + 1;
        }

        foreach (var entry in budget.Entries)
        {
            counts.TryGetValue(entry.category, out int placed);

            if (placed < entry.minimum)
            {
                GameDebug.LogWarning($"ContentPlacer validation: '{entry.displayName}' " +
                                     $"placed {placed}, minimum is {entry.minimum}.");
                return false;
            }

            if (entry.maximum > 0 && placed > entry.maximum)
            {
                GameDebug.LogWarning($"ContentPlacer validation: '{entry.displayName}' " +
                                     $"placed {placed}, maximum is {entry.maximum}.");
                return false;
            }
        }

        return true;
    }

    private List<((int entryIdx, float weight) item, float weight)> BuildWeightedList(
        ContentBudget budget)
    {
        var list = new List<((int, float), float)>();

        for (int i = 0; i < budget.Entries.Length; i++)
        {
            var entry = budget.Entries[i];
            _entryPlacedCounts.TryGetValue(i, out int placedForEntry);

            // Skip entries that have reached their maximum
            if (entry.maximum > 0 && placedForEntry >= entry.maximum)
                continue;

            list.Add(((i, entry.weight), entry.weight));
        }

        return list;
    }

    private PlacementResult CreatePlacement(ContentEntry entry)
    {
        return new PlacementResult
        {
            Prefab = entry.prefab,
            Category = entry.category,
            RequiredRoomTag = entry.requiredRoomTag
        };
    }

    private void IncrementCounts(string category, int entryIdx)
    {
        _placedCounts.TryGetValue(category, out int catCount);
        _placedCounts[category] = catCount + 1;

        _entryPlacedCounts.TryGetValue(entryIdx, out int entryCount);
        _entryPlacedCounts[entryIdx] = entryCount + 1;
    }
}
```

---

## 6. Generation Pipeline Diagram

```
                         PROCEDURAL GENERATION PIPELINE
    =========================================================================

    +-----------+     +----------------+     +------------------+
    |   SEED    |---->|   CONFIG (SO)  |---->|    GENERATOR     |
    | (int)     |     |                |     |  IGenerator<T>   |
    | Player or |     | NoiseConfig    |     |                  |
    | auto-gen  |     | DungeonConfig  |     | Pure data logic  |
    +-----------+     | ContentBudget  |     | No side effects  |
                      +----------------+     +--------+---------+
                                                      |
                                                      | TOutput (data only)
                                                      v
                      +------------------+     +------+----------+
                      |   CONSTRAINTS    |<----|   RAW DATA      |
                      |                  |     |                  |
                      | Min/max rooms    |     | List<Room>       |
                      | Connectivity     |     | Grid<bool>       |
                      | Content budget   |     | DungeonData      |
                      +--------+---------+     +------+----------+
                               |                      |
                               | Validated            |
                               v                      v
                      +------------------+     +------+----------+
                      |   CONTENT        |     |   PLACER        |
                      |   BUDGET         |---->|                  |
                      |                  |     | Mandatory first  |
                      | Per-category     |     | Weighted random  |
                      | min/max/weight   |     | Budget tracking  |
                      +------------------+     +--------+---------+
                                                        |
                                                        | List<PlacementResult>
                                                        v
                      +--------------------------------------------------+
                      |              INSTANTIATION LAYER                  |
                      |                                                  |
                      |  +-- Editor Mode ----+  +-- Runtime Mode -----+  |
                      |  | AssetDatabase     |  | Awaitable async     |  |
                      |  | Bake to .asset    |  | Spread across frames|  |
                      |  | Preview in Scene  |  | Object pooling      |  |
                      |  +------------------+  +---------------------+  |
                      +--------------------------------------------------+


    KEY PRINCIPLES:
    -----------------------------------------------------------------------
    1. Seed flows DOWN -- every subsystem derives its seed from the master
    2. Config is DATA -- ScriptableObjects, never hardcoded values
    3. Generator is PURE -- no MonoBehaviour, no Instantiate, just data
    4. Constraints VALIDATE -- post-generation checks, not mid-generation hacks
    5. Instantiation is SEPARATE -- same data, different output targets
    -----------------------------------------------------------------------


    SEED DERIVATION:
    -----------------------------------------------------------------------

    Master Seed: 42
        |
        +-- DeriveSeed(42, "terrain")  --> 7291034   --> SeededRandom(7291034)
        |
        +-- DeriveSeed(42, "dungeon")  --> 1840293   --> SeededRandom(1840293)
        |
        +-- DeriveSeed(42, "loot")     --> 5938471   --> SeededRandom(5938471)
        |
        +-- DeriveSeed(42, "enemies")  --> 3629184   --> SeededRandom(3629184)

    Each subsystem gets its own deterministic stream.
    Changing enemy generation does NOT affect terrain or loot.
    -----------------------------------------------------------------------
```
