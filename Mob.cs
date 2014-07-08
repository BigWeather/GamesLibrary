using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    public class Mob : MapObject
    {
        public enum Behavior
        {
            Follow, Flee, Patrol, None, MoveTo, Explore, Wander
        }

        //public string text
        //{
        //    get
        //    {
        //        return _text;
        //    }
        //}
        //private string _text;

        public float speed // world coordinates per millisecond
        {
            get
            {
                return _speed;
            }
        }
        private float _speed;

        public double facing
        {
            get
            {
                return getFacing();
            }
        }

        public List<Vector2> path
        {
            get
            {
                return _path;
            }
        }
        public List<Vector2> _path;

        private Vector2 _posDest;
        private Vector2 _posOld;

        private Mob _target;

        public Behavior behavior
        {
            get { return _behavior; }
        }
        private Behavior _behavior;

        public Map.MapNode currentMapNode { get; private set; }
        public List<Map.MapNode> visibleMapNodes { get; protected set; }

        public bool notesLandmarks = false; // does this map object note landmarks?
        public HashSet<MapObject> notedLandmarks = new HashSet<MapObject>(); // landmarks that have been noted by the map object

        private bool initialized = false;
        private System.Random _rnd = new System.Random();

        public Mob(string text, Vector2 pos, float speed) : base(text, pos)
        {
            _speed = speed;

            _posOld = pos;

            this.visibleMapNodes = new List<Map.MapNode>();

            this.mobile = true;
        }

        // Public methods
        #region Public_methods
        public override void process<T>(VariableBundle gameState, MapGrid<T> map)
        {
            if (isDead())
                return;

            // TODO: Not sure I can remove this, but need to for the AI to work...
#if false
            if (this.behavior == Behavior.MoveTo)
                return;
#endif

            base.process(gameState, map);
        }

        public void moveDirection<T>(MapGrid<T> map, Vector2 v2Direction, double milliseconds)
        {
            if (v2Direction == Vector2.Zero)
                return;

            float effectiveSpeed = getEffectiveSpeed(map);
            if (effectiveSpeed == 0.0f)
                return;

            v2Direction.Normalize();
            Vector2 v2Move = v2Direction * (float)milliseconds * effectiveSpeed;

            _posDest = this.positionWorld + v2Move;

            Map.MapNode currentMapNode = map.getMapNode(this.positionWorld);
            if (currentMapNode == null)
                return;

            Map.MapNode mapNode = map.getMapNode(this.positionWorld + v2Move);
            if (mapNode == null)
                return;

            if (map.getCost(this, currentMapNode, mapNode) == float.MaxValue)
                return;

            if (currentMapNode != mapNode)
                mapNodeChanged(currentMapNode, mapNode);

            move(this.positionWorld + v2Move);
        }

        public void update<T>(MapGrid<T> map, double milliseconds)
        {
            // TODO: Yuck, better way?
            if (!initialized)
            {
                Map.MapNode mn = map.getMapNode(this.positionWorld);
                mapNodeChanged(mn, mn);
                initialized = true;
            }

            Vector2 destination = Vector2.Zero;

            // TODO: Explore must be able to handle sailing!

#if !OLD_AI
            if ((_behavior == Behavior.Explore) || (_behavior == Behavior.Wander) || (_behavior == Behavior.Patrol))
            {
                if ((_target != null) && (Vector2.Distance(_target.positionWorld, this.positionWorld) < 50.0f)) // TODO: Hard coded, ick.
                    _target = null;

                if (_target == null)
                {
                    // TODO: Do better here...
                    Map.MapNode currentMapNode = map.getMapNode(this.positionWorld);
                    List<Map.MapNode> connectedWalkableNodes = map.getConnectedNodes(currentMapNode).Where(mapnode => map.getCost(this, currentMapNode, mapnode) != float.MaxValue).ToList();
                    if (connectedWalkableNodes.Count > 0)
                    {
                        Map.MapNode targetMapNode = null;
                        if (_behavior == Behavior.Explore)
                        {
                            // TODO: Eventually do something interesting here.
                            targetMapNode = connectedWalkableNodes[_rnd.Next(connectedWalkableNodes.Count)];
                        }
                        else
                        {
                            targetMapNode = connectedWalkableNodes[_rnd.Next(connectedWalkableNodes.Count)];
                        }
                        _target = new Mob("dummy", map.gridToWorld(map.getNodeGridPosition(targetMapNode)), 0.0f);
                    }
                }
            }
#endif

            if (_target != null)
            {
                destination = _target.positionWorld;
                if (_behavior == Behavior.Flee)
                    destination = this.positionWorld + (-(_target.positionWorld - this.positionWorld));
            }

            // TODO: Respect buffers a MUCH better way...
            if ((_behavior == Behavior.Follow) && (_target != null))
            {
                if (Vector2.Distance(_target.positionWorld, this.positionWorld) < 150) // TODO: Hard coded!
                    destination = this.positionWorld + (-(_target.positionWorld - this.positionWorld));
            }

            _path = map.getPath(this, this.positionWorld, destination);

            Vector2 v2Mob = Vector2.Zero;
            if (_path.Count == 0) // no path, return
                return;
            if (_path.Count == 1) // in same hex, use destination directly
                v2Mob = destination - this.positionWorld;
            if (_path.Count >= 2) // in different hex, use next point of path as destination
                v2Mob = map.gridToWorld(_path[1]) - this.positionWorld;

            moveDirection(map, v2Mob, milliseconds);
        }

        //public virtual void mapNodeChanged<T>(MapGrid<T> map, Map.MapNode oldMapNode, Map.MapNode newMapNode)
        public virtual void mapNodeChanged(Map.MapNode oldMapNode, Map.MapNode newMapNode)
        {
            this.currentMapNode = newMapNode;
            //this.visibleMapNodes = map.getVisibleNodes(newMapNode, 4); // TODO: Fix, get real visibility!!!
        }

        public void follow(Mob target)
        {
            if (target == null)
                return;

            _behavior = Behavior.Follow;
            _target = target;
        }

        public void flee(Mob target)
        {
            if (target == null)
                return;

            _behavior = Behavior.Flee;
            _target = target;
        }

        public void stop()
        {
            _behavior = Behavior.None;
            _target = null;
        }

        public void patrol()
        {
            // TODO: How to do this?
            if (_behavior != Behavior.Patrol)
            {
                _behavior = Behavior.Patrol;
                _target = null;
            }
        }

        public void moveTo(Vector2 v2Location)
        {
            // TODO: How to do this?
            _behavior = Behavior.MoveTo;
            _target = new Mob("dummy", v2Location, 0.0f);
        }

        public void explore()
        {
            // TODO: How to do this?
            if (_behavior != Behavior.Explore)
            {
                _behavior = Behavior.Explore;
                _target = null;
            }
        }

        public void wander()
        {
            // TODO: How to do this?
            if (_behavior != Behavior.Wander)
            {
                _behavior = Behavior.Wander;
                _target = null;
            }
        }

        public void position(Vector2 v2Location)
        {
            //_behavior = Behavior.None;
            //_target = null;
            stop();

            // TODO: Call mapNodeChanged (somehow)?

            this.positionWorld = v2Location;
        }

        public override string getGraphicIdentifier()
        {
            return null;
        }

        /// <summary>
        /// Returns whether or not the Mob is dead.
        /// </summary>
        /// <returns></returns>
        public virtual bool isDead()
        {
            return false;
        }
        #endregion

        #region Protected_methods
        protected override List<Command> getCommands()
        {
            return null;
        }
        #endregion

        // Private methods
        #region Private_methods
        private void move(Vector2 posNew)
        {
            //if (_posOld == posNew)
            //    return;

            if (_posOld != posNew)
                _posOld = this.positionWorld;
            this.positionWorld = posNew;
        }

        private float getEffectiveSpeed<T>(MapGrid<T> map)
        {
            //Terrain terrain = map.getTile(this.pos);
            //float modifier = map.getMovementModifier(this.pos);
            //Map.MapNode mapNode = map.getMapNode(this.pos);
            //if (mapNode == null)
            //    return 0.0f;

            float modifier = map.getMovementModifier(this, this.positionWorld);
            //if (modifier == int.MaxValue)
            //    return 0.0f;

            return speed * modifier;
        }

        private double getFacing()
        {
            Vector2 v2Dir = this.positionWorld - _posOld;
            v2Dir.Normalize();
            float dot = Vector2.Dot(v2Dir, -Vector2.UnitY);
            double angle = Math.Acos(dot);
            if (double.IsNaN(angle))
                return 0;
            if (v2Dir.X < 0.0f)
                angle *= -1.0f;
            return angle;
        }
        #endregion
    }
}
