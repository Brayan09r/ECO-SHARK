using System.Collections.Generic;
using UnityEngine;

namespace EcoShark
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string label = "Basura";
        public GameObject prefab;
        [Min(0)] public int maxAlive = 10;
        [Min(0.1f)] public float interval = 3f;
        [Tooltip("Franja de altura donde aparece: 0 = fondo, 1 = superficie.")]
        public Vector2 heightRange = new Vector2(0f, 1f);
        public bool randomRotation = true;
    }

    /// <summary>
    /// Genera basura y peligros dentro del OceanArea, respetando un máximo por tipo
    /// y sin aparecer encima del tiburón. Todo se ajusta desde el Inspector.
    /// </summary>
    public class OceanSpawner : MonoBehaviour
    {
        [SerializeField] SpawnEntry[] entries = new SpawnEntry[0];
        [SerializeField] Transform player;
        [SerializeField] float minDistanceFromPlayer = 7f;
        [SerializeField] float edgeMargin = 1.5f;
        [Tooltip("Al iniciar, llena cada tipo hasta esta fracción de su máximo.")]
        [Range(0f, 1f)]
        [SerializeField] float prewarmFraction = 0.5f;

        public bool Spawning { get; set; } = true;

        List<GameObject>[] alive;
        float[] timers;

        void Start()
        {
            alive = new List<GameObject>[entries.Length];
            timers = new float[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                alive[i] = new List<GameObject>();
                if (entries[i] == null) continue;
                timers[i] = entries[i].interval;
                int prewarm = Mathf.RoundToInt(entries[i].maxAlive * prewarmFraction);
                for (int k = 0; k < prewarm; k++) Spawn(i);
            }
        }

        void Update()
        {
            if (!Spawning || alive == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                SpawnEntry e = entries[i];
                if (e == null || e.prefab == null) continue;

                List<GameObject> list = alive[i];
                for (int k = list.Count - 1; k >= 0; k--)
                    if (list[k] == null) list.RemoveAt(k); // ya fue tragado/destruido

                if (list.Count >= e.maxAlive) continue;

                timers[i] -= Time.deltaTime;
                if (timers[i] <= 0f)
                {
                    timers[i] = e.interval;
                    Spawn(i);
                }
            }
        }

        void Spawn(int index)
        {
            SpawnEntry e = entries[index];
            if (e == null || e.prefab == null) return;

            OceanArea area = OceanArea.Current;
            Vector3 pos = Vector3.zero;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                pos = area != null
                    ? area.RandomPoint(e.heightRange, edgeMargin)
                    : new Vector3(Random.Range(-20f, 20f), Random.Range(-10f, 5f), 0f);

                if (player == null) break;
                Vector2 diff = new Vector2(pos.x - player.position.x, pos.y - player.position.y);
                if (diff.magnitude >= minDistanceFromPlayer) break;
            }

            Quaternion rot = e.randomRotation ? Random.rotation : e.prefab.transform.rotation;
            alive[index].Add(Instantiate(e.prefab, pos, rot));
        }
    }
}
