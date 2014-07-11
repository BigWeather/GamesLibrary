using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endif

namespace GamesLibrary
{
    public class WindowBackground
    {
        public enum Style { Tiled, Centered, Stretched }

        public Graphic background { get; set; }
        public Style style { get; set; }
    }

    public class WindowDecorations
    {
        public Graphic cornerNW { get; set; }
        public Graphic cornerNE { get; set; }
        public Graphic cornerSE { get; set; }
        public Graphic cornerSW { get; set; }
        public Graphic sideN { get; set; }
        public Graphic sideE { get; set; }
        public Graphic sideS { get; set; }
        public Graphic sideW { get; set; }
    }

    public class WindowEffect
    {
        public Graphic graphic { get; set; }
        public int msDuration { get; set; }
        public Vector2 v2Start { get; set; }
        public Vector2 v2End { get; set; }

        private bool _started
        {
            get
            {
                return (_ticksStart != -1);
            }
        }

        private long _ticksStart = -1;

        private void start(GameTime gameTime)
        {
            _ticksStart = gameTime.TotalGameTime.Ticks;
        }

        internal int elapsed(GameTime gameTime)
        {
            if (!_started)
                start(gameTime);

            long elapsedTicks = gameTime.TotalGameTime.Ticks - _ticksStart;

            return (int)((float)elapsedTicks / (float)TimeSpan.TicksPerMillisecond);
        }
    }

    public abstract class Window
    {
        public Rectangle bounds 
        { 
            get { return _bounds; } 
            protected set
            {
                _bounds = value;
                onSetBounds(); 
            }
        }
        private Rectangle _bounds;

        // TODO: Do we need this AND center below?
        public Vector2 v2Center 
        { 
            get { return _v2Center; }
            protected set { _v2Center = value; }
        }
        private Vector2 _v2Center = default(Vector2);

        protected Vector2 pointerPosWindow { get; private set; }

        protected Hotspot primaryFocusHotspot { get; private set; }
        protected Hotspot secondaryFocusHotspot { get; private set; }
        protected Hotspot pointerOverHotspot { get; private set; }

        protected Matrix mxTWindowToScreen;
        protected Matrix mxTIScreenToWindow;

        public WindowDecorations decorations { get; set; }
        public WindowBackground background { get; set; }
        public List<WindowEffect> effects { get; private set; }

        public BaseGame baseGame { get; private set; } // TODO: Ick, find a way to nuke this...

        public Vector2 center
        {
            get
            {
                Point rectCenter = this.bounds.Center;
                return new Vector2(rectCenter.X - this.bounds.X, rectCenter.Y - this.bounds.Y);
            }
        }

        public UIObjectManager uiObjectManager { get; private set; }

        protected List<Hotspot> hotspots { get; set; }

        public static RasterizerState rasterizerState = new RasterizerState() { ScissorTestEnable = true }; // TODO: Ick, public!

        protected int textTopBuffer = 10;
        protected int buttonBottomBuffer = 10;
        protected int betweenBuffer = 10;
        protected int leftBuffer = 10;
        protected int rightBuffer = 10;

        public Window(Vector2 v2Center, BaseGame baseGame)
            : this(new Rectangle(0, 0, 0, 0), baseGame)
        {
            this.v2Center = v2Center;
            //onSetBounds();
        }

        public Window(Rectangle bounds, BaseGame baseGame)
        {
            this.bounds = bounds;
            this.baseGame = baseGame;

            this.uiObjectManager = new UIObjectManager();

            initializeTransformMatrices();

            this.pointerPosWindow = new Vector2(0, 0);

            this.hotspots = new List<Hotspot>();

            this.effects = new List<WindowEffect>();
        }

        private void onSetBounds()
        {
            if (v2Center != default(Vector2))
                _bounds = new Rectangle((int)v2Center.X - (this.bounds.Width / 2), (int)v2Center.Y - (this.bounds.Height / 2), this.bounds.Width, this.bounds.Height);

            initializeTransformMatrices();
        }

        private void initializeTransformMatrices()
        {
            this.mxTWindowToScreen = Matrix.Identity;
            this.mxTWindowToScreen *= Matrix.CreateTranslation(new Vector3(bounds.X, bounds.Y, 0.0f));

            this.mxTIScreenToWindow = Matrix.Invert(this.mxTWindowToScreen);
        }

        private bool _primaryPreviouslySelected = false;
        private bool _secondaryPreviouslySelected = false;

        public void HandleInputEx(GameTime gameTime)
        {
            //HandleInput(gameTime);

            bool primarySelected = false;
            bool secondarySelected = false;

#if WINDOWS
            MouseState ms = Mouse.GetState();
            this.pointerPosWindow = screenToWindow(new Vector2(ms.X, ms.Y));
            primarySelected = (ms.LeftButton == ButtonState.Pressed);
            secondarySelected = (ms.RightButton == ButtonState.Pressed);
#endif

            Hotspot hotspot = hotspots.OrderByDescending(hs => hs.zorder).FirstOrDefault(hs => hs.contains(this.pointerPosWindow));
            if (hotspot != default(Hotspot))
            {
                // PrimaryFocus
                if (primarySelected && !_primaryPreviouslySelected)
                {
                    // If nothing else has primary focus let this hotspot have primary focus.
                    if (this.primaryFocusHotspot == null)
                    {
                        this.primaryFocusHotspot = hotspot;
                        hotspot.primaryFocus();
                    }
                }

                // PrimaryActivation
                if (!primarySelected && _primaryPreviouslySelected)
                {
                    // If this hotspot has primary focus then the release of the primary
                    // constitutes the completion of getting this hotspot selected.
                    if (this.primaryFocusHotspot == hotspot)
                        hotspot.primaryActivation();

                    // If this hotspot's parent has primary focus then the release of the primary
                    // constitutes the completion of getting this hotspot selected.  This is the
                    // click hold on parent, mouse over child, and release on child interaction.
                    if ((hotspot.parentHotspot != null) && (this.primaryFocusHotspot == hotspot.parentHotspot))
                        hotspot.primaryActivation();

                    // Regardless, no hotspot now has primary focus.
                    //this.primaryFocusHotspot = null;
                }

                // SecondaryFocus
                if (secondarySelected && !_secondaryPreviouslySelected)
                {
                    // If nothing else has secondary focus let this hotspot have secondary focus.
                    if (this.secondaryFocusHotspot == null)
                    {
                        this.secondaryFocusHotspot = hotspot;
                        hotspot.secondaryFocus();
                    }
                }

                // SecondaryActivation
                if (!secondarySelected && _secondaryPreviouslySelected)
                {
                    // If this hotspot has secondary focus then the release of the secondary
                    // constitutes the completion of getting this hotspot selected.
                    if (this.secondaryFocusHotspot == hotspot)
                        hotspot.secondaryActivation();

                    // Regardless, no hotspot now has secondary focus.
                    //this.secondaryFocusHotspot = null;
                }

                //bool focusLocked = (this.primaryFocusHotspot != null);

                //// PointerEnter (and PointerExit)
                //if ((this.pointerOverHotspot != hotspot) && !focusLocked)
                //{
                //    // Exit from the old hotspot, if necessary...
                //    if (this.pointerOverHotspot != null)
                //        this.pointerOverHotspot.pointerExit();

                //    this.pointerOverHotspot = hotspot;
                //    this.pointerOverHotspot.pointerEnter();
                //}
            }

            // If primary is not selected no hotspot has primary focus.
            if (!primarySelected)
                this.primaryFocusHotspot = null;

            // If secondary is not selected no hotspot has secondary focus.
            if (!secondarySelected)
                this.secondaryFocusHotspot = null;

            bool focusLocked = (this.primaryFocusHotspot != null);

            // PointerEnter (and PointerExit)
            if ((this.pointerOverHotspot != hotspot) && !focusLocked)
            {
                // Exit from the old hotspot, if necessary...
                if (this.pointerOverHotspot != null)
                    this.pointerOverHotspot.pointerExit();

                this.pointerOverHotspot = hotspot;

                if (this.pointerOverHotspot != null)
                    this.pointerOverHotspot.pointerEnter();
            }

            // PointerOver
            // NOTE: This fires regardless of whether the pointer is over a hotspot, as it may be focus locked...
            if (this.pointerOverHotspot != null)
                this.pointerOverHotspot.pointerOver(this.pointerPosWindow - new Vector2(pointerOverHotspot.bounds.Location.X, pointerOverHotspot.bounds.Location.Y), this.primaryFocusHotspot == this.pointerOverHotspot);

            _primaryPreviouslySelected = primarySelected;
            _secondaryPreviouslySelected = secondarySelected;

            if (!focusLocked)
                HandleInput(gameTime);
        }
        public abstract void HandleInput(GameTime gameTime);

        public void DrawEx(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, GraphicsDevice GraphicsDevice)
        {
            // Do clipping to the Window's bounds.
            spriteBatch.GraphicsDevice.ScissorRectangle = this.bounds;

            // Draw the background, if any...
            drawBackground(gameTime, gameState, spriteBatch);

            Draw(gameTime, gameState, spriteBatch);

            // Let the UI objects draw.
            foreach (UIObject uiObject in this.uiObjectManager.getScene())
                uiObject.Draw(gameTime, spriteBatch);

            // Draw the buttons, if any...
            this.hotspots.Where(hotspot => hotspot is Button).ToList().ForEach(hotspot => ((Button)hotspot).Draw(gameTime, gameState, spriteBatch, this.baseGame, this.mxTWindowToScreen));

            // Draw the decorations, if any...
            drawDecorations(gameTime, gameState, spriteBatch);

            // Draw the effects, if any...
            drawEffects(gameTime, gameState, spriteBatch);
        }
        public abstract void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch);

        protected Vector2 screenToWindow(Vector2 v2screen)
        {
            return Vector2.Transform(v2screen, mxTIScreenToWindow);
        }

        protected Vector2 windowToScreen(Vector2 v2window)
        {
            return Vector2.Transform(v2window, mxTWindowToScreen);
        }

        private void drawBackground(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch)
        {
            Window.drawBackground(gameTime, gameState, spriteBatch, this.background, this.bounds, this.mxTWindowToScreen);
        }

        private void drawDecorations(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch)
        {
            Window.drawDecorations(gameTime, gameState, spriteBatch, this.decorations, this.bounds, this.mxTWindowToScreen);
        }

        private void drawEffects(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch)
        {
            Window.drawEffects(gameTime, gameState, spriteBatch, this.effects, this.bounds, this.mxTWindowToScreen);
        }
        
        public static void drawBackground(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, WindowBackground windowBackground, Rectangle bounds, Matrix windowToScreenTransform)
        {
            if ((windowBackground == null) || (windowBackground.background == null))
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, Window.rasterizerState, null, windowToScreenTransform);

            Frame fBackground = windowBackground.background.getCurrentFrame(gameTime, gameState);

            Rectangle boundsClipped = bounds;

            switch (windowBackground.style)
            {
                case WindowBackground.Style.Centered:
                    {
                        windowBackground.background.Draw(gameTime, gameState, spriteBatch, new Rectangle((boundsClipped.Width / 2) - (fBackground.bounds.Width / 2), (boundsClipped.Height / 2) - (fBackground.bounds.Height / 2), fBackground.bounds.Width, fBackground.bounds.Height));
                        break;
                    }
                case WindowBackground.Style.Stretched:
                    {
                        windowBackground.background.Draw(gameTime, gameState, spriteBatch, new Rectangle(0, 0, boundsClipped.Width, boundsClipped.Height));
                        break;
                    }
                case WindowBackground.Style.Tiled:
                    {
                        for (int x = 0; x < boundsClipped.Width; x += fBackground.bounds.Width)
                            for (int y = 0; y < boundsClipped.Height; y += fBackground.bounds.Height)
                                windowBackground.background.Draw(gameTime, gameState, spriteBatch, new Point(x, y));
                        break;
                    }
                default:
                    break;
            }

            spriteBatch.End();
        }

        public static void drawDecorations(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, WindowDecorations windowDecorations, Rectangle bounds, Matrix windowToScreenTransform)
        {
            if (windowDecorations == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, Window.rasterizerState, null, windowToScreenTransform);

            if (windowDecorations.cornerNW != null)
            {
                Frame fCornerNW = windowDecorations.cornerNW.getCurrentFrame(gameTime, gameState);
                windowDecorations.cornerNW.Draw(gameTime, gameState, spriteBatch, new Point(0, 0));
            }

            if (windowDecorations.cornerNE != null)
            {
                Frame fCornerNE = windowDecorations.cornerNE.getCurrentFrame(gameTime, gameState);
                windowDecorations.cornerNE.Draw(gameTime, gameState, spriteBatch, new Point(bounds.Width - fCornerNE.bounds.Width, 0));
            }

            if (windowDecorations.cornerSE != null)
            {
                Frame fCornerSE = windowDecorations.cornerSE.getCurrentFrame(gameTime, gameState);
                windowDecorations.cornerSE.Draw(gameTime, gameState, spriteBatch, new Point(bounds.Width - fCornerSE.bounds.Width, bounds.Height - fCornerSE.bounds.Height));
            }

            if (windowDecorations.cornerSW != null)
            {
                Frame fCornerSW = windowDecorations.cornerSW.getCurrentFrame(gameTime, gameState);
                windowDecorations.cornerSW.Draw(gameTime, gameState, spriteBatch, new Point(0, bounds.Height - fCornerSW.bounds.Height));
            }

            if (windowDecorations.sideN != null)
            {
                Frame fSideN = windowDecorations.sideN.getCurrentFrame(gameTime, gameState);

                int startX = 0;
                if (windowDecorations.cornerNW != null)
                    startX = windowDecorations.cornerNW.getCurrentFrame(gameTime, gameState).bounds.Width;

                int endX = bounds.Width;
                if (windowDecorations.cornerNE != null)
                    endX = bounds.Width - windowDecorations.cornerNE.getCurrentFrame(gameTime, gameState).bounds.Width;

                // TODO: Go until (endX - fSideN.bounds.Width) and then do a partial for the remainder, eventually?
                for (int x = startX; x < endX; x += fSideN.bounds.Width)
                    windowDecorations.sideN.Draw(gameTime, gameState, spriteBatch, new Point(x, 0));
            }

            if (windowDecorations.sideE != null)
            {
                Frame fSideE = windowDecorations.sideE.getCurrentFrame(gameTime, gameState);

                int startY = 0;
                if (windowDecorations.cornerNE != null)
                    startY = windowDecorations.cornerNE.getCurrentFrame(gameTime, gameState).bounds.Height;

                int endY = bounds.Height;
                if (windowDecorations.cornerSE != null)
                    endY = bounds.Height - windowDecorations.cornerSE.getCurrentFrame(gameTime, gameState).bounds.Width;

                // TODO: Go until (endY - fSideE.bounds.Height) and then do a partial for the remainder, eventually?
                for (int y = startY; y < endY; y += fSideE.bounds.Height)
                    windowDecorations.sideE.Draw(gameTime, gameState, spriteBatch, new Point(bounds.Width - fSideE.bounds.Width, y));
            }

            if (windowDecorations.sideS != null)
            {
                Frame fSideS = windowDecorations.sideS.getCurrentFrame(gameTime, gameState);

                int startX = 0;
                if (windowDecorations.cornerSW != null)
                    startX = windowDecorations.cornerSW.getCurrentFrame(gameTime, gameState).bounds.Width;

                int endX = bounds.Width;
                if (windowDecorations.cornerSE != null)
                    endX = bounds.Width - windowDecorations.cornerSE.getCurrentFrame(gameTime, gameState).bounds.Width;

                // TODO: Go until (endX - fSideS.bounds.Width) and then do a partial for the remainder, eventually?
                for (int x = startX; x < endX; x += fSideS.bounds.Width)
                    windowDecorations.sideS.Draw(gameTime, gameState, spriteBatch, new Point(x, bounds.Height - fSideS.bounds.Height));
            }

            if (windowDecorations.sideW != null)
            {
                Frame fSideW = windowDecorations.sideW.getCurrentFrame(gameTime, gameState);

                int startY = 0;
                if (windowDecorations.cornerNW != null)
                    startY = windowDecorations.cornerNW.getCurrentFrame(gameTime, gameState).bounds.Height;

                int endY = bounds.Height;
                if (windowDecorations.cornerSW != null)
                    endY = bounds.Height - windowDecorations.cornerSW.getCurrentFrame(gameTime, gameState).bounds.Width;

                // TODO: Go until (endY - fSideW.bounds.Height) and then do a partial for the remainder, eventually?
                for (int y = startY; y < endY; y += fSideW.bounds.Height)
                    windowDecorations.sideW.Draw(gameTime, gameState, spriteBatch, new Point(0, y));
            }

            spriteBatch.End();
        }

        public static void drawEffects(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, List<WindowEffect> windowEffects, Rectangle bounds, Matrix windowToScreenTransform)
        {
            if ((windowEffects == null) || (windowEffects.Count <= 0))
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, Window.rasterizerState, null, windowToScreenTransform);

            List<WindowEffect> cleanup = new List<WindowEffect>();
            foreach (WindowEffect effect in windowEffects)
            {
                int elapsed = effect.elapsed(gameTime);
                if (elapsed > effect.msDuration)
                {
                    cleanup.Add(effect);
                    continue;
                }

                if (effect.graphic == null)
                    continue;

                Frame fEffect = effect.graphic.getCurrentFrame(gameTime, gameState);

                Vector2 v2pos = Vector2.Lerp(effect.v2Start, effect.v2End, (float)elapsed / (float)effect.msDuration);

                effect.graphic.Draw(gameTime, gameState, spriteBatch, new Point((int)v2pos.X, (int)v2pos.Y));
            }

            foreach (WindowEffect effect in cleanup)
                windowEffects.Remove(effect);

            spriteBatch.End();
        }

        public void addButton(Button button)
        {
            this.hotspots.Add(button);
        }
    }
}
