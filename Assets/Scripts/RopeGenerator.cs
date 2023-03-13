using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    private Mesh _mesh;
    private MeshRenderer _renderer;
    [SerializeField]
    private Material _ropeMaterial;
    [SerializeField]
    private int _numOfSides;
    [SerializeField]
    private int _size;
    [SerializeField]
    private float _radius;
    [SerializeField]
    private float _distanceInBetweenSize;
    Vector3[] currentVertices;
    private void Awake()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        _mesh = new Mesh();
        filter.mesh = _mesh;

        _renderer = GetComponent<MeshRenderer>();
    
    }

    private void Start()
    {
        //GenerateCylinderFace();
        //GenerateCylinder();

        var goStart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goStart.GetComponent<SphereCollider>().radius = .2f;
        goStart.GetComponent<MeshFilter>().mesh = new Mesh();
        GenerateCylinder(goStart.GetComponent<MeshFilter>().mesh);
        goStart.AddComponent<Rigidbody>().isKinematic = true;
        goStart.transform.position = transform.TransformPoint(Vector3.zero);



        var goEnd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goEnd.GetComponent<SphereCollider>().radius = .2f;
        goEnd.GetComponent<MeshFilter>().mesh = new Mesh();
        GenerateCylinder(goEnd.GetComponent<MeshFilter>().mesh);
        goEnd.AddComponent<Rigidbody>().isKinematic = false;
        goEnd.transform.position = transform.TransformPoint(Vector3.up + (Vector3.up* goEnd.GetComponent<SphereCollider>().radius));


        goStart.AddComponent<SpringJoint>();

        goStart.GetComponent<SpringJoint>().connectedBody = goEnd.GetComponent<Rigidbody>();

    }

    private void OnDrawGizmosSelected()
    {
        if(currentVertices != null && currentVertices.Length > 0)
        {
            foreach(var vertex in currentVertices)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(vertex), .1f);
            }
        }
    }

    private void GenerateCylinder(Mesh mesh)
    {
        //GenerateCylinderFace();
        List<Vector3> vertices = new List<Vector3>();
        float angleDelta = (360f / _numOfSides) * Mathf.Deg2Rad;

        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _numOfSides; j++)
            {
                vertices.Add(new Vector3(Mathf.Cos(angleDelta * j), -_distanceInBetweenSize * i, Mathf.Sin(angleDelta * j)) * _radius);
            }
        }

        List<int> triangles = new List<int>();

        //triangles[0] = 0;
        //triangles[1] = 7;
        //triangles[2] = 6;

        //triangles[3] = 0;
        //triangles[4] = 1;
        //triangles[5] = 7;

        //triangles[6] = 1;
        //triangles[7] = 8;
        //triangles[8] = 7;

        //triangles[9] = 1;
        //triangles[10] = 2;
        //triangles[11] = 8;

        for (int i = 0; i < _size-1; i++)
        {
            //Debug.Log("___INDICIES___ For : " + i);

            for (int j = 0; j < _numOfSides; j++)
            {
                //Debug.Log("___INDICIES___ For Side : " +j);
                //Triangle One


                //Top Left
                triangles.Add((j + (i * _numOfSides)));
                //Bottom Right

                triangles.Add((((i + 1) * _numOfSides) + ((j + 1) % _numOfSides)));
                //Bottom Left
                triangles.Add(((i + 1) * _numOfSides) + j);

                //Triangle Two

                //Top Left
                triangles.Add((j + (i * _numOfSides)));
                //Top Right
                triangles.Add((i * _numOfSides) + ((j + 1) % _numOfSides));
                //Bottom Right
                triangles.Add(((i + 1) * _numOfSides) + ((j + 1) % _numOfSides));

                ////Top Left
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6)] = (j + (i * _numOfSides));
                ////Bottom Right
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6) + 1] = ((i + 1) * _numOfSides) + ((j + 1) % _numOfSides);
                ////Bottom Left
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6) + 2] = ((i+1)* _numOfSides) + j;

                ////Triangle Two

                ////Top Left
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6) + 3] = (j + (i * _numOfSides));
                ////Top Right
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6) + 4] = (i*_numOfSides) + ((j+1) % _numOfSides);
                ////Bottom Right
                //triangles[(j * _numOfSides) + (i * _numOfSides * 6) + 5] = ((i+1)* _numOfSides) + ((j+1) % _numOfSides);

                ////INDEX INTO TRIANGLE
                ////Top Left
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6));
                ////Bottom Right
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6) + 1);

                ////Bottom Left
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6) + 2);


                ////Triangle Two

                ////Top Left
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6) + 3);

                ////Top Right
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6) + 4);

                ////Bottom Left
                //Debug.Log((j * _numOfSides) + (i * _numOfSides * 6) + 5);



                //if (i == 1)
                //{
                //    Debug.Log("+++++++NEW SIDE: " + j);

                //    Debug.Log("+++++LEFT");
                //    Value AT TRIANGLE
                //    Top Left
                //    Debug.Log((j + (i * _numOfSides)));
                //    Bottom Right
                //    Debug.Log((((j + 1) + ((i + 1) * _numOfSides)) % ((i + 1) * _numOfSides)) + ((i + 1) * _numOfSides));

                //    Bottom Left
                //    Debug.Log((j + ((i + 1) * _numOfSides)));


                //    Debug.Log("+++++RIGHT");
                //    Triangle Two

                //    Top Left
                //    Debug.Log((j + (i * _numOfSides)));

                //    Top Right
                //    Debug.Log(((j + 1 + (i * _numOfSides)) % _numOfSides) == 0 ? (i * _numOfSides) : ((j + 1 + (i * _numOfSides)) % _numOfSides));

                //    Bottom Right
                //    Debug.Log((((j + 1) + ((i + 1) * _numOfSides)) % ((i + 1) * _numOfSides)) + ((i + 1) * _numOfSides));
                //}





            }
        }
        //Debug.Log("Vertices Total: " + triangles.Length);

        //for (int i = 0; i < triangles.Length; i++)
        //    Debug.Log("POINT : " + i + ", VALUE: " + triangles[i]);
        currentVertices = vertices.ToArray();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();


        mesh.RecalculateNormals();

       
    }

    private void GenerateCylinderFace()
    {
        Vector3[] vertices = new Vector3[16];

        vertices[0] = new Vector3(Mathf.Cos(0),0, Mathf.Sin(0));
        vertices[1] = new Vector3(Mathf.Cos(270f * Mathf.Deg2Rad), 0, Mathf.Sin(270f * Mathf.Deg2Rad));
        vertices[2] = new Vector3(Mathf.Cos(180f * Mathf.Deg2Rad), 0, Mathf.Sin(180f * Mathf.Deg2Rad));
        vertices[3] = new Vector3(Mathf.Cos(90f * Mathf.Deg2Rad), 0, Mathf.Sin(90f * Mathf.Deg2Rad));
        int[] triangles = new int[] {
            0,1,3,
            1, 2, 3
        };

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;

        
        _mesh.RecalculateNormals();

        _renderer.sharedMaterial = _ropeMaterial;
    }

}
