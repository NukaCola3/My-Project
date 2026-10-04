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

        HashSet<string> centerIDs =
            new HashSet<string>();


        // -----------------------------------------------------
        // CORNERS
        // -----------------------------------------------------

        foreach (Cubie cubie in corners)
        {
            if (string.IsNullOrEmpty(cubie.pieceID))
            {
                Debug.LogError(
                    "Corner ohne gültige Piece-ID gefunden: " +
                    cubie.name
                );

                return false;
            }

            if (!cornerIDs.Add(cubie.pieceID))
            {
                Debug.LogError(
                    "Doppelte Corner-ID gefunden: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        // -----------------------------------------------------
        // EDGES
        // -----------------------------------------------------

        foreach (Cubie cubie in edges)
        {
            if (string.IsNullOrEmpty(cubie.pieceID))
            {
                Debug.LogError(
                    "Edge ohne gültige Piece-ID gefunden: " +
                    cubie.name
                );

                return false;
            }

            if (!edgeIDs.Add(cubie.pieceID))
            {
                Debug.LogError(
                    "Doppelte Edge-ID gefunden: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        // -----------------------------------------------------
        // CENTERS
        // -----------------------------------------------------

        foreach (Cubie cubie in centers)
        {
            if (string.IsNullOrEmpty(cubie.pieceID))
            {
                Debug.LogError(
                    "Center ohne gültige Piece-ID gefunden: " +
                    cubie.name
                );

                return false;
            }

            if (!centerIDs.Add(cubie.pieceID))
            {
                Debug.LogError(
                    "Doppelte Center-ID gefunden: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        return true;
    }


    // =========================================================
    // EINDEUTIGE POSITIONEN PRÜFEN
    // =========================================================

    public bool HasUniquePositions()
    {
        HashSet<Vector3Int> cornerPositions =
            new HashSet<Vector3Int>();

        HashSet<Vector3Int> edgePositions =
            new HashSet<Vector3Int>();

        HashSet<Vector3Int> centerPositions =
            new HashSet<Vector3Int>();


        // -----------------------------------------------------
        // CORNERS
        // -----------------------------------------------------

        foreach (Cubie cubie in corners)
        {
            if (!cornerPositions.Add(cubie.logicalPosition))
            {
                Debug.LogError(
                    "Doppelte Corner-Position gefunden: " +
                    cubie.logicalPosition +
                    " | Piece=" +
                    cubie.pieceID
                );

                return false;
            }
        }


        // -----------------------------------------------------
        // EDGES
        // -----------------------------------------------------

        foreach (Cubie cubie in edges)
        {
            if (!edgePositions.Add(cubie.logicalPosition))
            {
                Debug.LogError(
                    "Doppelte Edge-Position gefunden: " +
                    cubie.logicalPosition +
                    " | Piece=" +
                    cubie.pieceID
                );

                return false;
            }
        }


        // -----------------------------------------------------
        // CENTERS
        // -----------------------------------------------------

        foreach (Cubie cubie in centers)
        {
            if (!centerPositions.Add(cubie.logicalPosition))
            {
                Debug.LogError(
                    "Doppelte Center-Position gefunden: " +
                    cubie.logicalPosition +
                    " | Piece=" +
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


        // -----------------------------------------------------
        // CORNERS
        // -----------------------------------------------------

        foreach (Cubie cubie in corners)
        {
            // WICHTIG:
            // Die Orientation wird nicht hier neu berechnet.
            //
            // Sie ist Bestandteil des logischen CubeStates
            // und wird bereits während eines Zuges in Cubie
            // aktualisiert.

            if (
                cubie.orientation < 0 ||
                cubie.orientation > 2
            )
            {
                Debug.LogError(
                    "Ungültige Corner-Orientierung bei " +
                    cubie.pieceID +
                    ": " +
                    cubie.orientation
                );

                return false;
            }

            cornerOrientationSum +=
                cubie.orientation;
        }


        // -----------------------------------------------------
        // EDGES
        // -----------------------------------------------------

        foreach (Cubie cubie in edges)
        {
            // Auch die Edge-Orientation wird nur gelesen.
            // CubeState verändert sie nicht.

            if (
                cubie.orientation < 0 ||
                cubie.orientation > 1
            )
            {
                Debug.LogError(
                    "Ungültige Edge-Orientierung bei " +
                    cubie.pieceID +
                    ": " +
                    cubie.orientation
                );

                return false;
            }

            edgeOrientationSum +=
                cubie.orientation;
        }


        // -----------------------------------------------------
        // CORNER-INVARIANTE
        // -----------------------------------------------------
        //
        // Bei einem echten Rubik's Cube muss die Summe
        // aller Corner-Orientierungen durch 3 teilbar sein.
        // -----------------------------------------------------

        if (cornerOrientationSum % 3 != 0)
        {
            Debug.LogError(
                "Ungültige Corner-Orientierung! " +
                "Summe = " +
                cornerOrientationSum
            );

            return false;
        }


        // -----------------------------------------------------
        // EDGE-INVARIANTE
        // -----------------------------------------------------
        //
        // Die Summe aller Edge-Orientierungen muss gerade sein.
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // ANZAHL
        // -----------------------------------------------------

        if (!IsValid())
        {
            Debug.LogError(
                "CubeState: Falsche Anzahl an Cubies."
            );

            valid = false;
        }


        // -----------------------------------------------------
        // PIECE IDs
        // -----------------------------------------------------

        if (!HasUniquePieceIDs())
        {
            valid = false;
        }


        // -----------------------------------------------------
        // POSITIONEN
        // -----------------------------------------------------

        if (!HasUniquePositions())
        {
            valid = false;
        }


        // -----------------------------------------------------
        // ORIENTIERUNGEN
        // -----------------------------------------------------

        if (!HasValidOrientations())
        {
            valid = false;
        }


        if (valid)
        {
            Debug.Log(
                "CubeState ist gültig."
            );
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


        // -----------------------------------------------------
        // CORNERS
        // -----------------------------------------------------

        Debug.Log(
            "--- CORNERS ---"
        );

        foreach (Cubie cubie in corners)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        // -----------------------------------------------------
        // EDGES
        // -----------------------------------------------------

        Debug.Log(
            "--- EDGES ---"
        );

        foreach (Cubie cubie in edges)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        // -----------------------------------------------------
        // CENTERS
        // -----------------------------------------------------

        Debug.Log(
            "--- CENTERS ---"
        );

        foreach (Cubie cubie in centers)
        {
            Debug.Log(
                cubie.GetState()
            );
        }


        // -----------------------------------------------------
        // VALIDIERUNG
        // -----------------------------------------------------

        bool valid =
            Validate();

        Debug.Log(
            "Valid: " +
            valid
        );


        // -----------------------------------------------------
        // CENTER DETAILS
        // -----------------------------------------------------

        Debug.Log(
            "--- CENTER DETAILS ---"
        );

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


        Debug.Log(
            "================================"
        );
    }
}