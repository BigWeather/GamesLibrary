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
    public class UIObjectManager
    {
        public List<UIObject> uiObjects { get; private set; }

        public UIObjectManager()
        {
            this.uiObjects = new List<UIObject>();
        }

        public List<UIObject> pick(Rectangle rectPick)
        {
            return pick(rectPick, true);
        }

        public List<UIObject> pick(Rectangle rectPick, bool partial)
        {
            List<UIObject> pickedObjects = new List<UIObject>();

            // TODO: Do the picking.
            pickedObjects.AddRange(this.uiObjects);

            return pickedObjects;
        }

        public List<UIObject> getScene()
        {
            // TODO: Return in back-to-front, only those visible.
            return this.uiObjects;
        }
    }

    public interface UIEventSinkInterface
    {
        void click();
        void mouseOver();
    }

    public interface DrawInterface
    {
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }

    public abstract class UIObject : DrawInterface, UIEventSinkInterface
    {
        protected Rectangle _rectPosition;

        public UIObject(Rectangle rectPosition)
        {
            _rectPosition = rectPosition;
        }

        public abstract void click();
        public abstract void mouseOver();

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }

    public abstract class ButtonX : UIObject
    {
        public ButtonX(Rectangle rectPosition) : base(rectPosition) { }
    }

#if false
    public class ToggleButton : ButtonX
    {
        Texture2D _txOn;
        Texture2D _txOff;
        SpriteFont _sfText;
        string _textOn;
        string _textOff;
        Color _textColor;

        bool _on = false;

        public ToggleButton(Rectangle rectPosition, Texture2D txOn, Texture2D txOff)
            : this(rectPosition, txOn, txOff, null, null, null, Color.Black)
        {
        }

        public ToggleButton(Rectangle rectPosition, Texture2D txOn, Texture2D txOff, SpriteFont sfText, string textOn, string textOff, Color textColor)
            : base(rectPosition)
        {
            _txOn = txOn;
            _txOff = txOff;
            _sfText = sfText;
            _textOn = textOn;
            _textOff = textOff;
            _textColor = textColor;
        }

        public override void click()
        {
            _on = !_on;
        }

        public override void mouseOver()
        {
            //throw new NotImplementedException();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw the texture.
            Texture2D texture = null;
            if (_on)
                texture = _txOn;
            else
                texture = _txOff;

            if (texture != null)
                spriteBatch.Draw(texture, _rectPosition, Color.White);

            // Draw the text.
            string text = null;
            if (_sfText != null)
            {
                if (_on && (_textOn != null))
                    text = _textOn;
                if (!_on && (_txOff != null))
                    text = _textOff;
            }

            if (text != null)
            {
                // Center the text.
                Vector2 v2StringSize = _sfText.MeasureString(text);

                spriteBatch.DrawString(_sfText, text, new Vector2((_rectPosition.Width - v2StringSize.X) / 2, (_rectPosition.Height - v2StringSize.Y) / 2), _textColor);
            }
        }
    }
#endif
}
