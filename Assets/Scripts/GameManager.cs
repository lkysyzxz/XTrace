using System.Timers;
using UnityEngine;
using XTrace;

public class GameManager : MonoBehaviour
{
    private const string DeltaTimeSamplerName = "DeltaTime";
    
    void Start()
    {
    }

    void Update()
    {
        var deltaTimeSampler = XTraceSampler.GetOrCreateSampler(DeltaTimeSamplerName, "Frame delta time monitor");
        deltaTimeSampler?.Sample(Time.deltaTime, "Frame delta time recording");
    }
}
