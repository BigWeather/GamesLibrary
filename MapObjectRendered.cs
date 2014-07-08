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
    public class MapObjectRendered
    {
        public Hotspot hotspot { get; private set; }
        public Graphic graphic { get; set; } // TODO: Make protected?  // DRAW
        public MapObject mapObject { get; private set; }

        public MapObjectRendered(MapObject mapObject)
        {
            this.mapObject = mapObject;
        }

        public virtual void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch, Vector2 positionScreen, BaseGame baseGame)
        {
            Color color = Color.White;
            SpriteEffects spriteEffects = SpriteEffects.None;
            float layerDepth = 0f;

            // Content frame.
            Frame frame = this.graphic.getCurrentFrame(gameTime, gameState);
            Rectangle rectangleDest = new Rectangle((int)positionScreen.X, (int)positionScreen.Y, frame.bounds.Width, frame.bounds.Height);

            // Draw the content.
#if OLD_TEXTURE
            spriteBatch.Draw(baseGame.getTexture(this.graphic), rectangleDest, frame.bounds, color, 0.0f, new Vector2(frame.anchor.X, frame.anchor.Y), spriteEffects, layerDepth); // 0 is front, 1 is back
#else
            this.graphic.Draw(gameTime, gameState, spriteBatch, rectangleDest, color, 0.0f, new Vector2(frame.anchor.X, frame.anchor.Y), layerDepth); // 0 is front, 1 is back
#endif

            // Create the hotspot for interaction with the map object.
            Rectangle rectangleHotspot = rectangleDest;
            rectangleHotspot.Offset(-frame.anchor.X, -frame.anchor.Y);
            if (this.hotspot == null)
                this.hotspot = new Hotspot(rectangleHotspot, true);
            else
                this.hotspot.bounds = rectangleHotspot;
        }
    }
}
