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
        private float _stateSince;
        private Vector3 _anchor;
        private float _yaw;
        private float _now;

        public State Current { get; private set; } = State.Parked;
        public bool IsFree => Current == State.Parked;
        public bool ReadyToBoard => Current == State.Waiting && _rider != null;
        public bool HasPassenger => Current == State.Carrying;
        public Vector3 Home => _home;

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
            var flat = new Vector2(to.x - _from.x, to.z - _from.z).magnitude;
            _duration = 2.5f + flat / CruiseSpeed;
            // A short hop barely clears the buildings; a long crossing climbs higher.
            _peak = Mathf.Clamp(2.5f + flat * 0.22f, 2.5f, 8f);
            _flightStart = _now;
            Current = state;
            _stateSince = _now;
        }

        /// <summary>Point on the current flight at normalised time <paramref name="t"/> (0..1).</summary>
        public Vector3 Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            var s = t * t * (3f - 2f * t);   // eases out of the lift-off and into the landing
            var p = Vector3.Lerp(_from, _to, s);
            p.y += _peak * Mathf.Sin(Mathf.PI * t);
            return p;
        }

        private void FlightStep(float deltaTime)
        {
            var t = Mathf.Clamp01((_now - _flightStart) / _duration);
            var before = transform.position;
            var position = Evaluate(t);

            // Turn the viewport toward the way it travels and lean a few degrees into it.
            var velocity = deltaTime > 0f ? (position - before) / deltaTime : Vector3.zero;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            if (flat.sqrMagnitude > 0.04f)
                _yaw = Mathf.MoveTowardsAngle(_yaw, Quaternion.LookRotation(flat).eulerAngles.y, 90f * deltaTime);
            var lean = Mathf.Clamp(flat.magnitude * 2.2f, 0f, 7f);
            transform.SetPositionAndRotation(position, Quaternion.Euler(lean, _yaw, 0f));

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
