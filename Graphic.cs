using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endif

namespace GamesLibrary
{
    //public class Tile
    //{
    //    public Rectangle rectContentPos;
    //    public string texture;

    //    public Tile(int row, int column, int tilesize, string texture)
    //        : this(new Rectangle((column * (tilesize + 2)) + 1, (row * (tilesize + 2)) + 1, tilesize, tilesize), texture)
    //    {
    //    }

    //    public Tile(Rectangle rectContentPos, string texture)
    //    {
    //        this.rectContentPos = rectContentPos;
    //        this.texture = texture;
    //    }
    //}

    public class TextureManager
    {
        public static TextureManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TextureManager();

                return _instance;
            }
        }
        private static TextureManager _instance = null;
            
        Dictionary<string, object> _textureByName = new Dictionary<string, object>();

        public object getTexture(string name)
        {
            object texture;
            if (!_textureByName.TryGetValue(name, out texture))
                return null;
            return texture;
        }

        public void setTexture(string name, object texture)
        {
            if (!_textureByName.ContainsKey(name))
                _textureByName.Add(name, texture);
            else
                _textureByName[name] = texture;
        }
    }

    public class Graphic
    {
        #region public members
        public string identifier { get; protected set; } 
        public string texture { get; protected set; }
        public List<Animation> animations { get; protected set; }

        public Rectangle defaultBounds
        { 
            get 
            {
                if ((this.animations == null) || (this.animations.Count <= 0))
                    return default(Rectangle);

                if ((this.animations[0].frames == null) || (this.animations[0].frames.Count <= 0))
                    return default(Rectangle);

                return this.animations[0].frames[0].bounds; 
            } 
        }
        #endregion

        #region constructors
        public Graphic(string identifier, string texture, List<Animation> animations)
            : this(identifier, texture)
        {
            this.animations = animations;
        }

        protected Graphic(string identifier, string texture)
        {
            this.identifier = identifier;
            this.texture = texture;
        }
        #endregion

        #region public methods
        public Frame getCurrentFrame(GameTime gameTime, VariableBundle gameState)
        {
            if ((this.animations == null) || (this.animations.Count <= 0))
                return null;

            // Get the appropriate animation based on the conditions.
            foreach (Animation animation in this.animations)
            {
                if (!animation.isValid(gameState))
                    continue;

                return animation.getCurrentFrame(gameTime);
            }

            // Have to return something...
            // TODO: Consider just returning null?
            return this.animations[0].getCurrentFrame(gameTime);
        }

#if OLD_TEXTURE
        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, BaseGame baseGame, Rectangle destinationRectangle)
#else
        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, Rectangle destinationRectangle)
#endif
        {
#if false
            Frame frame = getCurrentFrame(gameTime, gameState);
#if OLD_TEXTURE
            Texture2D texture = baseGame.getTexture(this);
            spriteBatch.Draw(texture, destinationRectangle, frame.bounds, Color.White);
#else
            spriteBatch.DrawEx(this.texture, destinationRectangle, frame.bounds, Color.White);
#endif
#else
            Draw(gameTime, gameState, spriteBatch, destinationRectangle, Color.White);
#endif
        }

        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, Rectangle destinationRectangle, Color color)
        {
            Frame frame = getCurrentFrame(gameTime, gameState);
            spriteBatch.DrawEx(this.texture, destinationRectangle, frame.bounds, color);
        }

        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, Point destinationPoint)
        {
            Frame frame = getCurrentFrame(gameTime, gameState);
            spriteBatch.DrawEx(this.texture, new Rectangle(destinationPoint.X, destinationPoint.Y, frame.bounds.Width, frame.bounds.Height), frame.bounds, Color.White);
        }

        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, Rectangle destinationRectangle, Color color, float rotation, Vector2 origin, float layerDepth)
        {
            // NOTE: origin is in SOURCE (frame) coordinates!
            Frame frame = getCurrentFrame(gameTime, gameState);
            spriteBatch.DrawEx(this.texture, destinationRectangle, frame.bounds, color, rotation, origin, SpriteEffects.None, layerDepth);
        }
        #endregion
    }

    public class GraphicSimple : Graphic
    {
        #region constructors
        public GraphicSimple(string identifier, string texture, int row, int column, Rectangle tilesize)
            : this(identifier, texture, row, column, tilesize, new Point(0, 0)) { }

        public GraphicSimple(string identifier, string texture, int row, int column, Rectangle tilesize, Point anchor)
            : base(identifier, texture)
        {
            Rectangle bounds = Frame.getTileRectangle(row, column, tilesize);
            initialize(bounds, anchor);
        }

        public GraphicSimple(string identifier, string texture, Rectangle bounds) : this(identifier, texture, bounds, new Point(0, 0)) { }

        public GraphicSimple(string identifier, string texture, Rectangle bounds, Point anchor)
            : base(identifier, texture)
        {
            initialize(bounds, anchor);
        }
        #endregion

        #region private methods
        private void initialize(Rectangle bounds, Point anchor)
        {
            Frame frame = new Frame();
            frame.bounds = bounds;
            frame.anchor = anchor;
            List<Frame> frames = new List<Frame>();
            frames.Add(frame);

            Animation animation = new Animation(new List<Condition>(), Animation.Behavior.Static, frames);

            this.animations = new List<Animation>();
            this.animations.Add(animation);
        }
        #endregion
    }

    public class Animation
    {
        public enum Behavior { OneShot, ForwardLoop, ForwardAndBackLoop, Static, Random, RandomDifferent }

        #region public members
        public List<Condition> conditions
        {
            get { return _conditions; }
            set { _conditions = value; }
        }
        private List<Condition> _conditions;

        public Behavior behavior
        {
            get { return _behavior; }
            set { _behavior = value; }
        }
        private Behavior _behavior;

        public List<Frame> frames
        {
            get { return _frames; }
            //set { _frames = value; }
        }
        private List<Frame> _frames;
        #endregion

        #region private members
        private int _currentFrame = 0;
        private int _elapsedTimeCurrentFrame = 0;
        private bool _forward = true;
        private bool _stopped = false; // TODO: Honor _stopped.
        private System.Random _rnd = new System.Random();

        private static bool _conditionAND = true;
        #endregion

        #region constructors
        public Animation() { }
        public Animation(Condition condition, Behavior behavior, Frame frame) : this(new List<Condition>(new Condition[] { condition }), behavior, new List<Frame>(new Frame[] { frame })) { }
        public Animation(Condition condition, Behavior behavior, List<Frame> frames) : this(new List<Condition>(new Condition[] { condition }), behavior, frames) { }
        public Animation(List<Condition> conditions, Behavior behavior, Frame frame) : this(conditions, behavior, new List<Frame>(new Frame[] { frame })) { }
        public Animation(List<Condition> conditions, Behavior behavior, List<Frame> frames)
        {
            _conditions = conditions;
            _behavior = behavior;
            _frames = frames;
        }
        #endregion

        #region public methods
        public void play()
        {
            _stopped = false;
        }

        public void stop()
        {
            _stopped = true;
        }

        // TODO: Consider moving this somewhere (Condition?) and we need to implement some sort of "scoring" system --
        //       so that two conditions matching is better than just one (note in either case, a non-match is a non-starter),
        //       and even maybe some operations "mean" more than others.
        //       For example, if you have one Animation with a condition of A=0 and another with A=0 and B=0 then gameState
        //       with both A and B=0 should pick the second, not the first, animation, since it is a more exact match.  
        //       Also, a condition with A=5 and another with 2<A<6 should favor the second if A=5 in the gameState.
        public bool isValid(VariableBundle gameState)
        {
            if ((_conditions == null) || (_conditions.Count <= 0))
                return true;

            // AND
            if (Animation._conditionAND)
            {
                foreach (Condition condition in _conditions)
                {
                    if (condition.isValid(gameState))
                        continue;

                    return false;
                }

                return true;
            }

            // OR
            foreach (Condition condition in _conditions)
            {
                if (!condition.isValid(gameState))
                    continue;

                return true;
            }

            return false;
        }

        public Frame getCurrentFrame(GameTime gameTime)
        {
            if ((_frames == null) || (_frames.Count <= 0))
                return null;

            if (_frames.Count == 1)
                return _frames[0];

            int msElapsed = gameTime.ElapsedGameTime.Milliseconds;
            int msLeftCurrentFrame = _frames[_currentFrame].duration - _elapsedTimeCurrentFrame;

            if (msLeftCurrentFrame > msElapsed)
            {
                // If the time left (duration - elapsed) on the current frame exceeds the time since last call let's just
                // increment the time elapsed for the frame (essentially, lowering the time left for the frame).
                _elapsedTimeCurrentFrame += msElapsed;
            }
            else
            {
                // Time left (duration - elapsed) on the current frame was less than the time since last call -- so we know
                // we need to advance at least one frame.

                int msLeftTotal = msElapsed;

                // First, subtract the duration left from the elapsed time.
                msLeftTotal -= msLeftCurrentFrame;

                // While elapsed is positive keep getting next frames...
                int frameIdx = _currentFrame;
                while (msLeftTotal > 0)
                {
                    // Get the next frame.
                    frameIdx = getNextFrame();

                    // Subtract the duration from the time left.
                    msLeftTotal -= _frames[frameIdx].duration;
                }

                _currentFrame = frameIdx;
                _elapsedTimeCurrentFrame = _frames[_currentFrame].duration - Math.Abs(msLeftTotal);
            }

            return _frames[_currentFrame];
        }
        #endregion

        #region private methods
        private int getNextFrame()
        {
            if ((_frames == null) || (_frames.Count <= 0))
                return int.MinValue;

            if (_frames.Count == 1)
                return 0;

            int nextFrame = 0;

            switch (_behavior)
            {
                case Behavior.ForwardAndBackLoop:
                    {
                        if (_forward && (_currentFrame == (_frames.Count - 1)))
                            _forward = false;
                        else if (!_forward && (_currentFrame == 0))
                            _forward = true;

                        if (_forward)
                            nextFrame = (_currentFrame + 1) % _frames.Count;
                        else
                            nextFrame = Math.Max(0, _currentFrame - 1);

                        break;
                    }
                case Behavior.ForwardLoop:
                    {
                        nextFrame = (_currentFrame + 1) % _frames.Count;
                        break;
                    }
                case Behavior.OneShot:
                    {
                        nextFrame = Math.Min(_currentFrame + 1, _frames.Count - 1);
                        break;
                    }
                case Behavior.Random:
                    {
                        nextFrame = _rnd.Next(0, _frames.Count);
                        break;
                    }
                case Behavior.RandomDifferent:
                    {
                        nextFrame = _currentFrame;
                        while (nextFrame == _currentFrame)
                            nextFrame = _rnd.Next(0, _frames.Count);
                        break;
                    }
                case Behavior.Static:
                    {
                        nextFrame = _currentFrame;
                        break;
                    }
                default:
                    {
                        nextFrame = _currentFrame;
                        break;
                    }
            }

            return nextFrame;
        }
        #endregion
    }

    public class Frame
    {
        public Rectangle bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }
        private Rectangle _bounds;

        public Point anchor
        {
            get { return _anchor; }
            set { _anchor = value; }
        }
        private Point _anchor;

        public int duration
        {
            get 
            { 
                return Math.Max(0, _duration); 
            }
            //set { _duration = value; }
        }
        private int _duration;

        public Color color
        {
            get { return _color; }
            set { _color = value; }
        }
        private Color _color = Color.White;

        public Frame() { }

        public Frame(int row, int column, Rectangle tilesize) : this(getTileRectangle(row, column, tilesize), new Point(0, 0), -1) { }

        public Frame(int row, int column, Rectangle tilesize, Point anchor, int duration) : this(getTileRectangle(row, column, tilesize), anchor, duration) { }

        public Frame(Rectangle bounds, Point anchor, int duration) : this(bounds, anchor, duration, Color.White) { }

        public Frame(Rectangle bounds, Point anchor, int duration, Color color)
        {
            _bounds = bounds;
            _anchor = anchor;
            _duration = duration;
            _color = color;
        }

        internal static Rectangle getTileRectangle(int row, int column, Rectangle tilesize)
        {
            return new Rectangle((column * (tilesize.Width + 2)) + 1, (row * (tilesize.Height + 2)) + 1, tilesize.Width, tilesize.Height);
        }
    }
}
