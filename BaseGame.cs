using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endif

namespace GamesLibrary
{
    public abstract class BaseGame
    {
        public GraphicsDevice GraphicsDevice;
        public GraphicsDeviceManager graphics;
        public ContentManager Content;

        private Stack<Window> _windowStack = new Stack<Window>();
        public Vector2 mousePos = new Vector2(0, 0);
#if OLD_TEXTURE
        protected Dictionary<string, Texture2D> dictTextures = new Dictionary<string, Texture2D>();
#endif
        public Dictionary<string, Graphic> dictGraphics = new Dictionary<string,Graphic>();
#if OLD_TEXTURE
        public Texture2D tx2dSmallSquare;
#endif
        public SpriteFont miramonte;
        public Rectangle rectTileSafeArea;
        public VariableBundle gameState = new VariableBundle();

        public int frameRate { get; private set; }
        private int _frameCounter = 0;
        private TimeSpan _elapsedTime = TimeSpan.Zero;

        public System.Random rnd = new System.Random();

        public bool exit
        {
            get { return _exit; }
        }
        private bool _exit = false; // If set to true, game will exit.

        public void quit()
        {
            _exit = true;
        }

        public void InitializeGraphicsEx()
        {
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            rectTileSafeArea = GraphicsDevice.Viewport.TitleSafeArea;

            InitializeGraphics();
        }
        public abstract void InitializeGraphics();

        public void InitializeEx()
        {
            Initialize();
        }
        public abstract void Initialize();

        public void LoadContentEx()
        {
            // Load textures
#if OLD_TEXTURE
            tx2dSmallSquare = Content.Load<Texture2D>(@"SmallSquare");
#else
            this.loadTexture("SmallSquare", @"SmallSquare");
#endif

            // Load fonts
            miramonte = Content.Load<SpriteFont>(@"Miramonte");

#if OLD_TEXTURE
            // Store textures in a dictionary
            dictTextures.Add("SmallSquare", tx2dSmallSquare);
#endif

            LoadContent();

            PostLoadContent();
        }
        public abstract void LoadContent();

        /// <summary>
        /// Called after loading of the content, last thing done before the game transitions to the 
        /// handle input / draw loop.
        /// </summary>
        public abstract void PostLoadContent();

        public void UnloadContentEx()
        {
            UnloadContent();
        }
        public abstract void UnloadContent();

        public void UpdateEx(GameTime gameTime)
        {
            if (gameTime.IsRunningSlowly)
                System.Diagnostics.Debug.WriteLine("IsRunningSlowly is true in Update()! (" + gameTime.TotalGameTime.Ticks + ")");

            _elapsedTime += gameTime.ElapsedGameTime;
            if (_elapsedTime > TimeSpan.FromSeconds(1))
            {
                _elapsedTime -= TimeSpan.FromSeconds(1);
                this.frameRate = _frameCounter;
                _frameCounter = 0;
            }

#if WINDOWS
            MouseState ms = Mouse.GetState();
            this.mousePos.X = ms.X;
            this.mousePos.Y = ms.Y;
#endif

            // Have the current window handle input.
            _windowStack.Peek().HandleInputEx(gameTime);

            // Update the game state.
            UpdateGameState(gameTime);
        }
        public abstract void UpdateGameState(GameTime gameTime);

        /// <summary>
        /// Base drawing routine, not to be called except by Game.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="spriteBatch"></param>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            _frameCounter++;

            // TODO: If any Window obscures completely one(s) below it then some Windows don't need to draw.

            // Must let all the windows on the stack draw, from back to front.
            List<Window> windows = _windowStack.ToList();
            windows.Reverse();
            windows.ForEach(w => w.DrawEx(gameTime, gameState, spriteBatch, GraphicsDevice));

#if WINDOWS
            // Draw mouse cursor.  This is done last to ensure that it draws on top of everything else.
            Graphic mousePointer = getMousePointerGraphic(this.mousePos);
            if (mousePointer != null)
            {
                spriteBatch.Begin();
                Frame mousePointerFrame = mousePointer.getCurrentFrame(gameTime, gameState);
#if OLD_TEXTURE
                spriteBatch.Draw(getTexture(mousePointer), new Rectangle((int)this.mousePos.X, (int)this.mousePos.Y, mousePointerFrame.bounds.Width, mousePointerFrame.bounds.Height), mousePointerFrame.bounds, Color.White);
#else
                mousePointer.Draw(gameTime, gameState, spriteBatch, new Point((int)this.mousePos.X, (int)this.mousePos.Y));
#endif
                spriteBatch.End();
            }
#endif
        }

        /// <summary>
        /// Show a Window.
        /// </summary>
        /// <param name="window"></param>
        public void Show(Window window)
        {
            _windowStack.Push(window);
        }

        /// <summary>
        /// Hides a Window.  NOTE: Currently only will hide the top-most Window.
        /// </summary>
        /// <param name="window"></param>
        public void Hide(Window window)
        {
            // TODO: Consider allowing hide of non-top-most Window?  Would messing with the display stack be prudent?

            if (window != _windowStack.Peek())
                return;

            _windowStack.Pop();
        }

#if OLD_TEXTURE
        public Texture2D getTexture(Graphic graphic)
        {
            return this.dictTextures[graphic.texture];
        }
#endif

#if OLD_TEXTURE
        public void loadTexture(string identifier, string assetName)
        {
            Texture2D tx2d = this.Content.Load<Texture2D>(@assetName);

#if OLD_TEXTURE
            // Store textures in a dictionary
            this.dictTextures.Add(identifier, tx2d);
#endif

            TextureManager.Instance.setTexture(identifier, tx2d);
        }
#endif

        public void addGraphic(Graphic graphic)
        {
            this.dictGraphics.Add(graphic.identifier, graphic);
        }

        protected abstract Graphic getMousePointerGraphic(Vector2 mousePos);
    }
}
