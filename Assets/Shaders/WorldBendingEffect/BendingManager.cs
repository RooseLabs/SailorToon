using System;
using UnityEngine;

[ExecuteAlways]
public class BendingManager : MonoBehaviour
{
    private const string BENDING_FEATURE = "_ENABLE_BEND";

    private void Awake()
    {
        if( Application.isPlaying )
            Shader.EnableKeyword(BENDING_FEATURE);
        else
            Shader.DisableKeyword(BENDING_FEATURE);
    }   
}
