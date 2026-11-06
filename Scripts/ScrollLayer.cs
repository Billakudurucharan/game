using System.Collections.Generic;
using UnityEngine;

/// <summary>A horizontally tiling, parallax-scrolling strip of one sprite.</summary>
public sealed class ScrollLayer
{
    readonly Transform root;
    readonly Sprite sprite;
    readonly float tileW;
    readonly float y;
    readonly int order;
    readonly string name;
    readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
    Color tint = Color.white;

    /// <summary>How fast this layer moves relative to the world scroll (1 = same as pipes/ground).</summary>
    public float Speed;

    public ScrollLayer(string name, Transform parent, Sprite sprite, float y, int order, float speed)
    {
        this.name = name;
        this.sprite = sprite;
        this.y = y;
        this.order = order;
        Speed = speed;
        tileW = sprite.bounds.size.x;
        root = new GameObject(name).transform;
        root.SetParent(parent, false);
    }

    public void SetColor(Color c)
    {
        tint = c;
        for (int i = 0; i < tiles.Count; i++) tiles[i].color = c;
    }

    void AddTile()
    {
        var go = new GameObject(name + "_" + tiles.Count);
        go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = tint;
        tiles.Add(sr);
    }

    public void Update(float scroll, float halfWidth)
    {
        int need = Mathf.CeilToInt(2f * halfWidth / tileW) + 2;
        while (tiles.Count < need) AddTile();

        float off = Mathf.Repeat(-scroll * Speed, tileW);
        for (int i = 0; i < tiles.Count; i++)
        {
            float x = -halfWidth - tileW * 0.5f + i * tileW + off;
            tiles[i].transform.position = new Vector3(x, y, 0f);
        }
    }
}
