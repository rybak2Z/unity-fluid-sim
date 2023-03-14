using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidCell : MonoBehaviour
{
    public Vector2Int coordinates = new Vector2Int();

    public void SetCoordinates(int x, int y)
    {
        coordinates.x = x;
        coordinates.y = y;
    }
}
