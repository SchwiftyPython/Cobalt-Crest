using UnityEngine;

namespace Utils
{
    public static class Steering
    {
        public static Vector2 RotateTowards(Vector2 current, Vector2 target, float maxDeltaDeg)
        {
            if (target.sqrMagnitude < 1e-6f) return current;
            if (current.sqrMagnitude < 1e-6f) return target.normalized;

            var ca = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
            var ta = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
            var delta = Mathf.DeltaAngle(ca, ta);
            var clamped = Mathf.Clamp(delta, -maxDeltaDeg, maxDeltaDeg);
            var nextDeg = ca + clamped;
            var r = new Vector2(Mathf.Cos(nextDeg * Mathf.Deg2Rad), Mathf.Sin(nextDeg * Mathf.Deg2Rad));
            return r.normalized;
        }

        public static Vector2 FromAngleDeg(float deg)
        {
            var rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}