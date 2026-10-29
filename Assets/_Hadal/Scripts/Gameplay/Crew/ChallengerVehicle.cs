using UnityEngine;

namespace Hadal.Gameplay.Crew
{
    /// <summary>Someone a Challenger can carry (a crew member; tests use a stand-in).</summary>
    public interface IChallengerRider
    {
        Transform Body { get; }

        /// <summary>The rider is now in the pilot sphere; it stops moving on its own.</summary>
        void OnBoarded();

        /// <summary>The Challenger has landed at the drop-off; the rider steps out at <paramref name="at"/>.</summary>
        void OnArrived(Vector3 at);
    }

    /// <summary>
    /// One Challenger submersible (art/blender/challenger.py). Cosmetic: hovers by its hangar, makes
    /// short salvage dives to the seabed around the base, and gives crew members a lift between distant
    /// buildings. Flights rise almost vertically, cross in a gentle arc and settle straight down, the way
    /// a vertical submersible moves; the hull turns slowly while it hovers.
    /// </summary>
    public sealed class ChallengerVehicle : MonoBehaviour
    {
        public enum State
        {
            Parked,
            ToPickup,
            Waiting,
            Carrying,
            Returning,
            ToSite,
            AtSite
        }

        /// <summary>Where a passenger sits in the pilot sphere (Blender SEAT (0, -0.14, 0.18) → Unity).</summary>
        public static readonly Vector3 SeatLocal = new(0f, 0.13f, 0.14f);

        /// <summary>A 1.1 m crew member shrunk to fit the 0.66 m sphere.</summary>
        public const float RiderScale = 0.42f;

        private const float CruiseSpeed = 2.6f;   // m/s across the base
        private const float HoverSpin = 9f;       // deg/s while hovering (the real one turned slowly as it sank)
        private const float SiteSeconds = 5f;
        private const float PitchSeconds = 1f;      // lying down after departure / standing up before the stop
        private const float DescendSeconds = 1.3f;  // the last vertical sink onto the stop
        private const float RiseHeight = 1.2f;
        private const float ApproachHeight = 1.6f;
        private const float TurnRate = 120f;        // deg/s toward the heading

        private Transform _pilot;
        private IChallengerRider _rider;
        private Vector3 _riderScale = Vector3.one;
        private Transform _riderParent;
        private Vector3 _home;
        private Vector3 _dropoff;
        private Vector3 _from;
        private Vector3 _to;
        private float _flightStart;
        private float _duration = 1f;
        private float _peak;
        private float _cruiseSeconds;
        private float _cruisePitch;
        private float _heading;
        private float _pitch;
        private float _bank;
        private float _stateSince;
        private Vector3 _anchor;
        private float _yaw;
        private float _now;

        public State Current { get; private set; } = State.Parked;
        public bool IsFree => Current == State.Parked;
        public bool ReadyToBoard => Current == State.Waiting && _rider != null;
        public bool HasPassenger => Current == State.Carrying;
        public Vector3 Home => _home;

        /// <summary>0 upright, 90 lying along the direction of travel.</summary>
        public float Pitch => _pitch;

        /// <summary>
        /// Upright hull turned to <paramref name="yaw"/>, pitched forward by <paramref name="pitch"/> (90 = the
        /// top points along the heading, the viewport and light bar toward the seabed, the sphere trailing)
        /// and rolled about its long axis by <paramref name="bank"/>.
        /// </summary>
        public static Quaternion Pose(float yaw, float pitch, float bank) =>
            Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, bank, 0f);

        public void Initialise(Vector3 home, float yaw)
        {
            _pilot = transform.Find("pilot");
            _home = home;
            _yaw = yaw;
            transform.SetPositionAndRotation(home, Quaternion.Euler(0f, yaw, 0f));
            Park();
        }

        public void SetHome(Vector3 home)
        {
            _home = home;
            if (Current == State.Parked && (transform.position - home).sqrMagnitude > 4f)
                Fly(home, State.Returning);
        }

        /// <summary>Sends a parked Challenger to land at <paramref name="pickup"/> for <paramref name="rider"/>.</summary>
        public bool Book(IChallengerRider rider, Vector3 pickup, Vector3 dropoff)
        {
            if (!IsFree || rider == null)
                return false;

            _rider = rider;
            _dropoff = dropoff;
            Fly(pickup, State.ToPickup);
            return true;
        }

        /// <summary>The rider has reached the landed Challenger: seat them and take off for the drop-off.</summary>
        public void Board()
        {
            if (!ReadyToBoard)
                return;

            var body = _rider.Body;
            _riderScale = body.localScale;
            _riderParent = body.parent;
            body.SetParent(transform, false);
            body.localPosition = SeatLocal;
            body.localRotation = Quaternion.identity;
            body.localScale = _riderScale * RiderScale;
            if (_pilot != null)
                _pilot.gameObject.SetActive(false);   // the passenger drives
            _rider.OnBoarded();
            Fly(_dropoff, State.Carrying);
        }

        /// <summary>The rider gave up waiting (or left the base); the Challenger goes home.</summary>
        public void Cancel(IChallengerRider rider)
        {
            if (_rider != rider)
                return;

            if (Current == State.Carrying)
                Unload();
            _rider = null;
            if (Current is State.ToPickup or State.Waiting or State.Carrying)
                Fly(_home, State.Returning);
        }

        /// <summary>A short salvage dive to the seabed and back (ambient life while nobody needs a lift).</summary>
        public void Dive(Vector3 site)
        {
            if (IsFree)
                Fly(site, State.ToSite);
        }

        private void Update() => Step(Time.time, Time.deltaTime);

        /// <summary>Advances the vehicle; public so tests can drive it without a frame loop.</summary>
        public void Step(float time, float deltaTime)
        {
            _now = time;
            if (_rider != null && _rider.Body == null)
            {
                // The crew member left the base (headcount shrank) while booked or riding.
                _rider = null;
                if (_pilot != null)
                    _pilot.gameObject.SetActive(true);
                if (Current is State.ToPickup or State.Waiting or State.Carrying)
                    Fly(_home, State.Returning);
            }

            switch (Current)
            {
                case State.ToPickup:
                case State.Carrying:
                case State.Returning:
                case State.ToSite:
                    FlightStep(deltaTime);
                    break;

                case State.Waiting:
                    // Landed on its skids, light bar on, turning a little while it waits.
                    Spin(deltaTime * 0.3f);
                    break;

                case State.AtSite:
                    Spin(deltaTime);
                    if (time - _stateSince >= SiteSeconds)
                        Fly(_home, State.Returning);
                    break;

                default:
                    Hover(deltaTime);
                    break;
            }
        }

        private void Fly(Vector3 to, State state)
        {
            _from = transform.position;
            _to = to;
            var trip = new Vector3(to.x - _from.x, 0f, to.z - _from.z);
            var flat = trip.magnitude;
            // Only a real trip lies down to cruise; a short repositioning stays upright.
            _cruisePitch = flat > 3f ? 90f : 0f;
            if (flat > 0.01f)
                _heading = Quaternion.LookRotation(trip).eulerAngles.y;
            else
                _heading = _yaw;
            // A short hop barely clears the buildings; a long crossing climbs higher.
            _peak = Mathf.Clamp(1.5f + flat * 0.18f, 1.5f, 6.5f);
            _cruiseSeconds = 1.2f + flat / CruiseSpeed;
            _duration = PitchSeconds + _cruiseSeconds + PitchSeconds + DescendSeconds;
            _flightStart = _now;
            Current = state;
            _stateSince = _now;
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>
        /// Pose on the current flight at <paramref name="seconds"/> after departure: rise while pitching
        /// over (~1 s), cruise level along a gentle arc with the hull horizontal (top first, pilot sphere
        /// at the back), slow down and pitch upright again (~1 s), then sink straight down onto the stop.
        /// </summary>
        public Vector3 Evaluate(float seconds, out float pitch)
        {
            var cruiseFrom = _from + Vector3.up * RiseHeight;
            var cruiseTo = _to + Vector3.up * ApproachHeight;
            var t = seconds;

            if (t < PitchSeconds)
            {
                var s = Smooth(t / PitchSeconds);
                pitch = _cruisePitch * s;
                return Vector3.Lerp(_from, cruiseFrom, s);
            }

            t -= PitchSeconds;
            if (t < _cruiseSeconds)
            {
                var u = t / _cruiseSeconds;
                var h = Smooth(u);   // gathers speed after the pitch-over, slows before the stop
                var p = Vector3.Lerp(cruiseFrom, cruiseTo, h);
                p.y += _peak * Mathf.Sin(Mathf.PI * u);
                // Nose follows the arc a little: up while climbing, down while gliding in.
                pitch = _cruisePitch > 0f ? _cruisePitch - Mathf.Sin(2f * Mathf.PI * u) * 10f : 0f;
                return p;
            }

            t -= _cruiseSeconds;
            if (t < PitchSeconds)
            {
                pitch = _cruisePitch * (1f - Smooth(t / PitchSeconds));
                return cruiseTo;
            }

            t -= PitchSeconds;
            pitch = 0f;
            return Vector3.Lerp(cruiseTo, _to, Smooth(t / DescendSeconds));
        }

        private void FlightStep(float deltaTime)
        {
            var elapsed = _now - _flightStart;
            var t = Mathf.Clamp01(elapsed / _duration);
            var position = Evaluate(Mathf.Min(elapsed, _duration), out _pitch);

            // Head for the destination; bank into the turn and roll gently while cruising.
            var before = _yaw;
            if (_cruisePitch > 0f)
                _yaw = Mathf.MoveTowardsAngle(_yaw, _heading, TurnRate * deltaTime);
            var yawRate = deltaTime > 0f ? Mathf.DeltaAngle(before, _yaw) / deltaTime : 0f;
            var lying = _pitch / 90f;
            var wantedBank = Mathf.Clamp(-yawRate * 0.18f, -22f, 22f) + Mathf.Sin(_now * 0.9f) * 4f;
            _bank = Mathf.Lerp(_bank, wantedBank * lying, deltaTime > 0f ? Mathf.Clamp01(deltaTime * 3f) : 1f);
            transform.SetPositionAndRotation(position, Pose(_yaw, _pitch, _bank));

            if (t < 1f)
                return;

            switch (Current)
            {
                case State.ToPickup:
                    Current = State.Waiting;
                    break;
                case State.Carrying:
                    Unload();
                    Fly(_home, State.Returning);
                    return;
                case State.ToSite:
                    Current = State.AtSite;
                    break;
                default:
                    Park();
                    return;
            }

            _stateSince = _now;
        }

        private void Unload()
        {
            var rider = _rider;
            _rider = null;
            if (_pilot != null)
                _pilot.gameObject.SetActive(true);
            if (rider == null || rider.Body == null)
                return;

            var body = rider.Body;
            body.SetParent(_riderParent, true);
            body.localScale = _riderScale;
            var step = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward * 0.8f;   // out in front of the viewport
            var at = new Vector3(transform.position.x + step.x, 0f, transform.position.z + step.z);
            body.position = at;
            body.rotation = Quaternion.Euler(0f, _yaw, 0f);
            rider.OnArrived(at);
        }

        private void Park()
        {
            _pitch = 0f;
            _bank = 0f;
            Current = State.Parked;
            _stateSince = _now;
            _anchor = transform.position;
        }

        private void Hover(float deltaTime)
        {
            // Slow drift and a gentle rise and fall around where it parked; zero offset on arrival.
            var tau = _now - _stateSince;
            var drift = new Vector3(Mathf.Sin(tau * 0.31f) * 0.8f, Mathf.Sin(tau * 0.5f) * 0.35f, (1f - Mathf.Cos(tau * 0.23f)) * 0.6f);
            Spin(deltaTime);
            transform.SetPositionAndRotation(_anchor + drift, Quaternion.Euler(0f, _yaw, 0f));
        }

        private void Spin(float deltaTime)
        {
            _yaw = Mathf.Repeat(_yaw + HoverSpin * deltaTime, 360f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
