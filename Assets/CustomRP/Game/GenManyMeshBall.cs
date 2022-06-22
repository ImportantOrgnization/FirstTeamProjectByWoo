using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenManyMeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    [SerializeField]
    private Mesh mesh = default;
    [SerializeField]
    private Material material = default;
    Matrix4x4[] matrices = new Matrix4x4[1022];
    Vector4[] baseColors = new Vector4[1022];
    float[] metallics = new float[1022];
    float[] smoothnesses = new float[1022];

    private MaterialPropertyBlock block;

    void  Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 12f,
                Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f),
                Vector3.one * Random.Range(0.5f, 1.5f));
            baseColors[i] = new Vector4(Random.value , Random.value,Random.value ,Random.Range(0.5f,1f));
            //metallics[i] = Random.value < .25f ? 1.0f : 0f;
            metallics[i] = 0.5f;
            smoothnesses[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId,baseColors);
            block.SetFloatArray(metallicId,metallics);
            block.SetFloatArray(smoothnessId,smoothnesses);
        }
        
        Graphics.DrawMeshInstanced(mesh,0,material,matrices,1022,block);
    }

}
