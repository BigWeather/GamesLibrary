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
    public class MessageBox : Window
    {
        private SpriteFont _spriteFont;
        private string _text;
        private Vector2 _v2TextSize;
        private Button _buttonOK;

        public MessageBox(string text, SpriteFont spriteFont, Vector2 v2Center, BaseGame baseGame)
            : this(text, spriteFont, v2Center, baseGame, null)
        {
        }

        public MessageBox(string text, SpriteFont spriteFont, Vector2 v2Center, BaseGame baseGame, EventHandler<PressEventArgs> OnOK) : base(v2Center, baseGame)
        {
            _text = text;
            _spriteFont = spriteFont;

            _buttonOK = new Button(new Rectangle(0, 0, 0, 0), "OK", _spriteFont, Color.Black, true);
            _buttonOK.Press += new EventHandler<PressEventArgs>(_buttonOK_Press);
            if (OnOK != null)
                _buttonOK.Press += OnOK;

            _v2TextSize = _spriteFont.MeasureString(_text);
            int height = textTopBuffer + (int)(_v2TextSize.Y) + betweenBuffer + _buttonOK.bounds.Height + buttonBottomBuffer;
            int width = leftBuffer + (int)(_v2TextSize.X) + rightBuffer;
            Rectangle bounds = new Rectangle((int)this.v2Center.X, (int)this.v2Center.Y, width, height);
            bounds.Offset(-(bounds.Width / 2), -(bounds.Height / 2));

            this.bounds = bounds;

            Rectangle boundsOK = new Rectangle((this.bounds.Width / 2) - (_buttonOK.bounds.Width / 2), this.bounds.Height - buttonBottomBuffer - _buttonOK.bounds.Height, _buttonOK.bounds.Width, _buttonOK.bounds.Height);
            _buttonOK.bounds = boundsOK;

            this.addButton(_buttonOK);
        }

        void _buttonOK_Press(object sender, PressEventArgs e)
        {
            this.baseGame.Hide(this);
        }

        public override void HandleInput(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, Window.rasterizerState, null, this.mxTWindowToScreen);
            spriteBatch.DrawString(_spriteFont, _text, new Vector2(this.center.X - (_v2TextSize.X / 2), textTopBuffer), Color.Black);
            spriteBatch.End();

            //_buttonOK.background = this.background;
            _buttonOK.decorations = this.decorations;
            _buttonOK.Draw(gameTime, gameState, spriteBatch, this.baseGame, this.mxTWindowToScreen);
        }
    }
}
