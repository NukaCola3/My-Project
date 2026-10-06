using System.Collections.Generic;
using System.Text;
using UnityEngine;


// ==========================================================
// SOLVER PIECE STATE
// ==========================================================
//
// Repräsentiert den logischen Zustand eines einzelnen
// beweglichen Pieces.
//
// Corner:
// orientation = 0, 1 oder 2
//
// Edge:
// orientation = 0 oder 1
// ==========================================================

public struct SolverPieceState
{
    public string pieceID;

    public Vector3Int position;

    public int orientation;

    // Aktuelle Richtung des für die Orientation maßgeblichen Stickers.
    // Corner: ursprünglicher U/D-Sticker
    // Edge mit U/D-Sticker: ursprünglicher U/D-Sticker
    // Edge ohne U/D-Sticker: ursprünglicher F/B-Sticker
    public FaceDirection referenceDirection;


    public SolverPieceState(
        string pieceID,
        Vector3Int position,
        int orientation,
        FaceDirection referenceDirection)
    {
        this.pieceID = pieceID;
        this.position = position;
        this.orientation = orientation;
        this.referenceDirection = referenceDirection;
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
//
// Unabhängige logische Repräsentation des Rubik's Cube.
//
// Enthält:
// - 8 Corners
// - 12 Edges
//
// Unity-GameObjects werden hier NICHT verändert.
// ==========================================================

public class SolverState
{
    // ======================================================
    // PIECES
    // ======================================================

    public List<SolverPieceState> corners =
        new List<SolverPieceState>();


    public List<SolverPieceState> edges =
        new List<SolverPieceState>();


    // ======================================================
    // CONSTRUCTOR FROM UNITY CUBIES
    // ======================================================

    public SolverState(
        List<Cubie> cubies)
    {
        BuildFromCubies(
            cubies
        );
    }


    // ======================================================
    // PRIVATE EMPTY CONSTRUCTOR
    // ======================================================
    //
    // Wird von Clone() verwendet.
    // ======================================================

    private SolverState()
    {
        corners =
            new List<SolverPieceState>();

        edges =
            new List<SolverPieceState>();
    }


    // ======================================================
    // BUILD FROM UNITY
    // ======================================================

    private void BuildFromCubies(
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


        foreach (
            Cubie cubie
            in cubies
        )
        {
            if (cubie == null)
            {
                continue;
            }


            if (
                cubie.Type ==
                CubieType.Corner
            )
            {
                FaceDirection referenceDirection =
                    GetCornerReferenceDirection(
                        cubie
                    );

                SolverPieceState state =
                    new SolverPieceState(
                        cubie.pieceID,
                        cubie.logicalPosition,
                        cubie.orientation,
                        referenceDirection
                    );

                corners.Add(
                    state
                );
            }


            else if (
                cubie.Type ==
                CubieType.Edge
            )
            {
                FaceDirection referenceDirection =
                    GetEdgeReferenceDirection(
                        cubie
                    );

                SolverPieceState state =
                    new SolverPieceState(
                        cubie.pieceID,
                        cubie.logicalPosition,
                        cubie.orientation,
                        referenceDirection
                    );

                edges.Add(
                    state
                );
            }
        }


        // ==================================================
        // STABILE REIHENFOLGE
        // ==================================================
        //
        // Wichtig für:
        // - State Keys
        // - reproduzierbare Ausgabe
        // - spätere Solver-Suche
        // ==================================================

        corners.Sort(
            (a, b) =>
                string.CompareOrdinal(
                    a.pieceID,
                    b.pieceID
                )
        );


        edges.Sort(
            (a, b) =>
                string.CompareOrdinal(
                    a.pieceID,
                    b.pieceID
                )
        );
    }


    // ======================================================
    // REFERENCE-DIRECTION AUS UNITY ÜBERNEHMEN
    // ======================================================

    private FaceDirection GetCornerReferenceDirection(
        Cubie cubie)
    {
        foreach (
            CubieSticker sticker
            in cubie.stickers
        )
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                return
                    sticker.currentDirection;
            }
        }

        Debug.LogError(
            "SolverState: Corner " +
            cubie.pieceID +
            " besitzt keinen U/D-Referenzsticker."
        );

        return FaceDirection.PositiveY;
    }


    private FaceDirection GetEdgeReferenceDirection(
        Cubie cubie)
    {
        // Zuerst wie in Cubie.UpdateEdgeOrientationFromStickers()
        // nach einem ursprünglichen U/D-Sticker suchen.
        foreach (
            CubieSticker sticker
            in cubie.stickers
        )
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                return
                    sticker.currentDirection;
            }
        }

        // Hat die Edge keinen U/D-Sticker, ist ihr ursprünglicher
        // F/B-Sticker die Referenz.
        foreach (
            CubieSticker sticker
            in cubie.stickers
        )
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveZ ||
                sticker.originalDirection ==
                    FaceDirection.NegativeZ
            )
            {
                return
                    sticker.currentDirection;
            }
        }

        Debug.LogError(
            "SolverState: Edge " +
            cubie.pieceID +
            " besitzt keinen gültigen Referenzsticker."
        );

        return FaceDirection.PositiveZ;
    }


    // ======================================================
    // CLONE
    // ======================================================
    //
    // Erzeugt eine unabhängige Kopie.
    //
    // Da SolverPieceState ein struct ist, werden die
    // einzelnen Piece-Zustände als Werte kopiert.
    // ======================================================

    public SolverState Clone()
    {
        SolverState clone =
            new SolverState();


        clone.corners =
            new List<SolverPieceState>(
                corners
            );


        clone.edges =
            new List<SolverPieceState>(
                edges
            );


        return clone;
    }


    // ======================================================
    // STATE COMPARISON
    // ======================================================
    //
    // Prüft, ob zwei SolverStates exakt denselben
    // Würfelzustand repräsentieren.
    // ======================================================

    public bool IsSameState(
        SolverState other)
    {
        if (other == null)
        {
            return false;
        }


        if (
            corners.Count !=
            other.corners.Count
        )
        {
            return false;
        }


        if (
            edges.Count !=
            other.edges.Count
        )
        {
            return false;
        }


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            SolverPieceState piece
            in corners
        )
        {
            bool found = false;


            foreach (
                SolverPieceState otherPiece
                in other.corners
            )
            {
                if (
                    piece.pieceID !=
                    otherPiece.pieceID
                )
                {
                    continue;
                }


                found = true;


                if (
                    piece.position !=
                    otherPiece.position
                )
                {
                    return false;
                }


                if (
                    piece.orientation !=
                    otherPiece.orientation
                )
                {
                    return false;
                }


                break;
            }


            if (!found)
            {
                return false;
            }
        }


        // ==================================================
        // EDGES
        // ==================================================

        foreach (
            SolverPieceState piece
            in edges
        )
        {
            bool found = false;


            foreach (
                SolverPieceState otherPiece
                in other.edges
            )
            {
                if (
                    piece.pieceID !=
                    otherPiece.pieceID
                )
                {
                    continue;
                }


                found = true;


                if (
                    piece.position !=
                    otherPiece.position
                )
                {
                    return false;
                }


                if (
                    piece.orientation !=
                    otherPiece.orientation
                )
                {
                    return false;
                }


                break;
            }


            if (!found)
            {
                return false;
            }
        }


        return true;
    }


    // ======================================================
    // STATE KEY
    // ======================================================
    //
    // Erzeugt einen eindeutigen String für den kompletten
    // Solver-Zustand.
    //
    // Identischer Zustand:
    //     gleicher Key
    //
    // Andere Position oder Orientation:
    //     anderer Key
    //
    // Die Piece-Listen besitzen eine stabile Sortierung
    // nach pieceID.
    // ======================================================

    public string GetStateKey()
    {
        StringBuilder builder =
            new StringBuilder();


        // ==================================================
        // CORNERS
        // ==================================================

        builder.Append(
            "C:"
        );


        foreach (
            SolverPieceState corner
            in corners
        )
        {
            builder.Append(
                corner.pieceID
            );

            builder.Append(
                "@"
            );

            builder.Append(
                corner.position.x
            );

            builder.Append(
                ","
            );

            builder.Append(
                corner.position.y
            );

            builder.Append(
                ","
            );

            builder.Append(
                corner.position.z
            );

            builder.Append(
                ":"
            );

            builder.Append(
                corner.orientation
            );

            builder.Append(
                "|"
            );
        }


        // ==================================================
        // EDGES
        // ==================================================

        builder.Append(
            "E:"
        );


        foreach (
            SolverPieceState edge
            in edges
        )
        {
            builder.Append(
                edge.pieceID
            );

            builder.Append(
                "@"
            );

            builder.Append(
                edge.position.x
            );

            builder.Append(
                ","
            );

            builder.Append(
                edge.position.y
            );

            builder.Append(
                ","
            );

            builder.Append(
                edge.position.z
            );

            builder.Append(
                ":"
            );

            builder.Append(
                edge.orientation
            );

            builder.Append(
                "|"
            );
        }


        return builder.ToString();
    }


    // ======================================================
    // APPLY MOVE FROM STRING
    // ======================================================
    //
    // Unterstützt:
    //
    // U U'
    // D D'
    // R R'
    // L L'
    // F F'
    // B B'
    //
    // Zusätzlich:
    //
    // U2 D2 R2 L2 F2 B2
    // ======================================================

    public bool ApplyMove(
        string move)
    {
        if (string.IsNullOrWhiteSpace(
            move
        ))
        {
            return false;
        }


        string token =
            move
                .Trim()
                .ToUpperInvariant();


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
            // ==============================================
            // U
            // ==============================================

            case "U":

                axis =
                    RotationAxis.Y;

                layer =
                    1;

                direction =
                    1;

                break;


            // ==============================================
            // D
            // ==============================================

            case "D":

                axis =
                    RotationAxis.Y;

                layer =
                    -1;

                direction =
                    -1;

                break;


            // ==============================================
            // R
            // ==============================================

            case "R":

                axis =
                    RotationAxis.X;

                layer =
                    1;

                direction =
                    1;

                break;


            // ==============================================
            // L
            // ==============================================

            case "L":

                axis =
                    RotationAxis.X;

                layer =
                    -1;

                direction =
                    -1;

                break;


            // ==============================================
            // F
            // ==============================================

            case "F":

                axis =
                    RotationAxis.Z;

                layer =
                    1;

                direction =
                    -1;

                break;


            // ==============================================
            // B
            // ==============================================

            case "B":

                axis =
                    RotationAxis.Z;

                layer =
                    -1;

                direction =
                    1;

                break;


            default:

                Debug.LogError(
                    "SolverState: Unbekannter Move: " +
                    move
                );

                return false;
        }


        // ==================================================
        // PRIME
        // ==================================================

        if (prime)
        {
            direction *= -1;
        }


        // ==================================================
        // MOVE AUSFÜHREN
        // ==================================================

        ApplyMove(
            axis,
            layer,
            direction
        );


        // ==================================================
        // 180° MOVE
        // ==================================================

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
    // APPLY LOGICAL MOVE
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
    // APPLY CORNER MOVE
    // ======================================================

    private void ApplyCornerMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        for (
            int i = 0;
            i < corners.Count;
            i++
        )
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

            // Referenzsticker exakt wie in Cubie.RotateStickers()
            // mathematisch mitdrehen.
            corner.referenceDirection =
                RotateFaceDirection(
                    corner.referenceDirection,
                    axis,
                    direction
                );

            // Position drehen.
            corner.position =
                RotatePosition(
                    oldPosition,
                    axis,
                    direction
                );

            // Corner-Orientation inkrementell aktualisieren.
            // Exakt dieselbe Konvention wie auf der Unity-Seite.
            int delta = 0;

            if (axis == RotationAxis.X)
            {
                bool sameSign =
                    oldPosition.y == oldPosition.z;

                delta =
                    sameSign
                        ? 1
                        : 2;
            }
            else if (axis == RotationAxis.Z)
            {
                bool sameSign =
                    oldPosition.x == oldPosition.y;

                delta =
                    sameSign
                        ? 2
                        : 1;
            }

            if (direction < 0 && delta != 0)
            {
                delta =
                    delta == 1
                        ? 2
                        : 1;
            }

            corner.orientation =
                (corner.orientation + delta) % 3;

            corners[i] =
                corner;
        }
    }


    // ======================================================
    // APPLY EDGE MOVE
    // ======================================================

    private void ApplyEdgeMove(
        RotationAxis axis,
        int layer,
        int direction)
    {
        for (
            int i = 0;
            i < edges.Count;
            i++
        )
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

            edge.referenceDirection =
                RotateFaceDirection(
                    edge.referenceDirection,
                    axis,
                    direction
                );

            edge.position =
                RotatePosition(
                    oldPosition,
                    axis,
                    direction
                );

            // Edge-Orientation exakt wie auf der Unity-Seite:
            // Nur F/B-Züge (Z-Achse) flippen die betroffenen Edges.
            // U/D (Y) und R/L (X) verändern die Orientation nicht.
            if (axis == RotationAxis.Z)
            {
                edge.orientation =
                    1 - edge.orientation;
            }

            edges[i] =
                edge;
        }
    }


    // ======================================================
    // ORIENTATION AUS REFERENZRICHTUNG
    // ======================================================

    private int GetCornerOrientation(
        string pieceID,
        FaceDirection referenceDirection)
    {
        if (
            referenceDirection ==
                FaceDirection.PositiveY ||
            referenceDirection ==
                FaceDirection.NegativeY
        )
        {
            return 0;
        }

        bool isUCorner =
            pieceID.StartsWith("U");

        if (
            referenceDirection ==
                FaceDirection.PositiveX ||
            referenceDirection ==
                FaceDirection.NegativeX
        )
        {
            return
                isUCorner
                    ? 1
                    : 2;
        }

        if (
            referenceDirection ==
                FaceDirection.PositiveZ ||
            referenceDirection ==
                FaceDirection.NegativeZ
        )
        {
            return
                isUCorner
                    ? 2
                    : 1;
        }

        Debug.LogError(
            "SolverState: Corner-Orientation konnte nicht bestimmt werden: " +
            pieceID
        );

        return 0;
    }


    private int GetEdgeOrientation(
        string pieceID,
        FaceDirection referenceDirection)
    {
        bool hasUDSticker =
            pieceID.Contains("U") ||
            pieceID.Contains("D");

        if (hasUDSticker)
        {
            if (
                referenceDirection ==
                    FaceDirection.PositiveY ||
                referenceDirection ==
                    FaceDirection.NegativeY
            )
            {
                return 0;
            }

            return 1;
        }

        if (
            referenceDirection ==
                FaceDirection.PositiveZ ||
            referenceDirection ==
                FaceDirection.NegativeZ
        )
        {
            return 0;
        }

        return 1;
    }


    // ======================================================
    // FACE-RICHTUNG DREHEN
    // ======================================================
    //
    // Exakt dieselbe Richtungsabbildung wie
    // Cubie.RotateFaceDirection().
    // ======================================================

    private FaceDirection RotateFaceDirection(
        FaceDirection face,
        RotationAxis axis,
        int direction)
    {
        if (axis == RotationAxis.X)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveY;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveY;
                }
            }
        }

        if (axis == RotationAxis.Y)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveX;
                }
            }
        }

        if (axis == RotationAxis.Z)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveX;
                }
            }
        }

        return face;
    }


    // ======================================================
    // IS PIECE IN LAYER?
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
                    position.x ==
                    layer;


            case RotationAxis.Y:

                return
                    position.y ==
                    layer;


            case RotationAxis.Z:

                return
                    position.z ==
                    layer;
        }


        return false;
    }


    // ======================================================
    // ROTATE POSITION
    // ======================================================
    //
    // Exakt dieselben Transformationen wie
    // RubiksCube.UpdateLogicalState().
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
        // X AXIS
        // ==================================================

        if (
            axis ==
            RotationAxis.X
        )
        {
            if (direction == 1)
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
        // Y AXIS
        // ==================================================

        else if (
            axis ==
            RotationAxis.Y
        )
        {
            if (direction == 1)
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
        // Z AXIS
        // ==================================================

        else if (
            axis ==
            RotationAxis.Z
        )
        {
            if (direction == 1)
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


        return
            new Vector3Int(
                x,
                y,
                z
            );
    }


    // ======================================================
    // VALIDATE SOLVER STATE
    // ======================================================

    public bool IsValid()
    {
        // ==================================================
        // COUNTS
        // ==================================================

        if (corners.Count != 8)
        {
            Debug.LogError(
                "SolverState: Corner-Anzahl falsch: " +
                corners.Count
            );

            return false;
        }


        if (edges.Count != 12)
        {
            Debug.LogError(
                "SolverState: Edge-Anzahl falsch: " +
                edges.Count
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
            if (
                string.IsNullOrEmpty(
                    corner.pieceID
                )
            )
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
            if (
                string.IsNullOrEmpty(
                    edge.pieceID
                )
            )
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
        // ORIENTATION INVARIANTS
        // ==================================================

        if (
            cornerOrientationSum % 3 != 0
        )
        {
            Debug.LogError(
                "SolverState: Corner-Summe nicht durch 3 teilbar: " +
                cornerOrientationSum
            );

            return false;
        }


        if (
            edgeOrientationSum % 2 != 0
        )
        {
            Debug.LogError(
                "SolverState: Edge-Summe nicht durch 2 teilbar: " +
                edgeOrientationSum
            );

            return false;
        }


        return true;
    }


    // ======================================================
    // PRINT
    // ======================================================

    public void Print()
    {
        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "SOLVER STATE"
        );

        Debug.Log(
            "========================================"
        );


        // ==================================================
        // CORNERS
        // ==================================================

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


        // ==================================================
        // EDGES
        // ==================================================

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
            "========================================"
        );
    }
}