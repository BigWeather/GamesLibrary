using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Allow_XNA
using Microsoft.Xna.Framework;
#endif

namespace GamesLibrary
{
    public class Cache : MapObject
    {
        public MapObject owner { get; private set; }

        public Cache(string text, Vector2 positionWorld, MapObject owner)
            : base(text, positionWorld)
        {
            this.owner = owner;
            this.landmark = true;
        }

        protected override List<Command> getCommands()
        {
            return null;
        }

        public override string getGraphicIdentifier()
        {
            return null;
        }
    }
}
