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
    public class MapGridWindow<T> : Window
    {
        protected MapGrid<T> map;

        public bool hexBased
        {
            get
            {
                if (this.map is MapHex<T>)
                    return true;
                return false;
            }
        }

        public float rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }
        float _rotation = 0.0f;
        public float scale
        {
            get { return _scale; }
            set { _scale = value; }
        }
        private float _scale = 1.0f;
        public float scaleMin = 1.0f;
        public float scaleMax = 3.0f;
        protected Vector2 cameraPos;
        protected Matrix mxTWorldToScreen;
        protected Matrix mxTScreenToWorld;
        protected Matrix mxTScaleOnly;
        protected Matrix mxTAssetToWorld;
        protected Matrix mxTWorldToAsset;
        protected Matrix mxTBFY;
        protected Matrix mxTBFYI;
        private Vector2 _tileSizeA;

        public int scrollSpeed = 10;
        public float scrollArea = 0.2f;
        public bool allowScaling = false;
        public bool allowRotation = false;

        public bool drawGrid = false;

        private int _initialMouseWheelValue = int.MinValue;

        private Dictionary<MapObject, MapObjectRendered> _dictMapObjectRenderedByMapObject = new Dictionary<MapObject, MapObjectRendered>();

        public MapGridWindow(MapGrid<T> map, Rectangle bounds, Vector2 tileSizeA, Vector2 cameraPos, BaseGame baseGame)
            : base(bounds, baseGame)
        {
            this.map = map;
            _tileSizeA = tileSizeA;
            this.cameraPos = cameraPos;

            updateTransformMatrices();
        }

        public override void HandleInput(GameTime gameTime)
        {
#if WINDOWS
            KeyboardState kbs = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            if (_initialMouseWheelValue == int.MinValue)
                _initialMouseWheelValue = ms.ScrollWheelValue;

            if (allowRotation)
            {
                if (kbs.IsKeyDown(Keys.PageDown))
                    _rotation += 0.25f;
                else if (kbs.IsKeyDown(Keys.PageUp))
                    _rotation -= 0.25f;
                if (_rotation < 0.0f)
                    _rotation = (float)(2 * Math.PI) - 0.1f;
                if (_rotation > (2 * Math.PI))
                    _rotation = 0.1f;
            }

            if (allowScaling)
            {
#if true
                if (kbs.IsKeyDown(Keys.Insert))
                    _scale += 0.25f;
                else if (kbs.IsKeyDown(Keys.Delete))
                    _scale -= 0.25f;
#else
                _scale = (ms.ScrollWheelValue - _initialMouseWheelValue) * 0.25f;
#endif
                if (_scale < scaleMin)
                    _scale = scaleMin;
                if (_scale > scaleMax)
                    _scale = scaleMax;

            }

            // Scroll the map (by moving the camera)
            // Scrolling is in screen coordinates (so it acts independent of zoom, etc.)
            // The closer to the border the faster the scroll.
            if (this.bounds.Contains(new Point(ms.X, ms.Y)))
            {
                Vector2 cameraPosScreen = worldToScreen(cameraPos);
                if (ms.X > (this.bounds.Width * (1.0f - scrollArea)))
                    cameraPosScreen.X += ((float)this.scrollSpeed * ((Math.Min(ms.X, this.bounds.Width) - (this.bounds.Width * (1.0f - scrollArea))) / (this.bounds.Width - (this.bounds.Width * (1.0f - scrollArea)))));
                else if (ms.X < (this.bounds.Width * scrollArea))
                    cameraPosScreen.X -= ((float)this.scrollSpeed * (((this.bounds.Width * scrollArea) - Math.Max(ms.X, 0)) / (this.bounds.Width * scrollArea)));
                if (ms.Y > (this.bounds.Height * (1.0f - scrollArea)))
                    cameraPosScreen.Y += ((float)this.scrollSpeed * ((Math.Min(ms.Y, this.bounds.Height) - (this.bounds.Height * (1.0f - scrollArea))) / (this.bounds.Height - (this.bounds.Height * (1.0f - scrollArea)))));
                else if (ms.Y < (this.bounds.Height * scrollArea))
                    cameraPosScreen.Y -= ((float)this.scrollSpeed * (((this.bounds.Height * scrollArea) - Math.Max(ms.Y, 0)) / (this.bounds.Height * scrollArea))); ;
                cameraPos = screenToWorld(cameraPosScreen);
            }
#endif

            updateTransformMatrices();
        }

        // TODO: Move over many of the grid drawing stuff in OSDCMapWindow::Draw.
        public override void Draw(GameTime gameTime, VariableBundle gameState, SpriteBatch spriteBatch)
        {
        }

        protected Vector2 screenToWorld(Vector2 v2screen)
        {
            return Vector2.Transform(v2screen, mxTScreenToWorld);
        }

        protected Vector2 worldToScreen(Vector2 v2world)
        {
            return Vector2.Transform(v2world, mxTWorldToScreen);
        }

        protected Vector2 assetToWorld(Vector2 v2asset)
        {
            return Vector2.Transform(v2asset, mxTAssetToWorld);
        }

        protected Vector2 worldToAsset(Vector2 v2world)
        {
            return Vector2.Transform(v2world, mxTWorldToAsset);
        }

        private void updateTransformMatrices()
        {
            // OFFICIALLY WORKS, DON'T DELETE!
            //this.mxT = Matrix.Identity; // world to screen
            //this.mxT *= Matrix.CreateTranslation(-cameraPos); // get to 0,0 in world coordinates
            //this.mxT *= Matrix.CreateScale((float)_tileSize / (float)this.map.worldCoordinatesPerTile); // needed if world coords per tile is ever not 1:1 with tile size
            //this.mxT *= Matrix.CreateScale(_scale); // scale (in screen coordinates)
            //this.mxT *= Matrix.CreateRotationZ(_rotation); // rotate
            //this.mxT *= Matrix.CreateTranslation(this.center); // move back to the center of the screen (TODO: does this imply the camera is always center?! Consider using cameraPos in screen coords -- but how, since we don't have mxT yet?!)
            //this.mxTI = Matrix.Invert(this.mxT); // inverse is screen to world

            Vector3 v3CameraPos = new Vector3(cameraPos.X, cameraPos.Y, 0);
            Vector3 v3TileSizeA = new Vector3(_tileSizeA.X, _tileSizeA.Y, 1);
            Vector3 v3WorldCoordinatesPerTile = new Vector3(this.map.worldCoordinatesPerTile.X, this.map.worldCoordinatesPerTile.Y, 1);

            this.mxTWorldToScreen = Matrix.Identity;
#if true
            this.mxTWorldToScreen *= Matrix.CreateTranslation(-v3CameraPos); // translate world camera to (0, 0)
            this.mxTWorldToScreen *= Matrix.CreateScale(v3TileSizeA / v3WorldCoordinatesPerTile); // scale asset to world
            //this.mxTWorldToScreen *= Matrix.CreateScale(_scale);
            this.mxTWorldToScreen *= Matrix.CreateScale(_scale, _scale, 1.0f); // apply map scale 
            this.mxTWorldToScreen *= Matrix.CreateRotationZ(_rotation); // apply map rotation
            this.mxTWorldToScreen *= Matrix.CreateTranslation(new Vector3(this.center.X, this.center.Y, 0)); // translate to screen middle of window
            this.mxTScreenToWorld = Matrix.Invert(this.mxTWorldToScreen);
#else
            Matrix mxTranslation = Matrix.CreateTranslation(-v3CameraPos);
            this.mxTWorldToScreen = this.mxTWorldToScreen * mxTranslation;

            Matrix mxScale1 = Matrix.CreateScale(v3TileSizeA / v3WorldCoordinatesPerTile);
            this.mxTWorldToScreen = this.mxTWorldToScreen * mxScale1;

            //this.mxT *= Matrix.CreateScale(_scale);

            Matrix mxScale2 = Matrix.CreateScale(_scale, _scale, 1.0f);
            this.mxTWorldToScreen = this.mxTWorldToScreen * mxScale2;

            Matrix mxRotationZ = Matrix.CreateRotationZ(_rotation);
            this.mxTWorldToScreen = this.mxTWorldToScreen * mxRotationZ;

            Matrix mxTranslationCenter = Matrix.CreateTranslation(new Vector3(this.center.X, this.center.Y, 0));
            this.mxTWorldToScreen = this.mxTWorldToScreen * mxTranslationCenter;

            this.mxTScreenToWorld = Matrix.Invert(this.mxTWorldToScreen);
#endif

            Vector2 cameraPosS = worldToScreen(cameraPos);
            Vector3 v3CameraPosS = new Vector3(cameraPosS.X, cameraPosS.Y, 0);

            this.mxTScaleOnly = Matrix.Identity;
            this.mxTScaleOnly *= Matrix.CreateTranslation(-v3CameraPosS);
            this.mxTScaleOnly *= Matrix.CreateScale(_scale, _scale, 1.0f); // don't scale in Z as this can cause our layer depth to exit the 0..1 range causing sprite not to draw
            this.mxTScaleOnly *= Matrix.CreateTranslation(v3CameraPosS);

            this.mxTBFY = Matrix.Identity;
            this.mxTBFY *= Matrix.CreateScale(v3TileSizeA / v3WorldCoordinatesPerTile);
            this.mxTBFYI = Matrix.Invert(this.mxTBFY);

            this.mxTAssetToWorld = Matrix.Identity;
            this.mxTAssetToWorld *= Matrix.CreateScale(v3WorldCoordinatesPerTile / v3TileSizeA);
            this.mxTWorldToAsset = Matrix.Invert(this.mxTAssetToWorld);
        }

        public MapObjectRendered getMapObjectRendered(MapObject mapObject)
        {
            MapObjectRendered mapObjectRendered;
            if (!_dictMapObjectRenderedByMapObject.TryGetValue(mapObject, out mapObjectRendered))
            {
                mapObjectRendered = createMapObjectRendered(mapObject);
                _dictMapObjectRenderedByMapObject.Add(mapObject, mapObjectRendered);
            }

            return mapObjectRendered;
        }

        protected virtual MapObjectRendered createMapObjectRendered(MapObject mapObject)
        {
            return new MapObjectRendered(mapObject);
        }
    }
}
