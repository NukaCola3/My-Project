using System.Collections.Generic;
using UnityEngine;


// ==========================================================
// SOLVER PIECE STATE
// ==========================================================

public struct SolverPieceState
{
    public string pieceID;
    public Vector3Int position;
    public int orientation;


    public SolverPieceState(
        string pieceID,
        Vector3Int position,
        int orientation)
    {
        this.pieceID = pieceID;
        this.position = position;
        this.orientation = orientation;
    }


    public override string ToString()
    {
        return
            $"Piece={pieceID} | " +
            $"Position={position} | " +
            $"Orientation={orientation}";
    }
}


// ==========================================================
// SOLVER STATE
// ==========================================================

public class SolverState
{
    public List<SolverPieceState> corners =
        new List<SolverPieceState>();

    public List<SolverPieceState> edges =
        new List<SolverPieceState>();


    // ======================================================
    // KONSTRUKTOREN
    // ======================================================

    public SolverState()
    {
    }


    public SolverState(List<Cubie> cubies)
    {
        BuildFromCubies(cubies);
    }


    // ======================================================
    // AUS UNITY-CUBIES KOPIEREN
    // ======================================================

    public void BuildFromCubies(
        List<Cubie> cubies)
    {
        corners.Clear();
        edges.Clear();


        if (cubies == null)
        {
            Debug.LogError(
                "SolverState: Cubie-Liste ist null."
            );

            return;
        }


        foreach (Cubie cubie in cubies)
        {
            if (cubie == null)
                continue;


            if (cubie.Type == CubieType.Corner)
            {
                corners.Add(
                    new SolverPieceState(
                        cubie.pieceID,
                        cubie.logicalPosition,
                        cubie.orientation
                    )
                );
            }


            else if (cubie.Type == CubieType.Edge)
            {
                edges.Add(
                    new SolverPieceState(
                        cubie.pieceID,
                        cubie.logicalPosition,
                        cubie.orientation
                    )
                );
            }
        }


        SortPieces();
    }


    // ======================================================
    // FESTE REIHENFOLGE
    // ======================================================

    private void SortPieces()
    {
        corners.Sort(
            (a, b) =>
                string.Compare(
                    a.pieceID,
                    b.pieceID,
                    System.StringComparison.Ordinal
                )
        );


        edges.Sort(
            (a, b) =>
                string.Compare(
                    a.pieceID,
                    b.pieceID,
                    System.StringComparison.Ordinal
                )
        );
    }


    // ======================================================
    // MOVE AUS STRING
    // ======================================================

    public bool ApplyMove(string move)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            Debug.LogError(
                "SolverState: Leerer Move."
            );

            return false;
        }


        string token =
            move.Trim().ToUpperInvariant();


        bool prime =
            token.EndsWith("'");


        bool twice =
            token.EndsWith("2") ||
            token.EndsWith("2'");


        string baseMove =
            token
                .Replace("'", "")
                .Replace("2", "");


        RotationAxis axis;
        int layer;
        int direction;


        switch (baseMove)
        {
            case "U":
                axis = RotationAxis.Y;
                layer = 1;
                direction = 1;
                break;


            case "D":
                axis = RotationAxis.Y;
                layer = -1;
                direction = -1;
                break;


            case "R":
                axis = RotationAxis.X;
                layer = 1;
                direction = 1;
                break;


            case "L":
                axis = RotationAxis.X;
                layer = -1;
                direction = -1;
                break;


            case "F":
                axis = RotationAxis.Z;
                layer = 1;
                direction = -1;
                break;


            case "B":
                axis = RotationAxis.Z;
                layer = -1;
                direction = 1;
                break;


            default:
                Debug.LogError(
                    "SolverState: Unbekannter Move: " +
                    move
                );

                return false;
        }


        if (prime)
        {
            direction *= -1;
        }


        ApplyMove(
            axis,
            layer,
            direction
        );


        if (twice)
        {
            ApplyMove(
                axis,
                layer,
                direction
            );
        }


        return true;
    }


    // ======================================================
    // MOVE DIREKT AUSFÜHREN
    // ======================================================

    public void ApplyMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        ApplyCornerMove(
            axis,
            layer,
            direction
        );


        ApplyEdgeMove(
            axis,
            layer,
            direction
        );
    }


    // ======================================================
    // CORNERS DREHEN
    // ======================================================

    private void ApplyCornerMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        for (int i = 0; i < corners.Count; i++)
        {
            SolverPieceState corner =
                corners[i];


            if (!IsInLayer(
                corner.position,
                axis,
                layer
            ))
            {
                continue;
            }


            Vector3Int oldPosition =
                corner.position;


            // ==================================================
            // CORNER ORIENTATION
            // ==================================================
            //
            // Gleiche Regeln wie in Cubie.UpdateCornerOrientation.
            //
            // Y-Züge verändern die Corner-Orientation nicht.
            // ==================================================

            if (axis != RotationAxis.Y)
            {
                int delta = 0;


                // ----------------------------------------------
                // X
                // ----------------------------------------------

                if (axis == RotationAxis.X)
                {
                    if (direction > 0)
                    {
                        delta =
                            oldPosition.z > 0
                                ? 1
                                : 2;
                    }
                    else
                    {
                        delta =
                            oldPosition.z > 0
                                ? 2
                                : 1;
                    }
                }


                // ----------------------------------------------
                // Z
                // ----------------------------------------------

                else if (axis == RotationAxis.Z)
                {
                    if (direction < 0)
                    {
                        delta =
                            oldPosition.x > 0
                                ? 2
                                : 1;
                    }
                    else
                    {
                        delta =
                            oldPosition.x > 0
                                ? 1
                                : 2;
                    }
                }


                corner.orientation =
                    (
                        corner.orientation +
                        delta
                    ) % 3;
            }


            // ==================================================
            // POSITION
            // ==================================================

            corner.position =
                RotatePosition(
                    oldPosition,
                    axis,
                    direction
                );


            // struct zurückschreiben
            corners[i] =
                corner;
        }
    }


    // ======================================================
    // EDGES DREHEN
    // ======================================================

    private void ApplyEdgeMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            SolverPieceState edge =
                edges[i];


            if (!IsInLayer(
                edge.position,
                axis,
                layer
            ))
            {
                continue;
            }


            Vector3Int oldPosition =
                edge.position;


            // ==================================================
            // EDGE ORIENTATION
            // ==================================================
            //
            // Gleiche Konvention wie im Unity-Cubie:
            //
            // U/D -> kein Flip
            // R/L -> kein Flip
            // F/B -> Flip
            // ==================================================

            if (axis == RotationAxis.Z)
            {
                edge.orientation =
                    1 -
                    edge.orientation;
            }


            // ==================================================
            // POSITION
            // ==================================================

            edge.position =
                RotatePosition(
                    oldPosition,
                    axis,
                    direction
                );


            // struct zurückschreiben
            edges[i] =
                edge;
        }
    }


    // ======================================================
    // LIEGT PIECE IN EBENE?
    // ======================================================

    private bool IsInLayer(
        Vector3Int position,
        RotationAxis axis,
        int layer)
    {
        switch (axis)
        {
            case RotationAxis.X:
                return
                    position.x == layer;


            case RotationAxis.Y:
                return
                    position.y == layer;


            case RotationAxis.Z:
                return
                    position.z == layer;
        }


        return false;
    }


    // ======================================================
    // POSITION DREHEN
    // ======================================================

    private Vector3Int RotatePosition(
        Vector3Int oldPosition,
        RotationAxis axis,
        int direction)
    {
        int x =
            oldPosition.x;

        int y =
            oldPosition.y;

        int z =
            oldPosition.z;


        // ==================================================
        // X
        // ==================================================

        if (axis == RotationAxis.X)
        {
            if (direction > 0)
            {
                y =
                    -oldPosition.z;

                z =
                    oldPosition.y;
            }
            else
            {
                y =
                    oldPosition.z;

                z =
                    -oldPosition.y;
            }
        }


        // ==================================================
        // Y
        // ==================================================

        else if (axis == RotationAxis.Y)
        {
            if (direction > 0)
            {
                x =
                    oldPosition.z;

                z =
                    -oldPosition.x;
            }
            else
            {
                x =
                    -oldPosition.z;

                z =
                    oldPosition.x;
            }
        }


        // ==================================================
        // Z
        // ==================================================

        else if (axis == RotationAxis.Z)
        {
            if (direction > 0)
            {
                x =
                    -oldPosition.y;

                y =
                    oldPosition.x;
            }
            else
            {
                x =
                    oldPosition.y;

                y =
                    -oldPosition.x;
            }
        }


        return new Vector3Int(
            x,
            y,
            z
        );
    }


    // ======================================================
    // VALIDIERUNG
    // ======================================================

    public bool IsValid()
    {
        if (corners.Count != 8)
        {
            Debug.LogError(
                "SolverState: Falsche Corner-Anzahl: " +
                corners.Count +
                " statt 8."
            );

            return false;
        }


        if (edges.Count != 12)
        {
            Debug.LogError(
                "SolverState: Falsche Edge-Anzahl: " +
                edges.Count +
                " statt 12."
            );

            return false;
        }


        HashSet<string> cornerIDs =
            new HashSet<string>();

        HashSet<string> edgeIDs =
            new HashSet<string>();

        HashSet<Vector3Int> cornerPositions =
            new HashSet<Vector3Int>();

        HashSet<Vector3Int> edgePositions =
            new HashSet<Vector3Int>();


        int cornerOrientationSum = 0;
        int edgeOrientationSum = 0;


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            SolverPieceState corner
            in corners
        )
        {
            if (string.IsNullOrEmpty(
                corner.pieceID
            ))
            {
                Debug.LogError(
                    "SolverState: Corner ohne Piece-ID."
                );

                return false;
            }


            if (!cornerIDs.Add(
                corner.pieceID
            ))
            {
                Debug.LogError(
                    "SolverState: Doppelte Corner-ID: " +
                    corner.pieceID
                );

                return false;
            }


            if (!cornerPositions.Add(
                corner.position
            ))
            {
                Debug.LogError(
                    "SolverState: Doppelte Corner-Position: " +
                    corner.position
                );

                return false;
            }


            if (
                corner.orientation < 0 ||
                corner.orientation > 2
            )
            {
                Debug.LogError(
                    "SolverState: Ungültige Corner-Orientation: " +
                    corner.pieceID +
                    " = " +
                    corner.orientation
                );

                return false;
            }


            cornerOrientationSum +=
                corner.orientation;
        }


        // ==================================================
        // EDGES
        // ==================================================

        foreach (
            SolverPieceState edge
            in edges
        )
        {
            if (string.IsNullOrEmpty(
                edge.pieceID
            ))
            {
                Debug.LogError(
                    "SolverState: Edge ohne Piece-ID."
                );

                return false;
            }


            if (!edgeIDs.Add(
                edge.pieceID
            ))
            {
                Debug.LogError(
                    "SolverState: Doppelte Edge-ID: " +
                    edge.pieceID
                );

                return false;
            }


            if (!edgePositions.Add(
                edge.position
            ))
            {
                Debug.LogError(
                    "SolverState: Doppelte Edge-Position: " +
                    edge.position
                );

                return false;
            }


            if (
                edge.orientation < 0 ||
                edge.orientation > 1
            )
            {
                Debug.LogError(
                    "SolverState: Ungültige Edge-Orientation: " +
                    edge.pieceID +
                    " = " +
                    edge.orientation
                );

                return false;
            }


            edgeOrientationSum +=
                edge.orientation;
        }


        // ==================================================
        // ORIENTATION-INVARIANTEN
        // ==================================================

        if (cornerOrientationSum % 3 != 0)
        {
            Debug.LogError(
                "SolverState: Corner-Summe ungültig: " +
                cornerOrientationSum
            );

            return false;
        }


        if (edgeOrientationSum % 2 != 0)
        {
            Debug.LogError(
                "SolverState: Edge-Summe ungültig: " +
                edgeOrientationSum
            );

            return false;
        }


        return true;
    }


    // ======================================================
    // DEBUG
    // ======================================================

    public void Print()
    {
        Debug.Log(
            "================ SOLVER STATE ================"
        );


        Debug.Log(
            "--- CORNERS ---"
        );


        foreach (
            SolverPieceState corner
            in corners
        )
        {
            Debug.Log(
                corner.ToString()
            );
        }


        Debug.Log(
            "--- EDGES ---"
        );


        foreach (
            SolverPieceState edge
            in edges
        )
        {
            Debug.Log(
                edge.ToString()
            );
        }


        Debug.Log(
            "Valid: " +
            IsValid()
        );


        Debug.Log(
            "=============================================="
        );
    }
}