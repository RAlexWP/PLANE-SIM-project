using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Copyright (c) 2021 Ryan Vazques 'Vazgriz'
 This code is based on Vazgriz's custom scale6 function */

/* similar to Vector3.Scale, but has separate factor negative values on each axis*/
public class Scale_Custom : MonoBehaviour {
    
    public Vector3 ScaleCustom(
        Vector3 value,
        float posX, float negX,
        float posY, float negY,
        float posZ, float negZ
        ) {

        Vector3 result = value;
        if(result.x > 0) {
            result.x *= posX;
        } 
        else if (result.x < 0) {
            result.x *= negX;
        }

        if(result.y > 0 ) {
            result.y *= posY;
        }
        else if (result.y < 0) {
            result.y *= negY;
        }

        if(result.z > 0 ) {
            result.z *= posZ;
        }
        else if (result.z < 0){
            result.z *= negZ;
        }
        return result;
    }
}
