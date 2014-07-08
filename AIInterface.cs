using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    public interface AIInterface
    {
        void process<T>(VariableBundle gameState, MapGrid<T> map);
        List<MapObject> getActionTargets();
        List<ResolvedAction> getPossibleActions<T>(List<MapObject> targets, MapGrid<T> map);
        ResolvedAction chooseAction<T>(List<ResolvedAction> actions, VariableBundle gameState, MapGrid<T> map);
    }
}
