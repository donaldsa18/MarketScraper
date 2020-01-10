using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using System.Collections.Generic;

namespace MarketQuery
{
    class GameState
    {
        public ICollection<CharacterSummary> characterList;
        public ChannelServerAddress mmoChannel;
        public int townID = 0;
    }
}
