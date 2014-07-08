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
    public abstract class MapObject : AIInterface
    {
        public Vector2 positionWorld { get; protected set; }
        public VariableBundle state { get; private set; }
        public string text { get; protected set; }
        public List<Action> sourceActions { get; private set; }
        public List<Action> targetActions { get; private set; }
        public List<Action> defaultActions { get; private set; }
        public Dictionary<string, List<string>> inheritanceChains { get; private set; }
        public List<string> needs { get; protected set; }
        public List<string> wants { get; protected set; }
        public List<ResolvedAction> recentActions { get; private set; }
        public bool landmark = false; // is map object remembered for actions even when not in sight?
        public bool mobile = false; // is the object mobile?

        public static System.Random rnd = new System.Random();

        public MapObject(string text, Vector2 positionWorld)
        {
            this.text = text;
            this.positionWorld = positionWorld;

            this.state = new VariableBundle();

            this.sourceActions = new List<Action>();
            this.targetActions = new List<Action>();
            this.defaultActions = new List<Action>();

            this.inheritanceChains = new Dictionary<string, List<string>>();

            this.needs = new List<string>();
            this.wants = new List<string>();

            this.recentActions = new List<ResolvedAction>();
        }

        // AIInterface methods
        #region AIInterface_methods
        // TODO: Eventually make not virtual, right?  Method chooseAction will
        //       be the one that allows subclasses to do fancy things.
        // TODO: Map passed to constructor, perhaps?
        public virtual void process<T>(VariableBundle gameState, MapGrid<T> map)
        {
            // Weed out any recent actions that are no longer on cooldown.
            int turn = gameState.getValue<int>("turn");
            this.recentActions = this.recentActions.Where(recentAction => (turn - recentAction.resolvedTurn) < recentAction.action.cooldown).ToList();

            // Get the possible targets.
            List<MapObject> targets = getActionTargets();

            // If this is a mob and notes landmarks, note any targets that are landmarks.
            // TODO: Better done down in Mob?  Or have that capability move up to MapObject?
            Mob mob = this as Mob;
            if ((mob != null) && mob.notesLandmarks)
            {
                List<MapObject> landmarks = targets.Where(target => target.landmark).ToList();
                foreach (MapObject landmark in landmarks)
                {
                    if (mob.notedLandmarks.Contains(landmark))
                        continue;
                    mob.notedLandmarks.Add(landmark);
                }
            }

            // Get the possible actions between this MapObject and the possible targets.
            List<ResolvedAction> possibleActions = getPossibleActions(targets, map);

            // TODO: Figure out how to get default actions to run with others!  E.g., we never choose to sail when leave_boat is an option.

            // Choose an action.
            ResolvedAction chosenAction = chooseAction(possibleActions, gameState, map);
            if ((chosenAction == null) && (this.defaultActions != null) && (this.defaultActions.Count > 0))
                chosenAction = new ResolvedAction(this.defaultActions[MapObject.rnd.Next(this.defaultActions.Count)], this, null);

            // Execute the action.
            if (chosenAction != null)
            {
                // If not in range of the target then let's get in range!
                if (this.mobile && (chosenAction.target != null) && (chosenAction.target.landmark) && !inRange(map, chosenAction.target, chosenAction.action.rangeMN))
                {
                    Mob mob1 = this as Mob;
                    if (mob1 != null)
                        mob1.moveTo(chosenAction.target.positionWorld);
                    return;
                }

                chosenAction.execute(turn);
                onExecute(chosenAction);

                if (chosenAction.action.cooldown > 0)
                    this.recentActions.Add(chosenAction);
            }
        }

        // TODO: Does this need to be on AI interface?
        public virtual List<MapObject> getActionTargets()
        {
            return new List<MapObject>();
        }

        // TODO: Does this need to be on AI interface?
        public List<ResolvedAction> getPossibleActions<T>(List<MapObject> targets, MapGrid<T> map)
        {
            List<ResolvedAction> possibleActions = new List<ResolvedAction>();

            MapObject source = this; // TODO: Remove eventually, once we are sure we don't need to pass source.

            if (source != null)
            {
                foreach (GamesLibrary.Action action in source.sourceActions)
                {
                    if (!action.requiresTarget)
                    {
                        if (action.isPossible(source, null))
                            possibleActions.Add(new ResolvedAction(action, source, null));
                        continue;
                    }

                    if (targets == null)
                        continue;

                    foreach (MapObject target in targets)
                    {
                        if (target == source)
                            continue;

                        if (!target.targetActions.Contains(action))
                            continue;

                        // Consider Action.range if map object can't move.
                        if (!this.mobile && !inRange(map, target, action.rangeMN))
                            continue;

                        if (action.isPossible(source, target))
                            possibleActions.Add(new ResolvedAction(action, source, target));
                    }
                }
            }

            // Only those actions that are not on cooldown still are possible.
            return possibleActions.Where(possibleAction => !this.recentActions.Select(recentAction => recentAction.action).ToList().Contains(possibleAction.action)).ToList();
        }

        // TODO: Does this need to be on AI interface?
        public virtual ResolvedAction chooseAction<T>(List<ResolvedAction> actions, VariableBundle gameState, MapGrid<T> map)
        {
            // Call the source and get needs and wants, prioritizing based on that.  Others can override and provide their
            // own choice if they want.

            // TODO: We'll mainly try and fulfill source's needs, then wants.  But if the source and target are friendly then 
            //       they could work together to fulfill both their goals, perhaps.

            List<string> needs = getNeeds(gameState, map); 
            if ((this.needs != null) && (this.needs.Count > 0))
            {
                List<ResolvedAction> needActions = actions.Where(action => action.action.goalsFulfilled.Any(goal => this.needs.Contains(goal))).ToList();
                if (needActions.Count > 0)
                    return needActions[rnd.Next(needActions.Count)];
            }

            List<string> wants = getWants(gameState, map);
            if ((this.wants != null) && (this.wants.Count > 0))
            {
                List<ResolvedAction> wantActions = actions.Where(action => action.action.goalsFulfilled.Any(goal => this.wants.Contains(goal))).ToList();
                if (wantActions.Count > 0)
                    return wantActions[rnd.Next(wantActions.Count)];
            }

            return null;
        }

        protected virtual List<string> getNeeds<T>(VariableBundle gameState, MapGrid<T> map)
        {
            this.needs.Clear();

            return this.needs;
        }

        protected virtual List<string> getWants<T>(VariableBundle gameState, MapGrid<T> map)
        {
            this.wants.Clear();

            return this.wants;
        }

        public virtual bool isPlayer()
        {
            return false;
        }
        #endregion

        // TODO: Not sure I like this here, but it's not THAT bad I guess?  Still odd to have anything graphic related here...
        public abstract string getGraphicIdentifier();
        protected abstract List<Command> getCommands();

        public bool inRange<T>(MapGrid<T> map, MapObject mapObject, int range)
        {
            Map.MapNode thisMapNode = map.getMapNode(this.positionWorld);
            Map.MapNode mapNode = map.getMapNode(mapObject.positionWorld);
            // TODO: Do I need to special case the 0 case?
            if ((range == 0) && (thisMapNode == mapNode))
                return true;
            //return map.getAdjacentNodes(thisMapNode, range).Contains(mapNode);
            return (map.getHeuristic(thisMapNode, mapNode) <= range);
        }

        public List<Command> getCommands<T>(MapGrid<T> map, MapObject requester, int range)
        {
            //if (Vector2.Distance(this.positionWorld, requester.positionWorld) > range)
            //    return null;
            Map.MapNode requesterMapNode = map.getMapNode(requester.positionWorld);
            if (!inRange(map, requester, range))
                return null;

            return getCommands();
        }

        public virtual void onExecute(ResolvedAction action)
        {
        }
    }
}
