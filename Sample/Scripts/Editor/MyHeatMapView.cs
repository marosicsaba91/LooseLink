using UnityEngine;
using Utility.SerializableCollection.Editor;

public class MyHeatMapView : Matrix2DIntHeatmapView
{
    static readonly Color minColor = new Color(0.54f, 0.94f, 0.67f);
    static readonly Color middleColor = new Color(0.24f, 0.8f, 0.92f);
    static readonly Color maxColor = new Color(0.37f, 0.39f, 0.79f); 
    protected override Color? CellColor(int x, int y, int element)=>
        HeatMapHelper.GetColor(element, min, max, minColor, middleColor, maxColor);
}
