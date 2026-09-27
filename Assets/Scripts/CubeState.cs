using System.Collections.Generic;
using UnityEngine;

public class CubeState
{
    public List<Cubie> corners = new List<Cubie>();
    public List<Cubie> edges = new List<Cubie>();
    public List<Cubie> centers = new List<Cubie>();


    // =========================================================
    // CUBIES EINLESEN
    // =========================================================

    public void Build(List<Cubie> cubies)
    {
        corners.Clear();
        edges.Clear();
        centers.Clear();

        foreach (Cubie cubie in cubies)
        {
            switch (cubie.Type)
            {
                case CubieType.Corner:
                    corners.Add(cubie);
                    break;

                case CubieType.Edge:
                    edges.Add(cubie);
                    break;

                case CubieType.Center:
                    centers.Add(cubie);
                    break;
            }
        }
    }


    // =========================================================
    // GRUNDLEGENDE VALIDIERUNG
    // =========================================================

    public bool IsValid()
    {
        return
            corners.Count == 8 &&
            edges.Count == 12 &&
            centers.Count == 6;
    }


    // =========================================================
    // EINDEUTIGE IDs PRÜFEN
    // =========================================================

    public bool HasUniquePieceIDs()
    {
        HashSet<string> cornerIDs =
            new HashSet<string>();

        HashSet<string> edgeIDs =
            new HashSet<string>();


        foreach (Cubie cubie in corners)
        {
            if (!cornerIDs.Add(cubie.pieceID))
            {
                Debug.LogError(
                    "Doppelte Corner-ID gefunden: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        foreach (Cubie cubie in edges)
        {
            if (!edgeIDs.Add(cubie.pieceID))
            {
                Debug.LogError(
                    "Doppelte Edge-ID gefunden: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        return true;
    }


    // =========================================================
    // ORIENTIERUNGEN PRÜFEN
    // =========================================================

    public bool HasValidOrientations()
    {
        int cornerOrientationSum = 0;
        int edgeOrientationSum = 0;


        foreach (Cubie cubie in corners)
        {
            cubie.UpdateOrientation();

            if (
                cubie.orientation < 0 ||
                cubie.orientation > 2
            )
            {
                Debug.LogError(
                    "Ungültige Corner-Orientierung bei " +
                    cubie.pieceID
                );

                return false;
            }

            cornerOrientationSum +=
                cubie.orientation;
        }


        foreach (Cubie cubie in edges)
        {
            cubie.UpdateOrientation();

            if (
                cubie.orientation < 0 ||
                cubie.orientation > 1
            )
            {
                Debug.LogError(
                    "Ungültige Edge-Orientierung bei " +
                    cubie.pieceID
                );

                return false;
            }

            edgeOrientationSum +=
                cubie.orientation;
        }


        // Bei einem echten Rubik's Cube muss die Summe
        // der Corner-Orientierungen durch 3 teilbar sein.

        if (cornerOrientationSum % 3 != 0)
        {
            Debug.LogError(
                "Ungültige Corner-Orientierung! " +
                "Summe = " +
                cornerOrientationSum
            );

            return false;
        }


        // Die Summe der Edge-Orientierungen muss gerade sein.

        if (edgeOrientationSum % 2 != 0)
        {
            Debug.LogError(
                "Ungültige Edge-Orientierung! " +
                "Summe = " +
                edgeOrientationSum
            );

            return false;
        }


        return true;
    }


    // =========================================================
    // KOMPLETTE VALIDIERUNG
    // =========================================================

    public bool Validate()
{
    Debug.Log(
        "CubeState: " +
        corners.Count + " Corners, " +
        edges.Count + " Edges, " +
        centers.Count + " Centers"
    );

    bool valid = true;

        Debug.Log(
        "CubeState Anzahl: " +
        corners.Count + " Corners | " +
        edges.Count + " Edges | " +
        centers.Count + " Centers"
        );

    if (!IsValid())
    
    {
        Debug.LogError(
            "CubeState: Falsche Anzahl an Cubies."
        );

        valid = false;
    }

    if (!HasUniquePieceIDs())
    {
        valid = false;
    }

    if (!HasValidOrientations())
    {
        valid = false;
    }

    return valid;
}


    // =========================================================
    // ZUSTAND AUSGEBEN
    // =========================================================

    public void Print()
    {
        Debug.Log(
            "========== CUBE STATE =========="
        );


        Debug.Log(
            "--- CORNERS ---"
        );

        foreach (Cubie cubie in corners)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        Debug.Log(
            "--- EDGES ---"
        );

        foreach (Cubie cubie in edges)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        Debug.Log(
            "--- CENTERS ---"
        );

        foreach (Cubie cubie in centers)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        Debug.Log(
            "Valid: " +
            Validate()
        );


        Debug.Log(
            "================================"
        );

                Debug.Log("--- CENTERS ---");

        foreach (Cubie cubie in centers)
        {
            Debug.Log(
                "CENTER: " +
                cubie.name +
                " | Position=" +
                cubie.logicalPosition +
                " | ID=" +
                cubie.pieceID
            );
        }
    }
}