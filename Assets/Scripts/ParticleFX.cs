using System.Collections.Generic;
using UnityEngine;

/// <summary>Very small sprite-based particle pool (feathers, puffs, sparkles).</summary>
public sealed class ParticleFX
{
    sealed class P
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float life, maxLife, spin, gravity, size0, size1;
        public Color color;
        public bool active;
    }

    readonly List<P> pool = new List<P>();
    readonly Transform root;
    readonly Sprite sprite;
    readonly int capacity;
    readonly float spriteSize;

    public ParticleFX(Transform parent, Sprite sprite, int capacity)
    {
        this.sprite = sprite;
        this.capacity = capacity;
        spriteSize = sprite.bounds.size.x;
        root = new GameObject("Particles").transform;
        root.SetParent(parent, false);
    }

    public void Emit(Vector2 pos, Vector2 vel, Color color, float life, float size0, float size1, float gravity, float spin)
    {
        P p = null;
        for (int i = 0; i < pool.Count; i++)
            if (!pool[i].active) { p = pool[i]; break; }

        if (p == null)
        {
            if (pool.Count >= capacity) return;
            p = new P();
            var go = new GameObject("p");
            go.transform.SetParent(root, false);
            p.t = go.transform;
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sprite = sprite;
            p.sr.sortingOrder = 25;
            pool.Add(p);
        }

        p.active = true;
        p.vel = vel;
        p.life = p.maxLife = life;
        p.size0 = size0;
        p.size1 = size1;
        p.gravity = gravity;
        p.spin = spin;
        p.color = color;
        p.t.position = new Vector3(pos.x, pos.y, 0f);
        p.t.rotation = Quaternion.identity;
        p.t.gameObject.SetActive(true);
    }

    public void Update(float dt)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            P p = pool[i];
            if (!p.active) continue;

            p.life -= dt;
            if (p.life <= 0f)
            {
                p.active = false;
                p.t.gameObject.SetActive(false);
                continue;
            }

            float k = 1f - p.life / p.maxLife;           // 0 -> 1 over the particle's life
            p.vel.y += p.gravity * dt;
            Vector3 pos = p.t.position;
            pos.x += p.vel.x * dt;
            pos.y += p.vel.y * dt;
            p.t.position = pos;
            p.t.Rotate(0f, 0f, p.spin * dt);

            float size = Mathf.Lerp(p.size0, p.size1, k) / spriteSize;
            p.t.localScale = new Vector3(size, size, 1f);
            Color c = p.color;
            c.a *= 1f - k * k;
            p.sr.color = c;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].active = false;
            pool[i].t.gameObject.SetActive(false);
        }
    }
}
