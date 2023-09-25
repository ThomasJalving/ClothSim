using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothMesh : MonoBehaviour
{
    Mesh mesh;

    [Header("Size")]
    public int width = 10;
    public int height = 10;
    public float distance = 0.1f;

    [Header("Variables")]
    public float vertexWeight = 1f;
    [Range(0f, 1000f)]
    public float stiffnessK = 500f;
    [Range(0f, 0.1f)]
    public float dampingK = 0.01f;
    [Range(0f, 10f)]
    public float airResistance = 0.1f;
    public float breakForce = 10000000;
    [Header("Wind Force")]
    public bool localWind = true;
    public Vector3 windForce = Vector3.zero;
    public Vector2Int[] fixedVertices = { new Vector2Int(0, 0), new Vector2Int(9, 0) };
    public int[] fixedColumns = {};
    public int[] fixedRows = {};

    Vector3[] vertices;
    Vector3[] previousVertices;
    Vector3[] velocities;
    Vector3[] forces;
    List<int> triangles;
    List<Spring> springs;
    float inverseWeight;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        createVertices();
        assignVertices();
        createTriangles();
        mesh.triangles = triangles.ToArray();
        createSprings();
        velocities = new Vector3[width * height];
    }

    private void FixedUpdate()
    {
        inverseWeight = 1 / vertexWeight;
        forces = new Vector3[width * height];
        //external forces
        for (int i = 0; i < width * height; i++)
        {
            bool isFixed = isFixedVertexVertex(i);
            if(!isFixed)
            {
                Quaternion transform = GetComponent<Transform>().rotation;
                //relative to world
                forces[i] = Physics.gravity * vertexWeight; //add gravity
                if (!localWind)
                    forces[i] += windForce;
                //transform relative to world to relative to object
                forces[i] = Quaternion.Inverse(transform) * forces[i];
                if (localWind)
                    forces[i] += windForce;
                //relative to object
                forces[i] -= airResistance * velocities[i]; //deduct air resistance
            }
        }
        //internal forces
        foreach (Spring spring in springs)
        {
            Vector3 xij = vertices[spring.endVertexIndex] - vertices[spring.startVertexIndex];
            Vector3 vij = velocities[spring.endVertexIndex] - velocities[spring.startVertexIndex];
            float xijDistance = xij.magnitude;
            //add spring force
            forces[spring.startVertexIndex] += stiffnessK * (xijDistance - spring.restLength) * xij.normalized;
            forces[spring.endVertexIndex] -= stiffnessK * (xijDistance - spring.restLength) * xij.normalized;
            //add damping force
            forces[spring.startVertexIndex] += dampingK * (Vector3.Dot(vij, xij.normalized)) * xij.normalized;
            forces[spring.endVertexIndex] -= dampingK * (Vector3.Dot(vij, xij.normalized)) * xij.normalized;
        }

        //integrateVelocity();
        //integratePosition();
        Vector3 current;
        RaycastHit hit;
        for (int i = 0; i < width * height; i++)
        {
            if (!isFixedVertexVertex(i))
            {
                current = vertices[i];
                vertices[i] += vertices[i] - previousVertices[i] + forces[i] * Time.fixedDeltaTime * Time.fixedDeltaTime * inverseWeight;
                previousVertices[i] = current;
                Debug.DrawRay(transform.TransformPoint(previousVertices[i]), vertices[i] - previousVertices[i], Color.red, 0f);
                if(Physics.Raycast(transform.TransformPoint(previousVertices[i]) - 0.3f * (vertices[i] - previousVertices[i]), vertices[i] - previousVertices[i], out hit, (vertices[i] - previousVertices[i]).magnitude * 1.5f, Physics.AllLayers)){
                    vertices[i] = transform.InverseTransformPoint(hit.point);
                }
                velocities[i] = (vertices[i] - previousVertices[i]) / Time.fixedDeltaTime;
            }
            
        }

        assignVertices();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    void integratePosition()
    {
        for(int i = 0; i < vertices.Length; i++)
        {
            bool isFixed = isFixedVertexVertex(i);
            if (!isFixed)
            {
                vertices[i] += Time.fixedDeltaTime * velocities[i];
            }
            
        }
    }

    void assignVertices(){
        Vector3[] verticesTwice = new Vector3[2*vertices.Length];
        vertices.CopyTo(verticesTwice, 0);
        vertices.CopyTo(verticesTwice, vertices.Length);
        mesh.vertices = verticesTwice;
    }

    bool isFixedVertexVertex(int index){
        bool isFixed = false;
        foreach (Vector2 fixedVertex in fixedVertices)
            {
                isFixed = isFixed || index == fixedVertex.x + width * fixedVertex.y;
            }
            foreach (int fixedColumn in fixedColumns)
            {
                for(int i = 0; i < height; i++)
                    isFixed = isFixed || index == fixedColumn + width * i;
            }
            foreach (int fixedRow in fixedRows)
            {
                isFixed = isFixed || (index >= 0 + width * fixedRow && index <= width - 1 + width * fixedRow);
            }
            return isFixed;
    }

    void integrateVelocity()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            velocities[i] += forces[i] * Time.fixedDeltaTime * inverseWeight;
        }
    }

    void createVertices()
    {
        vertices = new Vector3[width * height];
        previousVertices = new Vector3[width * height];
        //create vertices row wise
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                vertices[x + width * y] = new Vector3(x * distance, -y * distance, 0);
                previousVertices[x + width * y] = vertices[x + width * y];
            }
    }

    void createTriangles()
    {
        triangles = new List<int>();

        for (int y = 0; y < height - 1; y++)
            for (int x = 0; x < width - 1; x++)
            {
                //forward facing triangles
                triangles.Add(x + width * y);
                triangles.Add(x + 1 + width * y);
                triangles.Add(x + width * (y + 1));

                triangles.Add(x + 1 + width * y);
                triangles.Add(x + +1 + width * (y + 1));
                triangles.Add(x + width * (y + 1));

                //backward facing triangles
                triangles.Add(width * height + x + width * (y + 1));
                triangles.Add(width * height + x + 1 + width * y);
                triangles.Add(width * height + x + width * y);


                triangles.Add(width * height + x + width * (y + 1));
                triangles.Add(width * height + x + +1 + width * (y + 1));
                triangles.Add(width * height + x + 1 + width * y);
            }
    }

    void createSprings()
    {
        springs = new List<Spring>();
        //horizontal and vertical
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if(x != width - 1)
                    springs.Add(new Spring(x + width * y, x + 1 + width * y, distance));
                if (y != height - 1)
                    springs.Add(new Spring(x + width * y, x + width * (y + 1), distance));
            }
        //diagonal
        for (int y = 0; y < height - 1; y++)
            for (int x = 0; x < width - 1; x++)
            {
                springs.Add(new Spring(x + width * y, x + 1 + width * (y+1), Mathf.Sqrt(distance * distance * 2)));
                springs.Add(new Spring(x + 1 + width * y, x + width * (y + 1), Mathf.Sqrt(distance * distance * 2)));
            }
    }
}
