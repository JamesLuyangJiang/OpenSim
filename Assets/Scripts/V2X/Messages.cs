using UnityEngine;

namespace V2X
{
    /// <summary>
    /// Basic Safety Message - Standard V2X message containing vehicle state information
    /// Broadcasted periodically by vehicles to inform other vehicles and infrastructure
    /// </summary>
    public struct BSM                 // Basic Safety Message
    {
        public int   id;              // Vehicle identifier
        public float time;            // Time.time at transmission
        public Unity.Mathematics.float3 pos;  // Vehicle position in world coordinates
        public Unity.Mathematics.float3 vel;  // Vehicle velocity vector
        public float headingDeg;      // Vehicle heading in degrees

        public BSM(int id, Vector3 p, Vector3 v, float h)
        {
            this.id = id;
            time = Time.time;
            pos = p;
            vel = v;
            headingDeg = h;
        }
    }

    /// <summary>
    /// Road Side Unit commands for intersection management
    /// </summary>
    public enum RsuCmd : byte 
    { 
        RequestEntry,  // Vehicle requests permission to enter intersection
        Grant,         // RSU grants permission to enter
        Wait,          // RSU tells vehicle to wait
        Clear          // Vehicle signals it has cleared the intersection
    }

    /// <summary>
    /// Message sent between vehicles and Road Side Units (RSUs)
    /// Used for intersection access control and coordination
    /// </summary>
    public struct RsuMessage
    {
        public int   vehId;           // Target vehicle ID
        public RsuCmd cmd;            // Command to execute
        public float  timestamp;      // Message timestamp for tracking

        public RsuMessage(int id, RsuCmd c)
        {
            vehId = id;
            cmd = c;
            timestamp = Time.time;
        }
    }
}
