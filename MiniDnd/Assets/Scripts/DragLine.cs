using UnityEngine;

public class DragLine : MonoBehaviour
{
    public RectTransform StartKnob;
    public RectTransform EndKnob;
    public RectTransform Line;

    public Vector2 Start;
    public Vector2 End;

    public void Update()
    {
        StartKnob.position = Start;
        EndKnob.position = End;
        Line.position = Start;

        var delta = End - Start;
        var rotationAngle = Mathf.Atan2(delta.y, delta.x);
        var rotation = Quaternion.Euler(0, 0, rotationAngle * Mathf.Rad2Deg); 
        
        Line.sizeDelta = new Vector2(delta.magnitude, Line.sizeDelta.y);
        Line.rotation = rotation;
        StartKnob.rotation = rotation;
        EndKnob.rotation = rotation;
    }
}
