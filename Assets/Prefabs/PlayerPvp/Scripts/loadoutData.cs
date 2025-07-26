using System.Collections.Generic;
using UnityEngine;

public static class LoadoutData
{
    public static GameObject[] selectedUnits = new GameObject[6];

    public static List<GameObject> selectedFrontline => new List<GameObject> { selectedUnits[0], selectedUnits[1], selectedUnits[2] };
    public static List<GameObject> selectedBackline => new List<GameObject> { selectedUnits[3], selectedUnits[4], selectedUnits[5] };
}
