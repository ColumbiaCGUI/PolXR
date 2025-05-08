using UnityEngine;

namespace LinePicking
{
    public class LinePickingPointInfo
    {
        public Transform HitRadargram;

        public Vector3 Point;

        public Vector2 UVCoordinates;

        public Vector3 HitNormal;

        public GameObject DebugVisual;

        public GameObject LineVisual;
    }

    public enum LinePickingDirection
    {
        Forward,
        Backward
    }
}