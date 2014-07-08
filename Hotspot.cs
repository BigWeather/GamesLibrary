using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    #region EventArgs classes
    public class PrimaryFocusEventArgs : EventArgs { }
    public class PrimaryActivationEventArgs : EventArgs { }
    public class SecondaryFocusEventArgs : EventArgs { }
    public class SecondaryActivationEventArgs : EventArgs { }
    public class PointerOverEventArgs : EventArgs 
    {
        public Vector2 pointerLocationHotspot { get; private set; }
        public bool hasFocus { get; private set; }

        public PointerOverEventArgs(Vector2 pointerLocationHotspot, bool hasFocus)
            : base()
        {
            this.pointerLocationHotspot = pointerLocationHotspot;
            this.hasFocus = hasFocus;
        }
    }
    public class PointerEnterEventArgs : EventArgs { }
    public class PointerExitEventArgs : EventArgs { }
    #endregion

    public class Hotspot
    {
        public Rectangle bounds 
        {
            get
            {
                return _bounds;
            }
            set
            {
                _bounds = value;
                onSetBounds();
            }
        }
        protected Rectangle _bounds;

        public int zorder { get; set; }
        public bool enabled { get; set; }

        public Hotspot parentHotspot { get; set; }

        private bool _oval = false;

        #region EventHandler instances
        public event EventHandler<PrimaryFocusEventArgs> PrimaryFocus;
        public event EventHandler<PrimaryActivationEventArgs> PrimaryActivation;
        public event EventHandler<SecondaryFocusEventArgs> SecondaryFocus;
        public event EventHandler<SecondaryActivationEventArgs> SecondaryActivation;
        public event EventHandler<PointerOverEventArgs> PointerOver;
        public event EventHandler<PointerEnterEventArgs> PointerEnter;
        public event EventHandler<PointerExitEventArgs> PointerExit;
        #endregion

        public Hotspot(Rectangle bounds) : this(bounds, false, 0, true) { }
        public Hotspot(Rectangle bounds, bool oval) : this(bounds, oval, 0, true) { }
        public Hotspot(Rectangle bounds, int zorder) : this(bounds, false, zorder, true) { }
        public Hotspot(Rectangle bounds, bool oval, int zorder) : this(bounds, oval, zorder, true) { }
        public Hotspot(Rectangle bounds, bool oval, int zorder, bool enabled)
        {
            this.bounds = bounds;
            _oval = oval;
            this.zorder = zorder;
            this.enabled = enabled;

            this.parentHotspot = null;
        }

        public bool contains(Vector2 v2pos)
        {
            // TODO: Right now just supporting circles, beef up for ovals later.
            if (_oval)
            {
                //Vector2 v = new Vector2(this.bounds.Center.X - this.bounds.X, this.bounds.Center.Y - this.bounds.Y);
                //float fd = Vector2.Distance(v2pos, v);
                //int id = (int)fd;
                return (Vector2.Distance(v2pos, new Vector2(this.bounds.Center.X, this.bounds.Center.Y)) < ((float)this.bounds.Width / 2.0f));
            }

            return this.bounds.Contains((int)v2pos.X, (int)v2pos.Y);
        }

        protected virtual void onSetBounds() { }

        #region OnEvent methods
        protected virtual void OnPrimaryFocus(PrimaryFocusEventArgs e)
        {
            if (PrimaryFocus != null)
                PrimaryFocus(this, e);
        }

        protected virtual void OnPrimaryActivation(PrimaryActivationEventArgs e)
        {
            if (PrimaryActivation != null)
                PrimaryActivation(this, e);
        }

        protected virtual void OnSecondaryFocus(SecondaryFocusEventArgs e)
        {
            if (SecondaryFocus != null)
                SecondaryFocus(this, e);
        }

        protected virtual void OnSecondaryActivation(SecondaryActivationEventArgs e)
        {
            if (SecondaryActivation != null)
                SecondaryActivation(this, e);
        }

        protected virtual void OnPointerOver(PointerOverEventArgs e)
        {
            if (PointerOver != null)
                PointerOver(this, e);
        }

        protected virtual void OnPointerEnter(PointerEnterEventArgs e)
        {
            if (PointerEnter != null)
                PointerEnter(this, e);
        }

        protected virtual void OnPointerExit(PointerExitEventArgs e)
        {
            if (PointerExit != null)
                PointerExit(this, e);
        }
        #endregion

        #region Event raising methods
        public void primaryFocus()
        {
            OnPrimaryFocus(new PrimaryFocusEventArgs());
        }

        public void primaryActivation()
        {
            OnPrimaryActivation(new PrimaryActivationEventArgs());
        }

        public void secondaryFocus()
        {
            OnSecondaryFocus(new SecondaryFocusEventArgs());
        }

        public void secondaryActivation()
        {
            OnSecondaryActivation(new SecondaryActivationEventArgs());
        }

        public void pointerOver(Vector2 pointerLocationHotspot, bool hasFocus)
        {
            OnPointerOver(new PointerOverEventArgs(pointerLocationHotspot, hasFocus));
        }

        public void pointerEnter()
        {
            OnPointerEnter(new PointerEnterEventArgs());
        }

        public void pointerExit()
        {
            OnPointerExit(new PointerExitEventArgs());
        }
        #endregion
    }
}
