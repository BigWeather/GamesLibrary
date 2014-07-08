using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    public enum Side { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest, All };

#if false
    public class PerlinNoise2D
    {
        private float _p; // persistence
        private int _numberOctaves;
        private int _width;
        private int _height;
        public double[] _rnds;
        private int _rndsOffset;

        public PerlinNoise2D(int width, int height) : this(width, height, 0.25f, 4, 27) { }
        public PerlinNoise2D(int width, int height, float p, int numberOctaves, int seed)
        {
            _width = width;
            _height = height;
            _p = p;
            _numberOctaves = numberOctaves;

            _rnds = new double[_numberOctaves * _height * _width];
            System.Random rnd = new System.Random(seed);
            for (int i = 0; i < _rnds.Length; i++)
                //_rnds[i] = rnd.NextDouble();
                //_rnds[i] = -1.0f + (2 * rnd.NextDouble());
                _rnds[i] = ((float)rnd.NextDouble() - 0.5f) * 2.0f;
        }

        public float getValue(float x, float y)
        {
            float total = 0;

            for (int o = 0; o < _numberOctaves; o++)
            {
                _rndsOffset = o * (_height * _width);

                float frequency = (float)Math.Pow(2, o);
                float amplitude = (float)Math.Pow(_p, o);
                total += (interpolatedNoise(x * frequency, y * frequency) * amplitude);
            }

            return total;
        }

        private float interpolatedNoise(float x, float y)
        {
            int integer_X = (int)x;
            float fractional_X = x - integer_X;

            int integer_Y = (int)y;
            float fractional_Y = y - integer_Y;

            float v1 = smoothedNoise(integer_X, integer_Y);
            float v2 = smoothedNoise(integer_X + 1, integer_Y);
            float v3 = smoothedNoise(integer_X, integer_Y + 1);
            float v4 = smoothedNoise(integer_X + 1, integer_Y + 1);

            float i1 = interpolate(v1, v2, fractional_X);
            float i2 = interpolate(v3, v4, fractional_X);

            return interpolate(i1, i2, fractional_Y);
        }

        private float smoothedNoise(float x, float y)
        {
            float corners = (noise(x - 1, y - 1) + noise(x + 1, y - 1) + noise(x - 1, y + 1) + noise(x + 1, y + 1)) / 16;
            float sides = (noise(x - 1, y) + noise(x + 1, y) + noise(x, y - 1) + noise(x, y + 1)) / 8;
            float center = noise(x, y) / 4;

            return corners + sides + center;
        }

        private float interpolate(float a, float b, float x)
        {
            float ft = x * 3.1415927f;
            float f = (1.0f - (float)Math.Cos(ft)) * 0.5f;

            return (a * (1.0f - f)) + (b * f);
        }

#if true
        private float noise(float fx, float fy)
        {
            int x = (int)fx;
            int y = (int)fy;

            x = Math.Max(x, 0);
            x = Math.Min(x, _width - 1);
            y = Math.Max(y, 0);
            y = Math.Min(y, _height - 1);

            return (float)_rnds[_rndsOffset + (y * _width) + x];
        }
#else
        private float noise(float fx, float fy)
        {
            int x = (int)fx;
            int y = (int)fy;

            int n = x + y * 57;
            n = (n << 13) ^ n;

            return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 7fffffff) / 1073741824.0);
        }
#endif
    }
#endif

    public class WorldBuilder
    {
        private int _width;
        private int _height;

        public WorldBuilder(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public float[,] generateHeightMap(out int seed)
        {
            float[,] heightMap = new float[_height, _width];

            //seed = new System.Random().Next(100);
            //seed = new System.Random().Next(int.MaxValue - 100); // TODO: This is the correct line.
            seed = 27;



            //PerlinNoise2D perlinNoise2D = new PerlinNoise2D(_width, _height, 0.25f, 8, seed);

            //for (int y = 0; y < _height; y++)
            //    for (int x = 0; x < _width; x++)
            //        heightMap[y, x] = perlinNoise2D.getValue(x, y);



            // Create some seed points.  We'll do a %age of the passed in size (w*h).
            int[,] seedMap = new int[_height, _width];
            int numSeeds = (int)(0.05 * (_width * _height));
            List<Vector2> seeds = new List<Vector2>(numSeeds);
            float[] seedHeightMap = new float[numSeeds];
            MyRandom rnd = new MyRandom(seed);
            MyRandom rndHeight = new MyRandom(seed + 1);
            for (int s = 0; s < numSeeds; s++)
            {
                seeds.Add(new Vector2(rnd.Next(_width), rnd.Next(_height)));
                seedHeightMap[s] = (float)rndHeight.NextDouble();
            }



            // For each point, find the closest seed.
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    Vector2 tile = new Vector2(x, y);

                    int minIdx = -1;
                    float minDistance = float.MaxValue;
                    for (int s = 0; s < seeds.Count; s++)
                    {
                        float distance = Vector2.DistanceSquared(tile, seeds[s]);
                        if (distance >= minDistance)
                            continue;

                        minDistance = distance;
                        minIdx = s;
                    }

                    // Store the seed for this point.
                    seedMap[y, x] = minIdx;
                }



            // Lower the height of every seed that borders the map to 0.
            // TODO: Consider a method that increases chances to knock out seeds the closer to 
            //       the map edge.
#if false
            int skip = 1;
            for (int i = 0; i < (int)(0.2 * _width); i++)
            {
                for (int y = 0; y < _height; y += skip)
                {
                    seedHeightMap[seedMap[y, i]] = 0.0f;
                    //seedHeightMap[seedMap[y, (_width - 1) - i]] = 0.0f;
                    seedHeightMap[seedMap[y, (_width - 1) - (i * 3)]] = 0.0f;
                    seedHeightMap[seedMap[y, (_width - 1) - ((i * 3) + 1)]] = 0.0f;
                    seedHeightMap[seedMap[y, (_width - 1) - ((i * 3) + 2)]] = 0.0f;
                }

                //skip = Math.Max(skip * 2, 16);
                //skip += i;
                skip *= 2;
            }
#else
            // TODO: Maybe have X and not just Y factor in to avoid vertical continents.
            //       Also consider an axis other than _width/2, and differing axes for N and S
            //       hemispheres.
            MyRandom rndKeep = new MyRandom(seed + 2);
            for (int i = 0; i < _width; i++)
            {
                Dictionary<int, bool> seedsD = new Dictionary<int,bool>();
                for (int y = 0; y < _height; y++)
                {
                    if (seedsD.ContainsKey(seedMap[y, i]))
                        continue;

                    seedsD.Add(seedMap[y, i], true);
                }

                int distanceFromCenter = Math.Abs((_width / 2) - i);
                float chanceTossDivisor = 3.0f; // higher means more land
                float chanceToss = ((float)distanceFromCenter / ((float)_width / 2.0f)) / chanceTossDivisor;
                if ((i > (int)((float)_width * 0.65f)) || (i < (int)((float)_width * 0.20f)))
                    chanceToss *= 4;
                foreach (int seedKey in seedsD.Keys)
                {
                    if (rndKeep.NextDouble() < chanceToss)
                        seedHeightMap[seedKey] = 0.0f;
                }
            }
#endif
            for (int y = 0; y < _height; y++)
            {
                seedHeightMap[seedMap[y, 0]] = 0.0f;
                seedHeightMap[seedMap[y, _width - 1]] = 0.0f;
            }
            for (int x = 0; x < _width; x++)
            {
                seedHeightMap[seedMap[0, x]] = 0.0f;
                seedHeightMap[seedMap[_height - 1, x]] = 0.0f;
            }



            // For each point, find the closest seed.
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    if (seedHeightMap[seedMap[y, x]] == 0.0f)
                        continue;

                    Vector2 tile = new Vector2(x, y);

                    SortedList<float, int> closestSL = new SortedList<float, int>();
                    for (int s = 0; s < seeds.Count; s++)
                    {
                        float distance = Vector2.DistanceSquared(tile, seeds[s]);

                        // TODO: Handle duplicate distance keys how?
                        if (closestSL.ContainsKey(distance))
                            continue;

                        closestSL.Add(distance, s);
                    }

                    // TODO: VERY temporary, basically sets the height to the seed#.
                    //heightMap[y, x] = (float)minIdx / (float)numSeeds;

                    float totalDistance = 0.0f;
                    float[] dists = new float[4];
                    float[] heights = new float[4];
                    for (int i = 0; i < 4; i++)
                    {
                        float dist = closestSL.Keys[i];
                        float height = seedHeightMap[closestSL.Values[i]];

                        totalDistance += dist;
                        dists[i] = dist;
                        heights[i] = height;
                    }

                    // TODO: This formula is probably wrong -- doesn't it give greater weight to further distance?  
                    //       Also shouldn't it be height-centric rather than distance-centric?
                    heightMap[y, x] = ((dists[0] * heights[0]) + (dists[1] * heights[1]) + (dists[2] * heights[2]) + (dists[3] * heights[3])) / totalDistance;
                    if ((heightMap[y, x] < 0.0f) || (heightMap[y, x] > 1.0f))
                        System.Console.WriteLine("Height out of bounds!");
                }

            return heightMap;
        }

        public List<List<Point>> getRivers(float[,] heightMap)
        {
            if (heightMap == null)
                return null;

            //int seed = new System.Random().Next(100);
            //seed = 27;

            //System.Random rnd = new System.Random(seed);

            int heightMapHeight = heightMap.GetLength(0);
            int heightMapWidth = heightMap.GetLength(1);

            int minRiverLength = 2;
            minRiverLength = 4;

            List<List<Point>> rivers = new List<List<Point>>();

            int[,] riverDistance = new int[heightMapHeight, heightMapWidth];
            List<Point>[,] riverSources = new List<Point>[heightMapHeight, heightMapWidth];

            // Let's generate some rivers.
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    //if ((y == 35) && (x == 23))
                    //    seed = 28;

                    List<Point> river = new List<Point>();

                    //Point ptHighest = new Point();
                    //float highest = float.MinValue;
                    //for (int y = 0; y < _height; y++)
                    //    for (int x = 0; x < _width; x++)
                    //    {
                    //        if (heightMap[y, x] < highest)
                    //            continue;

                    //        highest = heightMap[y, x];

                    //        ptHighest.X = x;
                    //        ptHighest.Y = y;
                    //    }



                    //Point ptHighest;
                    //float highest = float.MinValue;
                    //int x1 = 0;
                    //int y1 = 0;
                    //while (highest <= 0.0f)
                    //{
                    //    x1 = rnd.Next(heightMapWidth);
                    //    y1 = rnd.Next(heightMapHeight);
                    //    highest = heightMap[y1, x1];
                    //}
                    //ptHighest = new Point(x1, y1);


                    Point ptHighest = new Point(x, y);
                    float highest = heightMap[ptHighest.Y, ptHighest.X];

                    float currentHeight = highest;
                    float mostDelta = float.MaxValue;
                    float delta = 0.0f;
                    Vector2 v2CurrentPoint = new Vector2((ptHighest.X * 2) + (((ptHighest.Y % 2) == 0) ? 0 : 1), ptHighest.Y);
                    Point ptHeightMap = new Point(ptHighest.X, ptHighest.Y);
                    Point ptHeightMapNew = new Point();
                    river.Add(new Point((int)v2CurrentPoint.X, (int)v2CurrentPoint.Y));
                    Vector2 v2Delta = Vector2.Zero;
                    Vector2 v2Delta2 = Vector2.Zero;
                    bool first = true;
                    Point ptRiverStart = new Point(ptHighest.X, ptHighest.Y);
                    int currentDistance = 0;

                    while (currentHeight > 0.0f)
                    {
                        // First, if we bump into a source then erase that river, this new one will win.
                        // TODO: More efficient to just paste on all of the points of that river (and
                        //       delete it) rather than re-run the river with this new one.  Or will
                        //       all of the river distances have to be re-run?
                        List<Point> existingRiver = riverSources[ptHeightMap.Y, ptHeightMap.X];
                        if (existingRiver != null)
                            rivers.Remove(existingRiver);

                        // Didn't bump into a source, but part of the river.  If the current distance
                        // is too short then we just won't let this fork of the river live.  It looks dumb if we do.
                        // TODO: Can't we just call the river done at this point?  Won't we be running on top
                        //       of the existing one?
                        if (riverDistance[ptHeightMap.Y, ptHeightMap.X] != 0)
                        {
                            if (currentDistance <= 2)
                                break;
                        }

                        mostDelta = float.MaxValue;
                        bool oddRow = ((ptHeightMap.Y % 2) != 0);

                        float adjLeft = 0.0f;
                        if (ptHeightMap.X > 0)
                            adjLeft = heightMap[ptHeightMap.Y, ptHeightMap.X - 1];

                        float adjRight = 0.0f;
                        if (ptHeightMap.X < (heightMapWidth - 1))
                            adjRight = heightMap[ptHeightMap.Y, ptHeightMap.X + 1];

                        float adjDownLeft = 0.0f;
                        if (ptHeightMap.Y < (heightMapHeight - 1))
                        {
                            if (oddRow)
                                adjDownLeft = heightMap[ptHeightMap.Y + 1, ptHeightMap.X];
                            else
                            {
                                if (ptHeightMap.X > 0)
                                    adjDownLeft = heightMap[ptHeightMap.Y + 1, ptHeightMap.X - 1];
                            }
                        }

                        float adjDownRight = 0.0f;
                        if (ptHeightMap.Y < (heightMapHeight - 1))
                        {
                            if (oddRow)
                            {
                                if (ptHeightMap.X < (heightMapWidth - 1))
                                    adjDownRight = heightMap[ptHeightMap.Y + 1, ptHeightMap.X + 1];
                            }
                            else
                                adjDownRight = heightMap[ptHeightMap.Y + 1, ptHeightMap.X];
                        }

                        float adjUpLeft = 0.0f;
                        if (ptHeightMap.Y > 0)
                        {
                            if (oddRow)
                                adjUpLeft = heightMap[ptHeightMap.Y - 1, ptHeightMap.X];
                            else
                            {
                                if (ptHeightMap.X > 0)
                                    adjUpLeft = heightMap[ptHeightMap.Y - 1, ptHeightMap.X - 1];
                            }
                        }

                        float adjUpRight = 0.0f;
                        if (ptHeightMap.Y > 0)
                        {
                            if (oddRow)
                            {
                                if (ptHeightMap.X < (heightMapWidth - 1))
                                    adjUpRight = heightMap[ptHeightMap.Y - 1, ptHeightMap.X + 1];
                            }
                            else
                                adjUpRight = heightMap[ptHeightMap.Y - 1, ptHeightMap.X];
                        }

                        float adjUpLeftLeft = 0.0f;
                        if (ptHeightMap.Y > 0)
                        {
                            if (oddRow)
                            {
                                if (ptHeightMap.X > 0)
                                    adjUpLeftLeft = heightMap[ptHeightMap.Y - 1, ptHeightMap.X - 1];
                            }
                            else
                            {
                                if (ptHeightMap.X > 1)
                                    adjUpLeftLeft = heightMap[ptHeightMap.Y - 1, ptHeightMap.X - 2];
                            }
                        }

                        // TODO: Not sure whether to keep this...
                        if ((adjLeft == 0.0f) || (adjUpLeft == 0.0f))
                        {
                            if (first)
                                break;
                        }
                        first = false;

                        // left
                        if (ptHeightMap.X > 0)
                        {
                            delta = adjLeft - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;
                                ptHeightMapNew = new Point(ptHeightMap.X - 1, ptHeightMap.Y);
                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if ((adjUpLeft != 0.0f) && (adjLeft != 0.0f))
                                {
                                    v2Delta = -Vector2.UnitX;
                                    if ((adjLeft != 0.0f) && (adjUpLeftLeft != 0.0f))
                                        v2Delta2 = -Vector2.UnitX;
                                }
                            }
                        }
                        // right
                        if (ptHeightMap.X < (heightMapWidth - 1))
                        {
                            delta = adjRight - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;
                                ptHeightMapNew = new Point(ptHeightMap.X + 1, ptHeightMap.Y);
                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if (adjUpLeft != 0.0f)
                                {
                                    v2Delta = Vector2.UnitX;
                                    if (adjUpRight != 0.0f)
                                        v2Delta2 = Vector2.UnitX;
                                }
                            }
                        }
                        // down (odd)
                        if ((ptHeightMap.Y < (heightMapHeight - 1)) /* && oddRow */)
                        {
                            delta = adjDownLeft - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;

                                if (oddRow)
                                    ptHeightMapNew = new Point(ptHeightMap.X, ptHeightMap.Y + 1);
                                else
                                    ptHeightMapNew = new Point(ptHeightMap.X - 1, ptHeightMap.Y + 1); // TODO: Bounds check on X.

                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if (adjLeft != 0.0f)
                                {
                                    v2Delta = Vector2.UnitY;
                                    if ((adjLeft != 0.0f) && (adjDownLeft != 0.0f))
                                        v2Delta2 = -Vector2.UnitX;
                                }
                            }
                        }
                        // down (even)
                        if ((ptHeightMap.Y < (heightMapHeight - 1)) /* && !oddRow */)
                        {
                            delta = adjDownRight - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;

                                if (!oddRow)
                                    ptHeightMapNew = new Point(ptHeightMap.X, ptHeightMap.Y + 1);
                                else
                                    ptHeightMapNew = new Point(ptHeightMap.X + 1, ptHeightMap.Y + 1); // TODO: Bounds check on X.

                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if (adjLeft != 0.0f)
                                {
                                    v2Delta = Vector2.UnitY;
                                    if (adjDownLeft != 0.0f)
                                        v2Delta2 = Vector2.UnitX;
                                }
                            }
                        }
                        // up (odd)
                        if ((ptHeightMap.Y > 0) /* && oddRow */)
                        {
                            delta = adjUpLeft - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;

                                if (oddRow)
                                    ptHeightMapNew = new Point(ptHeightMap.X, ptHeightMap.Y - 1);
                                else
                                    ptHeightMapNew = new Point(ptHeightMap.X - 1, ptHeightMap.Y - 1); // TODO: Bounds check on X.

                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if ((adjLeft != 0.0f) && (adjUpLeft != 0.0f))
                                {
                                    v2Delta = -Vector2.UnitX;
                                    if ((adjUpLeft != 0.0f) && (adjUpLeftLeft != 0.0f))
                                        v2Delta2 = -Vector2.UnitY;
                                }
                            }
                        }
                        // up (even)
                        if ((ptHeightMap.Y > 0) /* && !oddRow */)
                        {
                            delta = adjUpRight - currentHeight;
                            if (delta < mostDelta)
                            {
                                mostDelta = delta;

                                if (!oddRow)
                                    ptHeightMapNew = new Point(ptHeightMap.X, ptHeightMap.Y - 1);
                                else
                                    ptHeightMapNew = new Point(ptHeightMap.X + 1, ptHeightMap.Y - 1); // TODO: Bounds check on X.

                                v2Delta = Vector2.Zero;
                                v2Delta2 = Vector2.Zero;

                                if (adjUpLeft != 0.0f)
                                {
                                    v2Delta = Vector2.UnitX;
                                    if ((adjUpLeft != 0.0f) && (adjUpRight != 0.0f))
                                        v2Delta2 = -Vector2.UnitY;
                                }
                            }
                        }

                        if (mostDelta >= 0.0f)
                            break;

                        if (v2Delta != Vector2.Zero)
                        {
                            v2CurrentPoint += v2Delta;
                            river.Add(new Point((int)v2CurrentPoint.X, (int)v2CurrentPoint.Y));
                            currentDistance++;
                        }
                        if (v2Delta2 != Vector2.Zero)
                        {
                            v2CurrentPoint += v2Delta2;
                            river.Add(new Point((int)v2CurrentPoint.X, (int)v2CurrentPoint.Y));
                            currentDistance++;
                        }

                        riverDistance[ptHeightMap.Y, ptHeightMap.X] = currentDistance;
                        ptHeightMap = ptHeightMapNew;
                        currentHeight = heightMap[ptHeightMap.Y, ptHeightMap.X];
                    }
                    if ((currentHeight <= 0.0f) && (river.Count >= minRiverLength))
                    {
                        rivers.Add(river);

                        // Store the river in case we need to delete it later (as other ones lead into the source).
                        riverSources[ptRiverStart.Y, ptRiverStart.X] = river;
                    }
                }

            return rivers;
        }
    }
}
