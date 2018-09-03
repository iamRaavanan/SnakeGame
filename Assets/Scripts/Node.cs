using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Raavanan
{
    public class Node
    {
        public int                  _x;
        public int                  _y;
        public Vector3              _WorldPos;

        public Node (int pX, int pY, Vector3 pWorldPos)
        {
            _x = pX;
            _y = pY;
            _WorldPos = pWorldPos;
        }
    }
}