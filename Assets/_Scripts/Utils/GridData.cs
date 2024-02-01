
using Unity.Mathematics;


using static Unity.Mathematics.math;

namespace Kaizerwald
{
    public readonly struct GridData
    {
        public readonly int CellSize;
        public readonly int ChunkSize;
        public readonly int NumCellInChunkX;

        public readonly int2 MapSize;
        public readonly int2 NumCellXY;
        public readonly int2 NumChunkXY;

        public GridData(in int2 mapSize, int cellSize, int chunkSize = 1)
        {
            CellSize = cellSize;
            ChunkSize = cellSize > chunkSize ? cellSize : chunkSize;
            MapSize = mapSize;

            NumCellInChunkX = max(1, ChunkSize >> floorlog2(CellSize));
            NumChunkXY = max(1, MapSize >> floorlog2(ChunkSize));
            NumCellXY = max(1, MapSize >> floorlog2(CellSize));
        }
        
        public GridData(in int2 mapSize, int chunkSize)
        {
            CellSize = 1;
            ChunkSize = chunkSize;
            MapSize = mapSize;

            NumCellInChunkX = max(1, ChunkSize >> floorlog2(CellSize));
            NumChunkXY = max(1, MapSize >> floorlog2(ChunkSize));
            NumCellXY = max(1, MapSize >> floorlog2(CellSize));
        }

        public readonly int TotalCells => NumCellXY.x * NumCellXY.y;
        public readonly int TotalChunk => NumChunkXY.x * NumChunkXY.y;
        public readonly int TotalCellInChunk => NumCellInChunkX * NumCellInChunkX;
    }
}