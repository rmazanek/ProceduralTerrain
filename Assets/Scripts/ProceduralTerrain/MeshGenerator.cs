using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public static class MeshGenerator
    {
        // Mesh has an outer layer used to calculate normals, which will not be rendered
        // Mesh has a second visible layer to stitch together other meshes at LOD == 0, 
        //     no matter what the rest of mesh has for a level of detail.
        // Mesh has a third layer, taking the edge connections from the high res second layer
        // Interior of Mesh (third layer and inwards) is "main mesh," where some vertices
        //     will be skipped when creating lower levels of detail (LOD > 0)
        private static int outOfVisibleMeshLines = 1; // To calc normals
        private static int meshEdgeLines = 1; // High res border to stitch different LODs
        private static int linesOutsideOfMainMesh = outOfVisibleMeshLines + meshEdgeLines;
        public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) 
        {
            int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
            int numVertsPerLine = meshSettings.NumVertsPerLine;

            Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

            MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.UseFlatShading);
            int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
            int meshVertexIndex = 0;
            int outOfMeshVertexIndex = -1;

            for (int y = 0; y < numVertsPerLine; y++)
            {
                for (int x = 0; x < numVertsPerLine; x++)
                {
                    bool isOutOfMeshVertex = IsOutOfVisibleMeshVertex(x, y, numVertsPerLine);
                    bool isSkippedVertex = IsSkippedVertex(x, y, numVertsPerLine, skipIncrement);

                    if (isOutOfMeshVertex)
                    {
                        vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                        outOfMeshVertexIndex--;
                    }
                    else if (!isSkippedVertex)
                    {
                        vertexIndicesMap[x, y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }

            for (int y = 0; y < numVertsPerLine; y++)
            {
                for (int x = 0; x < numVertsPerLine; x++)
                {
                    bool isSkippedVertex = IsSkippedVertex(x, y, numVertsPerLine, skipIncrement);
                    if (isSkippedVertex) continue;
                    
                    bool isOutOfMeshVertex = IsOutOfVisibleMeshVertex(x, y, numVertsPerLine);
                    bool isMeshEdgeVertex = IsVisibleMeshEdgeVertex(x, y, numVertsPerLine);
                    bool isMainVertex = IsMainVertex(x, y, numVertsPerLine, skipIncrement);
                    bool isMainMeshEdgeConnectionVertex = IsMainMeshEdgeConnectionVertex(x, y, numVertsPerLine, skipIncrement);

                    int vertexIndex = vertexIndicesMap[x, y];

                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - linesOutsideOfMainMesh - 1);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                    float height = heightMap[x, y];

                    if (isMainMeshEdgeConnectionVertex)
                    {
                        bool isVertical = x == linesOutsideOfMainMesh || x == numVertsPerLine - linesOutsideOfMainMesh - 1;
                        int distToMainVertexA = ((isVertical) ? y - linesOutsideOfMainMesh : x - linesOutsideOfMainMesh) % skipIncrement;
                        int distToMainVertexB = skipIncrement - distToMainVertexA;
                        float distPercentFromAToB = distToMainVertexA / (float)skipIncrement;

                        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distToMainVertexA, (isVertical) ? y - distToMainVertexA : y];
                        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distToMainVertexB, (isVertical) ? y + distToMainVertexB : y];

                        height = heightOfMainVertexA * ( 1 - distPercentFromAToB) + heightOfMainVertexB * distPercentFromAToB;
                    }
                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = IsVertexForTriangleCreation(x, y, numVertsPerLine, skipIncrement);

                    //Ignore right and bottom vertices when setting triangles
                    if (createTriangle)
                    {
                        int currentIncrement = IsMainMeshAndSkippedForTriangleCreation(x, y, numVertsPerLine, skipIncrement) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
            meshData.FinalizeVerticesAndUVs();

            return meshData;
        }
        private static bool IsOutOfVisibleMeshVertex(int x, int y, int numVertsPerLine)
        {
            return y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
        }
        private static bool IsVisibleMeshEdgeVertex(int x, int y, int numVertsPerLine)
        {
            bool isEdgeOfMeshVertex = y == outOfVisibleMeshLines || y == numVertsPerLine - outOfVisibleMeshLines - 1 || x == outOfVisibleMeshLines || x == numVertsPerLine - outOfVisibleMeshLines - 1;
            bool isOutOfMeshVertex = IsOutOfVisibleMeshVertex(x, y, numVertsPerLine);

            return isEdgeOfMeshVertex && !isOutOfMeshVertex;
        }
        private static bool IsMainMeshEdgeConnectionVertex(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            bool isEdgeConnectionVertex = y == linesOutsideOfMainMesh || y == numVertsPerLine - linesOutsideOfMainMesh - 1 || x == linesOutsideOfMainMesh || x == numVertsPerLine - linesOutsideOfMainMesh - 1;
            bool isMainVertex = IsMainVertex(x, y, numVertsPerLine, skipIncrement);
            bool isMeshEdgeVertex = IsVisibleMeshEdgeVertex(x, y, numVertsPerLine);
            bool isOutOfMeshVertex = IsOutOfVisibleMeshVertex(x, y, numVertsPerLine);

            return isEdgeConnectionVertex && !isMainVertex && !isMeshEdgeVertex && !isOutOfMeshVertex;
        }
        private static bool IsMainVertex(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            bool isDivisibleBySkipIncrement = (x - linesOutsideOfMainMesh) % skipIncrement == 0 && (y - linesOutsideOfMainMesh) % skipIncrement == 0;
            bool isOutOfMeshVertex = IsOutOfVisibleMeshVertex(x, y, numVertsPerLine);
            bool isMeshEdgeVertex = IsVisibleMeshEdgeVertex(x, y, numVertsPerLine);

            return isDivisibleBySkipIncrement && !isOutOfMeshVertex && !isMeshEdgeVertex;
        }
        private static bool IsSkippedVertex(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            bool isEdgeOfMainMesh = x > linesOutsideOfMainMesh && x < numVertsPerLine - linesOutsideOfMainMesh - 1 && y > linesOutsideOfMainMesh && y < numVertsPerLine - linesOutsideOfMainMesh - 1;
            bool isMainVertex = IsMainVertex(x, y, numVertsPerLine, skipIncrement);
            
            return isEdgeOfMainMesh && !isMainVertex;
        }
        private static bool IsMainMeshAndSkippedForTriangleCreation(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            bool isBottomOrRightOfMainMesh = x == numVertsPerLine - linesOutsideOfMainMesh - 1 || y == numVertsPerLine - linesOutsideOfMainMesh - 1;
            
            return IsMainVertex(x, y, numVertsPerLine, skipIncrement) && !isBottomOrRightOfMainMesh;
        }
        private static bool IsOutOfVisibleMeshAndSkippedForTriangleCreation(int x, int y, int numVertsPerLine)
        {
            return x >= numVertsPerLine - 1 || y >= numVertsPerLine - 1;
        }
        // Triangle creation has a direction and these edge connections should not create their own triangles
        private static bool IsSkippedEdgeConnectionVertexForTriangleCreation(int x, int y, int numVertsPerLine)
        {
            return x == linesOutsideOfMainMesh || y == linesOutsideOfMainMesh;
        }
        private static bool IsEdgeConnectionVertexForTriangleCreation(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            return !IsMainMeshEdgeConnectionVertex(x, y, numVertsPerLine, skipIncrement) || !IsSkippedEdgeConnectionVertexForTriangleCreation(x, y, numVertsPerLine);
        }
        private static bool IsVertexForTriangleCreation(int x, int y, int numVertsPerLine, int skipIncrement)
        {
            return !IsOutOfVisibleMeshAndSkippedForTriangleCreation(x, y, numVertsPerLine) && IsEdgeConnectionVertexForTriangleCreation(x, y, numVertsPerLine, skipIncrement);
        }
    }
}