using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astrah
{
    public class Buffers
    {
        // - [ Check Splat Map Sample If It's Bright Red (TerrainLayer0 is Bright Red) ]
        private static bool isGrass(float x, float z, int bladesPerX, Texture2D splatMap, 
            float curIncrementX, float curIncrementZ, Terrain terrain, FreeGrass freeGrass)
        {
            int curSplatToCheck_X       = (int)((x + 1) / bladesPerX * splatMap.width);
            int curSplatToCheck_Z       = (int)((z + 1) / bladesPerX * splatMap.height); //yes also bladesPerX, that's not a mistake.
            Color splat                 = splatMap.GetPixel((int)curSplatToCheck_X, (int)curSplatToCheck_Z);
            
            // - By default is the red value is above 0.8...
            if (splat.r > freeGrass.SplatMapGrassThreshold && splat.g < 0.2 && splat.b < 0.2 && splat.a == 0.0)
            {
                return true;
            } else
            {
                return false;
            }
        }

        // - [ Initialize Mesh Properties Buffer (Terrain Version) ]
        public static void InitializeMeshPropertiesBuffer_Terr(ref ComputeBuffer meshPropertiesBuffer,
            int ObjCount, ref Material material, Terrain terrain, float meshScale, FreeGrass freeGrass)
        {
            Structs.MeshProperties[] meshProperties = new Structs.MeshProperties[ObjCount];
          
            int squObjCount = (int)Mathf.Sqrt(ObjCount);
            Vector3 terPos = terrain.transform.position;
            float terSizeX = terrain.terrainData.size.x;
            float terSizeZ = terrain.terrainData.size.z;
            int heightMapRes = terrain.terrainData.heightmapResolution;
            float Xincrement = terSizeX / (squObjCount - 1);
            float Zincrement = terSizeZ / (squObjCount - 1);
            int total = 0;
            float curIncrement_X = 0f;
            float curIncrement_Z = 0f;

            Vector3 meshFinalScale = Vector3.one * meshScale;

            //this is where we should set based on splat map...
            Texture2D splatMap = terrain.terrainData.GetAlphamapTexture(0);

            float splatW = splatMap.width;
            float splatH = splatMap.height;

            for (float z = 0; z < freeGrass.bladesPerRow; z++) //yes we do put in 'bladesPer 'X'' here, that's not a mistake!
            {
                for (float x = 0; x < freeGrass.bladesPerRow; x++)
                {

                    Vector3 objPosition = new Vector3(99999f, 99999f, 99999f);
                    if (isGrass(x, z, freeGrass.bladesPerRow, splatMap, curIncrement_X, curIncrement_Z, terrain, freeGrass))
                    {
                        Vector3 spawnPoint = terrain.transform.position + new Vector3(curIncrement_X, 0f, curIncrement_Z);
                        float sampleHeight = terrain.SampleHeight(spawnPoint);

                        Debug.DrawRay(new Vector3(spawnPoint.x, sampleHeight, spawnPoint.z),Vector3.up, Color.blue, 0.5f);

                        objPosition = new Vector3(terPos.x,terPos.y,terPos.z) + new Vector3(curIncrement_X, sampleHeight, curIncrement_Z);
                    }
                    Structs.MeshProperties meshProperty     = new Structs.MeshProperties();
                    
                    //Vector3 
                    Quaternion objQRotation                 = Quaternion.Euler(0, 0, 0);
                    Vector3 objScale                        = meshFinalScale;

                    meshProperty.matrix     = Matrix4x4.TRS(objPosition, objQRotation, objScale);
                    meshProperty.color      = new Color(0.0f, 0.6f, 0.6f, 1.0f);

                    meshProperties[total] = meshProperty;
                    total++;

                    curIncrement_X += Xincrement;
                }
                
                curIncrement_Z += Zincrement;
                curIncrement_X = 0;
            }


            meshPropertiesBuffer = new ComputeBuffer(ObjCount, Structs.MeshProperties.Size());
            meshPropertiesBuffer.SetData(meshProperties);
            material.SetBuffer("_Properties", meshPropertiesBuffer);

        }
        // - [ Initialize Args Buffer ]
        public static void InitializeArgsBuffer(ref ComputeBuffer argsBuffer, Mesh mesh, int ObjCount)
        {
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = (uint)mesh.GetIndexCount(0);          //0 - > Number of Triangle Indices
            args[1] = (uint)ObjCount;                       //1 - > Object count to instantiate
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
        }
    }
}