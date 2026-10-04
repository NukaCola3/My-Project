using System;
using System.Collections.Generic;
using UnityEngine;


// ==================================================
// CUBIE-TYP
// ==================================================

public enum CubieType
{
    Corner,
    Edge,
    Center
}


// ==================================================
// FLÄCHENRICHTUNG
// ==================================================

public enum FaceDirection
{
    PositiveX,
    NegativeX,

    PositiveY,
    NegativeY,

    PositiveZ,
    NegativeZ
}


// ==================================================
// STICKER
// ==================================================

[Serializable]
public class CubieSticker
{
    public FaceDirection originalDirection;
    public FaceDirection currentDirection;

    public CubieSticker(FaceDirection direction)
    {
        originalDirection = direction;
        currentDirection = direction;
    }
}


// ==================================================
// CUBIE
// ==================================================

public class Cubie : MonoBehaviour
{
    // ==================================================
    // LOGISCHE DATEN
    // ==================================================

    [Header("Logical Position")]
    public Vector3Int logicalPosition;

    [HideInInspector]
    public Vector3Int originalPosition;


    [Header("Logical Rotation")]
    public Quaternion logicalRotation =
        Quaternion.identity;


    // ==================================================
    // IDENTITÄT
    // ==================================================

    [Header("Piece ID")]
    public string pieceID;


    // ==================================================
    // ORIENTIERUNG
    // ==================================================

    [Header("Orientation")]
    [Range(0, 2)]
    public int orientation = 0;


    // ==================================================
    // STICKER
    // ==================================================

    [Header("Stickers")]
    public List<CubieSticker> stickers =
        new List<CubieSticker>();


    // ==================================================
    // CUBIE-TYP
    // ==================================================

    public CubieType Type
    {
        get
        {
            int coordinateSum =
                Mathf.Abs(logicalPosition.x) +
                Mathf.Abs(logicalPosition.y) +
                Mathf.Abs(logicalPosition.z);

            if (coordinateSum == 3)
            {
                return CubieType.Corner;
            }

            if (coordinateSum == 2)
            {
                return CubieType.Edge;
            }

            return CubieType.Center;
        }
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        Initialize();
    }


    // ==================================================
    // INITIALISIERUNG
    // ==================================================

    private void Initialize()
    {
        // ------------------------------------------
        // LOGISCHE POSITION
        // ------------------------------------------

        logicalPosition =
            new Vector3Int(
                Mathf.RoundToInt(transform.localPosition.x),
                Mathf.RoundToInt(transform.localPosition.y),
                Mathf.RoundToInt(transform.localPosition.z)
            );

        originalPosition =
            logicalPosition;


        // ------------------------------------------
        // LOGISCHE ROTATION
        // ------------------------------------------

        logicalRotation =
            transform.localRotation;


        // ------------------------------------------
        // STICKER ERZEUGEN
        // ------------------------------------------

        InitializeStickers();


        // ------------------------------------------
        // PIECE-ID ERZEUGEN
        // ------------------------------------------

        GeneratePieceID();


        // ------------------------------------------
        // ORIENTIERUNG
        // ------------------------------------------

        orientation = 0;
    }


    // ==================================================
    // STICKER INITIALISIEREN
    // ==================================================

    private void InitializeStickers()
    {
        stickers.Clear();


        // ------------------------------------------
        // X-Achse
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveX
                )
            );
        }

        if (logicalPosition.x < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeX
                )
            );
        }


        // ------------------------------------------
        // Y-Achse
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveY
                )
            );
        }

        if (logicalPosition.y < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeY
                )
            );
        }


        // ------------------------------------------
        // Z-Achse
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveZ
                )
            );
        }

        if (logicalPosition.z < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeZ
                )
            );
        }
    }


    // ==================================================
    // PIECE-ID
    // ==================================================

    private void GeneratePieceID()
    {
        string id = "";


        // ------------------------------------------
        // Y
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            id += "U";
        }
        else if (logicalPosition.y < 0)
        {
            id += "D";
        }


        // ------------------------------------------
        // X
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            id += "R";
        }
        else if (logicalPosition.x < 0)
        {
            id += "L";
        }


        // ------------------------------------------
        // Z
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            id += "F";
        }
        else if (logicalPosition.z < 0)
        {
            id += "B";
        }


        pieceID = id;
    }


    // ==================================================
    // LOGISCHE ROTATION AKTUALISIEREN
    // ==================================================

    public void UpdateLogicalRotation(
        Vector3 rotationAxis,
        int direction)
    {
        Quaternion rotation =
            Quaternion.AngleAxis(
                90f * direction,
                rotationAxis
            );

        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // ALTERNATIVE ROTATIONS-METHODE
    // ==================================================

    public void UpdateLogicalRotation(
        Quaternion rotation)
    {
        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // STICKER ROTIEREN
    // ==================================================

    public void RotateStickers(
        RotationAxis axis,
        int direction)
    {
        foreach (CubieSticker sticker in stickers)
        {
            sticker.currentDirection =
                RotateFaceDirection(
                    sticker.currentDirection,
                    axis,
                    direction
                );
        }
    }


    // ==================================================
    // FACE-RICHTUNG ROTIEREN
    // ==================================================

    private FaceDirection RotateFaceDirection(
        FaceDirection face,
        RotationAxis axis,
        int direction)
    {
        // ------------------------------------------
        // X-ACHSE
        // ------------------------------------------

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


        // ------------------------------------------
        // Y-ACHSE
        // ------------------------------------------

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


        // ------------------------------------------
        // Z-ACHSE
        // ------------------------------------------

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


    // ==================================================
    // ORIENTIERUNG AKTUALISIEREN
    // ==================================================

    public void UpdateOrientation()
    {
        switch (Type)
        {
            case CubieType.Corner:
                // Corner Orientation wird bereits
                // während des Zuges inkrementell aktualisiert.
                break;

            case CubieType.Edge:
                // Edge Orientation wird jetzt ebenfalls
                // während des Zuges inkrementell aktualisiert.
                //
                // Hier NICHT neu berechnen!
                break;

            case CubieType.Center:
                orientation = 0;
                break;
        }
    }


    // ==================================================
    // CORNER ORIENTIERUNG
    // ==================================================

    public void UpdateCornerOrientation(
        RotationAxis axis,
        int direction,
        Vector3Int oldPosition)
    {
        if (Type != CubieType.Corner)
            return;

        // U und D verändern die Corner Orientation nicht.
        if (axis == RotationAxis.Y)
            return;

        int delta = 0;

        // R / L
        if (axis == RotationAxis.X)
        {
            if (direction > 0) // R
            {
                delta = oldPosition.z > 0 ? 1 : 2;
            }
            else // L
            {
                delta = oldPosition.z > 0 ? 2 : 1;
            }
        }

        // F / B
        else if (axis == RotationAxis.Z)
        {
            if (direction < 0) // F
            {
                delta = oldPosition.x > 0 ? 2 : 1;
            }
            else // B
            {
                delta = oldPosition.x > 0 ? 1 : 2;
            }
        }

        orientation = (orientation + delta) % 3;
    }


    // ==================================================
    // EDGE ORIENTIERUNG
    // ==================================================
    //
    // Edge Orientation wird als eigenständiger logischer
    // Zustand inkrementell gespeichert.
    //
    // Konvention:
    //
    // U / U' -> keine Änderung
    // D / D' -> keine Änderung
    // R / R' -> keine Änderung
    // L / L' -> keine Änderung
    //
    // F / F' -> betroffene Edges flippen
    // B / B' -> betroffene Edges flippen
    //
    // Da diese Methode nur für Cubies der aktuell
    // gedrehten Ebene aufgerufen wird, reicht die
    // Prüfung auf RotationAxis.Z.
    // ==================================================

    public void UpdateEdgeOrientation(
        RotationAxis axis)
    {
        if (Type != CubieType.Edge)
            return;

        if (axis == RotationAxis.Z)
        {
            orientation = 1 - orientation;
        }
    }


    // ==================================================
    // LOGISCHE POSITION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalPosition()
    {
        transform.localPosition =
            new Vector3(
                logicalPosition.x,
                logicalPosition.y,
                logicalPosition.z
            );
    }


    // ==================================================
    // LOGISCHE ROTATION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalRotation()
    {
        transform.localRotation =
            logicalRotation;
    }


    // ==================================================
    // AKTUELLEN ZUSTAND ALS TEXT
    // ==================================================

    public string GetState()
    {
        string stickerText = "";

        foreach (CubieSticker sticker in stickers)
        {
            stickerText +=
                sticker.originalDirection +
                "->" +
                sticker.currentDirection +
                " ";
        }

        return
            pieceID +
            " | " +
            Type +
            " | Pos=" +
            logicalPosition +
            " | Ori=" +
            orientation +
            " | Stickers=" +
            stickerText;
    }
}