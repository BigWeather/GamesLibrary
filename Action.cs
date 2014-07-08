using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    /// <summary>
    /// Action defines an interaction between two objects, and as such can define not only
    /// the actions the player may take with other objects in the world but also the actions
    /// that the AI may choose to perform.
    /// </summary>
    public abstract class Action
    {
        public string identifier; // name of the Action, used in persistence
        public int rangeMN = 0; // maximum distance that the action may be performed at, in map nodes
        public bool requiresTarget = true; // is a target required for this Action to be performed?
        public bool generateNews = false; // generates a news item?
        public List<string> goalsFulfilled = new List<string>(); // goals (potentially) fulfilled by the action
        public int severity; // severity of the act, 0 (none) to 5 (severe)
        public int cooldown = 0; // number of "turns" that must pass before the Action is available again
        public bool playerOnly = false; // action is only available to the player(s)

        public virtual bool isPossible(MapObject source, MapObject target)
        {
            if (this.playerOnly && !source.isPlayer())
                return false;

            return true;
        }

        public abstract void execute(MapObject source, MapObject target);
    }

    public class ResolvedAction
    {
        public Action action { get; private set;}
        public MapObject source { get; private set;}
        public MapObject target { get; private set; }
        public int resolvedTurn { get; private set; }

        public ResolvedAction(Action action, MapObject source, MapObject target)
        {
            this.action = action;
            this.source = source;
            this.target = target;
        }

        public void execute(int turn)
        {
            this.resolvedTurn = turn;

            this.action.execute(this.source, this.target);

            if (this.action.generateNews)
            {
                string news = "Action: " + this.action.identifier + ", source: " + this.source.text;
                if (this.target != null)
                    news += ", target: " + this.target.text;
                Console.WriteLine(news);
            }
        }
    }
}
