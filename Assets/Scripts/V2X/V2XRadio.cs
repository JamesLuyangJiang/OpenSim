using UnityEngine;
using Unity.Mathematics;
using static V2X.RsuCmd;

namespace V2X
{
    /// <summary>
    /// V2X Radio component that handles vehicle-to-vehicle and vehicle-to-infrastructure communication.
    /// Each vehicle should have one V2XRadio component attached to enable V2X communications.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class V2XRadio : MonoBehaviour
    {
        [Header("Vehicle Identification")]
        [Tooltip("Unique identifier for this vehicle - must be set at runtime")]
        public int VehicleId;                 // assign at runtime
        
        // Communication State
        public bool HasOutgoing { get; private set; }  // True when there's a message to send
        BSM _out;  // Outgoing Basic Safety Message

        // readonly System.Action<RsuMessage> _onRsu = null;
        Rigidbody _rb;

        /* —— API exposed to VehicleController —— */
        /// <summary>
        /// True when this vehicle has received a grant from an RSU
        /// </summary>
        public bool GrantReceived { get; private set; }

        /// <summary>
        /// Reset the grant status (called when exiting intersection)
        /// </summary>
        public void ResetGrantStatus()
        {
            GrantReceived = false;
        }

        void Awake()
        {
            // TODO: confirm this getcomponent is correct
            _rb = GetComponent<Rigidbody>();
            TryRegister();
        }

        void Start()
        {
            TryRegister();
        }

        void TryRegister()
        {
            if (V2XBus.I != null && !V2XBus.I.radios.Contains(this))
            {
                V2XBus.I.Register(this);
            }
        }

        void OnDestroy() => V2XBus.I.Unregister(this);

        /// <summary>
        /// Update vehicle state and prepare BSM for transmission
        /// </summary>
        void FixedUpdate()
        {
            _out = new BSM(VehicleId, transform.position, _rb.velocity,
                           transform.eulerAngles.y);
            HasOutgoing = true;
        }

        /// <summary>
        /// Get the current BSM and mark it as sent
        /// </summary>
        public void Flush(out BSM pkt)
        {
            pkt = _out;
            HasOutgoing = false;
        }

        /// <summary>
        /// Receive a message from the V2X bus
        /// </summary>
        public void Receive(in object packet)
        {
            if (packet is not RsuMessage msg) return;
            if (msg.vehId != VehicleId) return;

            if (msg.cmd == Grant) 
            {
                GrantReceived = true;
                // Debug.Log($"Vehicle {VehicleId} received GRANT");
            }
            else if (msg.cmd == Wait)
            {
                // Debug.Log($"Vehicle {VehicleId} received WAIT");
            }
        }

        /// <summary>
        /// Send a message to a specific RSU (called by vehicle when inside intersection zone)
        /// </summary>
        public void SendToRsu(RsuCmd cmd, IntersectionRSU rsu)
        {
            rsu.RadioInbox(new RsuMessage(VehicleId, cmd));
        }
    }
}
