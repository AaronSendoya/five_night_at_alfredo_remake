using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alfredo.EditorTools
{
    [Serializable]
    public class MapData
    {
        public List<ZoneJson> zones;
        public List<InstanceJson> instances;
    }

    [Serializable]
    public class ZoneJson
    {
        public string name, sprite;
        public float x, y, w, h, sx, sy;
    }

    [Serializable]
    public class InstanceJson
    {
        public string obj, layer, arrival;
        public float x, y, sx, sy, rot, tx, ty;
        public bool hasTarget;
    }

    [Serializable]
    public class SpriteMetaList
    {
        public List<SpriteMeta> sprites;
    }

    [Serializable]
    public class SpriteMeta
    {
        public string name, group;
        public int w, h, ox, oy, frames;
        public float fps;
    }
}
