using UnityEngine;

namespace SurroundingGenerating
{
    public static class MapDataGenerator2D
    {
        public delegate float NoiseFunction(int x, int y);
        
        private static float DefaultNoiseFunc(int x, int y)
        {
            var randomVal = Random.value;
            return randomVal;
        }

        public static void InitMap(ref int[,] map, Vector2Int mapSize, float wallDensity, NoiseFunction noiseFunction = null)
        {
            map = new int[mapSize.x, mapSize.y];
            if (noiseFunction == null)
            {
                noiseFunction = DefaultNoiseFunc;
            }

            FillMapRandomly(ref map, wallDensity, noiseFunction);
        }

        private static void FillMapRandomly(ref int[,] map, float wallDensity, NoiseFunction noiseFunction = null)
        {
            if (noiseFunction == null)
            {
                noiseFunction = DefaultNoiseFunc;
            }

            var size = new Vector2Int(map.GetLength(0), map.GetLength(1));
            for (int i = 0; i < size.x; ++i)
            {
                for (int j = 0; j < size.y; ++j)
                {                
                    var rndVal = noiseFunction(i, j);
                    map[i, j] = IsOnEdge(size, new Vector2Int(i, j)) || rndVal > (1f - wallDensity) ? 1 : 0;
                }
            }
        }

        public static void SmoothMap(ref int[,] map, int smoothParam)
        {
            if (smoothParam == -1)
            {
                return;
            }

            var counter = 0;
            var size = new Vector2Int(map.GetLength(0), map.GetLength(1));
            int[,] smoothMap = new int[size.x, size.y];
            for (int i = 0; i < size.x; ++i)
            {
                for (int j = 0; j < size.y; ++j)
                {   
                    int neighbourWallsCount = GetSurroundingWallsCount(map, i, j);
                    if (neighbourWallsCount >= smoothParam)
                    {
                        smoothMap[i, j] = 1;
                        counter++;
                    }
                    else if (neighbourWallsCount < smoothParam)
                    {
                        smoothMap[i, j] = 0;
                    }
                    else
                    {
                        smoothMap[i, j] = map[i, j];
                    }
                }
            }

            map = smoothMap;
        }

        private static bool IsOnEdge(Vector2Int size, Vector2Int coord)
        {
            return coord.x == 0 || coord.x == size.x -1 || coord.y == 0 || coord.y == size.y - 1;
        }

        private static int GetSurroundingWallsCount(int[,] map, int gridX, int gridY)
        {
            int wallsCount = 0;
            var size = new Vector2Int(map.GetLength(0), map.GetLength(1));
            for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; ++neighbourX)
            {
                for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; ++neighbourY)
                {
                    if (neighbourX >= 0 && neighbourX < size.x && neighbourY >= 0 && neighbourY < size.y)
                    {
                        if (neighbourX != gridX || neighbourY != gridY)
                        {
                            wallsCount += map[neighbourX, neighbourY];
                        }
                    }
                    else
                    {
                        wallsCount++;
                    }
                }
            }

            return wallsCount;
        }

        public static void GenerateMapData(ref int[,] map, Vector2Int mapSize, float wallDencity, int smoothParam, int smoothCycles, NoiseFunction noiseFunction = null)
        {
            // Initing and randomly filling map
            InitMap(ref map, mapSize, wallDencity, noiseFunction);
            
            // Smoothing map
            for (int i = 0; i < smoothCycles; ++i)
            {
                SmoothMap(ref map, smoothParam);
            }
        }
    }
}
