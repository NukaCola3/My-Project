using System;

[Serializable]
public struct CubeMove
{
    public RotationAxis axis;
    public int layer;
    public int direction;

    public CubeMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        this.axis = axis;
        this.layer = layer;
        this.direction = direction;
    }
}