using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    public interface PathFinderConsumerInterface<N>
    {
        float getHeuristic(N start, N end);
        List<N> getConnectedNodes(N node);
        float getCost<R>(R requester, N start, N end);
    }

    public interface PathFinderInterface<N>
    {
        List<N> findPath<R>(R requester, N start, N goal);
    }

    public class AStarPathFinder<N> : PathFinderInterface<N>
    {
        public PathFinderConsumerInterface<N> _pathFinderConsumer;

        private class NodeRecord
        {
            public N node;
            public NodeRecord connection;
            public float costSoFar;
            public float estimatedTotalCost;
        }

        public AStarPathFinder(PathFinderConsumerInterface<N> pathFinderConsumer) 
        {
            _pathFinderConsumer = pathFinderConsumer;
        }

        private NodeRecord findMinimalEstimatedTotalCost(Dictionary<N, NodeRecord> dictNodeRecordsByNode)
        {
            NodeRecord smallestRecord = null;
            float minEstimatedTotalCost = float.MaxValue;

            foreach (NodeRecord nodeRecord in dictNodeRecordsByNode.Values)
            {
                if (nodeRecord.estimatedTotalCost >= minEstimatedTotalCost)
                    continue;

                smallestRecord = nodeRecord;
                minEstimatedTotalCost = smallestRecord.estimatedTotalCost;
            }

            return smallestRecord;
        }

        public List<N> findPath<R>(R requester, N start, N goal)
        {
            if (start.Equals(goal))
            {
                List<N> pathAtGoal = new List<N>();
                pathAtGoal.Add(goal);
                return pathAtGoal;
            }

            // Initialize the record for the start node.
            NodeRecord startRecord = new NodeRecord();
            startRecord.node = start;
            startRecord.connection = null;
            startRecord.costSoFar = 0.0f;
            startRecord.estimatedTotalCost = _pathFinderConsumer.getHeuristic(start, goal);

            // Initialize the open and closed lists.
            Dictionary<N, NodeRecord> open = new Dictionary<N, NodeRecord>();
            open.Add(start, startRecord);
            Dictionary<N, NodeRecord> closed = new Dictionary<N, NodeRecord>();

            NodeRecord currentRecord = null;

            // Iterate through each node.
            int openCt = open.Count;
            while (openCt > 0)
            {
                // Find the smallest element in the open list (using the estimated total cost).
                currentRecord = findMinimalEstimatedTotalCost(open);
                N current = currentRecord.node;

                // If it is the goal node we are done.
                if (currentRecord.node.Equals(goal))
                    break;

                // Otherwise get its outgoing connections and continue processing.
                List<N> connections = _pathFinderConsumer.getConnectedNodes(current);

                // Loop through each connection...
                foreach (N connected in connections)
                {
                    // Get the cost estimate for the connected node from the current node.
                    // If the cost is MaxValue then it is not passable, skip to the next connection.
                    float connectedCost = _pathFinderConsumer.getCost(requester, current, connected);
                    if (connectedCost == float.MaxValue)
                        continue;

                    float connectedCostSoFar = currentRecord.costSoFar + connectedCost;

                    float connectedHeuristic = float.MaxValue;

                    NodeRecord connectedRecord = null;
                    if (closed.TryGetValue(connected, out connectedRecord))
                    {
                        // If the node is closed we may have to skip or remove it from the closed list.

                        // If we didn't find a shorter route then continue on...
                        if (connectedRecord.costSoFar <= connectedCostSoFar)
                            continue;

                        // Otherwise remove it from the closed list.
                        closed.Remove(connected);

                        // We can use the node's old cost values to calculate its heuristic.
                        connectedHeuristic = connectedRecord.estimatedTotalCost - connectedRecord.costSoFar;
                    }
                    else if (open.TryGetValue(connected, out connectedRecord))
                    {
                        // Skip if the node is open and we've not found a better route.

                        // If our route is no better then continue...
                        if (connectedRecord.costSoFar <= connectedCostSoFar)
                            continue;

                        // We can use the node's old cost values to calculate its heuristic.
                        connectedHeuristic = connectedRecord.estimatedTotalCost - connectedRecord.costSoFar;
                    }
                    else
                    {
                        // Otherwise we know we've got an unvisited node so let's make a record for it.

                        connectedRecord = new NodeRecord();
                        connectedRecord.node = connected;

                        // We'll need to calculate the heuristic value using the function as we've not gotten it yet.
                        connectedHeuristic = _pathFinderConsumer.getHeuristic(connected, goal);
                    }

                    // We have to update the node (cost, estimate, and connection)...
                    connectedRecord.costSoFar = connectedCostSoFar;
                    connectedRecord.connection = currentRecord;
                    connectedRecord.estimatedTotalCost = connectedCostSoFar + connectedHeuristic;

                    // Remove it from the open list (if it is there) as we'll need to re-insert to get it to sort...
                    if (open.ContainsKey(connected))
                        open.Remove(connected);

                    // Add to the open list keyed by the new estimated total cost.
                    open.Add(connected, connectedRecord);
                }

                // We've finished looking at the connections for the current node so add to the closed and remove from the open.
                open.Remove(current);
                closed.Add(current, currentRecord);

                openCt = open.Count;
            }

            // We've either found the goal or run out of nodes to search...

            List<N> path = new List<N>();

            if (!currentRecord.node.Equals(goal))
                return path;

            // Work back along the path, noting the connections.
            path.Add(goal);
            while (!currentRecord.node.Equals(start))
            {
                path.Add(currentRecord.connection.node);
                currentRecord = currentRecord.connection;
            }

            // Reverse the path so it goes from start to goal, not goal to start.
            path.Reverse();

            return path;
        }
    }
}
