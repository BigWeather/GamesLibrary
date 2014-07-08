using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    public class Command
    {
        // TODO: Too 360-y?
        public enum Slot { A, B, X, Y };

        public string identifier { get; private set; }
        public Slot slot { get; private set; }
        public MapObject owner { get; private set; }

        public Command(string identifier, Slot slot, MapObject owner)
        {
            this.identifier = identifier;
            this.slot = slot;
            this.owner = owner;
        }
    }
}
