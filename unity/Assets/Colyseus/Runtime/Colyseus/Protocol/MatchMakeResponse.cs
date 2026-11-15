using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colyseus
{
    /// <summary>
    ///     Wrapper class for a match making response
    /// </summary>
    /// <remarks>Returns room and sessionId if successful; code and error if not</remarks>
    [Serializable]
    public class SeatReservation
    {
        public string name;
        public string sessionId;
        public string roomId;
        public string publicAddress;
        public string processId;
        public string reconnectionToken;
        public bool devMode;
        public string protocol;

        /// <summary>
        /// Colyseus 0.15 servers nest room fields under <c>room</c>.
        /// SDK 0.18 expects a flat payload — call <see cref="NormalizeFromLegacyServer"/> after deserialize.
        /// </summary>
        public RoomAvailable room;

        public void NormalizeFromLegacyServer()
        {
            if (room == null) return;
            if (string.IsNullOrEmpty(roomId)) roomId = room.roomId;
            if (string.IsNullOrEmpty(processId)) processId = room.processId;
            if (string.IsNullOrEmpty(name)) name = room.name;
            if (string.IsNullOrEmpty(publicAddress)) publicAddress = room.publicAddress;
        }
    }
}
