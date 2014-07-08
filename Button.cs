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
    #region EventArgs classes
    public class PressEventArgs : EventArgs { }
    #endregion

    public class Button : Hotspot
    {
        #region EventHandler instances
        public event EventHandler<PressEventArgs> Press;
        #endregion

        //public Rectangle bounds 
        //{ 
        //    get { return _bounds; }
        //    set
        //    {
        //        _bounds = value;
        //        resize();
        //    }
        //}
        //private Rectangle _bounds;

        public string text
        {
            get { return _text; }
            set
            {
                _text = value;
                resize();
            }
        }
        private string _text;

        public Graphic graphic
        {
            get { return _graphic; }
            set
            {
                _graphic = value;
                resize();
            }
        }
        private Graphic _graphic;

        //public Texture2D texture { get; set; }

        public Color color { get; set; }
        //public bool enabled { get; set; }

        // TODO: This is pretty clunky, because we have to remember to hook up the Hotspot -- better way?
        //public Hotspot _hotspot { get; private set; }

        private SpriteFont _font;

        private Vector2 _textSize;

        public WindowBackground background { get; set; }
        public WindowDecorations decorations { get; set; }

        public static int _bufferTop = 10;
        public static int _bufferBottom = 10;
        public static int _bufferLeft = 10;
        public static int _bufferRight = 10;

        public Button(Rectangle bounds, bool oval, Graphic graphic, bool enabled)
            : base(bounds, oval, 1, enabled)
        {
            _graphic = graphic;

            this.color = Color.White;

            resize();
        }

        public Button(Rectangle bounds, string text, SpriteFont font, Color color, bool enabled)
            : base(bounds, false, 1, enabled)
        {
            //_bounds = bounds;
            _text = text;
            _font = font;
            this.color = color;
            //this.enabled = enabled;

            //_hotspot = new Hotspot(_bounds, false, 1, this.enabled);
            //_hotspot.PointerOver += new EventHandler<PointerOverEventArgs>(_hotspot_PointerOver);
            //_hotspot.PrimaryActivation += new EventHandler<PrimaryActivationEventArgs>(_hotspot_PrimaryActivation);

            resize();
        }

        //void _hotspot_PointerOver(object sender, PointerOverEventArgs e)
        //{
        //    // TODO: Handle hover state.
        //}

        //void _hotspot_PrimaryActivation(object sender, PrimaryActivationEventArgs e)
        //{
        //    // Send a Press event to any listeners.
        //    press();
        //}

        protected override void OnPointerOver(PointerOverEventArgs e)
        {
            base.OnPointerOver(e);
        }

        protected override void OnPrimaryActivation(PrimaryActivationEventArgs e)
        {
            base.OnPrimaryActivation(e);

            // Send a Press event to any listeners.
            press();
        }

        protected override void onSetBounds()
        {
            base.onSetBounds();

            resize();
        }

        private void resize()
        {
            if (_font != null)
            {
                _textSize = _font.MeasureString(_text);
                _bounds = new Rectangle(_bounds.X, _bounds.Y, _bufferLeft + (int)_textSize.X + _bufferRight, _bufferTop + (int)_textSize.Y + _bufferBottom);
            }

            if (_graphic != null)
            {
                Rectangle defaultBounds = _graphic.defaultBounds;
                if (defaultBounds != default(Rectangle))
                    _bounds = new Rectangle(_bounds.X, _bounds.Y, defaultBounds.Width, defaultBounds.Height);
            }

            //_hotspot.bounds = _bounds;
        }

        public void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, BaseGame baseGame, Matrix windowToScreenTransform)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            float layerDepth = 0f;

            Matrix buttonToScreenTransform = windowToScreenTransform;
            buttonToScreenTransform *= Matrix.CreateTranslation(_bounds.X, _bounds.Y, 0.0f);

            Rectangle oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            Vector2 v2ScreenXY = Vector2.Transform(new Vector2(_bounds.X, _bounds.Y), windowToScreenTransform);
            Rectangle newScissorRectangle = new Rectangle((int)v2ScreenXY.X, (int)v2ScreenXY.Y, _bounds.Width, _bounds.Height);
            spriteBatch.GraphicsDevice.ScissorRectangle = newScissorRectangle;

            Window.drawBackground(gameTime, gameState, spriteBatch, this.background, _bounds, buttonToScreenTransform);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, Window.rasterizerState, null, windowToScreenTransform);

            if (_graphic != null)
            {
                Frame frame = _graphic.getCurrentFrame(gameTime, gameState);
                // NOTE: We don't do anchor here because Button is Hotspot and we need to be sure that _bounds is correct coming in, period.
                //spriteBatch.Draw(baseGame.getTexture(_graphic), _bounds, frame.bounds, this.color, 0.0f, new Vector2(frame.anchor.X, frame.anchor.Y), spriteEffects, layerDepth);
#if OLD_TEXTURE
                spriteBatch.Draw(baseGame.getTexture(_graphic), _bounds, frame.bounds, this.color, 0.0f, Vector2.Zero, spriteEffects, layerDepth);
#else
                _graphic.Draw(gameTime, gameState, spriteBatch, _bounds, this.color, 0.0f, Vector2.Zero, layerDepth);
#endif
            }

            if ((_font != null) && (_text != null))
                spriteBatch.DrawString(_font, _text, new Vector2(_bounds.X + ((_bounds.Width - (int)_textSize.X) / 2), _bounds.Y + ((_bounds.Height - (int)_textSize.Y) / 2)), this.color);

            spriteBatch.End();

            Window.drawDecorations(gameTime, gameState, spriteBatch, this.decorations, _bounds, buttonToScreenTransform);

            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
        }

        #region OnEvent methods
        protected virtual void OnPress(PressEventArgs e)
        {
            if (Press != null)
                Press(this, e);
        }
        #endregion

        #region Event raising methods
        public void press()
        {
            OnPress(new PressEventArgs());
        }
        #endregion
    }
}
