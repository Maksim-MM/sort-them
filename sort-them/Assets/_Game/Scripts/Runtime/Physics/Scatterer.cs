using UnityEngine;

namespace SortThem
{
    public class Scatterer : MonoBehaviour
    {
        public Transform Source;
        public float MinSpeed = 7f;
        public float MaxSpeed = 13f;
        public float ConeHalfAngle = 55f;
        public float SpawnJitter = 0.3f;
        public int Seed = 12345;

        public Vector3 SourcePosition => Source != null ? Source.position : transform.position;

        public void LaunchCar(CarInstance car, System.Random rng)
        {
            var pos = SourcePosition + new Vector3(Range(rng, -SpawnJitter, SpawnJitter), Range(rng, 0f, SpawnJitter), Range(rng, -SpawnJitter, SpawnJitter));
            var rot = Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));
            car.Launch(pos, rot, RandomVelocity(rng));
        }

        public void LaunchBody(Rigidbody body, System.Random rng, float speedScale = 1f)
        {
            var pos = SourcePosition + new Vector3(Range(rng, -SpawnJitter, SpawnJitter), Range(rng, 0f, SpawnJitter), Range(rng, -SpawnJitter, SpawnJitter));
            var rot = Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));
            body.isKinematic = false;
            body.position = pos;
            body.rotation = rot;
            body.transform.SetPositionAndRotation(pos, rot);
            body.linearVelocity = RandomVelocity(rng) * speedScale;
            body.angularVelocity = new Vector3(Range(rng, -3f, 3f), Range(rng, -3f, 3f), Range(rng, -3f, 3f));
        }

        public Vector3 RandomVelocity(System.Random rng)
        {
            float angle = Range(rng, 0f, ConeHalfAngle) * Mathf.Deg2Rad;
            float azimuth = Range(rng, 0f, Mathf.PI * 2f);
            var dir = new Vector3(Mathf.Sin(angle) * Mathf.Cos(azimuth), Mathf.Cos(angle), Mathf.Sin(angle) * Mathf.Sin(azimuth));
            return dir * Range(rng, MinSpeed, MaxSpeed);
        }

        static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
