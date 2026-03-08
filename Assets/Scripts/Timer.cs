using UnityEngine;

public class TimerBack
{
    private static int Times = 0;

    public static float DeltaTime
    {
        get
        {
            Times++;
            if (Times == 20)
            {
                Times = 0;
                return 2.0f;
            }

            return 0.0333f;
        }
    }
}