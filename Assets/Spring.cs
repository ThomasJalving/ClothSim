using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring
{
    public int startVertexIndex;
    public int endVertexIndex;
    public float restLength;

    public Spring (int startVertexIndex, int endVertexIndex, float restLength)
    {
        this.startVertexIndex = startVertexIndex;
        this.endVertexIndex = endVertexIndex;
        this.restLength = restLength;
    }

}
