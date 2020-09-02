using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTests : MonoBehaviour
{
    private float[] threeDimensions;

    public Vector3Int dimensions;
    
    // Start is called before the first frame update
    void Start()
    {
//        dimensions.x = 3;
//        dimensions.y = 7;
//        dimensions.z = 11;

        threeDimensions = new float[dimensions.x * dimensions.y * dimensions.z];
        int counter = 0;

        for (int zIndex = 0; zIndex < dimensions.z; zIndex++)
        {
            for (int yIndex = 0; yIndex < dimensions.y; yIndex++)
            {
                for (int xIndex = 0; xIndex < dimensions.x; xIndex++)
                {
                    int flat = Flat(xIndex, yIndex, zIndex, dimensions);
                    threeDimensions[flat] = counter;
                    Debug.Log($"falt:{flat},x:{xIndex},y:{yIndex},z:{zIndex}, counter:"+counter);
                    counter++;
                }
            }
        }
        
        for (int i = 0; i < threeDimensions.Length; i++)
        {
            float r =threeDimensions[i];
            Vector3Int unflat = UnFlat(i, dimensions);
            Debug.Log($"index:{i},x:{unflat.x},y:{unflat.y},z:{unflat.z}, counterResult:"+r);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //z * ((amount.x) * (amount.y)) + y * (amount.x) + x
    int Flat(int x, int y, int z, Vector3Int amount)
    {
        return z * ((amount.x) * (amount.y)) + y * (amount.x) + x;
    }

    Vector3Int UnFlat(int index, Vector3Int amount)
    {
        int x = (index % (amount.x*amount.y)%amount.x);
        int y = (index % (amount.x*amount.y)) / amount.x;
        int z = index / (amount.x * amount.y);
        return new Vector3Int(x,y,z);
    }
}
