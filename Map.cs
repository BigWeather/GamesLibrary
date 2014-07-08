using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    //public interface MapInterface<T>
    public interface MapInterface
    {
        Vector2 getDimensions();
        //T getTile(Vector2 position);
    }

    public interface MapConsumerInterface
    {
        //float getEdgeCost<R>(R requester, Map.MapNode start, Map.MapNode end);
        float getCost<R>(R requester, Map.MapNode start, Map.MapNode end);
        float getMovementModifier<R>(R requester, Map.MapNode node);
        int getOpacity(Map.MapNode node);
    }

    //public abstract class Map<T> : MapInterface<T>, PathFinderConsumerInterface<Map<T>.MapNode>
    public abstract class Map : MapInterface, PathFinderConsumerInterface<Map.MapNode>
    {
        public class MapNode
        {
            //public T mapData;

            //public Vector2 position
            //{
            //    get { return _position; }
            //}
            //private Vector2 _position;

            //public MapNode(Vector2 position)
            //{
            //    _position = position;
            //}
        }

        //protected Dictionary<Vector2, MapNode> _dictPathNodesByVector2 = new Dictionary<Vector2, MapNode>();
        protected MapConsumerInterface mci;

        public Map(MapConsumerInterface mci)
        {
            this.mci = mci;
        }

        // MapInterface methods below...
        public abstract Vector2 getDimensions();
        //public abstract T getTile(Vector2 position);
        //public abstract MapNode getMapNode(Vector2 positionWorldCoordinates);
        // MapInterface methods above...

        // PathFinderConsumerInterface methods below...
        public abstract float getHeuristic(MapNode start, MapNode end);
        public abstract List<MapNode> getConnectedNodes(MapNode node);
        public abstract float getCost<R>(R requester, MapNode start, MapNode end);
        // PathFinderConsumerInterface methods above...

        //public List<Vector2> getPath(object requester, Vector2 start, Vector2 end)
        //{
        //    return getPath(requester, getMapNode(start), getMapNode(end));
        //}

        //protected List<Vector2> getPath<R>(R requester, MapNode startNode, MapNode endNode)
        //{
        //    List<Vector2> pathV2 = new List<Vector2>();

        //    if ((startNode == null) || (endNode == null))
        //        return pathV2;

        //    AStarPathFinder<MapNode> astar = new AStarPathFinder<MapNode>(this);
        //    List<MapNode> path = astar.findPath(requester, startNode, endNode);
        //    foreach (MapNode pathNode in path)
        //        pathV2.Add(pathNode.position);

        //    return pathV2;
        //}
        protected List<MapNode> getPath<R>(R requester, MapNode startNode, MapNode endNode)
        {
            List<MapNode> path = new List<MapNode>();

            if ((startNode == null) || (endNode == null))
                return path;

            AStarPathFinder<MapNode> astar = new AStarPathFinder<MapNode>(this);
            path = astar.findPath(requester, startNode, endNode);

            return path;
        }
    }

    //public abstract class MapGrid<T> : Map<T>
    public abstract class MapGrid<T> : Map
    {
        public class RowColumn
        {
            public int row { get; private set; }
            public int column { get; private set; }

            public RowColumn(int row, int column)
            {
                this.row = row;
                this.column = column;
            }
        }

        // TODO: Remove foot, etc. references and just have MapGrid hang on to worldCoordinatesPerTile -- what those
        //       world coordinates represent (feet, miles, etc.) is a per-game attribute.
        // TODO: This should have a X and Y component, if not then what is REALLY the difference between this and
        //       MapSquare?!
        public Vector2 worldCoordinatesPerTile
        {
            get
            {
                return _worldCoordinatesPerTile;
            }
        }
        private Vector2 _worldCoordinatesPerTile;

        protected T[,] tiles
        {
            get
            {
                return _tiles;
            }
            set
            {
                _tiles = value;
            }
        }
        private T[,] _tiles;

        public int width
        {
            get
            {
                return _tiles.GetLength(1);
            }
        }

        public int height
        {
            get
            {
                return _tiles.GetLength(0);
            }
        }

        //protected Dictionary<Vector2, MapNode> _dictMapNodesByVector2 = new Dictionary<Vector2, MapNode>();
        protected MapNode[,] _mapNodes;
        protected Dictionary<MapNode, Vector2> _dictVector2sByMapNode = new Dictionary<MapNode, Vector2>();

        public MapGrid(T[,] tiles, Vector2 worldCoordinatesPerTile, MapConsumerInterface mci) : base(mci)
        {
            _tiles = tiles;
            _worldCoordinatesPerTile = worldCoordinatesPerTile;

            new VisibilityRequester();

            _mapNodes = new MapNode[this.height, this.width];

            for (int y = 0; y < this.height; y++)
                for (int x = 0; x < this.width; x++)
                {
                    //MapNode node = new MapNode(new Vector2(x, y, 0.0f));
                    MapNode node = new MapNode();
                    //node.mapData = this.tiles[y, x];
                    //_dictPathNodesByVector2.Add(node.position, node);
                    Vector2 position = new Vector2(x, y);
                    //_dictMapNodesByVector2.Add(position, node);
                    _mapNodes[y, x] = node;
                    _dictVector2sByMapNode.Add(node, position);
                }
        }

        public Vector2 getSideW(RowColumn rc, Side side)
        {
            Vector2 v2CenterW = getCenterW(rc.row, rc.column);
            RowColumn rcAdjacent = getConnectedRowColumn(rc, side);
            Vector2 v2AdjacentCenterW = getCenterW(rcAdjacent.row, rcAdjacent.column);
            return Vector2.Lerp(v2CenterW, v2AdjacentCenterW, 0.5f);
        }

        protected abstract int getRow(Vector2 worldPosition);
        protected abstract int getColumn(Vector2 worldPosition);
        public abstract Vector2 getCenterW(int row, int column);
        public abstract Point getULPixel(int row, int column);
        public abstract Vector2 getMaxW();
        public abstract Point getTileExtents();
        //public abstract Vector2 getDistance(int row, int column, int row2, int column2);
        public abstract int getTileRadius();
        public abstract int getTileSideLength();
        public abstract RowColumn getConnectedRowColumn(RowColumn rc, Side side);

        // MapInterface methods below...
        public override Vector2 getDimensions()
        {
            return new Vector2(this.width, this.height);
        }
        // MapInterface methods above...

        // PathFinderConsumerInterface methods below...
        public override float getCost<R>(R requester, MapNode start, MapNode end)
        {
            if (requester is VisibilityRequester)
                return 1.0f;

            return this.mci.getCost(requester, start, end);
        }
        // PathFinderConsumerInterface methods above...



        public class VisibilityRequester
        {
            public static VisibilityRequester Instance
            {
                get { return _visibilityRequester; }
            }
            private static VisibilityRequester _visibilityRequester;

            public VisibilityRequester()
            {
                _visibilityRequester = this;
            }
        }

        // This is expensive, cache it!
        private Map.MapNode _visibleNodesCacheOwner = null;
        private List<Map.MapNode> _visibleNodesCacheValue = null;
        public List<Map.MapNode> getVisibleNodes(Map.MapNode mnp, int visibilityRange)
        {
            MapGrid<T> _map = this;

            if (_visibleNodesCacheOwner == mnp)
                return _visibleNodesCacheValue;

            Vector2 v3SourceRC = _map.getNodeGridPosition(mnp);
            Vector2 v3SourcePos;
            if (!_map.gridToWorld(v3SourceRC, out v3SourcePos))
                return null;

            List<Map.MapNode> visibleNodes = new List<Map.MapNode>();
            visibleNodes.Add(mnp);

            int baseOpacity = this.mci.getOpacity(mnp);

            List<Map.MapNode> adjacentNodes = _map.getAdjacentNodes(mnp, visibilityRange);
            foreach (Map.MapNode adjacentNode in adjacentNodes)
            {
                Vector2 v3DestRC = _map.getNodeGridPosition(adjacentNode);
                Vector2 v3DestPos;
                if (!_map.gridToWorld(v3DestRC, out v3DestPos))
                    continue;

                List<Vector2> path = _map.getPath(VisibilityRequester.Instance, v3SourcePos, v3DestPos);
                int highestOpacityFound = -1;
                foreach (Vector2 v3PathRC in path)
                {
                    Vector2 v3PathPos = _map.gridToWorld(v3PathRC);
                    Map.MapNode mn = _map.getMapNode(v3PathPos);
                    int opacity = this.mci.getOpacity(mn);

                    // If we've already encountered more opaque (beyond the first allowed), bail.
                    if (highestOpacityFound >= opacity)
                    {
                        if (highestOpacityFound > baseOpacity)
                            continue;
                    }

                    // If this is ourselves then continue, we always want that visible (and not to set highestOpacityFound).
                    if (mn == mnp)
                        continue;

                    // If the opacity is higher, set found opacity higher so that this is the end of the processing (we can't
                    // see past more opaque things).
                    if (opacity > highestOpacityFound)
                        highestOpacityFound = opacity;

                    // This has already been added, bail.  Can't do earlier because we don't want an already visible
                    // node to prevent highestOpacityFound from being set.
                    if (visibleNodes.Contains(mn))
                        continue;

                    visibleNodes.Add(mn);
                }
            }

            _visibleNodesCacheOwner = mnp;
            _visibleNodesCacheValue = visibleNodes;

            return visibleNodes;
        }



        /// <summary>
        /// Determines if a given world coordinate is in bounds.
        /// </summary>
        /// <param name="worldPosition">Point representing the world coordinate.</param>
        /// <returns></returns>
        protected bool inBounds(Vector2 worldPosition)
        {
            Vector2 constrainedWorldPosition = getConstrainedWorldPosition(worldPosition);

            if ((constrainedWorldPosition.X != worldPosition.X) || (constrainedWorldPosition.Y != worldPosition.Y))
                return false;

            return true;
        }

        private Vector2 getConstrainedWorldPosition(Vector2 worldPosition)
        {
            Vector2 constrainedWorldPosition = new Vector2(worldPosition.X, worldPosition.Y);

            if (worldPosition.X < 0)
                constrainedWorldPosition.X = float.MinValue;
            if (worldPosition.Y < 0)
                constrainedWorldPosition.Y = float.MinValue;

            if ((constrainedWorldPosition.X == float.MinValue) && (constrainedWorldPosition.Y == float.MinValue))
                return constrainedWorldPosition;

            // TODO: Cache this?
            Vector2 maxWorldPosition = getMaxW();

            if (worldPosition.X >= maxWorldPosition.X)
                constrainedWorldPosition.X = float.MaxValue;
            if (worldPosition.Y >= maxWorldPosition.Y)
                constrainedWorldPosition.Y = float.MaxValue;

            return constrainedWorldPosition;
        }

        protected bool inBounds(int row, int column)
        {
            if ((row < 0) || (row >= this.height))
                return false;
            if ((column < 0) || (column >= this.width))
                return false;

            return true;
        }

        //public override T getTile(Vector2 position)
        public virtual T getTile(Vector2 gridPosition)
        {
            if (!inBounds((int)gridPosition.Y, (int)gridPosition.X))
                return default(T);

            return _tiles[(int)gridPosition.Y, (int)gridPosition.X];
        }

        public virtual T getTile(Map.MapNode mapNode)
        {
            return getTile(_dictVector2sByMapNode[mapNode]);
        }

        //public List<Vector2> getPath(object requester, Vector2 start, Vector2 end)
        //{
        //    return getPath(requester, getMapNode(start), getMapNode(end));
        //}

        public List<Vector2> getPath<R>(R requester, Vector2 start, Vector2 end)
        {
            List<MapNode> path = getPath(requester, getMapNode(start), getMapNode(end));

            List<Vector2> pathV2 = new List<Vector2>();
            foreach (MapNode pathNode in path)
                //pathV3.Add(pathNode.position);
                pathV2.Add(_dictVector2sByMapNode[pathNode]);

            return pathV2;
        }

        public virtual MapNode getMapNode(Vector2 positionWorldCoordinates)
        {
            if ((positionWorldCoordinates.X < 0) || (positionWorldCoordinates.Y < 0))
                return null;

            Vector2 positionTiles = worldToGrid(positionWorldCoordinates);

            //if (!_dictMapNodesByVector2.ContainsKey(positionTiles))
            //    return null;

            //return _dictMapNodesByVector2[positionTiles];
            return getMapNodeG(positionTiles);
        }

        public virtual MapNode getMapNodeG(Vector2 positionGridCoordinates)
        {
            int y = (int)positionGridCoordinates.Y;
            int x = (int)positionGridCoordinates.X;
            if (!inBounds(y, x))
                return null;

            return _mapNodes[y, x];
        }

        public float getMovementModifier<R>(R requester, Vector2 positionWorldCoordinates)
        {
            MapNode mapNode = getMapNode(positionWorldCoordinates);

            return this.mci.getMovementModifier(requester, mapNode);
        }

        public List<MapNode> getAdjacentNodes(MapNode node, int radius)
        {
            Vector2 v3 = _dictVector2sByMapNode[node];

            List<MapNode> adjacentNodes = new List<MapNode>();
            for (int x = (int)v3.X - radius; x <= (int)v3.X + radius; x++)
                for (int y = (int)v3.Y - radius; y <= (int)v3.Y + radius; y++)
                {
                    if ((x < 0) || (x >= this.width))
                        continue;
                    if ((y < 0) || (y >= this.height))
                        continue;

                    Vector2 v2test = new Vector2(x, y);

                    //if ((radius > 1) && (Vector2.Distance(v3, v2test) > (radius)))
                    //    continue;

                    //if ((radius == 1) && (Vector2.DistanceSquared(v3, v2test) > 2))
                    //    continue;

                    //float distance = getHeuristic(node, _dictMapNodesByVector2[v2test]);
                    float distance = getHeuristic(node, _mapNodes[(int)v2test.Y, (int)v2test.X]);
                    if ((radius > 1) && (radius < distance))
                        continue;
                    if ((radius == 1) && (radius < Math.Floor(distance)))
                        continue;

                    //adjacentNodes.Add(_dictMapNodesByVector2[v2test]);
                    adjacentNodes.Add(_mapNodes[(int)v2test.Y, (int)v2test.X]);
                }

            return adjacentNodes;
        }

#if false
        public Vector2 worldToGrid(Vector2 v3World)
        {
            return worldToGrid(v3World, true);
        }
        public Vector2 worldToGrid(Vector2 v3World, bool boundToMap)
        {
            float x = int.MinValue;
            float y = int.MinValue;

            if (v3World.X < 0)
            {
                if (boundToMap)
                    x = 0.0f;
            }
            else
            {
                //x = (int)(v3World.X / _worldCoordinatesPerTile.X);
                x = getColumn(new Point((int)v3World.X, (int)v3World.Y));
                if (x == int.MinValue) // TODO: Ick, this is because getColumn returns int.MinValue if not in bounds...
                    x = this.width;
                if (x >= this.width)
                {
                    if (boundToMap)
                        x = this.width - 1;
                    else
                        x = int.MaxValue;
                }
            }

            if (v3World.Y < 0)
            {
                if (boundToMap)
                    y = 0.0f;
            }
            else
            {
                //y = (int)(v3World.Y / _worldCoordinatesPerTile.Y);
                y = getRow(new Point((int)v3World.X, (int)v3World.Y));
                if (y == int.MinValue) // TODO: Ick, this is because getColumn returns int.MinValue if not in bounds...
                    y = this.height;
                if (y >= this.height)
                {
                    if (boundToMap)
                        y = this.height - 1;
                    else
                        y = int.MaxValue;
                }
            }

            return new Vector2(x, y);                
        }
#else
        public Vector2 worldToGrid(Vector2 worldPosition, bool boundToMap)
        {
            Vector2 gridPosition = worldToGrid(worldPosition);

            if (gridPosition.X == float.MinValue)
                gridPosition.X = 0;
            else if (gridPosition.X == float.MaxValue)
                gridPosition.X = this.width - 1;

            if (gridPosition.Y == float.MinValue)
                gridPosition.Y = 0;
            else if (gridPosition.Y == float.MaxValue)
                gridPosition.Y = this.height - 1;

            return gridPosition;
        }
        public Vector2 worldToGrid(Vector2 worldPosition)
        {
            Vector2 constrainedWorldPosition = getConstrainedWorldPosition(worldPosition);

            float x = constrainedWorldPosition.X;
            float y = constrainedWorldPosition.Y;
            if (constrainedWorldPosition.X == worldPosition.X)
                x = getColumn(worldPosition);
            if (constrainedWorldPosition.Y == worldPosition.Y)
                y = getRow(worldPosition);

            return new Vector2(x, y);
        }
#endif

#if false
        public bool worldToGrid(Vector2 v3World, out Vector2 v3Grid)
        {
            v3Grid = Vector2.Zero;

            if ((v3World.X < 0) || (v3World.Y < 0))
                return false;

            v3Grid = new Vector2((int)(v3World.X / _worldCoordinatesPerTile), (int)(v3World.Y / _worldCoordinatesPerTile), 0.0f);

            if ((v3Grid.X >= this.width) || (v3Grid.Y >= this.height))
            {
                v3Grid = Vector2.Zero;
                return false;
            }

            return true;
        }
#endif

        public Vector2 gridToWorld(Vector2 v2Grid)
        {
            Vector2 v2World;
            gridToWorld(v2Grid, out v2World);
            return v2World;
        }

        public bool gridToWorld(Vector2 gridPosition, out Vector2 worldPosition)
        {
            worldPosition = Vector2.Zero;

            if (!inBounds((int)gridPosition.Y, (int)gridPosition.X))
                return false;

            Vector2 centerWorldPosition = getCenterW((int)gridPosition.Y, (int)gridPosition.X);
            worldPosition = new Vector2(centerWorldPosition.X, centerWorldPosition.Y); 

            return true;
        }

        // TODO: Find a way to get rid of this.
        public Vector2 getNodeGridPosition(MapNode node)
        {
            return _dictVector2sByMapNode[node];
        }

        public MapNode getConnectedNode(MapNode node, Side side)
        {
            Vector2 v2 = _dictVector2sByMapNode[node];

            RowColumn rc = getConnectedRowColumn(new RowColumn((int)v2.Y, (int)v2.X), side);
            if (rc == null)
                return null;

            return _mapNodes[rc.row, rc.column];
        }
    }

    public class MapSquare<T> : MapGrid<T>
    {
        bool _allowDiagonalPathing;
        int _sidelen;
        int _halfsidelen;

        public MapSquare(T[,] tiles, bool allowDiagonalPathing, Vector2 worldCoordinatesPerTile, MapConsumerInterface mci): base(tiles, worldCoordinatesPerTile, mci) 
        {
            _allowDiagonalPathing = allowDiagonalPathing;
            _sidelen = (int)worldCoordinatesPerTile.X;
            _halfsidelen = (int)(worldCoordinatesPerTile.X / 2);
        }

        protected override int getRow(Vector2 worldPosition)
        {
            return (int)(worldPosition.Y / _sidelen);
        }

        protected override int getColumn(Vector2 worldPosition)
        {
            return (int)(worldPosition.X / _sidelen);
        }

        public override Vector2 getCenterW(int row, int column)
        {
            if (!inBounds(row, column))
                return new Vector2(int.MinValue, int.MinValue);

            return new Vector2((column * _sidelen) + _halfsidelen, (row * _sidelen) + _halfsidelen);
        }

        public override Point getULPixel(int row, int column)
        {
            if (!inBounds(row, column))
                return new Point(int.MinValue, int.MinValue);

            return new Point(column * _sidelen, row * _sidelen);
        }

        public override Vector2 getMaxW()
        {
            return new Vector2(this.width * _sidelen, this.height * _sidelen);
        }

        public override Point getTileExtents()
        {
            return new Point(_sidelen, _sidelen);
        }

        public override int getTileRadius()
        {
            return (_sidelen / 2);
        }

        public override int getTileSideLength()
        {
            return _sidelen;
        }

        public override RowColumn getConnectedRowColumn(RowColumn rc, Side side)
        {
            int y = rc.row;
            int x = rc.column;

            RowColumn connectedRC = null;

            switch (side)
            {
                case Side.South:
                    {
                        if (y < (this.height - 1)) // south
                            connectedRC = new RowColumn(y + 1, x);
                        break;
                    }
                case Side.East:
                    {
                        if (x < (this.width - 1)) // east
                            connectedRC = new RowColumn(y, x + 1);
                        break;
                    }
                case Side.North:
                    {
                        if (y > 0) // north
                            connectedRC = new RowColumn(y - 1, x);
                        break;
                    }
                case Side.West:
                    {
                        if (x > 0) // west
                            connectedRC = new RowColumn(y, x - 1);
                        break;
                    }
                case Side.SouthEast:
                    {
                        if ((y < (this.height - 1)) && (x < (this.width - 1))) // southeast
                            connectedRC = new RowColumn(y + 1, x + 1);
                        break;
                    }
                case Side.NorthEast:
                    {
                        if ((y > 0) && (x < (this.width - 1))) // northeast
                            connectedRC = new RowColumn(y - 1, x + 1);
                        break;
                    }
                case Side.NorthWest:
                    {
                        if ((y > 0) && (x > 0)) // northwest
                            connectedRC = new RowColumn(y - 1, x - 1);
                        break;
                    }
                case Side.SouthWest:
                    {
                        if ((y < (this.height - 1)) && (x > 0)) // southwest
                            connectedRC = new RowColumn(y + 1, x - 1);
                        break;
                    }
                default:
                    break;
            }

            return connectedRC;
        }

        // PathFinderConsumerInterface methods below...
        public override float getHeuristic(MapNode start, MapNode end)
        {
            //return Vector2.Distance(start.position, end.position);
            return Vector2.Distance(_dictVector2sByMapNode[start], _dictVector2sByMapNode[end]);
        }

        public override List<MapNode> getConnectedNodes(MapNode node)
        {
            //Vector2 v3 = node.position;
            Vector2 v2 = _dictVector2sByMapNode[node];

            //List<MapNode> connectedNodes = new List<MapNode>();
            //MapNode connectedNode;
            //if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitY, out connectedNode))
            //    connectedNodes.Add(connectedNode);
            //if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitX, out connectedNode))
            //    connectedNodes.Add(connectedNode);
            //if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitY, out connectedNode))
            //    connectedNodes.Add(connectedNode);
            //if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitX, out connectedNode))
            //    connectedNodes.Add(connectedNode);
            //if (_allowDiagonalPathing)
            //{
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitY + Vector2.UnitX, out connectedNode))
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitX - Vector2.UnitY, out connectedNode))
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitY - Vector2.UnitX, out connectedNode))
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitX + Vector2.UnitY, out connectedNode))
            //        connectedNodes.Add(connectedNode);
            //}

            int y = (int)v2.Y;
            int x = (int)v2.X;

            List<MapNode> connectedNodes = new List<MapNode>(_allowDiagonalPathing ? 8 : 4); // four or eight neighbors depending on whether diagonals allowed

            if (y < (this.height - 1)) // south
                connectedNodes.Add(_mapNodes[y + 1, x]);
            if (x < (this.width - 1)) // east
                connectedNodes.Add(_mapNodes[y, x + 1]);
            if (y > 0) // north
                connectedNodes.Add(_mapNodes[y - 1, x]);
            if (x > 0) // west
                connectedNodes.Add(_mapNodes[y, x - 1]);
            if (_allowDiagonalPathing)
            {
                if ((y < (this.height - 1)) && (x < (this.width - 1))) // southeast
                    connectedNodes.Add(_mapNodes[y + 1, x + 1]);
                if ((y > 0) && (x < (this.width - 1))) // northeast
                    connectedNodes.Add(_mapNodes[y - 1, x + 1]);
                if ((y > 0) && (x > 0)) // northwest
                    connectedNodes.Add(_mapNodes[y - 1, x - 1]);
                if ((y < (this.height - 1)) && (x > 0)) // southwest
                    connectedNodes.Add(_mapNodes[y + 1, x - 1]);
            }

            return connectedNodes;
        }
        // PathFinderConsumerInterface methods above...
    }

    public class MapHex<T> : MapGrid<T>
    {
        // Great documentation on hex maps: http://www.gamedev.net/page/resources/_/reference/programming/sweet-snippets/coordinates-in-hexagon-based-tile-maps-r1800.

        private Hexagon _hex;
        private enum SectionType { A, B };

        public MapHex(T[,] tiles, float sideLen, Vector2 worldCoordinatesPerTile, MapConsumerInterface mci)
            : base(tiles, worldCoordinatesPerTile, mci) 
        {
            _hex = new Hexagon((int)sideLen);
        }

        protected override int getRow(Vector2 worldPosition)
        {
            return (int)getHexGridPosition(worldPosition).Y;
        }

        protected override int getColumn(Vector2 worldPosition)
        {
            return (int)getHexGridPosition(worldPosition).X;
        }

        public override Vector2 getCenterW(int row, int column)
        {
            if (!inBounds(row, column))
                return new Vector2(int.MinValue, int.MinValue);

            int x = (column * _hex.a) + (((row % 2) == 0) ? 0 : _hex.r) + (_hex.width / 2);
            int y = (row * (_hex.h + _hex.s)) + (_hex.height / 2);

            return new Vector2(x, y);
        }

        public override Point getULPixel(int row, int column)
        {
            if (!inBounds(row, column))
                return new Point(int.MinValue, int.MinValue);

            // Find the absolute x, y of this hex.
            int x = (column * _hex.a) + (((row % 2) == 0) ? 0 : _hex.r);
            int y = row * (_hex.h + _hex.s);

            return new Point(x, y);
        }

        public override Vector2 getMaxW()
        {
            return new Vector2((this.width * _hex.width) + _hex.r, this.height * (_hex.h + _hex.s));
        }

        public override Point getTileExtents()
        {
            return new Point(_hex.width, _hex.height);
        }

        public override int getTileRadius()
        {
            return _hex.r;
        }

        public override int getTileSideLength()
        {
            return _hex.s;
        }

        public override RowColumn getConnectedRowColumn(RowColumn rc, Side side)
        {
            int y = rc.row;
            int x = rc.column;

            RowColumn connectedRC = null;

            switch (side)
            {
                case Side.East:
                    {
                        if (x < (this.width - 1)) // east
                            connectedRC = new RowColumn(y, x + 1);
                        break;
                    }
                case Side.West:
                    {
                        if (x > 0) // west
                            connectedRC = new RowColumn(y, x - 1);
                        break;
                    }
                case Side.SouthEast:
                    {
                        if ((y % 2) == 0) // even row
                        {
                            if (y < (this.height - 1)) // southeast
                                connectedRC = new RowColumn(y + 1, x);
                        }
                        else // odd row
                        {
                            if ((y < (this.height - 1)) && (x < (this.width - 1))) // southeast
                                connectedRC = new RowColumn(y + 1, x + 1);
                        }
                        break;
                    }
                case Side.NorthEast:
                    {
                        if ((y % 2) == 0) // even row
                        {
                            if (y > 0) // northeast
                                connectedRC = new RowColumn(y - 1, x);
                        }
                        else // odd row
                        {
                            if ((y > 0) && (x < (this.width - 1))) // northeast
                                connectedRC = new RowColumn(y - 1, x + 1);
                        }
                        break;
                    }
                case Side.NorthWest:
                    {
                        if ((y % 2) == 0) // even row
                        {
                            if ((y > 0) && (x > 0)) // northwest
                                connectedRC = new RowColumn(y - 1, x - 1);
                        }
                        else // odd row
                        {
                            if (y > 0) // northwest
                                connectedRC = new RowColumn(y - 1, x);
                        }
                        break;
                    }
                case Side.SouthWest:
                    {
                        if ((y % 2) == 0) // even row
                        {
                            if ((y < (this.height - 1)) && (x > 0)) // southwest
                                connectedRC = new RowColumn(y + 1, x - 1);
                        }
                        else // odd row
                        {
                            if (y < (this.height - 1)) // southwest
                                connectedRC = new RowColumn(y + 1, x);
                        }
                        break;
                    }
                default:
                    break;
            }

            return connectedRC;
        }
         
        // PathFinderConsumerInterface methods below...
        // TODO: Do proper for Hexes when get the time...
        public override float getHeuristic(MapNode start, MapNode end)
        {
            //return Vector2.Distance(start.position, end.position);
            //return Vector2.Distance(_dictVector2sByPathNode[start], _dictVector2sByPathNode[end]);
            Vector2 v2HStart = arrayToHex(_dictVector2sByMapNode[start]);
            Vector2 v2HEnd = arrayToHex(_dictVector2sByMapNode[end]);
            Vector2 v2HDiff = v2HEnd - v2HStart;
            if (Math.Sign(v2HDiff.X) == Math.Sign(v2HDiff.Y))
                return Math.Max(Math.Abs(v2HDiff.X), Math.Abs(v2HDiff.Y));
            return Math.Abs(v2HDiff.X) + Math.Abs(v2HDiff.Y);
        }

        private Vector2 arrayToHex(Vector2 v2Array)
        {
            float Yd2 = v2Array.Y / 2.0f;
            return new Vector2(v2Array.X - (int)Math.Floor(Yd2), v2Array.X + (int)Math.Ceiling(Yd2));
        }

        public override List<MapNode> getConnectedNodes(MapNode node)
        {
            //Vector2 v3 = node.position;
            Vector2 v2 = _dictVector2sByMapNode[node];

            //List<MapNode> connectedNodes = new List<MapNode>();
            //MapNode connectedNode;
            //if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitX, out connectedNode)) // east
            //    connectedNodes.Add(connectedNode);
            //if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitX, out connectedNode)) // west
            //    connectedNodes.Add(connectedNode);
            //if ((v2.Y % 2) == 0)
            //{
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitY - Vector2.UnitX, out connectedNode)) // southwest
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitY - Vector2.UnitX, out connectedNode)) // northwest
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitY, out connectedNode)) // northeast
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitY, out connectedNode)) // southeast
            //        connectedNodes.Add(connectedNode);
            //}
            //else
            //{
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitY, out connectedNode)) // southwest
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 - Vector2.UnitY, out connectedNode)) // northwest
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitX - Vector2.UnitY, out connectedNode)) // northeast
            //        connectedNodes.Add(connectedNode);
            //    if (_dictMapNodesByVector2.TryGetValue(v2 + Vector2.UnitX + Vector2.UnitY, out connectedNode)) // southeast
            //        connectedNodes.Add(connectedNode);
            //}

            int y = (int)v2.Y;
            int x = (int)v2.X;

            List<MapNode> connectedNodes = new List<MapNode>(6); // six sides on a hex, so six connected
            if (x < (this.width - 1)) // east
                connectedNodes.Add(_mapNodes[y, x + 1]);
            if (x > 0) // west
                connectedNodes.Add(_mapNodes[y, x - 1]);
            if ((y % 2) == 0) // even row
            {
                if ((y < (this.height - 1)) && (x > 0)) // southwest
                    connectedNodes.Add(_mapNodes[y + 1, x - 1]);
                if ((y > 0) && (x > 0)) // northwest
                    connectedNodes.Add(_mapNodes[y - 1, x - 1]);
                if (y > 0) // northeast
                    connectedNodes.Add(_mapNodes[y - 1, x]);
                if (y < (this.height - 1)) // southeast
                    connectedNodes.Add(_mapNodes[y + 1, x]);
            }
            else // odd row
            {
                if (y < (this.height - 1)) // southwest
                    connectedNodes.Add(_mapNodes[y + 1, x]);
                if (y > 0) // northwest
                    connectedNodes.Add(_mapNodes[y - 1, x]);
                if ((y > 0) && (x < (this.width - 1))) // northeast
                    connectedNodes.Add(_mapNodes[y - 1, x + 1]);
                if ((y < (this.height - 1)) && (x < (this.width - 1))) // southeast
                    connectedNodes.Add(_mapNodes[y + 1, x + 1]);
            }
            return connectedNodes;
        }
        // PathFinderConsumerInterface methods above...

        // TODO: Have getConnectedNodes, above, call this...
        public static List<RowColumn> getConnectedRowColumns(int row, int column, int height, int width)
        {
            int x = column;
            int y = row;

            List<RowColumn> connectedRowColumns = new List<RowColumn>(6); // six sides on a hex, so six connected
            if (x < (width - 1)) // east
                connectedRowColumns.Add(new RowColumn(y, x + 1));
            if (x > 0) // west
                connectedRowColumns.Add(new RowColumn(y, x - 1));
            if ((y % 2) == 0) // even row
            {
                if ((y < (height - 1)) && (x > 0)) // southwest
                    connectedRowColumns.Add(new RowColumn(y + 1, x - 1));
                if ((y > 0) && (x > 0)) // northwest
                    connectedRowColumns.Add(new RowColumn(y - 1, x - 1));
                if (y > 0) // northeast
                    connectedRowColumns.Add(new RowColumn(y - 1, x));
                if (y < (height - 1)) // southeast
                    connectedRowColumns.Add(new RowColumn(y + 1, x));
            }
            else // odd row
            {
                if (y < (height - 1)) // southwest
                    connectedRowColumns.Add(new RowColumn(y + 1, x));
                if (y > 0) // northwest
                    connectedRowColumns.Add(new RowColumn(y - 1, x));
                if ((y > 0) && (x < (width - 1))) // northeast
                    connectedRowColumns.Add(new RowColumn(y - 1, x + 1));
                if ((y < (height - 1)) && (x < (width - 1))) // southeast
                    connectedRowColumns.Add(new RowColumn(y + 1, x + 1));
            }

            return connectedRowColumns;
        }

        private Vector2 getHexGridPosition(Vector2 worldPosition)
        {
            int xSection = (int)(worldPosition.X / (2 * _hex.r));
            int ySection = (int)(worldPosition.Y / (_hex.h + _hex.s));

            int xSectionPixel = (int)(worldPosition.X % (2 * _hex.r));
            int ySectionPixel = (int)(worldPosition.Y % (_hex.h + _hex.s));

            SectionType sectionType = ((ySection % 2) == 0) ? SectionType.A : SectionType.B;

#if false
            int yArray = int.MinValue;
            int xArray = int.MinValue;
            if (sectionType == SectionType.A)
            {
                yArray = ySection;
                xArray = xSection;
                if (ySectionPixel < (_hex.h - xSectionPixel * _hex.m))
                {
                    yArray = ySection - 1;
                    xArray = xSection - 1;
                }
                if (ySectionPixel < (-_hex.h + xSectionPixel * _hex.m))
                {
                    yArray = ySection - 1;
                    xArray = xSection;
                }
            }
            else if (sectionType == SectionType.B)
            {
                if (xSectionPixel >= _hex.r)
                {
                    if (ySectionPixel < (2 * _hex.h - xSectionPixel * _hex.m))
                    {
                        yArray = ySection - 1;
                        //xArray = xSection - 1;
                        xArray = xSection;
                    }
                    else
                    {
                        yArray = ySection;
                        xArray = xSection;
                    }
                }
                if (xSectionPixel < _hex.r)
                {
                    if (ySectionPixel < (xSectionPixel * _hex.m))
                    {
                        yArray = ySection - 1;
                        xArray = xSection;
                    }
                    else
                    {
                        yArray = ySection;
                        xArray = xSection - 1;
                    }
                }
            }
#else
            int yArray = ySection;
            int xArray = xSection;
            if (sectionType == SectionType.A)
            {
                if (ySectionPixel < (_hex.h - xSectionPixel * _hex.m))
                {
                    yArray = ySection - 1;
                    xArray = xSection - 1;
                }
                else if (ySectionPixel < (-_hex.h + xSectionPixel * _hex.m))
                    yArray = ySection - 1;
            }
            else if (sectionType == SectionType.B)
            {
                if (xSectionPixel >= _hex.r)
                {
                    if (ySectionPixel < (2 * _hex.h - xSectionPixel * _hex.m))
                        yArray = ySection - 1;
                }
                else
                {
                    if (ySectionPixel < (xSectionPixel * _hex.m))
                        yArray = ySection - 1;
                    else
                        xArray = xSection - 1;
                }
            }
#endif

            return new Vector2(xArray, yArray);
        }
    }

    // TODO: Change this to internal or even private to HexMap at some point.
    public class Hexagon
    {
        public int width
        {
            get { return _a; }
        }

        public int height
        {
            get { return _b; }
        }

        public int s
        {
            get { return _s; }
        }
        private int _s;

        public int h
        {
            get { return _h; }
        }
        private int _h;

        public int r
        {
            get { return _r; }
        }
        private int _r;

        public int a
        {
            get { return _a; }
        }
        private int _a;

        public int b
        {
            get { return _b; }
        }
        private int _b;

        public double m
        {
            get { return _m; }
        }
        private double _m;

        public Hexagon(int s)
        {
            _s = s;

            double radians = (Math.PI * 30.0) / 180.0;
            double dh = (Math.Sin(radians) * _s);
            double dr = (Math.Cos(radians) * _s);
            double db = s + (2 * dh);
            double da = 2 * dr;

            _h = (int)(dh + 0.5);
            _r = (int)(dr + 0.5);
            _b = (int)(db + 0.5);
            _a = (int)(da + 0.5);

            _m = dh / dr;
        }
    }
}
