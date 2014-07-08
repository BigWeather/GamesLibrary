using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GamesLibrary
{
    // TODO: Handle below better, need to be in XNASupport.cs?  At least not in UnitySupport.cs?
    // TODO: Get rid of all the gameTime, gameState passing -- instead have only those methods that are called with it (update, draw) store it on BaseGame.

    public static class Extensions
    {
        public static void DrawEx(this SpriteBatch spriteBatch, string texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color)
        {
            Texture2D t2D = TextureManager.Instance.getTexture(texture) as Texture2D;
            spriteBatch.Draw(t2D, destinationRectangle, sourceRectangle, color);
        }

        public static void DrawEx(this SpriteBatch spriteBatch, string texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            Texture2D t2D = TextureManager.Instance.getTexture(texture) as Texture2D;
            spriteBatch.Draw(t2D, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth);
        }

        public static void loadTexture(this BaseGame baseGame, string identifier, string assetName)
        {
            Texture2D tx2d = baseGame.Content.Load<Texture2D>(@assetName);
            TextureManager.Instance.setTexture(identifier, tx2d);
        }
    }
}
