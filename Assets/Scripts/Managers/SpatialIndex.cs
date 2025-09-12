using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Managers
{
    public class SpatialIndex : MonoBehaviour
    {
        public static SpatialIndex Instance { get; private set; }

        private SpatialHash2D<Producer> producers;
        private SpatialHash2D<Herbivore> herbivores;
        private SpatialHash2D<Carnivore> carnivores;

        private readonly List<Producer> pBuf = new(64);
        private readonly List<Herbivore> hBuf = new(64);
        private readonly List<Carnivore> cBuf = new(64);

        private float timer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var cell = (ConfigService.Instance?.Sim?.cellSize) ?? 2f;
            producers = new SpatialHash2D<Producer>(cell);
            herbivores = new SpatialHash2D<Herbivore>(cell);
            carnivores = new SpatialHash2D<Carnivore>(cell);
        }

        private void Update()
        {
            var interval = (ConfigService.Instance?.Sim?.indexRebuildInterval) ?? 0.25f;
            timer += Time.unscaledDeltaTime;
            if (timer < interval) return;
            timer = 0f;
            Rebuild();
        }

        public void Rebuild()
        {
            producers.Clear();
            herbivores.Clear();
            carnivores.Clear();

            foreach (var p in Producer.All) if (p != null) producers.Add(p.transform.position, p);
            foreach (var h in Herbivore.All) if (h != null) herbivores.Add(h.transform.position, h);
            foreach (var c in Carnivore.All) if (c != null) carnivores.Add(c.transform.position, c);
        }

        public int QueryProducers(Vector2 pos, float radius, List<Producer> outList)
        {
            producers.Query(pos, radius, pBuf);
            outList.Clear();
            var r2 = radius * radius;
            for (var i = 0; i < pBuf.Count; i++)
            {
                var p = pBuf[i];
                if (p == null) continue;
                if (((Vector2)p.transform.position - pos).sqrMagnitude <= r2) outList.Add(p);
            }
            return outList.Count;
        }

        public int QueryHerbivores(Vector2 pos, float radius, List<Herbivore> outList)
        {
            herbivores.Query(pos, radius, hBuf);
            outList.Clear();
            var r2 = radius * radius;
            for (var i = 0; i < hBuf.Count; i++)
            {
                var h = hBuf[i];
                if (h == null) continue;
                if (((Vector2)h.transform.position - pos).sqrMagnitude <= r2) outList.Add(h);
            }
            return outList.Count;
        }

        public int QueryCarnivores(Vector2 pos, float radius, List<Carnivore> outList)
        {
            carnivores.Query(pos, radius, cBuf);
            outList.Clear();
            var r2 = radius * radius;
            for (var i = 0; i < cBuf.Count; i++)
            {
                var c = cBuf[i];
                if (c == null) continue;
                if (((Vector2)c.transform.position - pos).sqrMagnitude <= r2) outList.Add(c);
            }
            return outList.Count;
        }
    }
}
